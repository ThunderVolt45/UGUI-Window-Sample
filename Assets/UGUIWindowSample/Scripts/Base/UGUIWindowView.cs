using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UGUIWindowView : MonoBehaviour
    {
        [Header("Base Components")]
        public UGUIWindowHeader windowHeader;
        public List<UGUIWindowBorder> windowBorders;
        public List<UGUIWindowEdge> windowEdges;

        private CanvasGroup canvasGroup;

        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            RectTransform = transform as RectTransform;
        }
        
        public void SetTitle(string title)
        {
            if (windowHeader != null)
            {
                windowHeader.SetTitle(title);
            }
        }

        public void SetExitButtonActive(bool value)
        {
            if (windowHeader != null)
            {
                windowHeader.SetExitButtonActive(value);
            }
        }

        public void SetMaximizeButtonActive(bool value)
        {
            if (windowHeader != null)
            {
                windowHeader.SetMaximizeButtonActive(value);
            }
        }

        public void SetHeaderActive(bool value)
        {
            if (windowHeader == null)
            {
                return;
            }

            windowHeader.gameObject.SetActive(value);
            ResolveBorderEdgeOverlap();
        }

        public void SetBorderActive(bool value)
        {
            foreach (var border in windowBorders)
            {
                border.gameObject.SetActive(value);
            }

            foreach (var edge in windowEdges)
            {
                edge.gameObject.SetActive(value);
            }

            ResolveBorderEdgeOverlap();
        }

        /// <summary>
        /// Header와 Border, Edge가 겹치지 않도록 하는 함수
        /// </summary>
        private void ResolveBorderEdgeOverlap()
        {
            if (windowHeader == null)
            {
                return;
            }

            bool isHeaderActive = windowHeader.gameObject.activeSelf;

            foreach (var border in windowBorders)
            {
                if (border.GetComponentInParent<UGUIWindowHeader>() == null && border.borderPosition == UGUIBorderPosition.North)
                {
                    border.gameObject.SetActive(!isHeaderActive);
                }
            }

            foreach (var edge in windowEdges)
            {
                if (edge.GetComponentInParent<UGUIWindowHeader>() == null)
                {
                    if (edge.edgePosition == UGUIEdgePosition.NorthEast || edge.edgePosition == UGUIEdgePosition.NorthWest)
                    {
                        edge.gameObject.SetActive(!isHeaderActive);
                    }
                }
            }
        }

        public async Awaitable Fade(float startAlpha, float targetAlpha, float startScale, float targetScale, float fadeDuration = 0.15f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                await Awaitable.NextFrameAsync();

                elapsedTime += Time.deltaTime;

                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
                float newScale = Mathf.Lerp(startScale, targetScale, elapsedTime / fadeDuration);

                canvasGroup.alpha = newAlpha;
                transform.localScale = new Vector3(newScale, newScale, newScale);
            }

            canvasGroup.alpha = targetAlpha;
            transform.localScale = new Vector3(targetScale, targetScale, targetScale);
        }

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void ApplyMaximizedState(float headerHeight)
        {
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;
            RectTransform.anchoredPosition = new Vector2(0, -headerHeight / 2);
            RectTransform.sizeDelta = new Vector2(0, -headerHeight);
        }

        public void ApplyRestoredState(UGUIWindowState state)
        {
            if (state == null) return;

            RectTransform.anchorMin = state.anchorMin;
            RectTransform.anchorMax = state.anchorMax;
            RectTransform.anchoredPosition = state.anchoredPosition;
            RectTransform.sizeDelta = state.sizeDelta;
        }
    }
}
