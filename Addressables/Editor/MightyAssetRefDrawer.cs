using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;

namespace MightyAddressables
{
    [CustomPropertyDrawer(typeof(MightyAssetRef<>))]
    public class MightyAssetRefDrawer : ExpandingSerializedPropertyDrawer
    {
        private SerializedProperty _asset;
        private PropertyDrawer _cachedDrawer;
        private bool _foldout;

        protected override float GetChildPropertyHeights(SerializedProperty property)
        {
            float propHeight = 0.0f;
            if (_asset != null && _asset.isExpanded) propHeight += _asset.GetHeight();

            return propHeight;
        }

        protected override bool DrawPropertyFoldout(Rect fieldPosition, GUIContent label, SerializedProperty property)
        {
            if (fieldPosition == null || property == null || label == null) return false;
            PropertyDrawer drawer = GetDrawer();

            if (drawer == null)
                return false;

            drawer.OnGUI(fieldPosition, property, label);

            return false;
        }

        private PropertyDrawer GetDrawer()
        {
            if (_cachedDrawer != null) return _cachedDrawer;

            Type drawerType = Type.GetType("UnityEditor.AddressableAssets.GUI.AssetReferenceDrawer, Unity.Addressables.Editor");
            _cachedDrawer = Activator.CreateInstance(drawerType) as PropertyDrawer;

            if (_cachedDrawer.fieldInfo != fieldInfo)
            {
                FieldInfo field = drawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(_cachedDrawer, fieldInfo);
            }

            return _cachedDrawer;
        }






    }

}