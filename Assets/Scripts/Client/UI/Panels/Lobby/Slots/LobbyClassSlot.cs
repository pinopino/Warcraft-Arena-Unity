using Common;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class LobbyClassSlot : MonoBehaviour
    {
        [SerializeField] private Button slotButton;
        [SerializeField] private Image selectedFrame;
        [SerializeField] private Image classFrame;
        [SerializeField] private ClassInfo classInfo;

        private static object EventTarget { get; } = new object();

        private void Awake()
        {
            slotButton.onClick.AddListener(OnSlotClicked);
            EventHandler.RegisterEvent(EventTarget, GameEvents.LobbyClassChanged, OnLobbyClassChanged);
        }

        private void Start()
        {
            UpdateSelection();
        }

        private void OnDestroy()
        {
            EventHandler.UnregisterEvent(EventTarget, GameEvents.LobbyClassChanged, OnLobbyClassChanged);
            slotButton.onClick.RemoveListener(OnSlotClicked);
        }

        private void UpdateSelection()
        {
            bool isSelected = PlayerPrefs.GetInt(UnitUtils.PreferredClassPrefName, 0) == (int)classInfo.ClassType;
            slotButton.interactable = classInfo.IsAvailable;
            selectedFrame.enabled = isSelected;
        }

        private void OnSlotClicked()
        {
            PlayerPrefs.SetInt(UnitUtils.PreferredClassPrefName, (int)classInfo.ClassType);

            EventHandler.ExecuteEvent(EventTarget, GameEvents.LobbyClassChanged);
        }

        private void OnLobbyClassChanged() => UpdateSelection();
    }
}
