using Common;
using Core;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class ActionErrorDisplay : MonoBehaviour
    {
        [SerializeField] private ActionErrorItem errorItemPrototype;
        [SerializeField] private ActionErrorDisplaySettings settings;
        [SerializeField] private SoundEntry errorAppearSound;
        [SerializeField] private RectTransform errorContainer;
        [SerializeField] private int preinstantiatedCount = 20;

        private readonly List<ActionErrorItem> activeErrors = new List<ActionErrorItem>();

        public void Initialize()
        {
            GameObjectPool.PreInstantiate(errorItemPrototype.gameObject, preinstantiatedCount);

            EventHandler.RegisterEvent<SpellCastResult>(GameEvents.ClientSpellFailed, OnClientSpellFailed);
        }

        public void Deinitialize()
        {
            EventHandler.UnregisterEvent<SpellCastResult>(GameEvents.ClientSpellFailed, OnClientSpellFailed);

            for (int i = activeErrors.Count - 1; i >= 0; i--)
            {
                GameObjectPool.Return(activeErrors[i], true);
                Destroy(activeErrors[i]);
            }

            activeErrors.Clear();
        }

        public void DoUpdate(float deltaTime)
        {
            for (int i = activeErrors.Count - 1; i >= 0; i--)
            {
                if (activeErrors[i].DoUpdate(deltaTime))
                {
                    GameObjectPool.Return(activeErrors[i], false);
                    activeErrors.RemoveAt(i);
                }
            }
        }

        private void OnClientSpellFailed(SpellCastResult castResult)
        {
            if (!settings.AllowRepeating)
                foreach (var item in activeErrors)
                    if (item.CastResult == castResult)
                        return;

            errorAppearSound?.Play();
            ActionErrorItem newError = GameObjectPool.Take(errorItemPrototype, errorContainer.position, errorContainer.rotation, errorContainer);
            newError.SetErrorText(castResult);
            newError.RectTransform.SetAsFirstSibling();
            activeErrors.Add(newError);
        }
    }
}