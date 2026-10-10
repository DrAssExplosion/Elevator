using Unity.Entities;
using Unity.NetCode;

namespace Unity.GhostBridge
{
    /// <summary>
    /// Состояние grab-объекта (drag & drop как в Skyrim).
    /// Добавляется Baker'ом (GrabbableAuthoring) на ghost-сущность предмета.
    /// ServerOnlySend: поле HolderNetworkId нужно только серверу (владельцем предмета),
    /// а позицию предмета реплицирует обычный ghost-трансформ.
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct GrabbableState : IComponentData
    {
        /// <summary>
        /// NetworkId игрока, который сейчас держит предмет; 0 — предмет свободно лежит.
        /// </summary>
        [GhostField(SendTypeOptimization = GhostSendType.ServerOnlySend)]
        public int HolderNetworkId;

        /// <summary>Радиус захвата предмета (пропекается из GrabbableAuthoring).</summary>
        public float GrabRadius;
    }
}
