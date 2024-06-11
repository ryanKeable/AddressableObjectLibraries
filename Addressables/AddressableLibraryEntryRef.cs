using System;
using UnityEditor;
using UnityEngine;


[Serializable]
public abstract class AddressableLibraryEntryRef
{
    [SerializeField, HideInInspector] protected string _key;
    public string Key => _key;
    public bool HasAsset => !string.IsNullOrEmpty(_key);


#if UNITY_EDITOR
    [SerializeField] protected UnityEngine.Object _asset;
    public abstract bool Validate();

#endif

}

[Serializable]
public class AddressableLibraryEntryRef<T> : AddressableLibraryEntryRef where T : UnityEngine.Object
{

#if UNITY_EDITOR
    public override bool Validate()
    {

        if (_asset != null)
        {
            bool isValid = MightyAddressables.AddressableDataLibrary.ValidateAssetOrComponent<T>(_asset);

            if (isValid)
            {
                _key = _asset.name;
                Debug.Log($"[{typeof(AddressableLibraryEntryRef).Name}] Key set to {_key}");
                return true;
            }

            _asset = null;
            _key = null;
        }

        return false;
    }

#endif

}
