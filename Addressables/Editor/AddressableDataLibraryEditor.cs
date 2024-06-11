using UnityEditor;
using UnityEngine;
using System.Linq;

namespace MightyAddressables
{
    [CustomEditor(typeof(AddressableDataLibrary<,>))]
    public abstract class AddressableDataLibraryEditor<TLibrary, TObject> : SingletonAddressableScriptableObjectEditor<TLibrary> where TLibrary : AddressableDataLibrary<TLibrary, TObject> where TObject : UnityEngine.Object
    {
        AddressableDataLibrary<TLibrary, TObject> Target => target as AddressableDataLibrary<TLibrary, TObject>;
        public static string LibraryName => AddressableDataLibrary<TLibrary, TObject>.TypeID;
        public static string LibraryEntry => typeof(TObject).Name;
        public string EntryAssetPath { get => serializedObject.FindProperty("_defualtAssetPath").stringValue; }

        new public static AddressableDataLibraryEditor<TLibrary, TObject> instance
        {
            get
            {
                GetSharedEditorInstance();
                return sharedEditorInstance as AddressableDataLibraryEditor<TLibrary, TObject>;
            }
        }

        public bool AddEntryToLibrary(TObject assetToAdd)
        {
            string assetPath = AssetDatabase.GetAssetPath(assetToAdd);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"[{LibraryName}] Cannot find assetPath for {assetToAdd} - cannot create an entry");
                return false;
            }

            string guid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            MightyAssetRef<TObject> entry = new(guid);

            if (entry.HasAsset == false)
                return false;

            entry.AddObjectToAddressables(LibraryName);
            scriptableObjectAsset.EntryRegistry.Add(entry);
            if (Target.GenerateKeys) EntryKeysCreator<TLibrary, TObject>.AddEntryKey(assetToAdd.name);

            Debug.Log($"[{LibraryName}] {assetToAdd} added to Library. Total Registered: {scriptableObjectAsset.EntryRegistry.Count}");
            return true;
        }
    }
}
