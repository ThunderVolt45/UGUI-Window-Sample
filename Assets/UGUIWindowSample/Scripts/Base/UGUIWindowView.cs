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

        public void SetExitButtonActive(bool isActive)
        {
            if (windowHeader != null)
            {
                windowHeader.SetExitButtonActive(isActive);
            }
        }

        public void SetMaximizeButtonActive(bool isActive)
        {
            if (windowHeader != null)
            {
                windowHeader.SetMaximizeButtonActive(isActive);
            }
        }

        public void SetHeaderActive(bool isActive)
        {
            if (windowHeader != null)
            {
                windowHeader.gameObject.SetActive(isActive);
            }
        }

        public void SetBorderActive(bool isActive)
        {
            foreach (var border in windowBorders)
            {
                border.gameObject.SetActive(isActive);
            }

            foreach (var edge in windowEdges)
            {
                edge.gameObject.SetActive(isActive);
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

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
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
