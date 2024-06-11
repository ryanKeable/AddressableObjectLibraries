using System;
using UnityEngine;
using UnityEditor;

namespace MightyAddressables
{
    [CustomPropertyDrawer(typeof(AddressableLibraryEntryRef<>))]
    public class AddressableLibraryEntryRefDrawer : ExpandingSerializedPropertyDrawer
    {
        private SerializedProperty _asset;

        protected override bool DrawPropertyFoldout(Rect fieldPosition, GUIContent label, SerializedProperty property)
        {
            if (fieldPosition == null || property == null || label == null) return false;

            _asset = property.FindPropertyRelative("_asset");
            EditorUtilities.DrawPropertyField(fieldPosition, _asset, label.text, () => OnChangeCheck(property));
            return _asset.isExpanded;
        }

        protected override void OnChangeCheck(SerializedProperty property)
        {
            AddressableLibraryEntryRef obj = property.GetValue<AddressableLibraryEntryRef>();

            _asset.serializedObject.ApplyModifiedProperties();

            if (obj.Validate() == false)
                _asset.serializedObject.Update();

            base.OnChangeCheck(property);
        }
    }

}