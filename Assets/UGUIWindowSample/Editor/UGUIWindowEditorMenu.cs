using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace UGUIWindow
{
    public class UGUIWindowEditorMenu : MonoBehaviour
    {
        [MenuItem("GameObject/UGUI Window/Create Window Templete")]
        public static void CreateWindowTemplete(MenuCommand menuCommand)
        {
            // 커스텀 게임 오브젝트를 생성한다.
            GameObject windowTemplete = new GameObject("UGUIWindowTemplete");
            windowTemplete.AddComponent<UGUIWindow>();

            // 선택한 오브젝트가 있다면 그 오브젝트를 부모 오브젝트로 삼도록 한다.
            GameObjectUtility.SetParentAndAlign(windowTemplete, menuCommand.context as GameObject);

            // 되돌리기 시스템에 생성된 게임 오브젝트를 등록한다.
            Undo.RegisterCreatedObjectUndo(windowTemplete, "Create " + windowTemplete.name);
            Selection.activeObject = windowTemplete;
        }
    }
}
