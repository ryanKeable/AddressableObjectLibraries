using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace MightyAddressables
{

    public abstract class AddressableDataLibraryEditorGUI<TLibrary, TObject> : SingletonAddressableScriptableObjectEditorGUI<TLibrary> where TLibrary : AddressableDataLibrary<TLibrary, TObject> where TObject : UnityEngine.Object
    {
        protected class EntryDataProperties<T> where T : UnityEngine.Object
        {
            public int Index { get; private set; }
            public string KeyLowered { get => _assetName.ToLowerInvariant(); }
            private string _assetName;
            private SerializedProperty _assetRef;
            private SerializedProperty _asset;
            private bool _foldoutState;

            public EntryDataProperties(SerializedProperty listElement, int index, AddressableDataLibrary<TLibrary, TObject> LibraryInstance)
            {
                Index = index;
                _foldoutState = false;

                _assetRef = listElement;
                _asset = listElement.FindPropertyRelative("_asset");

                MightyAssetRef<TObject> entry = LibraryInstance.EntryRegistry[index];
                _assetName = entry.AssetName;
            }

            public bool DrawEntryObject()
            {

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent content = new GUIContent("");
                    _foldoutState = EditorGUILayout.Foldout(_foldoutState, content, true);
                    if (_assetRef != null) EditorUtilities.DrawPropertyField(_assetRef, "", false, null, GUILayout.MaxWidth(1024), GUILayout.MinWidth(64));
                }

                if (_foldoutState)
                {
                    if (_asset != null && _asset.objectReferenceValue != null && _asset.objectReferenceValue.GetType().BaseType == typeof(ScriptableObject))
                        EditorUtilities.DrawPropertyField(_asset, "", false, null, GUILayout.MaxWidth(1024), GUILayout.MinWidth(64));
                }

                return _foldoutState;
            }

        }

        protected SerializedObject _library;
        protected SerializedProperty _assetPath;
        private SerializedProperty _registry;
        private SerializedProperty _generateKeys;
        protected UnityEngine.Object _selectedObject;
        private List<EntryDataProperties<TObject>> _entryProps = new();
        private List<string> _entryKeys = new();
        private Vector2 _scrollPos;
        private int _registryCount;
        private bool _checkForKeys = true;
        private static string entryFilter;
        protected AddressableDataLibraryEditor<TLibrary, TObject> LibraryEditorInstance => AddressableDataLibraryEditor<TLibrary, TObject>.instance;
        protected AddressableDataLibrary<TLibrary, TObject> LibraryInstance => AddressableDataLibrary<TLibrary, TObject>.instance;
        protected string WizardLabel { get { return $"Create a new {EntryType}"; } }
        protected string LibraryName => AddressableDataLibrary<TLibrary, TObject>.TypeID;
        protected string EntryType => typeof(TObject).Name;
        protected virtual bool HasWizard => true;

        protected virtual void OpenNewEntryWizard()
        {
        }

        protected override void AssignSerializedProps(SerializedObject serializedObject)
        {
            _library = serializedObject;
            _library.Update();
            _registry = _library.FindProperty("_registry");
            _generateKeys = _library.FindProperty("_generateKeys");

            base.AssignSerializedProps(_library);
        }

        protected override void DrawAssetGUI()
        {
            if (AddressableDataLibrary<TLibrary, TObject>.instance == null) return;

            CreateNewEntryObject();
            SortEntries();
            DrawUpdateKeys();
            DrawAdditionalGUIOptions();
            AddExistingEntry();
            DrawFilter();
            DrawDebugOptions();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawLibraryEntries();
        }


        protected override bool AssetExistsInProject(out string message)
        {
            message = "";

            bool libraryExists = AddressableDataLibraryEditor<TLibrary, TObject>.ExistsAtProjectPath(AddressableDataLibrary.DataLibraryPath, AssetName);

            if (!libraryExists)
            {
                message = $"{AssetName}.asset does not exist in project";

                if (GUILayout.Button($"Create [{AssetName}] Asset"))
                {
                    AddressableDataLibraryEditor<TLibrary, TObject>.CreateAndSetInstance(AddressableDataLibrary.DataLibraryPath);
                }
                EditorGUILayout.Space();
            }

            return libraryExists;
        }

        private void CreateNewEntryObject()
        {
            if (!HasWizard) return;

            if (GUILayout.Button($"Create New {EntryType} Object"))
            {
                OpenNewEntryWizard();
            }
        }

        private void SortEntries()
        {
            if (GUILayout.Button($"Sort Entries Alphabetically"))
            {
                _scriptableObjectAsset.SortRegistryList();
                MarkDirty();
            }
        }

        protected virtual void AddExistingEntry()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _selectedObject = (TObject)EditorGUILayout.ObjectField($"Add {EntryType}", _selectedObject, typeof(TObject), false);

            if (EditorGUI.EndChangeCheck())
            {
                if (_selectedObject == null) return;

                TObject entryToAdd = _selectedObject as TObject;
                if (entryToAdd == null)
                {
                    Debug.Log($"[{LibraryName}] WARNING - Must assign an object with Component of type {EntryType}");
                    _selectedObject = null;

                    return;
                }

                AddExistingEntryToLibrary(entryToAdd);
                _selectedObject = null;

                MarkDirty();
            }
        }

        private void DrawFilter()
        {
            EditorGUILayout.Space();

            using (var h = new EditorGUILayout.HorizontalScope())
            {
                entryFilter = EditorGUILayout.TextField("Entry Filter:", entryFilter);
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(entryFilter));
                if (GUILayout.Button($"Clear Filter"))
                {
                    entryFilter = string.Empty;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        protected override void DrawDebugOptions()
        {
            base.DrawDebugOptions();
            ToggleAutoUpdateKeys();
        }

        private void ToggleAutoUpdateKeys()
        {
            _generateKeys.boolValue = EditorGUILayout.Toggle("Toggle Auto Generate Keys:", _generateKeys.boolValue);
        }

        private void DrawUpdateKeys()
        {
            bool updateKeysFile = false;
            if (_checkForKeys)
            {
                string[] entryNames = AddressableDataLibrary<TLibrary, TObject>.instance.AllEntryNames;

                updateKeysFile = EntryKeysCreator<TLibrary, TObject>.CheckForUnusedOrMissingKeysOrSummaries(out bool scriptExists, entryNames);

                // stops the dialog being brought up over and over again when we cancel
                if (!scriptExists) _checkForKeys = false;
                else
                {
                    _entryKeys = EntryKeysCreator<TLibrary, TObject>.GetEntryKeys();
                    _checkForKeys = true;
                }
            }

            EditorGUI.BeginDisabledGroup(_checkForKeys && !updateKeysFile && _registryCount > 0);
            if (GUILayout.Button($"Update Entry Keys"))
            {
                EntryKeysCreator<TLibrary, TObject>.UpdateEntryKeys(AddressableDataLibrary<TLibrary, TObject>.instance.AllEntryNames);
            }
            EditorGUI.EndDisabledGroup();

            if (updateKeysFile)
            {
                string keyWarning = $"There are unused keys, missing keys or incorrect summaries for {typeof(TLibrary).Name}Keys.cs";
                EditorGUILayout.HelpBox($"{keyWarning}", MessageType.Warning);
            }
        }

        protected virtual void DrawAdditionalGUIOptions()
        {
            DrawDefaultFolderLocation();
            EditorGUILayout.Space();
        }

        protected virtual void DrawDefaultFolderLocation()
        {

            if (_assetPath == null) _assetPath = _seralizedAssetObj.FindProperty("_defualtAssetPath");
            string newAssetPath = DrawOpenFolderPanel(_assetPath.stringValue, $"Default Asset Location:");
            if (string.IsNullOrEmpty(newAssetPath)) return;
            if (newAssetPath != _assetPath.stringValue)
            {
                _assetPath.stringValue = newAssetPath;
                MarkDirty();
            }
        }

        protected string DrawOpenFolderPanel(string path, string label)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label}");
            if (GUILayout.Button($"{path}"))
            {
                path = EditorUtility.OpenFolderPanel(label, path, "");
            }

            EditorGUILayout.EndHorizontal();

            path = FileEditing.RemoveDataPath(path);

            return path;
        }

        private void DrawLibraryEntries()
        {
            if (_registry == null || _registry.arraySize == 0)
            {
                EditorGUILayout.HelpBox($"Can't display {AssetName}, registry is empty", MessageType.Error);
                return;
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos))
            {
                _scrollPos = scrollView.scrollPosition;

                if (_registryCount != _registry.arraySize)
                {
                    _entryProps.Clear();
                    _registryCount = _registry.arraySize;
                }

                EditorGUILayout.LabelField("Library Entries:", EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);

                EditorGUI.indentLevel++;

                for (int i = 0; i < _registryCount; i++)
                {
                    AddEntryAtIndex(i);
                }

                for (int i = 0; i < _entryProps.Count; i++)
                {
                    DrawEntryContext(_entryProps[i]);
                }

                EditorGUI.indentLevel--;
            }
        }

        private void AddEntryAtIndex(int index)
        {
            SerializedProperty listElement = _registry.GetArrayElementAtIndex(index);
            if (listElement == null) return;

            if (_entryProps.Count <= index)
            {
                _entryProps.Add(new EntryDataProperties<TObject>(listElement, index, LibraryInstance));
            }
        }

        private void DrawEntryContext(EntryDataProperties<TObject> props)
        {
            if (string.IsNullOrEmpty(entryFilter) || props.KeyLowered.Contains(entryFilter.ToLower()))
            {
                if (props.DrawEntryObject())
                {
                    DrawExpandedEntryContext(props);
                }
                EditorGUILayout.Space();
            }
        }

        private void DrawExpandedEntryContext(EntryDataProperties<TObject> props)
        {
            DrawExpandedEntryProps(props);
            DrawExpandedEntryWarnings(props);
            EditorGUILayout.Space(2f);
            DrawExpandedEntryButtons(props);
        }

        protected virtual void DrawExpandedEntryProps(EntryDataProperties<TObject> props)
        {
            string key = _entryKeys[props.Index];
            EditorGUILayout.LabelField($"{typeof(TObject).Name} Key: {key}");
        }

        /// <summary>
        /// Draws expanded entry warnings - override this function if you want to add additional warnings when expanded
        /// </summary>
        private void DrawExpandedEntryWarnings(EntryDataProperties<TObject> props)
        {
            string key = _entryKeys[props.Index];

            if (key.ToLower() != props.KeyLowered)
                EditorGUILayout.HelpBox($"Key does not match asset name. Update keys", MessageType.Warning);

        }

        /// <summary>
        /// Draws expanded entry buttons  - override this function if you want to add additional buttons when expanded
        /// </summary>
        private void DrawExpandedEntryButtons(EntryDataProperties<TObject> props)
        {
            DrawRemoveEntryButton(props);
        }

        protected bool AddExistingEntryToLibrary(TObject entryToAdd)
        {
            if (EditorUtility.DisplayDialog($"Add to Registry", $"Add {entryToAdd.name} to Registry?", "OK", "Canel"))
            {
                if (AssetEditorInstance != null)
                {
                    bool addedEntry = LibraryEditorInstance.AddEntryToLibrary(entryToAdd);

                    if (addedEntry == true)
                    {
                        MarkDirty();
                    }

                    return addedEntry;
                }
                else
                {
                    Debug.LogError($"{LibraryEditorInstance.name} cannot be found, cannot create new {EntryType} entry");
                }
            }

            return false;
        }

        private void DrawRemoveEntryButton(EntryDataProperties<TObject> props)
        {
            if (GUILayout.Button("Remove Entry"))
            {
                int index = props.Index;
                RemoveEntryAt(index);
            }
        }

        private void RemoveEntryAt(int index)
        {
            if (_entryProps.Count > 1)
                _entryProps.RemoveAt(index);
            else
                _entryProps.Clear();

            _registry.DeleteArrayElementAtIndex(index);

            MarkDirty();
        }

        protected override void MarkDirty()
        {
            _registry.serializedObject.ApplyModifiedProperties();
            _registry.serializedObject.Update();

            base.MarkDirty();
        }

    }
}