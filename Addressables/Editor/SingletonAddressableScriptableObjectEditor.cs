using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using System.Threading.Tasks;
using System;
using MightyAddressables;


/*

    Extendable Editor Class for SingletonScriptableObject. Each Extention class of SingletonScriptableObject should also extend this editor
    This will allow it to:
    - generate itself correctly and point to the correct location
    - add itself to addressables automatically with the correct data
    - load itself a boot time for access within the project

*/

[CustomEditor(typeof(SingletonScriptableObject), true)]
public class SingletonScriptableObjectEditor : Editor
{

}

[CustomEditor(typeof(SingletonAddressableScriptableObject<SingletonScriptableObject>), true)]
public class SingletonAddressableScriptableObjectEditor<T> : SingletonScriptableObjectEditor where T : SingletonAddressableScriptableObject<T>
{
    public static T scriptableObjectAsset;
    protected static SingletonAddressableScriptableObjectEditor<T> sharedEditorInstance;
    protected static SingletonAddressableScriptableObject<T> sharedTargetInstance;
    protected SingletonAddressableScriptableObjectEditorGUI<T> assetGUI;
    private SingletonScriptableObject theTarget;
    public static string AssetTypeID { get => SingletonAddressableScriptableObject<T>.TypeID; }
    protected virtual string GroupID { get => AssetTypeID; }

    public static SingletonAddressableScriptableObjectEditor<T> instance
    {
        get
        {
            GetSharedEditorInstance();
            return sharedEditorInstance;
        }
    }

    protected static void Instantiate()
    {
        EditorApplication.delayCall += () =>
        {
            LoadOnInstantiate();
        };
    }

    /// <summary>
    /// Unity doesnt appear to like directly running tasks in a InitializeOnLoadMethod call so this is a hack work around
    /// </summary>

    static async void LoadOnInstantiate()
    {
        bool addressablesInit = await InitilializeAddressables();

        if (addressablesInit == false)
        {
            Debug.LogWarning($"[{nameof(MightyAddressables)}] Addressables has not spun up correctly, abort loading any addressables objects");
            return;
        }

        bool resourceExists = await AddressablesHelper.ResourceHasLocation<T>(SingletonAddressableScriptableObject<T>.TypeID) && AddressablesEditorHelper.FindAddressbleEntriesOfType<T>() != null;

        if (resourceExists == false)
        {
            Debug.LogWarning($"[{nameof(MightyAddressables)}] no object instance of type {typeof(T).Name} exists in addressables to load");
            return;
        }


        await LoadScriptableObjectAsset();
        GetSharedEditorInstance();
        AssignDelegates();
    }

    static async Task<bool> InitilializeAddressables()
    {
        return await AddressablesHelper.InitializeAddressables();
    }

    static async Task LoadScriptableObjectAsset()
    {
        if (scriptableObjectAsset != null)
        {
            if (scriptableObjectAsset.VerboseLogging) Debug.LogWarning($"[{AssetTypeID}] cannot be loaded again");
            return;
        }

        if (scriptableObjectAsset == null) scriptableObjectAsset = await SingletonAddressableScriptableObject<T>.GetInstance();

        if (scriptableObjectAsset != null && scriptableObjectAsset.VerboseLogging) Debug.Log($"[{AssetTypeID}] has been successfully loaded");
    }

    protected static void AssignDelegates()
    {
        if (instance == null) return;
        AddressableAssetSettings.OnModificationGlobal += instance.OnSettingsModificationCustom;
        EditorApplication.playModeStateChanged += instance.PlayModeChanged;
        EditorSceneManager.sceneClosing += instance.SceneClosing;
        EditorSceneManager.sceneSaving += instance.SceneSaving;
    }

    protected static SingletonAddressableScriptableObjectEditor<T> GetSharedEditorInstance()
    {
        if (sharedEditorInstance != null) return sharedEditorInstance;
        if (scriptableObjectAsset == null) return null;

        if (sharedEditorInstance == null)
        {
            Editor thisEditor = CreateEditor(scriptableObjectAsset);

            if (thisEditor == null)
            {
                return null;
            }

            sharedEditorInstance = thisEditor as SingletonAddressableScriptableObjectEditor<T>;

            if (sharedEditorInstance == null)
            {
                return null;
            }
        }

        return sharedEditorInstance;
    }

    protected static void GetSharedTargetInstance()
    {
        if (sharedTargetInstance != null) return;

        if (instance == null)
        {
            return;
        }

        if (sharedEditorInstance.target == null)
        {
            return;
        }

        sharedTargetInstance = sharedEditorInstance.target as SingletonAddressableScriptableObject<T>;
    }


    protected static void SetInstanceOfScriptabelObject(T _scriptableObjectInstance)
    {
        SingletonAddressableScriptableObject<T>.SetInstance(_scriptableObjectInstance);
        if (scriptableObjectAsset == null) scriptableObjectAsset = _scriptableObjectInstance;
    }


    private void OnEnable()
    {
        // handles passing the selected objevt from the inspector if we do not already have an instance
        if (serializedObject == null || serializedObject.targetObject == null) return;
        T selectedInspectorObject = (T)serializedObject.targetObject;
        SetInstanceOfScriptabelObject(selectedInspectorObject);
        theTarget = target as SingletonScriptableObject;
    }

    protected virtual void OnValidate()
    {
    }

    public override void OnInspectorGUI()
    {
        CheckAssetState();
        DrawGUI();
    }

    public static void DrawHelpBox(string message)
    {
        EditorGUILayout.HelpBox($"Instance of {AssetTypeID} is not set up correctly: " + message, MessageType.Error);
        EditorGUILayout.Space();
    }

    public bool CheckAssetState()
    {

        if (!AssetIsInitialized(out string message))
        {
            DrawHelpBox(message);
            return false;
        }

        return true;
    }

    private bool AssetIsInitialized(out string message)
    {
        if (!AssetExistsInProject())
        {
            message = $"{AssetTypeID}.asset cannot be found in {AddressableDataLibrary.DataLibraryPath}";
            return false;
        }

        if (!AssetExistsInAddressables())
        {
            message = $"{AssetTypeID}.asset is not an addressable asset";
            return false;
        }

        if (scriptableObjectAsset == null)
        {
            message = $"{AssetTypeID}.asset is not loaded";
            return false;
        }

        message = "";
        return true;
    }

    private bool AssetExistsInProject()
    {
        bool locatedAtPath = ExistsAtProjectPath(AddressableDataLibrary.DataLibraryPath, AssetTypeID);

        if (!locatedAtPath)
        {
            if (GUILayout.Button($"Create [{AssetTypeID}] Asset"))
            {
                CreateAndSetInstance(AddressableDataLibrary.DataLibraryPath);
            }
            EditorGUILayout.Space();
        }

        return locatedAtPath;
    }

    private bool AssetExistsInAddressables()
    {
        bool isAddressableEntry = ExistsInAddressables();

        if (!isAddressableEntry)
        {
            if (GUILayout.Button($"Add Asset To Addressables"))
            {
                AddToAddressables();
            }
            EditorGUILayout.Space();
        }

        return isAddressableEntry;
    }


    protected virtual void DrawGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            this.MarkDirty();
            OnValidate();
        }
    }

    public static bool ExistsAtProjectPath(string rootPath, string fileName)
    {
        fileName += ".asset";
        if (File.Exists(Path.Combine(rootPath, fileName)))
            return true;

        foreach (string subDir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
        {
            if (File.Exists(Path.Combine(subDir, fileName)))
                return true;
        }

        return false;
    }

    public static bool ExistsInAddressables()
    {
        return AddressablesEditorHelper.FindFirstAddressbleEntryOfType<T>() != null;
    }

    public static void CreateAndSetInstance(string dataPath, string addressableID = "")
    {
        T asset = ScriptableObjectEditorHelper.CreateNewInstance<T>(dataPath);
        string groupID = !string.IsNullOrEmpty(addressableID) ? addressableID : AssetTypeID;

        ScriptableObjectEditorHelper.AddScriptableObjectToAddressables(asset, groupID);

        Selection.activeObject = asset;
    }

    public static void AddToAddressables(ScriptableObject asset, string groupID)
    {
        AddressablesEditorHelper.AddObjectToAddressables(asset, groupID, groupID);
    }

    public void AddToAddressables()
    {
        T obj = scriptableObjectAsset;
        string path = AddressableDataLibrary.DataLibraryPath;
        path = EditorUtility.OpenFilePanel("Open " + AssetTypeID, path, ".asset");

        if (obj == null)
            obj = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
        if (obj == null)
        {
            Debug.LogError($"[{AssetTypeID}] Could not add Library to addressables as the {AssetTypeID} cannot be found at {path}");
            return;
        }

        ScriptableObjectEditorHelper.AddScriptableObjectToAddressables(obj, GroupID);
    }

    private void OnSettingsModificationCustom(AddressableAssetSettings s, AddressableAssetSettings.ModificationEvent e, object o)
    {
        OnSettingsModificationCustom();
    }

    protected virtual void OnSettingsModificationCustom()
    {

    }

    private void PlayModeChanged(PlayModeStateChange state)
    {
        if (scriptableObjectAsset == null) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            OnPlayModeChanged();
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            OnPlayModeChanged();
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // make sure we unload any assets loaded at edit time once we start playing
            theTarget.UnloadInstanceAsset();
            return;
        }
    }

    protected virtual void OnPlayModeChanged()
    {

    }

    private void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        if (scriptableObjectAsset == null) return;
        OnSceneClosing();
    }

    protected virtual void OnSceneClosing()
    {
    }

    private void SceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (scriptableObjectAsset == null) return;
        OnSceneSaving();
    }

    protected virtual void OnSceneSaving()
    {
    }



}