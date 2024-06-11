using UnityEngine;
using UnityEditor;

namespace MightyAddressables
{
    public abstract class AddressableDataLibraryEditorWindow<TLibrary, TObject> : EditorWindow where TLibrary : AddressableDataLibrary<TLibrary, TObject> where TObject : UnityEngine.Object
    {
        protected TLibrary _library;
        protected AddressableDataLibraryEditorGUI<TLibrary, TObject> _libraryGUI;
        protected SerializedObject _seralizedLib;
        protected Vector2 _windowScrollPos;

        protected static string WindowLabel => AddressableDataLibrary<TLibrary, TObject>.TypeID;

        private void OnGUI()
        {
            TryGetLibrary();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(WindowLabel, EditorStyles.whiteLargeLabel);

            EditorGUILayout.Space();

            DrawWindow();

            EditorGUILayout.Space(10f);

        }

        protected abstract void DrawLibraryGUI();
        protected abstract void SetLibrary();

        private void TryGetLibrary()
        {
            if (_library != null && _seralizedLib != null) return;
            if (_library == null) SetLibrary();
            if (_library != null && _seralizedLib == null) _seralizedLib = new SerializedObject(_library);
        }

        private void DrawWindow()
        {
            _windowScrollPos = EditorGUILayout.BeginScrollView(_windowScrollPos);
            DrawLibraryGUI();
            EditorGUILayout.EndScrollView();
            UpdateSerializedLib();
        }

        private void UpdateSerializedLib()
        {
            if (_seralizedLib == null) return;

            _seralizedLib.SetIsDifferentCacheDirty();
            _seralizedLib.UpdateIfRequiredOrScript();
        }
    }
}