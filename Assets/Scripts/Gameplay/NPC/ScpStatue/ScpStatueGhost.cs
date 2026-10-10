using Unity.Entities;
using Unity.MP_FPS;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Unity.GhostBridge
{
    /// <summary>
    /// Прослойка GhostBridge для SCP-статуи (server-owned ghost, как Projectile).
    /// Клиент (Proxy) каждый тик проверяет, видит ли локальный игрок статую
    /// (фрустум камеры + raycast).
    /// Результат пишется в ghost-поле ScpStatueState.AnyClientSeesMe (SendTypeOptimization = OnlyServer),
    /// которое сервер читает в ScpStatueSystem.
    /// </summary>
    public class ScpStatueGhost : GhostMonoBehaviour, IUpdateServer, IUpdateClient
    {
        // Слои, блокирующие линию видимости
        private int _occlusionLayerMask;

        private Camera _playerCamera;

        // Интервал обновления флага видимости, чтобы не писать в ghost каждый тик
        private const float VisibilityUpdateInterval = 0.1f;
        private float _rpcTimer;
        private bool _lastReportedSeen;

        private void Awake()
        {
            _occlusionLayerMask = LayerMask.GetMask("Default", "Ground");
        }

        public void UpdateServer(float deltaTime)
        {
            // Вся серверная логика поведения — в ScpStatueSystem (ECS).
            // Здесь только таймер кулдауна после убийства.
            var state = GhostGameObject.ReadGhostComponentData<ScpStatueState>();
            if (state.KillCooldown > 0f)
            {
                state.KillCooldown = UnityEngine.Mathf.Max(0f, state.KillCooldown - deltaTime);
                GhostGameObject.WriteGhostComponentData(state);
            }
        }

        public void UpdateClient(float deltaTime)
        {
            if (_playerCamera == null || !_playerCamera.isActiveAndEnabled)
            {
                _playerCamera = Camera.main;
                if (_playerCamera == null)
                    return;
            }

            bool seen = IsVisibleToPlayer(_playerCamera.transform, transform.position);

            _rpcTimer += deltaTime;
            if (_rpcTimer >= VisibilityUpdateInterval)
            {
                _rpcTimer = 0f;
                if (seen != _lastReportedSeen)
                {
                    _lastReportedSeen = seen;
                    // Пишем флаг видимости в ghost-поле статуи (сервер получает его как OnlyServer-поле)
                    var state = ReadGhostComponentData<ScpStatueState>();
                    if (state.AnyClientSeesMe != seen)
                    {
                        state.AnyClientSeesMe = seen;
                        WriteGhostComponentData(state);
                    }
                }
            }
        }

        /// <summary>
        /// Статуя «видна», если она в пирамиде видимости камеры игрока
        /// и между глазами и статуей нет перекрывающей геометрии.
        /// </summary>
        private bool IsVisibleToPlayer(Transform cam, Vector3 statuePos)
        {
            Vector3 toStatue = statuePos - cam.position;
            float distance = toStatue.magnitude;
            if (distance < 0.01f)
                return true;

            // 1. Пирамида видимости камеры
            Vector3 viewport = cam.WorldToViewportPoint(statuePos);
            bool inFront = viewport.z > 0f;
            bool inFrame = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            if (!inFront || !inFrame)
                return false;

            // 2. Прямая линия до статуи не перекрыта геометрией
            if (Physics.Linecast(cam.position, statuePos, out RaycastHit hit, _occlusionLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.distance >= distance - 0.5f;
            }

            return true;
        }
    }
}
