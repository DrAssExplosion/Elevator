using Unity.Entities;
using Unity.NetCode;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Сетевое состояние SCP-статуи. Синхронизируется как ghost-компонент:
    /// клиенты пишут флаг видимости (<see cref="AnyClientSeesMe"/>), сервер читает его для логики поведения.
    /// </summary>
    public struct ScpStatueState : IComponentData
    {
        /// <summary>Истина, если хотя бы один живой игрок сейчас видит статую.</summary>
        [GhostField(SendTypeOptimization = GhostSendType.OnlyServer, Quantization = 0)] public bool AnyClientSeesMe;

        /// <summary>Кулдаун после убийства (сек). Пока &gt; 0 — статуя ждёт на дальней точке спавна.</summary>
        [GhostField(Quantization = 100)] public float KillCooldown;
    }
}
