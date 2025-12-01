using Bolt;
using Common;
using UdpKit;
using UnityEngine;

namespace Core
{
    public abstract class Entity : EntityBehaviour
    {
        public abstract class CreateToken : IProtocolToken
        {
            public abstract void Read(UdpPacket packet);

            public abstract void Write(UdpPacket packet);
        }

        [SerializeField, Header(nameof(Entity))] private BalanceReference balance;

        protected BalanceReference Balance => balance;
        protected bool IsValid { get; private set; }

        internal World World { get; private set; }
        internal abstract bool AutoScoped { get; }

        public BoltEntity BoltEntity => entity;
        public bool IsOwner => entity.IsOwner;
        public bool IsController => entity.HasControl;
        public ulong Id { get; private set; }

        private void Awake() => EventHandler.RegisterEvent<bool, World>(gameObject, GameEvents.EntityPooled, OnEntityPooled);

        private void OnDestroy() => EventHandler.UnregisterEvent<bool, World>(gameObject, GameEvents.EntityPooled, OnEntityPooled);

        public override void Attached()
        {
            base.Attached();
            Id = entity.NetworkId.PackedValue;
            IsValid = true;
            Debug.Log(DebugHelper.Prefix + "2.实际的创建动作：Entity中的Attach方法被bolt运行时执行，重点在于："); // 删除
            Debug.Log("     " + DebugHelper.Prefix + "2.a. 为该entity分配了id号：" + Id); // 删除
            Debug.Log("     " + DebugHelper.Prefix + "2.b. 赋值了World字段"); // 删除
        }

        public override void Detached()
        {
            IsValid = false;

            base.Detached();
        }

        internal virtual void DoUpdate(int deltaTime)
        {
        }

        private void OnEntityPooled(bool isTaken, World world) => World = isTaken ? world : null;
    }
}