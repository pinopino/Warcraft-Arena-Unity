using Bolt;

namespace Core
{
    public abstract class World
    {
        private readonly EntityPool entityPool = new EntityPool();
        private readonly IPrefabPool defaultPool = new DefaultPrefabPool();

        internal SpellManager SpellManager { get; }
        internal MapManager MapManager { get; }

        public UnitManager UnitManager { get; }

        /*
         * 说明：会有这两个变量是因为bolt的网络模式是支持本地充当服务端的；
         * 有点类似于p2p了（当然也不全是，因为文档看下来似乎bolt的所有东西还是会过一次它的云服务器）
         * 
         * 因此，可以说后续在不带网络的改造中其现有的服务端逻辑和客户端逻辑都应当保留下来才对，这算
         * 是一个基本的改造思路吧。
         * 
         * 比如，按照上面这种思路后续我们可以完全不要ClientWorld，只保留ServerWorld；因为可以理解成
         * 我们只有一个人打开了游戏并且他就是服务端，没有其它client连上来。
         * 
         */
        public bool HasServerLogic { get; protected set; }
        public bool HasClientLogic { get; protected set; }

        protected World()
        {
            entityPool.Initialize(this);
            BoltNetwork.SetPrefabPool(entityPool);

            UnitManager = new UnitManager();
            MapManager = new MapManager(this);
            SpellManager = new SpellManager(this);
        }

        public virtual void Dispose()
        {
            SpellManager.Dispose();
            UnitManager.Dispose();
            MapManager.Dispose();

            BoltNetwork.SetPrefabPool(defaultPool);
            entityPool.Deinitialize();
        }

        public virtual void DoUpdate(int deltaTime)
        {
            MapManager.DoUpdate(deltaTime);

            UnitManager.DoUpdate(deltaTime);

            if(HasServerLogic)
                SpellManager.DoUpdate(deltaTime);
        }

        public Map FindMap(int mapId)
        {
            return MapManager.FindMap(mapId);
        }
    }
}