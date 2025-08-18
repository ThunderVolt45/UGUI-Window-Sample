using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    public class UGUIMenu : UGUIWindow
    {
        [Header("UI Button")]
        [SerializeField] private Button buttonSetting;
        [SerializeField] private Button buttonQuit;

        protected override void Start()
        {
            base.Start();
            
            InitailizeButton();
        }

        private void InitailizeButton()
        {
            buttonSetting.onClick.AddListener(OpenSetting);
            buttonQuit.onClick.AddListener(CloseProgram);
        }

        private void OpenSetting()
        {
            UGUIWindowManager.CreateWindow<UGUIApplicationSetting>();
            Close();
        }

        private void CloseProgram()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
            Application.OpenURL("about:blank");
#else
            Application.Quit();
#endif
        }
    }
}
