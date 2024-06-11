using UnityEngine;
using UnityEditor;

public abstract class SingletonAddressableScriptableObjectEditorGUI<T> where T : SingletonAddressableScriptableObject<T>
{
    protected SingletonAddressableScriptableObject<T> _targetInstance { get; private set; }
    protected T _scriptableObjectAsset { get; private set; }
    protected SerializedObject _seralizedAssetObj;
    private SerializedProperty _verboseLogging;


    public SingletonAddressableScriptableObjectEditorGUI()
    {
    }

    public void DrawGUI(T assetObj, SerializedObject serializedObject)
    {
        if (_scriptableObjectAsset == null) _scriptableObjectAsset = assetObj;
        if (_seralizedAssetObj == null) _seralizedAssetObj = serializedObject;
        if (!ValidateGUI()) return;
        AssignSerializedProps(serializedObject);

        EditorGUILayout.Space();

        DrawAssetGUI();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        _seralizedAssetObj.ApplyModifiedProperties();
    }

    private bool ValidateGUI()
    {
        if (!AssetExistsInProject(out string message))
        {
            SingletonAddressableScriptableObjectEditor<T>.DrawHelpBox(message);
            return false;
        }

        if (!SingletonAddressableScriptableObjectEditor<T>.ExistsInAddressables())
        {
            SingletonAddressableScriptableObjectEditor<T>.DrawHelpBox($"[{AssetName}] is not an addressable asset. Please add it to addressables");
            return false;
        }
        if (SingletonAddressableScriptableObjectEditor<T>.instance == null)
        {
            SingletonAddressableScriptableObjectEditor<T>.DrawHelpBox($"[{AssetName}] editor instance has not been created");
            return false;
        }
        if (!SingletonAddressableScriptableObjectEditor<T>.instance.CheckAssetState()) return false;
        return true;
    }

    protected abstract bool AssetExistsInProject(out string message);

    protected virtual void DrawAssetGUI()
    {
        DrawDebugOptions();
    }

    protected virtual void AssignSerializedProps(SerializedObject serializedObject)
    {
        _verboseLogging = serializedObject.FindProperty("_verboseLogging");
    }


    protected virtual void DrawDebugOptions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Options:", EditorStyles.boldLabel);
        ToggleVerboseLogging();
    }

    private void ToggleVerboseLogging()
    {
        EditorGUILayout.PropertyField(_verboseLogging, new GUIContent("Toggle Verbose Logging:"), false);
    }


    protected void DrawSpacer(string label = "")
    {
        if (!string.IsNullOrEmpty(label))
        {
            label = " " + label + " ";
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("================" + label + "================");
        EditorGUILayout.Space();
    }

    protected virtual void MarkDirty()
    {
        EditorUtility.SetDirty(_scriptableObjectAsset);
        _seralizedAssetObj.ApplyModifiedProperties();
        _seralizedAssetObj.Update();

        RefreshObjectGUI();
    }

    private void RefreshObjectGUI()
    {
        Selection.activeObject = null;
        EditorApplication.delayCall += ReselectObject;
    }

    private void ReselectObject()
    {
        EditorApplication.delayCall += () => Selection.activeObject = _scriptableObjectAsset;
    }

    protected SingletonAddressableScriptableObjectEditor<T> AssetEditorInstance { get => SingletonAddressableScriptableObjectEditor<T>.instance; }
    protected SingletonAddressableScriptableObject<T> AssetInstance { get => SingletonAddressableScriptableObjectEditor<T>.scriptableObjectAsset; }
    protected string AssetName { get => SingletonAddressableScriptableObject<T>.TypeID; }

}
