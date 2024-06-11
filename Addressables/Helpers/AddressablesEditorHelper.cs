using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

using Object = UnityEngine.Object;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;


///<summary>
///  Provides wrapper functions for the way that we use AddressableAssets
///  Most methods are asynchronous and return a Task instead of the Handles used by Addressables
///</summary>
public static class AddressablesEditorHelper
{

    private struct AddressableData
    {
#nullable enable
        public AddressableAssetSettings? settings;
        public AddressableAssetGroup? group;
        public string? name;
        public string? guid;
        public string[]? labels;
#nullable disable
    }

    private static AddressableAssetSettings defaultAddressableSettings;

    public static AsyncOperationHandle<T> LoadAddressableHandleFromEntry<T>(AddressableAssetEntry entry, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (entry == null)
        {
            LogWarningInternal($"Could not load {typeof(T)} asset because the AddressableAssetEntry was null", callerName);
        }

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(entry.address);

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T)} asset with AddressableAssetEntry {entry.MainAsset.ToString()}", callerName);
        }

        return handle;
    }

    public static async Task<T> FindAndLoadFirstEntryOfType<T>([CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        AddressableAssetEntry entry = FindFirstAddressbleEntryOfType<T>();

        if (entry == null)
        {
            LogWarningInternal($"Failed to load any asset of Type {typeof(T)} - could not be found in addressables", callerName);
            return null;
        }

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(entry.address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load asset of Type {typeof(T)} - load op failed", callerName);
            return null;
        }

        return handle.Result;
    }


    public static List<AddressableAssetEntry> FindAddressbleEntriesWithName(string assetName, [CallerMemberName] string callerName = "")
    {
        List<AddressableAssetEntry> allAssets = new List<AddressableAssetEntry>();
        List<AddressableAssetEntry> matchingAssets = new List<AddressableAssetEntry>();

        DefaultAddressableSettings().GetAllAssets(allAssets, false);

        LogWarningInternal($"FindAddressbleEntriesContainingID is slow, please try to access assets with a known key or asset reference object", callerName);

        foreach (AddressableAssetEntry entry in allAssets)
        {
            if (entry.MainAsset.name == assetName)
            {
                matchingAssets.Add(entry);
            }
        }

        return matchingAssets;
    }

    /// <summary>
    /// Looks through all addressable entries to find any any objects of type T. Returns the first one found.
    /// This may become slower as the number of assets in addressables increases
    /// </summary>
    /// <typeparam name="T"></typeparam> Type To Find
    /// <param name="subObjects"></param> Looks for subObjects too
    /// <param name="callerName"></param>
    /// <returns></returns>
    public static AddressableAssetEntry FindFirstAddressbleEntryOfType<T>()
    {
        List<AddressableAssetEntry> allAssets = FindAddressbleEntriesOfType<T>();

        if (allAssets.Count == 0) return null;
        return allAssets[0];
    }

    /// <summary>
    /// Looks through all addressable entries to find any any objects of type T. Returns all matching assets
    /// This may become slower as the number of assets in addressables increases
    /// </summary>
    /// <typeparam name="T"></typeparam> Type To Find
    /// <param name="subObjects"></param> Looks for subObjects too
    /// <param name="callerName"></param>
    /// <returns>List of matching addressable types</returns>
    public static List<AddressableAssetEntry> FindAddressbleEntriesOfType<T>()
    {
        List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>();
        List<AddressableAssetEntry> matchingAssets = new List<AddressableAssetEntry>();

        AddressableAssetSettings settings = DefaultAddressableSettings();
        if (settings != null)
            settings.GetAllAssets(allEntries, false, null, e => AssetDatabase.GetMainAssetTypeAtPath(e.AssetPath) == typeof(T));

        return allEntries;
    }

    private static bool FilterEntryByType<T>(AddressableAssetEntry entry)
    {
        if (entry == null) return false;
        return AssetDatabase.GetMainAssetTypeAtPath(entry.AssetPath) == typeof(T);
    }


    public static bool AssetIsValidAddressableEntry(Object obj, [CallerMemberName] string callerName = "")
    {

        if (obj == null)
        {
            LogWarningInternal($"The Object to validate is null", callerName);
            return false;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        return FindAddressbleEntryFromPath(path, callerName) != null;
    }




    public static string AssetRefFilePath(AssetReference assetRef, [CallerMemberName] string callerName = "")
    {
        return AssetDatabase.GUIDToAssetPath((assetRef.AssetGUID));
    }

    public static AssetReference AssetReferecnceFromFilePath(string filePath, [CallerMemberName] string callerName = "")
    {
        AddressableAssetEntry entry = FindAddressbleEntryFromPath(filePath, callerName);
        if (entry == null) return null;

        AssetReference assetRef = new AssetReference(entry.guid);
        return assetRef;
    }

    public static AssetReference AssetReferecnceFromGUID(GUID guid, [CallerMemberName] string callerName = "")
    {
        AssetReference assetRef = new AssetReference(guid.ToString());
        return assetRef;
    }

    public static UnityEngine.Object AddressableObjectFromAssetPath(string assetPath, [CallerMemberName] string callerName = "")
    {
        AddressableAssetEntry entry = FindAddressbleEntryFromPath(assetPath);
        return entry.MainAsset;
    }

    public static AddressableAssetEntry FindAddressbleEntryFromPath(string assetPath, [CallerMemberName] string callerName = "")
    {
        AddressableAssetEntry entry = DefaultAddressableSettings().FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

        if (entry == null)
        {
            LogWarningInternal($"Asset at {assetPath} is not an Addressable entry", callerName);
        }
        return entry;
    }

    public static AddressableAssetEntry FindAddressbleEntryFromGUID(string assetGuid, [CallerMemberName] string callerName = "")
    {
        if (assetGuid == null)
        {
            LogWarningInternal($"Asset Guid is null", callerName);
            return null;
        }


        AddressableAssetEntry entry = DefaultAddressableSettings().FindAssetEntry(assetGuid, true);

        if (entry == null)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetPath))
                LogWarningInternal($"Asset path cannot be found", callerName);
            else
                LogWarningInternal($"Asset cannot be found at {assetPath}", callerName);
        }

        return entry;
    }

    public static AddressableAssetEntry AddObjectToAddressablesWithGUID(string name, string guid, string groupID = "", string[] labels = null)
    {

        CreateAddressableData(out AddressableData data, name, guid, groupID, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }

    public static AddressableAssetEntry AddObjectToAddressablesWithPath(string name, string assetPath, string groupID = "", string[] labels = null)
    {
        string guid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
        CreateAddressableData(out AddressableData data, name, guid, groupID, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }

    public static AddressableAssetEntry AddObjectToAddressables(UnityEngine.Object asset, string groupID = "", string[] labels = null)
    {
        CreateAddressableData(out AddressableData data, asset, groupID, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }

    public static AddressableAssetEntry AddObjectToAddressables(UnityEngine.Object asset, string groupID = "", string label = "")
    {
        string[] labels = null;
        if (!string.IsNullOrEmpty(label)) labels = new string[] { label };
        CreateAddressableData(out AddressableData data, asset, groupID, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }

    public static AddressableAssetEntry AddObjectToAddressables(string name, string guid, string id = "")
    {
        string[] labels = null;
        if (!string.IsNullOrEmpty(id)) labels = new string[] { id };
        CreateAddressableData(out AddressableData data, name, guid, id, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }


    public static AddressableAssetEntry AddObjectToAddressables(UnityEngine.Object asset, string id = "")
    {
        string[] labels = null;
        if (!string.IsNullOrEmpty(id)) labels = new string[] { id };
        CreateAddressableData(out AddressableData data, asset, id, labels);

        if (AssetIsNotAValidExistingEntry(data))
            return AddAssetToAddressables(data);
        else return null;
    }



    private static void CreateAddressableData(out AddressableData data, Object asset, string groupID = "", string[] labels = null)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        string guid = AssetDatabase.GUIDFromAssetPath(path).ToString();
        CreateAddressableData(out data, asset.name, guid, groupID, labels);
    }

    private static void CreateAddressableData(out AddressableData data, string name, string guid, string groupID = "", string[] labels = null)
    {
        data = new AddressableData();

        data.settings = DefaultAddressableSettings();
        data.group = GetAddressableGroup(groupID, data.settings);
        data.name = name;
        data.guid = guid;
        if (labels != null)
        {
            data.labels = labels;
        }
    }

    public static AddressableAssetSettings DefaultAddressableSettings()
    {
        if (defaultAddressableSettings != null) return defaultAddressableSettings;

        if (!UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.SettingsExists)
        {
            LogErrorInternal("Addressable Settings don't exist, creating new ones.");
            UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true); ;
        }

        defaultAddressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (defaultAddressableSettings == null)
        {
            LogErrorInternal("Addressable Settings have not been generated, please restart Unity.");
        }

        return defaultAddressableSettings;
    }

    public static void SetAddressableGroup(AddressableAssetEntry entry, string groupID, AddressableAssetSettings settings = null, [CallerMemberName] string callerName = "")
    {
        if (entry == null)
        {
            LogErrorInternal($"Asset can not be moved to Addressable Assets group {groupID} as it is not an Addressable Entry", callerName);
            return;
        }

        if (settings == null) settings = DefaultAddressableSettings();

        AddressableAssetGroup groupToSet = GetAddressableGroup(groupID, settings);

        settings.CreateOrMoveEntry(entry.guid, groupToSet, false, true);
        entry.parentGroup.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, false, true);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private static AddressableAssetGroup GetAddressableGroup(string groupID, AddressableAssetSettings settings = null)
    {
        if (settings == null) settings = DefaultAddressableSettings();
        AddressableAssetGroup defaultGroup = settings.DefaultGroup;

        if (string.IsNullOrEmpty((groupID)))
            return defaultGroup;

        if (settings.FindGroup(groupID))
            return settings.FindGroup(groupID);

        AddressableAssetGroup group = settings.CreateGroup(groupID, false, false, true, defaultGroup.Schemas);

        ContentUpdateGroupSchema contentGroupSchema = group.GetSchema<ContentUpdateGroupSchema>();
        if (contentGroupSchema != null) contentGroupSchema.StaticContent = true;

        BundledAssetGroupSchema bundleGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (contentGroupSchema != null)
        {
            bundleGroupSchema.UseAssetBundleCrc = false;
            bundleGroupSchema.InternalBundleIdMode = BundledAssetGroupSchema.BundleInternalIdMode.GroupGuid;
        }

        return group;
    }

    public static void SetAddressableLabels(AddressableAssetEntry entry, string[] labels, AddressableAssetSettings settings = null, [CallerMemberName] string callerName = "")
    {
        if (settings == null) settings = DefaultAddressableSettings();

        SetLabels(entry, labels, settings);

        entry.parentGroup.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, false, true);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    public static void SetAddressableLabels(AddressableAssetEntry entry, string label, AddressableAssetSettings settings = null, [CallerMemberName] string callerName = "")
    {
        string[] labels = new string[] { label };
        if (settings == null) settings = DefaultAddressableSettings();

        SetLabels(entry, labels, settings);

        entry.parentGroup.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, false, true);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private static void SetLabels(AddressableAssetEntry entry, string[] labels, AddressableAssetSettings settings)
    {
        if (labels != null)
        {
            foreach (string label in labels)
            {
                CheckForLabel(settings, label);
                entry.SetLabel(label, true);
            }
        }

    }

    private static bool AssetIsNotAValidExistingEntry(AddressableData data)
    {
        var findEntry = data.settings.FindAssetEntry(data.guid);

        if (findEntry != null && findEntry.parentGroup == data.group && findEntry.address == data.name && findEntry.labels.ToArray() == data.labels)
        {
            LogInternal($"{data.name} already exists as an addressable asset with the correct group and address");
            return false;
        }

        return true;
    }

    private static AddressableAssetEntry AddAssetToAddressables(AddressableData data)
    {
        AddressableAssetEntry entry = data.settings.CreateOrMoveEntry(data.guid, data.group, false, false);

        if (entry == null)
        {
            LogErrorInternal($"{data.name} can not be added to the Addressable Assets group {data.group.name} as it's guid is incorrect? {data.guid}");
            return null;
        }

        if (data.labels != null) SetLabels(entry, data.labels.ToArray(), data.settings);


        entry.address = data.name;
        LogInternal($"{data.name} has been added as an Addressable object");

        data.group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, false, false);
        data.settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return entry;
    }

    private static void CheckForLabel(AddressableAssetSettings settings, string labelToCheck)
    {
        bool labelExists = false;
        foreach (string label in settings.GetLabels())
        {
            if (label == labelToCheck) labelExists = true;
        }

        if (!labelExists) settings.AddLabel(labelToCheck);
    }


    // https://forum.unity.com/threads/is-it-possible-to-change-edit-addressable-lables.970950/
    public class AddressablesRemoveOldLabels
    {
        //[MenuItem("MightyTools/Addressables/Remove old Addressables Labels")]
        private static void RemoveOldAddressablesLabels()
        {
            AddressableAssetSettings settings = DefaultAddressableSettings();
            HashSet<string> validLabels = new HashSet<string>(settings.GetLabels());

            foreach (AddressableAssetGroup group in settings.groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    HashSet<string> entryLabels = new HashSet<string>(entry.labels);
                    foreach (string entryLabel in entryLabels)
                    {
                        if (!validLabels.Contains(entryLabel))
                        {
                            entry.SetLabel(entryLabel, false);
                        }
                    }
                }

                EditorUtility.SetDirty(group);
            }
        }
    }

    // <||| Logging |||>

    // Internal helpers for writing logs with meaningful member information
    //  to help identify where the log is relevant to
    // 
    // The truth is that I just really like [CallerMemberName] and want to use it (joking...maybe)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogInternal(string message, [CallerMemberName] string callerName = "")
        => Debug.Log(GetLogString(message, callerName));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogWarningInternal(string message, [CallerMemberName] string callerName = "")
        => Debug.LogWarning(GetLogString(message, callerName));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogErrorInternal(string message, [CallerMemberName] string callerName = "")
        => Debug.LogError(GetLogString(message, callerName));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetLogString(string message, string callerName)
    => $"[{nameof(AddressablesHelper)}] {callerName} {message}";


}
