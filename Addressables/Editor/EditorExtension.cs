using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EditorExtension
{

    public static void MarkDirty(this Editor editor)
    {
        editor.target.MarkDirty(editor.serializedObject);
    }

    public static void MarkDirty(this Object target, SerializedObject serializedObject = null)
    {
        if (serializedObject != null)
        {
            serializedObject.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(target);

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }
    }

    public static void ClearEditorSpacer(this Editor editor)
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    public static SerializedProperty DrawPropertyField(this Editor editor, string path)
    {
        SerializedProperty serializedProperty = editor.serializedObject.FindProperty(path);
        EditorGUILayout.PropertyField(serializedProperty);
        return serializedProperty;
    }

    public static void ExpandProperty(this Editor editor, string path, bool expandChildren = true)
    {
        SerializedProperty property = editor.serializedObject.FindProperty(path);
        if (property != null)
        {
            property.isExpanded = true;

            if (property.isArray && expandChildren)
            {
                int count = property.arraySize;
                for (int i = 0; i < count; i++)
                {
                    SerializedProperty listProperty = property.GetArrayElementAtIndex(i);
                    if (listProperty != null) listProperty.isExpanded = true;
                }
            }
        }
        else
        {
            Debug.LogError($"[EditorExtension] cannot find property at path '{path}'");
        }
    }

    public static void ScriptField(this Editor editor)
    {
        EditorGUI.BeginDisabledGroup(true);

        // It can be a MonoBehaviour or a ScriptableObject
        var monoScript = (editor.target as MonoBehaviour) != null
            ? MonoScript.FromMonoBehaviour((MonoBehaviour)editor.target)
            : MonoScript.FromScriptableObject((ScriptableObject)editor.target);

        EditorGUILayout.ObjectField("Script", monoScript, editor.GetType(), false);

        EditorGUI.EndDisabledGroup();
    }
}

