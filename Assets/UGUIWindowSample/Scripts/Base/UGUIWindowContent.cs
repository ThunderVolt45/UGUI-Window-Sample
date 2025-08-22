using UnityEngine;
using UnityEngine.EventSystems;

namespace UGUIWindow
{
    public class UGUIWindowContent : MonoBehaviour, IPointerDownHandler
    {
        private UGUIWindow parentWindow;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            parentWindow = GetComponentInParent<UGUIWindow>();
        }
        
        #region Interface
        public void OnPointerDown(PointerEventData eventData)
        {
            parentWindow.Focus();
        }
        #endregion
    }
}