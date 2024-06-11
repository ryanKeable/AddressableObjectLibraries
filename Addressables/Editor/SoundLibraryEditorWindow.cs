using UnityEditor;

namespace MightyAddressables
{
    public class SoundLibraryEditorWindow : AddressableDataLibraryEditorWindow<SoundLibrary, SoundEvent>
    {
        [MenuItem("Mighty/Open SoundLibrary Window")]
        public static void ShowWindow()
        {
            GetWindow<SoundLibraryEditorWindow>(WindowLabel);
        }

        protected override async void SetLibrary()
        {
            _library = await SoundLibrary.GetInstance();
        }


        protected override void DrawLibraryGUI()
        {
            if (_libraryGUI == null) _libraryGUI = new SoundLibraryEditorGUI();
            _libraryGUI.DrawGUI(_library, _seralizedLib);
        }

    }
}