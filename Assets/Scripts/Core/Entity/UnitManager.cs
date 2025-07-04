using System.Collections.Generic;
using Bolt;
using UnityEngine;

namespace Core
{
    public class UnitManager : EntityManager<Unit>
    {
        private readonly Dictionary<Collider, Unit> unitsByColliders = new Dictionary<Collider, Unit>();

        public bool TryFind(Collider unitCollider, out Unit entity)
        {
            return unitsByColliders.TryGetValue(unitCollider, out entity);
        }

        internal TEntity Create<TEntity>(PrefabId prefabId, Entity.CreateToken createToken = null) where TEntity : Unit
        {
            // 说明：这里之所以能直接getcomponent拿到unit是因为Assets\Resources\Entities
            // 这下面的三个prefab编辑器中就设置好了每个都绑定了一个bolt entity组件
            TEntity entity = BoltNetwork.Instantiate(prefabId, createToken).GetComponent<TEntity>();
            entity.ModifyDeathState(DeathState.Alive);
            entity.Attributes.SetHealth(entity.MaxHealth);
            entity.Motion.UpdateMovementControl(true);
            return entity;
        }

        protected override void EntityAttached(Unit entity)
        {
            base.EntityAttached(entity);

            unitsByColliders[entity.UnitCollider] = entity;
        }

        protected override void EntityDetached(Unit entity)
        {
            base.EntityDetached(entity);

            unitsByColliders.Remove(entity.UnitCollider);
        }
    }
}