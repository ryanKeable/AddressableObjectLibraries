using System;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;


// NOTES:

// this whole system is too abstract from bottom to top, cant find anything i need or get a clear idea of the structure at any point
namespace MightyAddressables
{
    public abstract class AddressableEntryWizard<TLibrary, TObject> : ObjectEditingWizard<TObject> where TLibrary : AddressableDataLibrary<TLibrary, TObject> where TObject : UnityEngine.Object
    {
        protected TObject _createdAsset;
        protected bool _entryExists;
        protected TObject CreatedAsset { get { return _createdAsset; } set { _createdAsset = value; } }
        protected async override void Instantiate()
        {
            if (!LibaryExists)
            {
                await AddressableDataLibrary<TLibrary, TObject>.GetInstance();
            }
        }

        protected override void SetLabelsAndContext()
        {
            _windowSummary = $"Create a new {EntryType}";
            _valdationMessage = $"{EntryType} Entry is valid";
            confirmationButtonLabel = $"Create a {EntryType}";


            if (String.IsNullOrEmpty(_newAssetName)) // only assign the entryType as name once - some wizards cause a re-compile on script gen
                _newAssetName = EntryType;

            _targetAssetPath = AddressableDataLibraryEditor<TLibrary, TObject>.instance.EntryAssetPath;

        }

        protected override void DrawBodyGUI()
        {
            DrawNameField(ref _newAssetName);
        }

        protected override void TargetObjectIsSelected()
        {
            base.TargetObjectIsSelected();
            _newAssetName = _newAssetName + "Copy";

            if (AssetExists())
            {
                string assetPath = AssetDatabase.GetAssetPath(TargetObject);
                base._targetAssetPath = FileEditing.GetAssetFolderPath(assetPath);
            }
        }


        private bool AssetExists()
        {
            string assetPath = AssetDatabase.GetAssetPath(TargetObject);
            return !string.IsNullOrEmpty(assetPath);
        }

        protected override void DrawAdditionalGUIContent()
        {
            DrawOpenFolderPanel("Asset Folder", ref _targetAssetPath);
            EditorGUILayout.Space(Spacer);

            DrawCreateAsset();
            EditorGUILayout.Space(Spacer);
        }

        protected virtual void DrawCreateAsset()
        {
            EditorGUI.BeginChangeCheck();

            DrawTargetObjectField();

            if (EditorGUI.EndChangeCheck())
            {
                EndObjectChangeCheck();
            }
        }

        protected override void ValidateWindowContent()
        {
            if (!ValidAssetName()) return;
            if (!ValidAssetPath()) return;

            base.ValidateWindowContent();
        }

        protected override bool CheckForTarget()
        {
            return true;
        }

        protected override bool CheckForTargetOfType()
        {
            return true;
        }

        private bool ValidAssetPath()
        {
            bool hasAssetPath = string.IsNullOrEmpty(_targetAssetPath) == false;
            ValidateContent(hasAssetPath, $"No path for the asset is set -  please select a folder location");

            return hasAssetPath;
        }

        private bool ValidAssetName()
        {
            bool validName;

            validName = _newAssetName != null;
            if (validName == false)
            {
                ValidateContent(false, $"{ObjectType} name is null");
                return false;
            }

            validName = _newAssetName != EntryType;
            if (validName == false)
            {
                ValidateContent(validName, $"Asset name cannot be the same as it's type, {EntryType}");
                return false;
            }

            _newAssetName = _newAssetName.Trim();
            _newAssetName = _newAssetName.Replace(" ", "");

            if (!string.IsNullOrEmpty(_newAssetName))
            {
                char[] chars = _newAssetName.ToCharArray();
                chars[0] = char.ToUpper(_newAssetName[0]);
                _newAssetName = new String(chars);
            }

            EditorApplication.QueuePlayerLoopUpdate();

            bool defualtID = _newAssetName.ToLower() == ObjectType.ToLower();
            bool emptyID = string.IsNullOrEmpty(_newAssetName);

            string nameToMatch = TargetObject != null ? TargetObject.name : "";
            bool matchesTargetObject = _newAssetName == nameToMatch;
            validName = !defualtID && !emptyID && !matchesTargetObject;

            if (!validName) _newAssetName = _newAssetName = "";
            ValidateContent(validName, $"{ObjectType} name is not valid, it may already exist as an asset");

            return validName;
        }

        protected override void ConfirmationAction(Action completion = null)
        {
            if (CreateAsset() == false) return;
            CreateEntry(completion);
        }

        protected virtual bool CreateAsset()
        {
            bool success = false;
            string assetPath = EditorUtility.SaveFilePanelInProject("Save " + _newAssetName, _newAssetName + "." + AssetSuffix, AssetSuffix, $"Enter a file name for the {EntryType}", _targetAssetPath);
            if (assetPath == "")
            {
                success = false;
                AddressableDataLibrary.LogError($"Failed to create new prefab for {_newAssetName} - path was invalid");
                return false;
            }

            if (TargetObject != null)
            {
                success = CopyAsset(assetPath);
            }
            else
            {
                CreateNewAsset();
                DefineAssetProperties();
                success = SaveCreatedAsset(assetPath);
            }

            if (!success)
            {
                AddressableDataLibrary.LogError($"Failed to create new prefab for {_newAssetName}");
            }

            return success;

        }
        protected virtual void CreateNewAsset()
        {
        }

        protected virtual string AssetSuffix => "asset";

        protected virtual void DefineAssetProperties()
        {
        }

        protected virtual bool CopyAsset(string assetPath)
        {
            string originalPath = AssetDatabase.GetAssetPath(TargetObject);

            AssetDatabase.CopyAsset(originalPath, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TargetObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TObject)) as TObject;
            return TargetObject != null;
        }

        protected virtual bool SaveCreatedAsset(string assetPath)
        {
            AssetDatabase.CreateAsset(CreatedAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TargetObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TObject)) as TObject;
            return TargetObject != null;
        }

        protected virtual void CreateEntry(Action completion)
        {
            CreateEntry<TObject>();
            if (completion != null) completion.Invoke();
        }

        protected T CreateEntry<T>() where T : TObject
        {
            LibraryEditorInstance.AddEntryToLibrary(TargetObject);
            return TargetObject as T;
        }

        protected abstract AddressableDataLibraryEditor<TLibrary, TObject> LibraryEditorInstance { get; }
        protected abstract AddressableDataLibrary<TLibrary, TObject> LibraryInstance { get; }
        protected override string NameTextFieldLabel { get => $"{EntryType} Name "; }
        protected static string EntryType { get => typeof(TObject).Name; }
        protected static string LibaryName { get => AddressableDataLibrary<TLibrary, TObject>.TypeID; }
        protected bool LibaryExists { get => LibraryInstance != null; }

    }
}