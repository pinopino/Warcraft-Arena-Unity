using Core;
using Bolt;
using UnityEngine;

using Event = Bolt.Event;

namespace Server
{
    public partial class PhotonBoltServerListener
    {
        public override void OnEvent(SpellCastRequestDestinationEvent spellCastRequest)
        {
            base.OnEvent(spellCastRequest);

            HandleSpellCast(spellCastRequest, spellCastRequest.SpellId, (MovementFlags)spellCastRequest.MovementFlags, spellCastRequest.Destination);
        }

        public override void OnEvent(SpellCastRequestEvent spellCastRequest)
        {
            base.OnEvent(spellCastRequest);

            HandleSpellCast(spellCastRequest, spellCastRequest.SpellId, (MovementFlags)spellCastRequest.MovementFlags);
        }

        public override void OnEvent(SpellCastCancelRequestEvent spellCancelRequest)
        {
            base.OnEvent(spellCancelRequest);

            Player caster = World.FindPlayer(spellCancelRequest.RaisedBy);
            if (caster != null && caster.SpellCast.IsCasting)
                caster.SpellCast.Cancel();
        }

        private void HandleSpellCast(Event request, int spellId, MovementFlags movementFlags, Vector3? destination = null)
        {
            Debug.Log("request.FromSelf=" + request.FromSelf); // 删除
            Player caster = World.FindPlayer(request.RaisedBy);
            Debug.Log("caster == null ? " + (caster == null)); // 删除
            SpellCastRequestAnswerEvent spellCastAnswer = request.FromSelf
                ? SpellCastRequestAnswerEvent.Create(GlobalTargets.OnlyServer)
                : SpellCastRequestAnswerEvent.Create(request.RaisedBy);

            Debug.Log("SpellCastRequestAnswerEvent构造如下："); // 删除
            if (spellCastAnswer.ProcessingEntries != null)
            {
                Debug.Log("spellCastAnswer.ProcessingEntries.Count=" + ((SpellProcessingToken)spellCastAnswer.ProcessingEntries).ProcessingEntries.Count); // 删除
                if (((SpellProcessingToken)spellCastAnswer.ProcessingEntries).ProcessingEntries.Count > 0)
                {
                    Debug.Log("spellCastAnswer.ProcessingEntries[0].Item1=" + ((SpellProcessingToken)spellCastAnswer.ProcessingEntries).ProcessingEntries[0].Item1); // 删除
                    Debug.Log("spellCastAnswer.ProcessingEntries[0].Item2=" + ((SpellProcessingToken)spellCastAnswer.ProcessingEntries).ProcessingEntries[0].Item2); // 删除
                }
            }
            spellCastAnswer.SpellId = spellId;

            if (caster == null)
            {
                spellCastAnswer.Result = (int)SpellCastResult.CasterNotExists;
                spellCastAnswer.Send();
                return;
            }

            if (!balance.SpellInfosById.TryGetValue(spellId, out SpellInfo spellInfo))
            {
                spellCastAnswer.Result = (int)SpellCastResult.SpellUnavailable;
                spellCastAnswer.Send();
                return;
            }

            SpellCastingOptions options = destination.HasValue
                ? new SpellCastingOptions(new SpellExplicitTargets { Destination = destination.Value })
                : new SpellCastingOptions(movementFlags: movementFlags);

            SpellCastResult castResult = caster.Spells.CastSpell(spellInfo, options);
            if (castResult != SpellCastResult.Success)
            {
                spellCastAnswer.Result = (int)castResult;
                spellCastAnswer.Send();
            }
        }
    }
}
