using Common;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class ButtonSlot : UIBehaviour, IPointerDownHandler, IDropHandler
    {
        [SerializeField] private HotkeyInputItem hotkeyInput;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ButtonContent buttonContent;
        [SerializeField] private SoundEntry pressSound;
        [SerializeField] private TextMeshProUGUI hotkeyText;

        public RectTransform RectTransform => rectTransform;
        public ButtonContent ButtonContent => buttonContent;

        public void Initialize()
        {
            buttonContent.Initialize(this);

            EventHandler.RegisterEvent<HotkeyState>(hotkeyInput, GameEvents.HotkeyStateChanged, OnHotkeyStateChanged);
            EventHandler.RegisterEvent(hotkeyInput, GameEvents.HotkeyBindingChanged, OnHotkeyBindingChanged);

            OnHotkeyBindingChanged();
        }

        public void Denitialize()
        {
            EventHandler.UnregisterEvent<HotkeyState>(hotkeyInput, GameEvents.HotkeyStateChanged, OnHotkeyStateChanged);
            EventHandler.UnregisterEvent(hotkeyInput, GameEvents.HotkeyBindingChanged, OnHotkeyBindingChanged);

            buttonContent.Deinitialize();
        }

        public void DoUpdate()
        {
            buttonContent.DoUpdate();
        }

        [Description("Also called from manually pressing button.")]
        public void Click()
        {
            if (!buttonContent.IsAlreadyPressed)
            {
                pressSound?.Play();
                buttonContent.Activate();
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrop(PointerEventData data)
        {
        }

        private void OnHotkeyStateChanged(HotkeyState state)
        {
            if (state == HotkeyState.Pressed)
                Click();

            Debug.Log(hotkeyInput.KeyCode + ", Pressed: " + HotkeyState.Pressed);
            buttonContent.HandleHotkeyState(state);
        }

        private void OnHotkeyBindingChanged()
        {
            hotkeyText.text = LocalizationReference.Localize(hotkeyInput);
        }
    }
}