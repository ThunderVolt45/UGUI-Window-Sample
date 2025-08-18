using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEditor;

public class UGUIWindowHelper
{
    // How to save a Prefab modified by an Editor script?
    // https://discussions.unity.com/t/how-to-save-a-prefab-modified-by-an-editor-script/218844
    public static void SetSerializedArray<T>(SerializedObject serializedObject, Expression<Func<T>> memberAccess, Array newArray)
    {
        string fieldName = ((MemberExpression)memberAccess.Body).Member.Name;
        SerializedProperty property = serializedObject.FindProperty(fieldName);

        property.arraySize = newArray.Length;

        for (int i = 0; i < newArray.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);

            switch (element.propertyType)
            {
                case SerializedPropertyType.Integer:
                    element.intValue = (int)newArray.GetValue(i);
                    break;
                case SerializedPropertyType.Boolean:
                    element.boolValue = (bool)newArray.GetValue(i);
                    break;
                case SerializedPropertyType.Float:
                    element.floatValue = (float)newArray.GetValue(i);
                    break;
                case SerializedPropertyType.String:
                    element.stringValue = (string)newArray.GetValue(i);
                    break;
                case SerializedPropertyType.Color:
                    element.colorValue = (Color)newArray.GetValue(i);
                    break;
                case SerializedPropertyType.ObjectReference:
                    element.objectReferenceValue = (UnityEngine.Object)newArray.GetValue(i);
                    break;
                default:
                    Debug.LogError("SetSerializedArray unhandled array type " + element.propertyType);
                    break;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
