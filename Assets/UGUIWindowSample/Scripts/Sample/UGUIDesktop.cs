using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUIDesktop : MonoBehaviour
    {
        private CanvasScaler canvasScaler;

        private void Start()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            UGUIWindowManager.Instance.OnDPIChanged.AddListener(OnDPIChange);
        }

        private void OnDPIChange(int screenWidth, int screenHeight, float dpi)
        {
            canvasScaler.referenceResolution =
                new Vector2(screenWidth / dpi, screenHeight / dpi);
        }
    }
}
