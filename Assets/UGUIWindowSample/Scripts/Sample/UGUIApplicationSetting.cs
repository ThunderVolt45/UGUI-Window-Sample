using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    public class UGUIApplicationSetting : UGUIWindow
    {
        [Header("Application Setting")]
        [Tooltip("지원할 최소 해상도")]
        [SerializeField] private Vector2 minimumResolution = new Vector2(1280, 720);

        [Tooltip("지원할 최소 FPS")]
        [SerializeField] [Range(10, 1000)] private int minimumFPS = 60;

        [Tooltip("지원할 DPI 설정 (1f = 100%)")]
        [SerializeField] [Range(1f, 4f)] private float[] supportDPI;

        [Header("UI Dropdown")]
        [SerializeField] private TMP_Dropdown dropdownWindowMode;
        [SerializeField] private TMP_Dropdown dropdownResolution;
        [SerializeField] private TMP_Dropdown dropdownFramerate;
        [SerializeField] private TMP_Dropdown dropdownDPI;

        [Header("UI Button")]
        [SerializeField] private Button buttonApply;
        [SerializeField] private Button buttonExit;

        private FullScreenMode targetWindowMode;
        private int targetWidth;
        private int targetHeight;
        private RefreshRate targetFrameRate;
        private float targetDPI;

        #region Initialize
        protected override void Awake()
        {
            base.Awake();

            InitializeDropdown();
            InitalizeButton();
        }

        private void OnEnable()
        {
            DetectCurrentSettings();
        }

        private void InitializeDropdown()
        {
            List<string> windowModeList = new List<string>();
            List<string> resolutionList = new List<string>();
            List<string> framerateList = new List<string>();
            List<string> DPIList = new List<string>();

            // 윈도우 모드 리스트
            windowModeList.Add("FullScreen");
            windowModeList.Add("Borderless Window");
            windowModeList.Add("Windowed");

            // 해상도 리스트와 프레임레이트 리스트
            foreach (var resolution in Screen.resolutions)
            {
                // 최소 해상도보다 작은 해상도 옵션은 제거한다.
                if (resolution.width < minimumResolution.x) continue;
                if (resolution.height < minimumResolution.y) continue;

                // 세로 화면비 해상도 옵션은 제거한다.
                if (resolution.width < resolution.height) continue;

                // 최소 FPS보다 낮은 해상도 옵션은 제거한다.
                if (resolution.refreshRateRatio.value < minimumFPS - 1) continue;

                string res = $"{resolution.width}x{resolution.height}";
                string fps = $"{(int)Math.Round(resolution.refreshRateRatio.value)}Hz";

                if (!resolutionList.Contains(res))
                {
                    resolutionList.Add(res);
                }

                if (!framerateList.Contains(fps))
                {
                    framerateList.Add(fps);
                }
            }

            // DPI 리스트
            for (int i = 0; i < supportDPI.Length; i++)
            {
                DPIList.Add($"{(int)(supportDPI[i] * 100)}%");
            }

            // 각 드롭다운 초기화
                dropdownWindowMode.ClearOptions();
            dropdownResolution.ClearOptions();
            dropdownFramerate.ClearOptions();
            dropdownDPI.ClearOptions();

            // 각 드롭다운에 옵션 등록
            dropdownWindowMode.AddOptions(windowModeList);
            dropdownResolution.AddOptions(resolutionList);
            dropdownFramerate.AddOptions(framerateList);
            dropdownDPI.AddOptions(DPIList);

            // 각 드롭다운에 이벤트 리스너 등록
            dropdownWindowMode.onValueChanged.AddListener(OnWindowModeChanged);
            dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);
            dropdownFramerate.onValueChanged.AddListener(OnFramerateChanged);
            dropdownDPI.onValueChanged.AddListener(OnDPIChanged);
        }

        private void InitalizeButton()
        {
            buttonApply.onClick.AddListener(ApplySetting);
            buttonExit.onClick.AddListener(Close);
        }

        private void DetectCurrentSettings()
        {
            var currentResolution = Screen.currentResolution;

            targetWindowMode = Screen.fullScreenMode;
            targetWidth = currentResolution.width;
            targetHeight = currentResolution.height;
            targetFrameRate = currentResolution.refreshRateRatio;
            targetDPI = UGUIWindowManager.CurrentDPI;

            // 현재 윈도우 모드를 찾아서 선택
            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    dropdownWindowMode.SetValueWithoutNotify(0);
                    break;
                case FullScreenMode.FullScreenWindow:
                    dropdownWindowMode.SetValueWithoutNotify(1);
                    break;
                case FullScreenMode.Windowed:
                    dropdownWindowMode.SetValueWithoutNotify(2);
                    break;
                default:
                    UGUIWindowLog.LogWarning($"Window Mode {Screen.fullScreenMode} is undefined!");
                    break;
            }

            // 현재 해상도를 찾아서 선택
            for (int i = 0; i < dropdownResolution.options.Count; i++)
            {
                if (dropdownResolution.options[i].text.Equals($"{targetWidth}x{targetHeight}"))
                {
                    dropdownResolution.SetValueWithoutNotify(i);
                    break;
                }
            }

            // 현재 프레임레이트를 찾아서 선택
            for (int i = 0; i < dropdownFramerate.options.Count; i++)
            {
                int framerate = int.Parse(dropdownFramerate.options[i].text.Replace("Hz", ""));
                if ((int)Math.Round(currentResolution.refreshRateRatio.value) == framerate)
                {
                    dropdownFramerate.SetValueWithoutNotify(i);
                    break;
                }
            }

            // 현재 DPI 값을 찾아서 선택
            for (int i = 0; i < supportDPI.Length; i++)
            {
                if (targetDPI.Equals(supportDPI[i]))
                {
                    dropdownDPI.SetValueWithoutNotify(i);
                }
            }
        }
        #endregion

        #region Event Listener
        private void OnWindowModeChanged(int value)
        {
            switch (value)
            {
                case 0: // FullScreen
                    targetWindowMode = FullScreenMode.ExclusiveFullScreen;
                    break;
                case 1: // Borderless Window
                    targetWindowMode = FullScreenMode.FullScreenWindow;
                    break;
                case 2: // Windowed
                    targetWindowMode = FullScreenMode.Windowed;
                    break;
                default:
                    UGUIWindowLog.LogWarning($"Window Mode {value} is undefined!");
                    break;
            }
        }

        private void OnResolutionChanged(int value)
        {
            string[] splited = dropdownResolution.options[value].text.Split('x');

            targetWidth = int.Parse(splited[0]);
            targetHeight = int.Parse(splited[1]);
        }

        private void OnFramerateChanged(int value)
        {
            List<RefreshRate> framerateList = new List<RefreshRate>();

            // 해상도 리스트
            foreach (var resolution in Screen.resolutions)
            {
                // 최저 해상도 보다 크면서 화면비는 가로가 세로보다 긴 비율의 해상도만 추가한다.
                if (resolution.width >= minimumResolution.x && resolution.height >= minimumResolution.y
                    && resolution.width > resolution.height)
                {
                    if (!framerateList.Contains(resolution.refreshRateRatio))
                    {
                        framerateList.Add(resolution.refreshRateRatio);
                    }
                }
            }

            targetFrameRate = framerateList[value];
        }

        private void OnDPIChanged(int value)
        {
            targetDPI = supportDPI[value];
        }
        #endregion

        #region Setting
        private void ApplySetting()
        {
            UGUIWindowLog.LogError($"Set Screen Resolution to: {targetWidth}x{targetHeight} {targetFrameRate}Hz {targetWindowMode}");

            if (targetWidth < minimumResolution.x || targetHeight < minimumResolution.y)
            {
                UGUIWindowLog.LogError($"{targetWidth}x{targetHeight} is too small!");
                return;
            }

            Screen.SetResolution(targetWidth, targetHeight, targetWindowMode, targetFrameRate);
            UGUIWindowManager.SetDPI(targetWidth, targetHeight, targetDPI);
        }
        #endregion
    }
}
