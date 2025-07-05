using Bolt;
using Bolt.Utils;
using Common;
using Core;
using UdpKit;
using UnityEngine;

namespace Server
{
    public partial class PhotonBoltServerListener : PhotonBoltBaseListener
    {
        [SerializeField] private BalanceReference balance;
        [SerializeField] private PhotonBoltReference photon;

        private new WorldServer World { get; set; }
        private ServerLaunchState LaunchState { get; set; }
        private ServerRoomToken ServerToken { get; set; }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            World = (WorldServer)world;

            EventHandler.RegisterEvent<ServerRoomToken>(photon, GameEvents.ServerMapLoaded, OnMapLoaded);
        }

        public override void Deinitialize()
        {
            EventHandler.UnregisterEvent<ServerRoomToken>(photon, GameEvents.ServerMapLoaded, OnMapLoaded);

            World = null;

            ServerToken = null;
            LaunchState = 0;

            base.Deinitialize();
        }

        public override void SceneLoadLocalDone(string map, IProtocolToken token)
        {
            // TODO(TwiiK): After upgrading Bolt from 1.2.9 to 1.2.15 the "Launcher" scene would be passed in here as
            // well, which cause errors like duplicate players etc. It wasn't like this originally, but I'm not sure
            // what exactly has changed in Bolt to cause this. I'm sure this can be fixed properly, and not with a hack
            // like this, but I'm not going to investigate that at the moment. In another project I've upgraded Bolt to
            // 1.3.2 and the problem is there as well, so I assume this is due to some change in Bolt itself.
            if (map == "Launcher")
            {
                return;
            }

            Debug.Log("5.boltServerListener.SceneLoadLocalDone");

            base.SceneLoadLocalDone(map, token);

            if (BoltNetwork.IsConnected)
            {
                World.MapManager.InitializeLoadedMap(1);
                Debug.Log("6.World.MapManager.InitializeLoadedMap"); // 删除

                EventHandler.ExecuteEvent(photon, GameEvents.ServerMapLoaded, (ServerRoomToken)token);
            }
        }

        // 说明：可以理解为目前这里是server，这个conn是客户端连接上来的时候为其创建的；
        // 当这个远程的客户端成功加载了场景的时候server能够收到这个事件，通过这里的
        // SceneLoadRemoteDone回调；
        public override void SceneLoadRemoteDone(BoltConnection connection)
        {
            base.SceneLoadRemoteDone(connection);

            World.CreatePlayer(connection);
        }

        public override void SessionCreated(UdpSession session)
        {
            base.SessionCreated(session);

            HandleRoomCreation((ServerRoomToken)session.GetProtocolToken());
        }

        public override void ConnectRequest(UdpEndPoint endpoint, IProtocolToken token)
        {
            base.ConnectRequest(endpoint, token);

            if (!(token is ClientConnectionToken clientToken) || !clientToken.IsValid)
            {
                BoltNetwork.Refuse(endpoint, new ClientRefuseToken(ConnectRefusedReason.InvalidToken));
                return;
            }

            if (clientToken.UnityId == SystemInfo.unsupportedIdentifier)
            {
                BoltNetwork.Refuse(endpoint, new ClientRefuseToken(ConnectRefusedReason.UnsupportedDevice));
                return;
            }

            if (clientToken.Version != ServerToken.Version)
            {
                BoltNetwork.Refuse(endpoint, new ClientRefuseToken(ConnectRefusedReason.InvalidVersion));
                return;
            }

            BoltNetwork.Accept(endpoint);
        }

        public override void Connected(BoltConnection boltConnection)
        {
            base.Connected(boltConnection);

            World.SetDefaultScope(boltConnection);
        }

        public override void Disconnected(BoltConnection boltConnection)
        {
            base.Disconnected(boltConnection);

            World.SetNetworkState(boltConnection, PlayerNetworkState.Disconnected);
        }

        public override void EntityAttached(BoltEntity entity)
        {
            base.EntityAttached(entity);

            World.EntityAttached(entity);
        }

        public override void EntityDetached(BoltEntity entity)
        {
            base.EntityDetached(entity);

            World.EntityDetached(entity);
        }

        private void OnMapLoaded(ServerRoomToken roomToken)
        {
            ProcessServerLaunchState(ServerLaunchState.MapLoaded);

            if (BoltNetwork.IsSinglePlayer)
                HandleRoomCreation(roomToken);
        }

        private void HandleRoomCreation(ServerRoomToken roomToken)
        {
            ServerToken = roomToken;

            ProcessServerLaunchState(ServerLaunchState.SessionCreated);
        }

        private void ProcessServerLaunchState(ServerLaunchState state)
        {
            Debug.Log("7.最终步，World.ServerLaunched，剩下就是创建player和creature了"); // 删除
            LaunchState |= state;

            if (LaunchState == ServerLaunchState.Complete)
                World.ServerLaunched(ServerToken);
        }
    }
}
