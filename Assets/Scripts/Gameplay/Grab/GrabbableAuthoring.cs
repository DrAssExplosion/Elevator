using Unity.Entities;
using UnityEngine;

namespace Unity.GhostBridge
{
    /// <summary>
    /// Authoring-компонент для предметов, которые можно брать в руки (drag & drop как в Skyrim).
    /// Вешается на GameObject ghost-префаба предмета (вместе с GhostGameObject);
    /// Baker добавляет GrabbableState на связанную ghost-сущность.
    /// </summary>
    public class GrabbableAuthoring : MonoBehaviour
    {
        /// <summary>Радиус, в пределах которого можно взять предмет (от позиции игрока).</summary>
        public float GrabRadius = 3f;

        private class GrabbableBaker : Baker<GrabbableAuthoring>
        {
            public override void Bake(GrabbableAuthoring authoring)
            {
                // компонент попадает на ghost-linked-сущность префаба
                AddComponent(new GrabbableState
                {
                    HolderNetworkId = 0,
                    GrabRadius = authoring.GrabRadius,
                });
            }
        }
    }
}
