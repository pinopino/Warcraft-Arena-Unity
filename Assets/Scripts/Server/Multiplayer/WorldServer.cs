using System.Collections.Generic;
using Common;
using Core;
using UnityEngine;

namespace Server
{
    public class WorldServer : World
    {
        private readonly List<PlayerServerInfo> playerInfos = new List<PlayerServerInfo>();
        private readonly Dictionary<BoltConnection, PlayerServerInfo> playerInfosByConnection = new Dictionary<BoltConnection, PlayerServerInfo>();
        private readonly Dictionary<ulong, PlayerServerInfo> playerInfosByPlayerId = new Dictionary<ulong, PlayerServerInfo>();
        private readonly GameSpellListener spellListener;
        private readonly GamePlayerListener playerListener;

        private PlayerServerInfo serverPlayerInfo;
        private ServerRoomToken serverRoomToken;

        private const int DisconnectedPlayerDestroyTime = 10000;

        public WorldServer(bool hasClientLogic)
        {
            HasServerLogic = true;
            HasClientLogic = hasClientLogic;

            spellListener = new GameSpellListener(this);
            playerListener = new GamePlayerListener(this);
        }

        public override void Dispose()
        {
            serverRoomToken = null;
            spellListener.Dispose();
            playerListener.Dispose();

            playerInfos.Clear();
            playerInfosByConnection.Clear();
            playerInfosByPlayerId.Clear();

            base.Dispose();
        }

        public override void DoUpdate(int deltaTime)
        {
            base.DoUpdate(deltaTime);

            for (int i = playerInfos.Count - 1; i >= 0; i--)
            {
                if (playerInfos[i].NetworkState == PlayerNetworkState.Disconnected)
                {
                    playerInfos[i].DisconnectTimeLeft -= deltaTime;
                    if (playerInfos[i].DisconnectTimeLeft <= 0)
                        UnitManager.Destroy(playerInfos[i].Player);
                }
            }
        }

        internal void ServerLaunched(ServerRoomToken sessionToken)
        {
            serverRoomToken = sessionToken;

            if (HasClientLogic)
            {
                Debug.Log("8.开始创建player，这里是创建服务器机主玩家"); // 删除
                CreatePlayer();
            }

            EventHandler.ExecuteEvent(this, GameEvents.ServerLaunched);
        }

        internal void EntityAttached(BoltEntity entity)
        {
            if (entity.PrefabId == BoltPrefabs.Movement)
            {
                entity.SetScopeAll(false);

                Player player = FindPlayer(entity.Source);
                if (player == null)
                    Object.Destroy(entity.gameObject);
                else
                    player.Motion.AttachMoveState(entity);
            }
        }

        internal void EntityDetached(BoltEntity entity)
        {
            if (entity.PrefabId == BoltPrefabs.Movement)
            {
                Player player = FindPlayer(entity.Source);
                if (player == null)
                    Object.Destroy(entity.gameObject);
                else
                    player.Motion.DetachMoveState(false);
            }

            if (playerInfosByPlayerId.ContainsKey(entity.NetworkId.PackedValue))
            {
                PlayerServerInfo removeInfo = playerInfosByPlayerId[entity.NetworkId.PackedValue];
                playerInfos.Remove(removeInfo);
                playerInfosByPlayerId.Remove(entity.NetworkId.PackedValue);

                if (serverPlayerInfo == removeInfo)
                    serverPlayerInfo = null;

                if (removeInfo.IsClient)
                    playerInfosByConnection.Remove(removeInfo.BoltConnection);
            }
        }

        internal void SetDefaultScope(BoltConnection connection)
        {
            UnitManager.SetDefaultScope(connection);
        }

        internal void SetNetworkState(BoltConnection connection, PlayerNetworkState state)
        {
            Assert.IsTrue(playerInfosByConnection.ContainsKey(connection), $"Failed to change connection state for {connection.RemoteEndPoint}");
            if (!playerInfosByConnection.ContainsKey(connection))
                return;

            playerInfosByConnection[connection].NetworkState = state;
            if (state == PlayerNetworkState.Disconnected)
                playerInfosByConnection[connection].DisconnectTimeLeft = DisconnectedPlayerDestroyTime;
        }

        /*
         * 说明：
         * 从CreatePlayer的两个调用方来看，一个是服务器房主这边加载好了world之后会创建房主对应的这个player；
         * 另一个则是如果服务器这边监听到远端的conn也加载好了场景，那么就会为这个远端的conn创建一个player对象；
         * （通过SceneLoadRemoteDone(BoltConnection connection)这个回调）
         * 
         */
        internal void CreatePlayer(BoltConnection boltConnection = null)
        {
            Map mainMap = MapManager.FindMap(1);
            Transform spawnPoint = RandomUtils.GetRandomElement(mainMap.Settings.FindSpawnPoints(Team.Alliance));

            ClassType classType;
            string playerName;
            string unityId;
            if (boltConnection == null)
            {
                playerName = serverRoomToken.LocalPlayerName;
                unityId = SystemInfo.deviceUniqueIdentifier;
                classType = (ClassType)PlayerPrefs.GetInt(UnitUtils.PreferredClassPrefName, 0);
            }
            else
            {
                // 说明：当conn不为空时，意味着需要为这个远程的conn创建一个player对象；
                // 在客户端中看见的就是一个新的玩家加载进来了。
                var connectionToken = (ClientConnectionToken)boltConnection.ConnectToken;
                playerName = connectionToken.Name;
                unityId = connectionToken.UnityId;
                classType = connectionToken.PrefferedClass;
            }

            if (!mainMap.Settings.Balance.ClassesByType.TryGetValue(classType, out ClassInfo classInfo) || !classInfo.IsAvailable)
                classType = ClassType.Mage;

            DebugHelper.PlayerName = playerName;

            var playerCreateToken = new Player.CreateToken
            {
                Position = spawnPoint.position,
                Rotation = spawnPoint.rotation,
                DeathState = DeathState.Alive,
                FreeForAll = true,
                ModelId = 1,
                ClassType = classType,
                OriginalModelId = 1,
                FactionId = mainMap.Settings.Balance.DefaultFaction.FactionId,
                PlayerName = playerName
            };
            Debug.Log(DebugHelper.Prefix + "1.实际的创建动作：调用UnitManager的Create方法（注意到因为player依次继承自Unit->WorldEntity->Entity，因此每个基类型都会有相关的初始化逻辑需要走）"); // 删除
            Player newPlayer = UnitManager.Create<Player>(BoltPrefabs.Player, playerCreateToken);
            /*
             * 说明：
             * 当conn不为空时，虽然local的这台unity client创建了该player entity，但是，
             * 应当清楚远程的conn才是这个entity的controller；因此，这里通过调用assignControl
             * 转移了控制权；当然，后续通过doc能知道，entity默认都是没给ctrl的，所以其实这里
             * conn为空的时候也是在赋值控制权
             * 
             */
            Debug.Log(DebugHelper.Prefix + "7.接着是调用AssignControl方法，bolt中没有显式赋值的时候控制权为false，newPlayer.IsOwner=" + newPlayer.IsOwner + "，newPlayer.IsController=" + newPlayer.IsController); // 删除
            newPlayer.AssignControl(boltConnection);
            newPlayer.UpdateVisibility(true);

            var newPlayerInfo = new PlayerServerInfo(boltConnection, newPlayer, unityId);
            playerInfos.Add(newPlayerInfo);
            playerInfosByPlayerId[newPlayer.Id] = newPlayerInfo;
            if (boltConnection != null)
                playerInfosByConnection[boltConnection] = newPlayerInfo;
            else
                serverPlayerInfo = newPlayerInfo;
        }

        public Player FindPlayer(BoltConnection boltConnection)
        {
            return boltConnection == null ? serverPlayerInfo?.Player : playerInfosByConnection.LookupEntry(boltConnection)?.Player;
        }

        public bool IsControlledByHuman(Player player) => playerInfosByPlayerId.ContainsKey(player.Id);
    }
}