namespace Server
{
    // 说明：区别于PhotonBoltBaseListener，一个是游戏内的监听器，另一个则是负责网络监听的
    internal abstract class BaseGameListener
    {
        protected readonly WorldServer World;

        internal BaseGameListener(WorldServer world)
        {
            World = world;
        }
    }
}
