using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine.Scripting;

namespace Unity.GhostBridge
{
    /// <summary>
    /// Серверная система drag & drop предметов (как в Skyrim):
    /// - игрок нажимает Grab — ближайший свободный grab-предмет в радиусе и в конусе взгляда
    ///   закрепляется за ним (GrabbableState.HolderNetworkId = NetworkId игрока);
    ///   сервер каждый тик переносит предмет перед игроком, позиция реплицируется ghost-трансформом;
    /// - игрок нажимает Drop (или умирает / отключается) — предмет отпускается.
    /// Клиентская часть (визуальное «удержание») не нужна: трансформ предмета приходит с сервера.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [Preserve]
    public partial struct GrabbableSystem : ISystem
    {
        private const float DefaultGrabRadius = 3f; // если GrabRadius не пропечён (0)
        private const float CarryDistance = 1.6f;   // на сколько метров впереди держать предмет
        private const float CarryHeightOffset = 0.2f; // чуть ниже уровня глаз
        private const float MaxCosAngle = 0.5f;     // ~60° конус взгляда для захвата

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<ClientsMap>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clientsMap = SystemAPI.GetSingletonBuffer<ClientsMap>();

            // Кэш позиций/взгляда живых игроков, индексированный по NetworkId
            var playerPos = new NativeParallelHashMap<int, float3>(clientsMap.Length, Allocator.Temp);
            var playerForward = new NativeParallelHashMap<int, float3>(clientsMap.Length, Allocator.Temp);

            for (int i = 0; i < clientsMap.Length; i++)
            {
                var client = clientsMap[i];
                if (client.PlayerEntity == Entity.Null || !state.EntityManager.Exists(client.PlayerEntity))
                    continue;
                if (!state.EntityManager.TryGetComponent<LocalTransform>(client.PlayerEntity, out var t))
                    continue;
                if (!state.EntityManager.TryGetComponent<PredictedPlayerGhost>(client.PlayerEntity, out var pg))
                    continue;
                if (pg.CurrentHealth <= 0f)
                    continue; // мёртвый игрок не может ничего держать

                var yawPitch = pg.LocalLookYawPitchDegrees;
                float yawRad = math.radians(yawPitch.x);
                float pitchRad = math.radians(yawPitch.y);
                var forward = math.normalize(new float3(
                    math.sin(yawRad) * math.cos(pitchRad),
                    math.sin(pitchRad),
                    math.cos(yawRad) * math.cos(pitchRad)));

                playerPos[i] = t.Position;
                playerForward[i] = forward;
            }

            foreach (var grabbable in SystemAPI.Query<RefRW<GrabbableState>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
            {
                var (gRef, tRef) = (grabbable.Item1, grabbable.Item2);
                var g = gRef.ValueRO;
                ref var t = ref tRef.ValueRW;

                // Мёртвые/отключившиеся владельцы автоматически роняют предмет
                if (g.HolderNetworkId != 0 && !playerPos.ContainsKey(g.HolderNetworkId))
                {
                    g.HolderNetworkId = 0;
                    gRef.ValueRW = g;
                }

                if (g.HolderNetworkId != 0)
                {
                    // Носим предмет перед игроком (Skyrim-style)
                    var ownerPos = playerPos[g.HolderNetworkId];
                    var ownerFwd = playerForward[g.HolderNetworkId];
                    t.Position = ownerPos + ownerFwd * CarryDistance + new float3(0f, CarryHeightOffset, 0f);

                    // Drop — отпустить: предмет остаётся там, где был в руках
                    if (TryGetGrabInput(state, g.HolderNetworkId, out var dropInput) && dropInput.Drop)
                    {
                        g.HolderNetworkId = 0;
                        gRef.ValueRW = g;
                    }
                    continue;
                }

                // Ищем игрока, который хочет взять именно этот предмет (Grab — триггер-флаг)
                int bestOwner = 0;
                float bestDistSq = float.MaxValue;
                foreach (var (input, owner) in SystemAPI.Query<ClientInput, RefRO<GhostOwner>>()
                         .WithEntityAccess())
                {
                    if (!input.PlayerInput.Grab)
                        continue;
                    int netId = owner.ValueRO.NetworkId;
                    if (!playerPos.TryGetValue(netId, out var pPos) ||
                        !playerForward.TryGetValue(netId, out var pFwd))
                        continue;

                    var toItem = t.Position - pPos;
                    float distSq = math.lengthsq(toItem);
                    float radius = g.GrabRadius > 0f ? g.GrabRadius : DefaultGrabRadius;
                    if (distSq > radius * radius || distSq >= bestDistSq)
                        continue;
                    if (math.dot(math.normalize(toItem), pFwd) < MaxCosAngle)
                        continue; // предмет вне конуса взгляда

                    bestDistSq = distSq;
                    bestOwner = netId;
                }

                if (bestOwner != 0)
                {
                    g.HolderNetworkId = bestOwner;
                    gRef.ValueRW = g;
                }
            }

            playerPos.Dispose();
            playerForward.Dispose();
        }

        /// <summary>Возвращает последний принятый с клиента ввод игрока с указанным NetworkId.</summary>
        private static bool TryGetGrabInput(SystemState state, int networkId, out PlayerInput input)
        {
            input = default;
            foreach (var (clientInput, owner) in SystemAPI.Query<ClientInput, RefRO<GhostOwner>>())
            {
                if (owner.ValueRO.NetworkId == networkId)
                {
                    input = clientInput.PlayerInput;
                    return true;
                }
            }
            return false;
        }
    }
}
