using Common;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Localization
{
    public abstract class LocalizationReference : ScriptableReference
    {
        [SerializeField] private LocalizedLanguageType defaultLanguage = LocalizedLanguageType.English;

        private static readonly List<LocalizedBehaviour> UsedBehaviours = new List<LocalizedBehaviour>();

        protected override void OnRegistered()
        {
            LoadLanguage(defaultLanguage);
        }

        protected override void OnUnregister()
        {
            UsedBehaviours.Clear();
        }

        internal static void AddBehaviour(LocalizedBehaviour behaviour)
        {
            behaviour.Localize();

            UsedBehaviours.Add(behaviour);
        }

        internal static void RemoveBehaviour(LocalizedBehaviour behaviour)
        {
            UsedBehaviours.Remove(behaviour);
        }

        private void LoadLanguage(LocalizedLanguageType languageType)
        {
            Resources.Load<LocalizedLanguage>($"Languages/{languageType}").Localize();
            Resources.UnloadUnusedAssets();

            foreach (var behaviour in UsedBehaviours)
                behaviour.Localize();
        }

        [ContextMenu("Set to English")]
        private void SetEnglishLanguage()
        {
            LoadLanguage(LocalizedLanguageType.English);
        }

        [ContextMenu("Set to Spanish")]
        private void SetSpanishLanguage()
        {
            LoadLanguage(LocalizedLanguageType.Spanish);
        }

        [ContextMenu("Set to German")]
        private void SetGermanLanguage()
        {
            LoadLanguage(LocalizedLanguageType.German);
        }

        [ContextMenu("Set to Italian")]
        private void SetItalianLanguage()
        {
            LoadLanguage(LocalizedLanguageType.Italian);
        }
    }
}
