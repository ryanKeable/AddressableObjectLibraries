using UnityEngine;
using UnityEditor;
using System;

public abstract class EditingWizard : EditorWindow
{
    protected abstract void SetLabelsAndContext();
    protected abstract void ConfirmationAction(Action completion = null);
    protected abstract Vector2 WindowSize();

}

public abstract class ObjectEditingWizard<TObject> : EditingWizard where TObject : UnityEngine.Object
{
    protected string _windowSummary = "Object Editing Window";
    protected string _valdationMessage = "Valid Object";
    protected string confirmationButtonLabel;
    protected string _newAssetName;
    protected string _warningContent;
    protected string _targetAssetPath;
    protected bool _validContent;
    protected bool _invalidatedContent;
    protected static bool firstTime = true;
    protected override Vector2 WindowSize() { return new Vector2(600, 300); }
    protected ObjectEditingWizard<TObject> theWindow;
    private TObject _targetObject;

    protected static string ObjectType { get => typeof(TObject).Name; }

    public static void Init<T>(string label = null) where T : ObjectEditingWizard<TObject>
    {
        if (label == null) label = $"Create a new {ObjectType}";
        DrawWindow<T>(label);
    }

    protected static void DrawWindow<T>(string label) where T : ObjectEditingWizard<TObject>
    {
        T window = (T)EditorWindow.GetWindow(typeof(T), true, label, true);
        window.minSize = window.WindowSize();
        window.maxSize = window.minSize + new Vector2(128, 128);
        window.Show();
    }

    private void OnEnable()
    {
        firstTime = true;
        _invalidatedContent = false;
        Instantiate();
        SetLabelsAndContext();
    }

    protected virtual void Instantiate()
    {
    }

    protected void OnGUI()
    {
        DrawWindowDesc();
        BeginChangeCheck();

        EditorGUILayout.Space();
        DrawBodyGUI();
        AdditionalGUI();
        EditorGUILayout.Space();
        RunValidation();
        DrawConfirmationGUI();
        EditorGUILayout.Space();
        ShowContext();

        firstTime = false;
    }


    protected virtual void DrawBodyGUI()
    {
        DrawNameField(ref _newAssetName);
        TargetObjectField();
    }


    private void AdditionalGUI()
    {
        EditorGUI.indentLevel++;

        EditorGUILayout.Space();
        DrawAdditionalGUIContent();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }


    protected virtual void DrawAdditionalGUIContent()
    {
        EditorGUILayout.Space();
    }

    private void TargetObjectField()
    {
        EditorGUI.BeginChangeCheck();

        DrawTargetObjectField();

        if (EditorGUI.EndChangeCheck())
        {
            EndObjectChangeCheck();
        }
    }

    protected virtual void DrawTargetObjectField(string label = "to copy")
    {
        DrawObjectField<TObject>(ref _targetObject, ObjectType + " " + label, true);
    }

    protected virtual void EndObjectChangeCheck()
    {
        if (TargetObject == null)
        {
            TargetObjectIsNull();
            return;
        }

        TargetObjectIsSelected();
    }

    protected virtual void TargetObjectIsNull()
    {
        _newAssetName = ObjectType;
    }

    protected virtual void TargetObjectIsSelected()
    {
        _newAssetName = TargetObject.name;
    }

    protected virtual void DrawConfirmationGUI()
    {
        DrawConditionalButton(confirmationButtonLabel + " " + CreateAssetLabel, OnClickConfirm, !_validContent);
    }

    protected void OnClickConfirm()
    {
        ConfirmationAction(Close);
    }

    protected virtual void DrawWindowDesc()
    {
        EditorGUILayout.Space(Spacer * 2.0f);
        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 };
        EditorGUILayout.LabelField(_windowSummary, style, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(Spacer * 2.0f);
    }

    protected UnityEngine.Object DrawObjectField<T>(ref T objectTarget, string label, bool allowSceneObjects = false) where T : UnityEngine.Object
    {
        objectTarget = (T)EditorGUILayout.ObjectField(
           label,
           objectTarget,
           typeof(T),
           allowSceneObjects,
           GUILayout.ExpandWidth(true));

        return objectTarget;
    }

    protected virtual void DrawOpenFolderPanel(string title, ref string path)
    {
        EditorGUILayout.Space();
        if (path == null) path = _targetAssetPath;
        if (GUILayout.Button($"Folder: {path}"))
        {
            string newPath = EditorUtility.OpenFolderPanel(title, path, "");

            if (!string.IsNullOrEmpty(newPath)) path = newPath;
        }

        if (path == null) return;
        path = FileEditing.RemoveDataPath(path);
    }


    /// <summary>
    /// Draws a text field to set the name of the object. Stores text as newObjectName
    /// </summary>
    protected virtual string DrawNameField(ref string nameField, string label = null)
    {
        string labelToUse = label != null ? label : NameTextFieldLabel;
        EditorGUILayout.Space(Spacer);
        nameField = EditorGUILayout.TextField(labelToUse, nameField);
        return nameField;
    }

    protected void DrawConditionalButton(string buttonLabel, Action buttonAction, bool disableCondition)
    {
        EditorGUI.BeginDisabledGroup(disableCondition);
        DrawButton(buttonLabel, buttonAction);
        EditorGUI.EndDisabledGroup();
    }

    protected void DrawButton(string buttonLabel, Action buttonAction)
    {
        EditorGUILayout.Space(Spacer);
        if (GUILayout.Button(buttonLabel))
        {
            buttonAction.Invoke();
        }
    }

    /// <summary>
    /// Displays a Help box to show if the object creation is valid or not, if it is not valid provides the reason;
    /// </summary>
    /// <param name="overrideMessage"></param>
    /// <param name="spacer"></param>
    protected void ShowContext(string overrideMessage = "")
    {
        EditorGUILayout.Space(Spacer);

        if (_validContent)
        {
            EditorGUILayout.HelpBox(_valdationMessage, MessageType.Info);
        }
        else
        {
            if (string.IsNullOrEmpty(overrideMessage))
                EditorGUILayout.HelpBox($"WARNING: {_warningContent}", MessageType.Warning);
            else
            {
                EditorGUILayout.HelpBox($"WARNING: {overrideMessage}", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(Spacer);
    }

    void BeginChangeCheck()
    {
        EditorGUI.BeginChangeCheck();
    }

    void RunValidation()
    {
        if (EndChangeCheck())
        {
            _invalidatedContent = true;
            ValidateWindowContent();
        }
    }

    protected virtual void ValidateWindowContent()
    {
        if (CheckForTarget() == false) return;
        if (CheckForTargetOfType() == false) return;
    }

    protected virtual bool CheckForTarget()
    {
        bool condition = TargetObject != null;
        string warningMessage = $"You must select a {ObjectType}";
        ValidateContent(condition, warningMessage);
        return condition;
    }

    protected virtual bool CheckForTargetOfType()
    {
        bool condition = TargetObject.GetType() == typeof(TObject);
        string warningMessage = $"You must select a {ObjectType}";
        ValidateContent(condition, warningMessage);
        return condition;
    }

    protected virtual void ValidateContent(bool isValid, string message)
    {
        if (_invalidatedContent == false) return;
        if (isValid == false) _invalidatedContent = false;

        _validContent = isValid;
        if (isValid)
        {
            _warningContent = "";
        }
        else
        {
            _warningContent = message;
        }
    }

    bool EndChangeCheck()
    {
        return firstTime || EditorGUI.EndChangeCheck();
    }

    protected string CreateAssetLabel
    {
        get => _newAssetName;
    }

    protected float Spacer { get => 3f; }
    protected virtual string NameTextFieldLabel { get => $"{ObjectType} Name "; }
    protected virtual TObject TargetObject { get => _targetObject; set { _targetObject = value; } }
}

