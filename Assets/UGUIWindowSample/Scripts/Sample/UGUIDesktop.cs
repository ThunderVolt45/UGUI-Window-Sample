using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUIDesktop : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private List<UGUIIcon> icons = new();
        [SerializeField] private bool createDemoWindowsOnStart = false;

        [Space(5f)]
        public UnityEvent<UGUIIcon> OnIconClicked;

        private CanvasScaler canvasScaler;

        #region Initialize
        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
        }

        private void Start()
        {
            FindIconInTransformRecursion(transform);
            OnIconClicked.AddListener(DivertOtherIcon);
            UGUITaskBar.Instance.AttachToDesktop(this);

            if (createDemoWindowsOnStart)
            {
                CreateDemoWindows();
            }
        }

        private void CreateDemoWindows()
        {
            UGUIWindowManager.CreateWindow<UGUIWindow>();
            UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>(null, -200, 0, 250, 250);
            UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -150, 50, 250, 250);
            UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -100, 100, 250, 250);
        }

        private void FindIconInTransformRecursion(Transform transform)
        {
            if (transform.TryGetComponent(out UGUIIcon icon))
            {
                icons.Add(icon);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                FindIconInTransformRecursion(child);
            }
        }
        #endregion

        #region Event Listener
        private void DivertOtherIcon(UGUIIcon clickedIcon)
        {
            foreach (var icon in icons)
            {
                if (icon != clickedIcon)
                {
                    icon.Divert();
                }
            }
        }

        public void OnDPIChange(int screenWidth, int screenHeight, float dpi)
        {
            canvasScaler.referenceResolution =
                new Vector2(screenWidth / dpi, screenHeight / dpi);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var icon in icons)
            {
                icon.Divert();
            }
        }
        #endregion
    }
}
