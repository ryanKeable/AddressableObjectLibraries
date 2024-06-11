using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

using Object = UnityEngine.Object;


///<summary>
///  Provides wrapper functions for the way that we use AddressableAssets
///  Most methods are asynchronous and return a Task instead of the Handles used by Addressables
///</summary>
public static class AddressablesHelper
{

    public static async Task<bool> InitializeAddressables()
    {
        Task init = Addressables.InitializeAsync().Task;
        await init;

        return init.IsCompletedSuccessfully;
    }
    ///<summary>
    ///  Loads an asset that derives from UnityEngine.Object at the provided AssetReferenceT
    ///</summary>
    public static async Task<T> LoadAddressableObjectFromRef<T>(AssetReference assetRef, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (assetRef.AssetGUID == null)
        {
            LogWarningInternal($"Could not load {typeof(T).FullName} asset because the provided AssetReference was null", callerName);
            return null;
        }

        AsyncOperationHandle<T> handle = assetRef.LoadAssetAsync<T>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T).FullName} asset", callerName);
            return null;
        }

        return handle.Result;
    }

    ///<summary>
    ///  Loads an asset that derives from UnityEngine.Object at the provided ID
    ///</summary>
    public static async Task<T> LoadAddressableObjectFromID<T>(string id, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(id))
        {
            LogWarningInternal($"Could not load {typeof(T).FullName} asset because the provided ID was null or empty", callerName);
            return null;
        }

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(id);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T).FullName} asset with ID {id}", callerName);
            return null;
        }

        return handle.Result;
    }

    ///<summary>
    ///  Loads an asset that derives from UnityEngine.Object at the provided ID
    ///</summary>
    public static async Task<T> LoadAddressableObject<T>(object key, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (key == null)
        {
            LogWarningInternal($"Could not load {typeof(T).FullName} asset because the provided ID was null or empty", callerName);
            return null;
        }

        key = key.ToString();
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T).FullName} asset with ID {key}", callerName);
            return null;
        }

        return handle.Result;
    }


    /// <summary>
    /// Loads and Instantiates a game object from provided AssetReference
    /// </summary>
    /// <param name="assetRef"></param>
    /// <param name="parent"></param>
    /// <param name="callerName"></param>
    /// <returns></returns>
    public static async Task<UnityEngine.GameObject> InstantiateAddressableGameObj(AssetReference assetRef, Transform parent, [CallerMemberName] string callerName = "")
    {
        if (assetRef.AssetGUID == null)
        {
            LogWarningInternal($"Could not load {typeof(UnityEngine.GameObject).FullName} asset because the provided AssetReference was null", callerName);
            return null;
        }

        AsyncOperationHandle<UnityEngine.GameObject> handle = Addressables.InstantiateAsync(assetRef, parent, false, true);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(UnityEngine.GameObject).FullName} asset", callerName);
            return null;
        }

        LogInternal($"GameObject asset {handle.Result} has been instantiated successfully!", callerName);

        if (assetRef != null && assetRef.IsValid()) assetRef.ReleaseAsset();

        return handle.Result;
    }

    /// <summary>
    /// Loads and Instantiates the first game object found in Addressables from provided ID
    /// </summary>
    /// <param name="assetRef"></param>
    /// <param name="parent"></param>
    /// <param name="callerName"></param>
    /// <returns></returns>
    public static async Task<UnityEngine.GameObject> InstantiateFirstAddressableGameObjectFromKey(object key, Transform parent = null, [CallerMemberName] string callerName = "")
    {
        LogInternal($"Addressable with {key} is being instantiated...", callerName);

        if (key == null)
        {
            LogWarningInternal($"Could not load {typeof(UnityEngine.GameObject).FullName} asset because the provided ID was null or empty", callerName);
            return null;
        }

        key = key.ToString();
        IList<IResourceLocation> locations = await LocateResourceFromKey<UnityEngine.GameObject>(key);
        IResourceLocation assetLocation = locations.FirstOrDefault();

        AsyncOperationHandle<UnityEngine.GameObject> handle = Addressables.InstantiateAsync(assetLocation, parent, false, true);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(UnityEngine.GameObject).FullName} asset with ID {key}", callerName);
            return null;
        }

        LogInternal($"GameObject asset {handle.Result} has been instantiated successfully!", callerName);

        return handle.Result;
    }

    public static AsyncOperationHandle<T> LoadAddressableHandleFromKey<T>(object key, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (key == null)
        {
            LogWarningInternal($"Could not load {typeof(T)} asset because the key ID was null", callerName);
        }

        key = key.ToString();
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T)} asset with ID {key}", callerName);
        }

        return handle;
    }

    public static async Task<T> FindAndLoadEntryWithKey<T>(object key, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (key == null)
        {
            LogWarningInternal($"Could not load {typeof(T)} asset because the key ID was null", callerName);
            return null;
        }

        key = key.ToString();
        IList<IResourceLocation> locations = await LocateResourceFromKey<T>(key);

        if (locations == null)
        {
            LogWarningInternal($"Failed to load {typeof(T)} asset with ID {key.ToString()}, no locations of this asset could be found", callerName);
            return null;
        }

        IResourceLocation assetLocation = locations.FirstOrDefault();

        if (assetLocation == null)
        {
            LogWarningInternal($"Failed to load {typeof(T)} asset with ID {key.ToString()}", callerName);
            return null;
        }

        return await LoadAddressableObjectFromLocation<T>(assetLocation);
    }


    public static async Task<IList<T>> FindAndLoadEntriesWithKey<T>(object key, [CallerMemberName] string callerName = "") where T : UnityEngine.Object
    {
        if (key == null)
        {
            LogWarningInternal($"Could not load {typeof(T)} asset because the key ID was null", callerName);
            return null;
        }

        key = key.ToString();
        IList<IResourceLocation> locations = await LocateResourceFromKey<T>(key);

        if (locations == null)
        {
            LogWarningInternal($"Failed to load {typeof(T)} asset with ID {key.ToString()}", callerName);
            return null;
        }

        AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(locations, null);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to load {typeof(T)} assets with ID {key}", callerName);
            return null;
        }

        return handle.Result;
    }

    public static async Task<bool> ResourceHasLocation<T>(object key)
    {
        IList<IResourceLocation> locationList = await LocateResourceFromKey<T>(key);
        if (locationList == null) return false;
        return locationList.Count > 0;
    }

    public static async Task<IList<IResourceLocation>> LocateResourceFromKey<T>(object key)
    {
        AsyncOperationHandle<IList<IResourceLocation>> locationHandle = Addressables.LoadResourceLocationsAsync(key, typeof(T));
        await locationHandle.Task;


        if (locationHandle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to fine resource location of key: {key}");
            return null;
        }

        if (locationHandle.Result.Count == 0)
        {
            return null;
        }

        IList<IResourceLocation> resourceLocation = locationHandle.Result;
        Addressables.Release(locationHandle);

        return resourceLocation;
    }


    private static async Task<T> LoadAddressableObjectFromLocation<T>(IResourceLocation location) where T : UnityEngine.Object
    {
        AsyncOperationHandle<Object> loadHandle = Addressables.LoadAssetAsync<Object>(location);
        await loadHandle.Task;


        if (loadHandle.Status == AsyncOperationStatus.Failed)
        {
            LogWarningInternal($"Failed to Instantiate Addressable {typeof(T)} asset for location key {location.PrimaryKey}");
        }

        Object obj = loadHandle.Result;
        Addressables.Release(loadHandle);

        return obj as T;
    }



    public static void ReleaseTrackedResource(UnityEngine.GameObject trackedInstance)
    {
        Addressables.ReleaseInstance(trackedInstance);
    }

    public static void ReleaseResource(Object obj)
    {
        Addressables.Release(obj);
    }


    public static bool IsValidRuntimeAsset(AssetReference assetRef, [CallerMemberName] string callerName = "")
    {
        if (assetRef.AssetGUID == null)
        {
            LogWarningInternal($"AssetReference {assetRef.ToString()} does not have a editorAsset referenced", callerName);

            return false;
        }
        return ValidRuntimeKey(assetRef, callerName) && ValidAssetGUID(assetRef, callerName);
    }

    private static bool ValidRuntimeKey(AssetReference assetRef, [CallerMemberName] string callerName = "")
    {
        bool valid = assetRef.RuntimeKeyIsValid();
        if (!valid)
        {
            LogWarningInternal($"AssetReference {assetRef.ToString()} does not have a valid runtime key", callerName);
        }

        return valid;
    }

    private static bool ValidAssetGUID(AssetReference assetRef, [CallerMemberName] string callerName = "")
    {
        bool valid = assetRef.AssetGUID != null;
        if (!valid)
        {
            LogWarningInternal($"AssetReference {assetRef.ToString()} does not have a valid AssetGUID", callerName);
        }

        return valid;
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