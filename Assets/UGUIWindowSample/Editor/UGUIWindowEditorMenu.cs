using UnityEditor;
using UnityEngine;

namespace UGUIWindow
{
    public class UGUIWindowEditorMenu
    {
        [MenuItem("GameObject/UGUI Window/Create Window Manager")]
        public static void CreateWindowManager()
        {
            // 씬 내에 다른 윈도우 매니저가 없는지 확인한다.
            if (GameObject.FindAnyObjectByType<UGUIWindowManager>())
            {
                Debug.LogWarning("A UGUI Window Manager instance already exists in the scene.");
                return;
            }

            // 리소스 폴더 내의 윈도우 매니저를 로드하고 생성한다.
            Object managerPrefab = AssetDatabase.LoadAssetAtPath<Object>("Assets/UGUIWindowSample/Resources/UGUIWindowManager.prefab");

            if (managerPrefab == null)
            {
                Debug.LogError("Failed to load prefab at path: Assets/UGUIWindowSample/Resources/UGUIWindowManager.prefab");
                return;
            }

            GameObject managerObject = PrefabUtility.InstantiatePrefab(managerPrefab) as GameObject;

            // 되돌리기 시스템에 생성된 매니저를 등록한다.
            Undo.RegisterCreatedObjectUndo(managerObject, "Create " + managerObject.name);
            Selection.activeObject = managerObject;
        }

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
