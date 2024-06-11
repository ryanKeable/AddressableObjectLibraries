using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Runtime.CompilerServices;


public abstract class SingletonScriptableObject : ScriptableObject
{
    [SerializeField] protected bool _verboseLogging = false;
    public bool VerboseLogging { get => _verboseLogging; }
    public virtual void OnLoadComplete()
    {
    }

    public virtual void UnloadInstanceAsset()
    {
    }
}

public abstract class SingletonAddressableScriptableObject<T> : SingletonScriptableObject where T : SingletonScriptableObject
{
    protected static T sharedInstance;
    private static AsyncOperationHandle<T> loadHandle;
    public static string TypeID { get => typeof(T).Name; }

    public static async Task<T> GetInstance(string uniqueAddressableID = "", [CallerMemberName] string callerName = "")
    {
        if (sharedInstance != null) return sharedInstance;
        if (loadHandle.IsValid())
        {
            if (loadHandle.IsDone || loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                sharedInstance = loadHandle.Result;
                return sharedInstance;
            }
            else
            {
                await loadHandle.Task;
                if (loadHandle.Task.IsCompletedSuccessfully && sharedInstance != null)
                {
                    return loadHandle.Result;
                }
            }
        }

        sharedInstance = await LoadAssetAwaiter(uniqueAddressableID, callerName);

        return sharedInstance;
    }

    private static async Task<T> LoadAssetAwaiter(string uniqueAddressableID = "", [CallerMemberName] string callerName = "")
    {
        if (!loadHandle.IsDone) return await loadHandle.Task;

        uniqueAddressableID = !string.IsNullOrEmpty(uniqueAddressableID) ? uniqueAddressableID : TypeID;
        loadHandle = Addressables.LoadAssetAsync<T>(uniqueAddressableID);
        T asset = await loadHandle.Task;
        loadHandle.CompletedTypeless += LoadCompleted;

        if (asset == null)
        {
            if (Application.isPlaying) Debug.LogError($"[{TypeID}] Could not find a Singleton Scriptable Object of type {typeof(T).Name} in Addressables"); // should be an error in playmode 
            else Debug.LogWarning($"[{TypeID}] Could not find a Singleton Scriptable Object of type {typeof(T).Name} in Addressables");
            sharedInstance = null;
        }
        else if (loadHandle.Task.IsCompletedSuccessfully)
        {
        }

        return asset;
    }

    public static async void LoadAsset()
    {
        await GetInstance();
    }

    public static void UnloadThisAsset()
    {
        if (sharedInstance == null) return;
        sharedInstance.UnloadInstanceAsset();
    }

    public override void UnloadInstanceAsset()
    {
        if (loadHandle.IsValid()) Addressables.Release<T>(loadHandle);
        sharedInstance = null;
        if (VerboseLogging) Debug.Log($"[{typeof(T)}] successfully unloaded it's asset");
    }

    private static void LoadCompleted(AsyncOperationHandle handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded) return;
        if (sharedInstance != null) sharedInstance.OnLoadComplete();
    }

    public static void SetInstance(T _instance)
    {
        if (sharedInstance == null)
            sharedInstance = _instance;
    }

    public static T instance
    {
        get
        {
            if (sharedInstance != null) return sharedInstance;

            try
            {
                // Wait very quickly on the main thread for this to load if it has not yet been loaded;
                GetInstance().Wait(100);
            }
            // Ignore exceptions here.
            catch (AggregateException)
            {

            }
            finally
            {

            }

            return sharedInstance;
        }
    }

    protected virtual void OnDestroy()
    {
        if (sharedInstance != null) Addressables.Release(sharedInstance);
    }

}
