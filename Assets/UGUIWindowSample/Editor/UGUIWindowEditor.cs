using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UGUIWindow
{
    [CustomEditor(typeof(UGUIWindow), true)]
    public class UGUIWindowEditor : Editor
    {
        private List<UGUIWindowBorder> _tempBorderList;
        private List<UGUIWindowEdge> _tempEdgeList;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10f);
            UGUIWindow window = (UGUIWindow)target;

            // 오브젝트 내의 윈도우 컴포넌트를 찾는 버튼
            if (GUILayout.Button("Auto find Base Components"))
            {
                AutoFindHeader();
                AutoFindBorder();
                AutoFindEdge();
            }

            // 오브젝트 내에 윈도우 컴포넌트를 생성하고 자동으로 할당하는 버튼
            if (GUILayout.Button("Create & Assignment Base Components"))
            {
                CreateBaseComponents();

                AutoFindHeader();
                AutoFindBorder();
                AutoFindEdge();
            }
        }

        /// <summary>
        /// UGUIWindow 오브젝트 내의 Header를 찾아 자동으로 inspector에 할당하는 함수
        /// </summary>
        public void AutoFindHeader()
        {
            UGUIWindow window = (UGUIWindow)target;
            serializedObject.FindProperty("windowHeader").objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

            FindHeaderInTransformRecursion(window.transform);
        }

        private void FindHeaderInTransformRecursion(Transform transform)
        {
            if (transform.TryGetComponent(out UGUIWindowHeader header))
            {
                UGUIWindow window = (UGUIWindow)target;
                serializedObject.FindProperty("windowHeader").objectReferenceValue = header;
                serializedObject.ApplyModifiedProperties();

                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                FindHeaderInTransformRecursion(child);
            }
        }

        /// <summary>
        /// UGUIWindow 오브젝트 내의 Border를 찾아 자동으로 inspector에 할당하는 함수
        /// </summary>
        public void AutoFindBorder()
        {
            UGUIWindow window = (UGUIWindow)target;
            _tempBorderList = new List<UGUIWindowBorder>();

            FindBorderInTransformRecursion(window.transform);
            UGUIWindowHelper.SetSerializedArray(serializedObject, () => window.windowBorders, _tempBorderList.ToArray());
        }

        // 주어진 트랜스폼 내에 Border 컴포넌트가 있는 지 재귀적으로 확인하고 리스트에 추가하는 함수
        private void FindBorderInTransformRecursion(Transform transform)
        {
            if (transform.TryGetComponent(out UGUIWindowBorder border))
            {
                _tempBorderList.Add(border);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                FindBorderInTransformRecursion(child);
            }
        }

        /// <summary>
        /// UGUIWindow 오브젝트 내의 Edge를 찾아 자동으로 inspector에 할당하는 함수
        /// </summary>
        public void AutoFindEdge()
        {
            UGUIWindow window = (UGUIWindow)target;
            _tempEdgeList = new List<UGUIWindowEdge>();

            FindEdgeInTransformRecursion(window.transform);
            UGUIWindowHelper.SetSerializedArray(serializedObject, () => window.windowEdges, _tempEdgeList.ToArray());
        }

        // 주어진 트랜스폼 내에 Edge 컴포넌트가 있는 지 재귀적으로 확인하고 리스트에 추가하는 함수
        private void FindEdgeInTransformRecursion(Transform transform)
        {
            if (transform.TryGetComponent(out UGUIWindowEdge edge))
            {
                _tempEdgeList.Add(edge);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                FindEdgeInTransformRecursion(child);
            }
        }

        /// <summary>
        /// UGUIWindow 오브젝트 하위에 기본 컴포넌트를 생성하는 함수
        /// </summary>
        public void CreateBaseComponents()
        {
            UGUIWindow window = (UGUIWindow)target;

            Object headerPrefab = AssetDatabase.LoadAssetAtPath<Object>("Assets/UGUIWindowSample/Resources/BaseComponents/Header.prefab");
            Object borderPrefab = AssetDatabase.LoadAssetAtPath<Object>("Assets/UGUIWindowSample/Resources/BaseComponents/Borders.prefab");
            Object edgePrefab = AssetDatabase.LoadAssetAtPath<Object>("Assets/UGUIWindowSample/Resources/BaseComponents/Edges.prefab");

            if (headerPrefab == null || borderPrefab == null || edgePrefab == null)
            {
                Debug.LogError("Failed to load prefab at path: Assets/UGUIWindowSample/Resources/BaseComponents/");
                return;
            }

            PrefabUtility.InstantiatePrefab(headerPrefab, window.transform);
            PrefabUtility.InstantiatePrefab(borderPrefab, window.transform);
            PrefabUtility.InstantiatePrefab(edgePrefab, window.transform);

            // 오브젝트를 "더러운" 상태로 표시해 변경점이 저장되어야 함을 에디터에 알림
            EditorUtility.SetDirty(window);
        }
    }
}
