using Bolt;
using Common;
using Core;
using UnityEngine;

namespace Server
{
    internal class GameSpellListener : BaseGameListener
    {
        internal GameSpellListener(WorldServer world) : base(world)
        {
            // 说明：目前分析下来GameEvents中这些Server打头的就是说这些事件都需要发生在服务端，
            // 不相信客户端的计算，然后结果再下发到客户端中去
            EventHandler.RegisterEvent<SpellDamageInfo>(GameEvents.ServerDamageDone, OnSpellDamageDone);
            EventHandler.RegisterEvent<SpellHealInfo>(GameEvents.ServerHealingDone, OnSpellHealingDone);
            EventHandler.RegisterEvent<Unit, SpellInfo, SpellProcessingToken>(GameEvents.ServerSpellLaunch, OnServerSpellLaunch);
            EventHandler.RegisterEvent<Unit, Unit, SpellInfo, SpellMissType>(GameEvents.ServerSpellHit, OnServerSpellHit);
            EventHandler.RegisterEvent<Player, Vector3>(GameEvents.ServerPlayerTeleport, OnServerPlayerTeleport);
            EventHandler.RegisterEvent<Player, SpellCooldown>(GameEvents.ServerSpellCooldown, OnServerSpellCooldown);
            EventHandler.RegisterEvent<Player, SpellChargeCooldown>(GameEvents.ServerSpellCharge, OnServerSpellChargeCooldown);
        }

        internal void Dispose()
        {
            EventHandler.UnregisterEvent<SpellDamageInfo>(GameEvents.ServerDamageDone, OnSpellDamageDone);
            EventHandler.UnregisterEvent<SpellHealInfo>(GameEvents.ServerHealingDone, OnSpellHealingDone);
            EventHandler.UnregisterEvent<Unit, SpellInfo, SpellProcessingToken>(GameEvents.ServerSpellLaunch, OnServerSpellLaunch);
            EventHandler.UnregisterEvent<Unit, Unit, SpellInfo, SpellMissType>(GameEvents.ServerSpellHit, OnServerSpellHit);
            EventHandler.UnregisterEvent<Player, Vector3>(GameEvents.ServerPlayerTeleport, OnServerPlayerTeleport);
            EventHandler.UnregisterEvent<Player, SpellCooldown>(GameEvents.ServerSpellCooldown, OnServerSpellCooldown);
            EventHandler.UnregisterEvent<Player, SpellChargeCooldown>(GameEvents.ServerSpellCharge, OnServerSpellChargeCooldown);
        }

        private void OnSpellDamageDone(SpellDamageInfo damageInfo)
        {
            if (damageInfo.Caster is Player player && World.IsControlledByHuman(player))
            {
                // 说明：通过iscontroller区分下是服务器房主还是其它玩家，
                // 而如果是其它玩家的话则这个事件需要下发到对应客户端去
                SpellDamageDoneEvent spellDamageEvent = player.IsController
                    ? SpellDamageDoneEvent.Create(GlobalTargets.OnlyServer, ReliabilityModes.ReliableOrdered)
                    : SpellDamageDoneEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);

                spellDamageEvent.Target = damageInfo.Target.BoltEntity.NetworkId;
                spellDamageEvent.DamageAmount = (int)damageInfo.Damage;
                spellDamageEvent.HitType = (int)damageInfo.HitType;
                spellDamageEvent.Send();
            }

            // 说明：用这个事件应该是想对应其它生物施放技能
            UnitSpellDamageEvent unitSpellDemageEvent = UnitSpellDamageEvent.Create(damageInfo.Target.BoltEntity, EntityTargets.Everyone);
            unitSpellDemageEvent.CasterId = damageInfo.Caster.BoltEntity.NetworkId;
            unitSpellDemageEvent.Damage = (int)damageInfo.Damage;
            unitSpellDemageEvent.HitType = (int)damageInfo.HitType;
            unitSpellDemageEvent.Send();
        }

        private void OnSpellHealingDone(SpellHealInfo healInfo)
        {
            if (healInfo.Healer is Player player && World.IsControlledByHuman(player))
            {
                SpellHealingDoneEvent spellDamageEvent = player.IsController
                    ? SpellHealingDoneEvent.Create(GlobalTargets.OnlyServer, ReliabilityModes.ReliableOrdered)
                    : SpellHealingDoneEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);

                spellDamageEvent.Target = healInfo.Target.BoltEntity.NetworkId;
                spellDamageEvent.HealAmount = (int)healInfo.Heal;
                spellDamageEvent.IsCrit = healInfo.HasCrit;
                spellDamageEvent.Send();
            }

            // ignore unit healing event, since it currently affects nothing
        }

        private void OnServerSpellLaunch(Unit caster, SpellInfo spellInfo, SpellProcessingToken processingToken)
        {
            UnitSpellLaunchEvent unitCastEvent = UnitSpellLaunchEvent.Create(caster.BoltEntity, EntityTargets.Everyone);
            unitCastEvent.SpellId = spellInfo.Id;
            unitCastEvent.ProcessingEntries = processingToken;
            unitCastEvent.Send();

            SpellCastRequestAnswerEvent spellCastAnswer = caster.IsController
                ? SpellCastRequestAnswerEvent.Create(GlobalTargets.OnlyServer, ReliabilityModes.ReliableOrdered)
                : SpellCastRequestAnswerEvent.Create(caster.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);

            spellCastAnswer.SpellId = spellInfo.Id;
            spellCastAnswer.Result = (int)SpellCastResult.Success;
            spellCastAnswer.ProcessingEntries = processingToken;
            spellCastAnswer.Send();
        }

        private void OnServerSpellHit(Unit caster, Unit target, SpellInfo spellInfo, SpellMissType missType)
        {
            if (missType != SpellMissType.None && caster is Player player && World.IsControlledByHuman(player))
            {
                SpellMissDoneEvent spellMissEvent = player.IsController
                    ? SpellMissDoneEvent.Create(GlobalTargets.OnlyServer, ReliabilityModes.ReliableOrdered)
                    : SpellMissDoneEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);

                spellMissEvent.TargetId = target.BoltEntity.NetworkId;
                spellMissEvent.MissType = (int)missType;
                spellMissEvent.Send();
            }

            UnitSpellHitEvent unitSpellHitEvent = UnitSpellHitEvent.Create(target.BoltEntity, EntityTargets.Everyone);
            unitSpellHitEvent.CasterId = caster.BoltEntity.NetworkId;
            unitSpellHitEvent.SpellId = spellInfo.Id;
            unitSpellHitEvent.MissType = (int)missType;
            unitSpellHitEvent.Send();
        }

        private void OnServerPlayerTeleport(Player player, Vector3 targetPosition)
        {
            if (player.BoltEntity.Controller != null)
            {
                SpellPlayerTeleportEvent spellTeleportEvent = SpellPlayerTeleportEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                spellTeleportEvent.TargetPosition = targetPosition;
                spellTeleportEvent.Send();
            }
        }

        private void OnServerSpellCooldown(Player player, SpellCooldown cooldown)
        {
            if (player.BoltEntity.Controller != null)
            {
                SpellCooldownEvent spellCooldownEvent = SpellCooldownEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                spellCooldownEvent.SpellId = cooldown.SpellId;
                spellCooldownEvent.CooldownTime = cooldown.Cooldown;
                spellCooldownEvent.ServerFrame = BoltNetwork.ServerFrame;
                spellCooldownEvent.Send();
            }
        }

        private void OnServerSpellChargeCooldown(Player player, SpellChargeCooldown cooldown)
        {
            if (player.BoltEntity.Controller != null)
            {
                SpellChargeEvent spellChargeEvent = SpellChargeEvent.Create(player.BoltEntity.Controller, ReliabilityModes.ReliableOrdered);
                spellChargeEvent.SpellId = cooldown.SpellId;
                spellChargeEvent.CooldownTime = cooldown.ChargeTime;
                spellChargeEvent.ServerFrame = BoltNetwork.ServerFrame;
                spellChargeEvent.Send();
            }
        }
    }
}
