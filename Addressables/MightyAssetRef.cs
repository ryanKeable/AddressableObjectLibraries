using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MightyAddressables
{
    [Serializable]
    public class MightyAssetRef<T> : AssetReferenceT<T> where T : UnityEngine.Object
    {
        [SerializeField] private bool isValid;
        [SerializeField, FormerlySerializedAs("_key")] private string key;

        private UnityEngine.Object _loadedAsset;
        private AsyncOperationHandle<UnityEngine.GameObject> handle = new();

        public string AssetName
        {
            get
            {
                // a bit hacky but we need to make sure the key is updated if the prefab name gets updated. 
                // i cant see a way where the asset name is known at runtime prior to loading
                // this should be a sep func but i have a busted collarbone and am reducing footprints
#if UNITY_EDITOR
                if (editorAsset != null && key != editorAsset.name) key = editorAsset.name;
#endif
                return key;
            }
        }
        public UnityEngine.Object LoadedAsset => _loadedAsset;

        public MightyAssetRef(string guid) : base(guid)
        {
#if UNITY_EDITOR

            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if (obj == null)
            {
                AddressableDataLibrary.LogError($" obj at path {path} with guid {guid} is null -- cannot validate as Asset Ref");
                return;
            }

            isValid = ValidateAsset(obj);
            if (isValid == false)
            {
                AddressableDataLibrary.LogError($" {obj.name} is not a Valid obj of {typeof(T).Name} -- cannot validate as Asset Ref");
                return;
            }
            SetEditorAsset(obj);
#endif
        }

        /// <summary>
        /// Overrides internal validation and checks if it matches type of T 
        /// </summary>
        /// <param name="obj">The Object to validate.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public override bool ValidateAsset(UnityEngine.Object obj)
        {
#if UNITY_EDITOR

            isValid = AddressableDataLibrary.ValidateAssetOrComponent<T>(obj);
            return isValid;
#else
            return isValid;
#endif
        }

        /// <summary>
        /// Overrides internal validation and checks if it matches type of T
        /// </summary>
        /// <param name="path">The path to the asset in question. (PATH IS IGNORED)</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public override bool ValidateAsset(string mainAssetPath)
        {

#if UNITY_EDITOR
            isValid = typeof(T).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(mainAssetPath));
            if (isValid == true)
            {
                return true;
            }
            else
            {
                var assets = LoadAllAssetsAtPath(mainAssetPath);
                if (assets == null) return false;
                isValid = assets.Any(o => AddressableDataLibrary.ValidateAssetOrComponent<T>(o));
                return isValid;
            }
#else
                return isValid;
#endif
        }

        public async Task<T> RetrieveAssetInstance(Transform parent = null, [CallerMemberName] string callerName = "")
        {
            if (_loadedAsset != null) return _loadedAsset as T;

            if (TypeExtensions.IsGameObject<T>())
            {
                return await InsantiateGameObject(parent, callerName) as T;

            }

            if (TypeExtensions.IsMonobehaviour<T>() || TypeExtensions.IsComponentType<T>())
            {
                return await InsantiateComponentGO(parent, callerName);
            }

            if (!OperationHandle.IsValid())
                LoadAssetAsync<T>();

            await OperationHandle.Task;

            _loadedAsset = OperationHandle.Result as T;


            return _loadedAsset as T;
        }

        private async Task<T> InsantiateComponentGO(Transform parent = null, [CallerMemberName] string callerName = "")
        {
            UnityEngine.GameObject gameObj = await InsantiateGameObject(parent, callerName);

            if (gameObj == null) return default(T);
            T gameObjT = gameObj.GetComponent<T>();

            return gameObjT;
        }

        private async Task<GameObject> InsantiateGameObject(Transform parent = null, [CallerMemberName] string callerName = "")
        {
            if (!handle.IsValid())
            {
                handle = Addressables.InstantiateAsync(this, parent.transform.position, parent.transform.rotation, parent, true);
            }

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                AddressableDataLibrary.LogWarning($"Failed to load {typeof(UnityEngine.GameObject).FullName} asset", callerName);
                return null;
            }

            AddressableDataLibrary.Log($"GameObject asset {handle.Result} has been instantiated successfully!", callerName);

            ReleaseAsset();

            handle.Result.SetActive(false);
            handle.Result.name = handle.Result.name.Replace("(Clone)", string.Empty);

            return handle.Result;
        }

        public override void ReleaseAsset()
        {
            if (Asset == null && _loadedAsset == null) return; // we cannot release something that isnt loaded
            _loadedAsset = null;
            base.ReleaseAsset();
        }

#if UNITY_EDITOR
        [SerializeField] private T _asset;

        public bool HasAsset => AssetGUID != null || _asset != null;
        public string AssetPath => AssetDatabase.GUIDToAssetPath(AssetGUID);

        public override bool SetEditorAsset(UnityEngine.Object value)
        {
            bool isSet = base.SetEditorAsset(value);

            if (CachedAsset == null)
                CachedAsset = FetchEditorAsset();

            if (isSet == true && CachedAsset != null)
            {
                if (TypeExtensions.IsMonobehaviour<T>() || TypeExtensions.IsComponentType<T>())
                {
                    GameObject go = CachedAsset as GameObject;
                    _asset = go.GetComponent<T>();
                    key = _asset.name;

                    return true;
                }

                if (CachedAsset as T == null)
                {
                    return false;
                }
                _asset = CachedAsset as T;
                key = _asset.name;

                return true;
            }
            else
            {
                _asset = null;
                key = null;

                return false;
            }
        }


        /// <summary>
        /// Used by the editor to represent the main asset referenced.
        /// </summary>
        public new UnityEngine.Object editorAsset
        {
            get { return GetEditorAssetInternal(); }
        }

        /// <summary>
        /// Helper function that can be used to override the base class editorAsset accessor.
        /// </summary>
        /// <returns>Returns the main asset referenced used in the editor.</returns>
        private UnityEngine.Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
                return CachedAsset;

            var asset = FetchEditorAsset();

            if (DerivedClassType == null)
                return CachedAsset = asset;

            if (asset == null)
                Debug.LogWarning("Assigned editorAsset does not match type " + DerivedClassType + ". EditorAsset will be null.");
            return CachedAsset = asset;
        }

        private UnityEngine.Object FetchEditorAsset()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset;
        }

        private UnityEngine.Object[] LoadAllAssetsAtPath(string assetPath)
        {
            return typeof(SceneAsset).Equals(AssetDatabase.GetMainAssetTypeAtPath(assetPath)) ?
                // prevent error "Do not use readobjectthreaded on scene objects!"
                new[] { AssetDatabase.LoadMainAssetAtPath(assetPath) } :
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
        }

        public void AddObjectToAddressables(string id)
        {
            if (editorAsset == null)
            {
                AddressableDataLibrary.LogError($"Could not add asset to addressables, the editorAsset is null");
                return;
            }
            AddressablesEditorHelper.AddObjectToAddressables(editorAsset.name, AssetGUID, id);
        }

#endif
    }

}