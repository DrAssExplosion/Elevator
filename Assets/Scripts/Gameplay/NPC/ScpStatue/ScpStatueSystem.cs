using Unity.Burst;
using Unity.Entities;
using Unity.MP_FPS;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine.Scripting;

namespace Unity.GhostBridge
{
    /// <summary>
    /// Серверная ECS-система поведения SCP-статуи:
    /// - пока статую кто-то видит (ScpStatueState.AnyClientSeesMe) — она неподвижна;
    /// - как только зона видимости потеряна — стремительно перемещается к ближайшему живому игроку;
    /// - достигнув игрока (дистанция &lt; KillRange) — мгновенно убивает его (CurrentHealth = 0,
    ///   смерть и респаун обрабатывает существующий ServerGameSystem.HandlePlayerDeathAndRespawn),
    ///   после чего телепортируется на случайную точку спавна и уходит в кулдаун.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [Preserve]
    public partial struct ScpStatueSystem : ISystem
    {
        private const float MoveSpeed = 45f;      // «очень быстрое» перемещение, м/с
        private const float KillRange = 2f;       // дистанция мгновенного убийства
        private const float KillCooldownSec = 8f; // пауза после убийства перед новой охотой

        private ComponentLookup<PredictedPlayerGhost> _playerGhostLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<ClientsMap>();
            _playerGhostLookup = state.GetComponentLookup<PredictedPlayerGhost>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _playerGhostLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var clientsMap = SystemAPI.GetSingletonBuffer<ClientsMap>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            uint serverTick = networkTime.ServerTick.TickIndexForValidTick;
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (statueState, transform) in
                     SystemAPI.Query<RefRW<ScpStatueState>, RefRW<LocalTransform>>())
            {
                var s = statueState.ValueRO;

                // После убийжения статуя ждёт на точке спавна
                if (s.KillCooldown > 0f)
                    continue;

                // Статую видят — замираем (классика SCP)
                if (s.AnyClientSeesMe)
                    continue;

                // Найти ближайшего живого игрока
                float bestDistSq = float.MaxValue;
                Entity bestPlayer = Entity.Null;
                for (int clientIdx = 1; clientIdx < clientsMap.Length; clientIdx++) // индекс 0 — сам сервер
                {
                    var playerEntity = clientsMap[clientIdx].PlayerEntity;
                    if (playerEntity == Entity.Null)
                        continue;
                    if (!_transformLookup.TryGetComponent(playerEntity, out var playerTransform))
                        continue;
                    if (!_playerGhostLookup.TryGetComponent(playerEntity, out var playerGhost))
                        continue;
                    if (playerGhost.CurrentHealth <= 0f)
                        continue;

                    float distSq = math.distancesq(transform.ValueRO.Position, playerTransform.Position);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestPlayer = playerEntity;
                    }
                }

                if (bestPlayer == Entity.Null)
                    continue;

                float3 targetPos = _transformLookup[bestPlayer].Position;
                float distance = math.distance(transform.ValueRO.Position, targetPos);

                // Мгновенное убийство при контакте
                if (distance <= KillRange)
                {
                    var victimRef = _playerGhostLookup.GetRefRW(bestPlayer);
                    var controllerState = victimRef.ValueRO.ControllerState;
                    controllerState.IsHit = true;
                    victimRef.ValueRW.ControllerState = controllerState;
                    victimRef.ValueRW.LastDamageAmount = victimRef.ValueRO.CurrentHealth;
                    victimRef.ValueRW.LastHitTick = serverTick;
                    victimRef.ValueRW.CurrentHealth = 0f; // смерть обработает HandlePlayerDeathAndRespawn

                    // Телепорт на наиболее удалённую точку спавна + кулдаун
                    if (TryFindRandomSpawnPoint(ref state, targetPos, out var spawnPos))
                    {
                        var t = transform.ValueRW;
                        t.Position = spawnPos;
                        transform.ValueRW = t;
                    }

                    s.KillCooldown = KillCooldownSec;
                    s.AnyClientSeesMe = false;
                    statueState.ValueRW = s;
                    continue;
                }

                // Очень быстрое преследование, когда никто не смотрит
                float3 dir = math.normalize(targetPos - transform.ValueRO.Position);
                float step = math.min(MoveSpeed * dt, distance);
                var lt = transform.ValueRW;
                lt.Position += dir * step;
                // Статуя «смотрит» на цель
                if (math.lengthsq(dir) > 0.0001f)
                    lt.Rotation = quaternion.LookRotationSafe(dir, math.up());
                transform.ValueRW = lt;
            }

        }

        private bool TryFindRandomSpawnPoint(ref SystemState state, float3 avoidPosition, out float3 spawnPos)
        {
            spawnPos = default;
            var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnPoint>(),
                ComponentType.ReadOnly<LocalToWorld>());
            var spawnPoints = query.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            query.Dispose();

            if (spawnPoints.Length == 0)
                return false;

            // Выбрать точку спавна, наиболее удалённую от убитого игрока
            int bestIndex = 0;
            float bestDistSq = -1f;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                float distSq = math.distancesq(spawnPoints[i].Position, avoidPosition);
                if (distSq > bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            spawnPos = spawnPoints[bestIndex].Position;
            spawnPoints.Dispose();
            return true;
        }
    }
}
