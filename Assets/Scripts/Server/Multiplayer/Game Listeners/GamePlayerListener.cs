using Bolt;
using Common;
using Core;
using UnityEngine;

namespace Server
{
    internal class GamePlayerListener : BaseGameListener
    {
        internal GamePlayerListener(WorldServer world) : base(world)
        {
            EventHandler.RegisterEvent<Player, UnitMoveType, float>(GameEvents.ServerPlayerSpeedChanged, OnPlayerSpeedChanged);
            EventHandler.RegisterEvent<Player, bool>(GameEvents.ServerPlayerRootChanged, OnPlayerRootChanged);
            EventHandler.RegisterEvent<Player, bool>(GameEvents.ServerPlayerMovementControlChanged, OnPlayerMovementControlChanged);
        }

        internal void Dispose()
        {
            EventHandler.UnregisterEvent<Player, UnitMoveType, float>(GameEvents.ServerPlayerSpeedChanged, OnPlayerSpeedChanged);
            EventHandler.UnregisterEvent<Player, bool>(GameEvents.ServerPlayerRootChanged, OnPlayerRootChanged);
            EventHandler.UnregisterEvent<Player, bool>(GameEvents.ServerPlayerMovementControlChanged, OnPlayerMovementControlChanged);
        }

        private void OnPlayerSpeedChanged(Player player, UnitMoveType moveType, float rate)
        {
            if (player.BoltEntity.Controller != null)
            {
                PlayerSpeedRateChangedEvent speedChangeEvent = PlayerSpeedRateChangedEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                speedChangeEvent.MoveType = (int)moveType;
                speedChangeEvent.SpeedRate = rate;
                speedChangeEvent.Send();
            }
        }

        private void OnPlayerRootChanged(Player player, bool applied)
        {
            if (player.BoltEntity.Controller != null)
            {
                PlayerRootChangedEvent rootChangedEvent = PlayerRootChangedEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                rootChangedEvent.Applied = applied;
                rootChangedEvent.Send();
            }
        }

        private void OnPlayerMovementControlChanged(Player player, bool hasControl)
        {
            if (player.BoltEntity.Controller != null)
            {
                Debug.Log("     " + DebugHelper.Prefix + "7.d. GamePlayerListener响应ServerPlayerMovementControlChanged事件，且player.BoltEntity.Controller不为空（即当前player（name=" + player.Name + "）是远程连上来的那个玩家）"); // 删除
                Debug.Log("     " + DebugHelper.Prefix + "7.e. 因此构造PlayerMovementControlChanged对象并Send()，以此来通知远程对端可以控制该player了"); // 删除
                PlayerMovementControlChanged movementControlEvent = PlayerMovementControlChanged.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                movementControlEvent.PlayerHasControl = hasControl;
                movementControlEvent.LastServerPosition = player.Position;
                movementControlEvent.LastServerMovementFlags = (int)player.MovementFlags;
                movementControlEvent.Send();
            }
            else
            {
                Debug.Log("     " + DebugHelper.Prefix + "7.d. GamePlayerListener响应ServerPlayerMovementControlChanged事件，但player.BoltEntity.Controller为空（name=" + player.Name + "），直接return了"); // 删除
            }
        }
    }
}
