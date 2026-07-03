using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class UGUITaskIcon : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        private readonly Color normalColor = new Color(0.18f, 0.2f, 0.24f, 0.96f);
        private readonly Color focusedColor = new Color(0.27f, 0.48f, 0.84f, 1f);
        private readonly Color minimizedColor = new Color(0.12f, 0.13f, 0.16f, 0.82f);

        private UGUIWindow targetWindow;

        public UGUIWindow TargetWindow
        {
            get { return targetWindow; }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetReferences(Image background, Image icon)
        {
            backgroundImage = background;
            iconImage = icon;
            ResolveReferences();
        }

        public void Initialize(UGUIWindow window)
        {
            ResolveReferences();

            targetWindow = window;
            Refresh(false);
        }

        public void Refresh(bool focused)
        {
            if (targetWindow == null)
            {
                return;
            }

            bool minimized = targetWindow.WindowMode == UGUIWindowMode.Minimized;

            if (backgroundImage != null)
            {
                backgroundImage.color = minimized ? minimizedColor : focused ? focusedColor : normalColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = targetWindow.WindowIcon;
                iconImage.enabled = targetWindow.WindowIcon != null;
                iconImage.preserveAspect = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || targetWindow == null)
            {
                return;
            }

            if (targetWindow.WindowMode == UGUIWindowMode.Minimized)
            {
                targetWindow.RestoreFromMinimized();
            }
            else
            {
                targetWindow.Focus();
            }
        }

        private void ResolveReferences()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
        }
    }
}
