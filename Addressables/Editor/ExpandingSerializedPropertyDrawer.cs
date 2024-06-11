using UnityEngine;
using UnityEditor;

public abstract class ExpandingSerializedPropertyDrawer : PropertyDrawer
{
    private bool _init = false;
    protected float LineSpacing => EditorUtilities.StandardLineHeight;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property != null && property.isExpanded)
        {
            float propHeight = LineSpacing;
            propHeight += GetChildPropertyHeights(property);
            return propHeight;
        }
        else
        {
            return LineSpacing;
        }
    }

    protected virtual float GetChildPropertyHeights(SerializedProperty property)
    {
        return property.GetHeight();
    }


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect fieldPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        BeginProperty(fieldPosition, label, property);

        EditorGUI.BeginChangeCheck();

        property.isExpanded = DrawPropertyFoldout(fieldPosition, label, property);

        if (property != null && property.isExpanded == false)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        DrawExpandedGUI(fieldPosition, property);
        EditorGUI.indentLevel--;

        if (EndChangeCheck())
        {
            OnChangeCheck(property);
        }

        EditorGUI.EndProperty();
    }


    protected virtual GUIContent BeginProperty(Rect fieldPosition, GUIContent label, SerializedProperty property)
    {
        return EditorGUI.BeginProperty(fieldPosition, label, property);
    }

    protected virtual bool DrawPropertyFoldout(Rect fieldPosition, GUIContent label, SerializedProperty property)
    {
        if (fieldPosition == null || property == null || label == null) return false;

        EditorUtilities.DrawPropertyField(fieldPosition, property, label.text, () => OnChangeCheck(property));
        return property.isExpanded;
    }

    protected virtual void DrawExpandedGUI(Rect fieldPosition, SerializedProperty property)
    {
        DrawContent(ref fieldPosition, property);
    }

    protected virtual void DrawContent(ref Rect fieldPosition, SerializedProperty property) { }
    protected virtual void DrawContent(SerializedObject serializedObj) { }
    protected virtual bool EndChangeCheck() { return _init == false || EditorGUI.EndChangeCheck(); }
    protected virtual void OnChangeCheck(SerializedProperty property)
    {
        property.serializedObject.ApplyModifiedProperties();
        property.serializedObject.Update();
        _init = true;
    }
    protected void IncrementYPos(ref Rect fieldPosition, int numOfLines = 1) { fieldPosition.y += LineSpacing * numOfLines; }

}
