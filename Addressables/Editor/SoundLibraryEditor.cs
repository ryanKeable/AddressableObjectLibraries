using UnityEditor;

namespace MightyAddressables
{
    [CustomEditor(typeof(SoundLibrary))]
    public sealed class SoundLibraryEditor : AddressableDataLibraryEditor<SoundLibrary, SoundEvent>
    {
        [MenuItem("Assets/Data Libraries/Create new SoundLibrary", false, 81), MenuItem("Mighty/Tools/Data Libraries/Create new SoundLibrary", false, 82)]
        private static void CreateSoundLibrary()
        {
            if (!FileEditing.CheckForDirectory(AddressableDataLibrary.DataLibraryPath + "/" + AssetTypeID)) return;
            CreateAndSetInstance(AddressableDataLibrary.DataLibraryPath, AssetTypeID);
        }

        [InitializeOnLoadMethod]
        public static void InitInstanceOnEditorLoad()
        {
            Instantiate();
        }


        protected override void DrawGUI()
        {
            if (assetGUI == null) assetGUI = new SoundLibraryEditorGUI();
            assetGUI.DrawGUI(scriptableObjectAsset, serializedObject);
        }


    }
}