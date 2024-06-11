using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MightyAddressables
{

    public static class AddressableDataLibrary
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message, [CallerMemberName] string callerName = "")
    => Debug.Log(GetLogString(message, callerName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message, [CallerMemberName] string callerName = "")
            => Debug.LogWarning(GetLogString(message, callerName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message, [CallerMemberName] string callerName = "")
=> Debug.LogError(GetLogString(message, callerName));
        public static string GetLogString(string message, string callerName)
        => $"[{LogHeader}] {callerName} {message}";

        private static string LogHeader => $"[MightyAddressables]";


#if UNITY_EDITOR

        public static string DataLibraryPath { get => "Assets/Project/Data"; }
        public static bool ValidateAssetOrComponent<T>(UnityEngine.Object obj) where T : UnityEngine.Object
        {
            if (obj == null) return false;

            if (typeof(T).IsAssignableFrom(obj.GetType()))
            {
                return true;
            }

            if (TypeExtensions.IsGameObject<T>() || TypeExtensions.IsComponentType<T>() || TypeExtensions.IsMonobehaviour<T>())
            {

                UnityEngine.GameObject go = obj as UnityEngine.GameObject;

                if (go == null)
                {
                    return false;
                }

                go.TryGetComponent(out T component);

                if (component != null)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }

    [Serializable]
    public abstract class AddressableDataLibrary<TLibrary, TObject> : SingletonAddressableScriptableObject<TLibrary> where TLibrary : AddressableDataLibrary<TLibrary, TObject> where TObject : UnityEngine.Object
    {

        [SerializeField] protected List<MightyAssetRef<TObject>> _registry = new();
        [SerializeField] private bool _generateKeys;
        public List<MightyAssetRef<TObject>> EntryRegistry => _registry;

        #region Properties
        protected string EntryType => typeof(TObject).Name;

        /// <summary>
        /// Returns all the names of assets registered in the library
        /// </summary>
        public string[] AllEntryNames => EntryRegistry.Select(e => e.AssetName).ToArray();

        #endregion

        #region  Static Methods

        /// <summary>
        /// Returns the entry asset, if the entry has not been loaded yet it will load the entry
        /// </summary>
        /// <param name="key">entry asset GUID or Name to find to load</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<TObject> RetrieveAssetAsync(string key, Transform parent = null, [CallerMemberName] string callerName = "")
        {
            await GetInstance();
            if (instance == null) return null;


            return await instance.RetrieveAssetInstance(key, parent, callerName);
        }

        /// <summary>
        /// Returns an entry asset of type T, if the entry has not been loaded yet it will load the entry
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <returns></returns>
        public async static Task<T> RetrieveAssetOfTypeAsync<T>(Transform parent = null, [CallerMemberName] string callerName = "") where T : TObject
        {
            await GetInstance();
            if (instance == null) return null;


            return await instance.RetrieveAssetInstance<T>(parent, callerName);
        }

        #endregion

        /// <summary>
        /// Returns true if the supplied guid matches an asset in the registry
        /// </summary>
        /// <param name="key">Either asset guid or asset name/param>
        public bool EntryExists(string key)
        {
            MightyAssetRef<TObject> entry = FindEntryByName(key);
            return entry != null;

        }

        public MightyAssetRef<TObject> TryGetEntry(string key, [CallerMemberName] string callerName = "")
        {
            MightyAssetRef<TObject> entry = FindEntryByName(key);

            if (entry == null)
            {
                DebugLogInternalError($"Could not find an {EntryType} with key {key}", callerName);
                return null;
            }

            return entry;
        }

        private MightyAssetRef<TObject> FindEntryByName(string key)
        {
            return _registry.FirstOrDefault(e => e.AssetName.ToLower() == key.ToLower());
        }

        private MightyAssetRef<T> FindEntryByType<T>() where T : TObject
        {
            MightyAssetRef<T>[] entries = _registry.OfType<MightyAssetRef<T>>().ToArray();
            if (entries.Length == 0) return null;

            return entries.FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TObject> RetrieveAssetInstance(string key, Transform parent = null, [CallerMemberName] string callerName = "")
        {
            MightyAssetRef<TObject> entry = FindEntryByName(key);
            if (entry == null)
            {
                DebugLogInternalError($"Could not find an {EntryType} with key {key}", callerName);
                return null;
            }

            TObject instance = await entry.RetrieveAssetInstance(parent, callerName);

            return instance;
        }

        public async Task<T> RetrieveAssetInstance<T>(Transform parent = null, [CallerMemberName] string callerName = "") where T : TObject
        {
            MightyAssetRef<T> entry = FindEntryByType<T>();
            if (entry == null)
            {
                DebugLogInternalError($"Could not find an {EntryType} with type {typeof(T)}", callerName);
                return null;
            }

            T instance = await entry.RetrieveAssetInstance(parent, callerName) as T;

            return instance;
        }


        public static void DebugLogInternal(string log, [CallerMemberName] string callerName = "")
        {
            if (instance.VerboseLogging) Debug.Log($"[{TypeID}] {callerName} {log}");
        }

        public static void DebugLogInternalError(string log, [CallerMemberName] string callerName = "")
        {
            Debug.LogError($"[{TypeID}] {callerName} {log}");
        }


#if UNITY_EDITOR

        [SerializeField] private string _defualtAssetPath;
        public bool GenerateKeys => _generateKeys;

        private void OnEnable()
        {
            if (_defualtAssetPath == null) _defualtAssetPath = DefaultAssetPath;
        }

        protected abstract string DefaultAssetPath { get; }

        public void SelectEditorAsset(string guidKey)
        {
            var entry = TryGetEntry(guidKey);
            Selection.activeObject = entry.editorAsset;
        }


        public virtual void OpenInPrefabMode(string guidKey) { }

        public void SortRegistryList()
        {
            _registry.Sort((x, y) => { return x.AssetName.CompareTo(y.AssetName); });
        }

#endif
    }
}