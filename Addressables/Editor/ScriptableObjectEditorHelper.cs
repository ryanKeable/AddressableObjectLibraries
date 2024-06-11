using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScriptableObjectEditorHelper
{
	public static T CreateNewInstance<T>(string directory) where T : ScriptableObject
	{
		bool dirExists = FileEditing.CheckForDirectory(directory);
		if (!dirExists) return null;

		string assetName = typeof(T).Name;

		// string path = Path.Combine(assetName, directory);
		string path = EditorUtility.SaveFilePanelInProject("Save " + assetName, assetName + ".asset", "asset", $"Enter a file name for the {assetName}.", directory);

		if (string.IsNullOrEmpty(path))
		{
			Debug.LogWarning($"[{typeof(ScriptableObjectEditorHelper).Name}] Canceled asset creation for {assetName} in {directory}");
			return null;
		}

		T newObjInstance = ScriptableObject.CreateInstance<T>();

		AssetDatabase.CreateAsset(newObjInstance, path);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

		T asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;

		EditorGUIUtility.PingObject(asset);

		return asset;
	}

	public static void AddScriptableObjectToAddressables<T>(T asset, string addressableGroup) where T : ScriptableObject
	{
		AddressablesEditorHelper.AddObjectToAddressables(asset, addressableGroup, addressableGroup);
	}


}