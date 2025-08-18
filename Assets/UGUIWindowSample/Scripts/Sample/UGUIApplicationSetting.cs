using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    public class UGUIApplicationSetting : UGUIWindow
    {
        [Header("Setting Dropdown")]
        [SerializeField] private TMP_Dropdown dropdownWindowMode;
        [SerializeField] private TMP_Dropdown dropdownResolution;
        [SerializeField] private TMP_Dropdown dropdownFramerate;
        [SerializeField] private TMP_Dropdown dropdownDPI;

        [Header("UI Button")]
        [SerializeField] private Button buttonExit;

        protected override void Start()
        {
            base.Start();

            InitializeDropdown();
            InitalizeButton();
        }

        private void InitializeDropdown()
        {

        }

        private void InitalizeButton()
        {
            buttonExit.onClick.AddListener(Close);
        }
    }
}
