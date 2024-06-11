using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public static class FileEditing
{
    public const string defaultScriptDir = "Assets/Project/Scripts/";
    public const string defaultScenePath = "Assets/Project/Scenes/";

    public static MonoScript RenameScript(string classToReplace, string replacementClass, string copyPath, string newPath)
    {
        File.WriteAllLines(copyPath, ReplaceLinesInScript(copyPath, classToReplace, replacementClass));
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        System.IO.File.Move(copyPath, newPath);
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        return LoadAndPingNewScript(newPath);
    }

    public static MonoScript CopyScriptAsNew(string classToReplace, string replacementClass, string copyPath, string newScriptName, string newScriptDir)
    {
        string[] scriptContent = ReplaceLinesInScript(copyPath, classToReplace, replacementClass);

        bool dirExists = CheckForDirectory(newScriptDir);
        if (!dirExists) return null;

        string newFilePath = CombinePaths(newScriptDir, newScriptName);
        return CreateNewScript(newFilePath, scriptContent);
    }

    public static MonoScript CreateNewScript(string directory, string scriptName, string[] content)
    {
        bool dirExists = CheckForDirectory(directory);
        if (!dirExists) return null;

        string scriptPath = CombinePaths(directory, scriptName);
        return CreateNewScript(scriptPath, content);
    }

    public static MonoScript CreateNewScript(string scriptPath, string[] content)
    {
        scriptPath += ".cs";
        File.WriteAllLines(scriptPath, content);

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        return LoadAndPingNewScript(scriptPath);
    }

    private static string[] ReplaceLinesInScript(string pathToRead, string classToReplace, string replacementClass)
    {
        List<string> contents = new List<string>(32);
        contents.AddRange(File.ReadAllLines(pathToRead));

        List<int> linesToReplace = new List<int>();
        List<string> replacementLines = new List<string>(32);
        foreach (string line in contents)
        {
            if (line.ToLower().Contains(classToReplace.ToLower()))
            {
                linesToReplace.Add(contents.IndexOf(line));
                string replacementLine = ReplaceContextInLine(line, classToReplace, replacementClass);
                replacementLines.Add(replacementLine);
            }
        }

        ReplaceLinesInScript(ref contents, linesToReplace.ToArray(), replacementLines.ToArray());

        return contents.ToArray();
    }

    private static string ReplaceContextInLine(string line, string context, string replacementContext)
    {
        int[] exactContextIndices = line.ToLower().AllIndexesOf(context.ToLower()).ToArray();
        string newLine = line;
        foreach (int exactContextIndex in exactContextIndices)
        {
            string exactContext = line.Substring(exactContextIndex, context.Length);

            // handle first char lowerCase
            if (Char.IsLower(exactContext[0]))
            {
                char r1 = replacementContext[0];
                char r1Lower = Char.ToLower(r1);
                replacementContext = replacementContext.Replace(r1, r1Lower);
                newLine = newLine.Replace(exactContext, replacementContext);
                continue;
            }

            // handle all chars upper class
            if (Char.IsUpper(exactContext[0]) && Char.IsUpper(exactContext[1]))
            {
                newLine = newLine.Replace(exactContext, replacementContext.ToUpper());
                continue;
            }

            // handle general replacement
            newLine = newLine.Replace(exactContext, replacementContext);
        }

        return newLine;
    }

    private static void ReplaceLinesInScript(ref List<string> contents, int[] linesToReplace, string[] replacement)
    {
        for (int i = 0; i < linesToReplace.Length; i++)
        {
            contents[linesToReplace[i]] = replacement[i];
        }
    }

    private static MonoScript LoadAndPingNewScript(string path)
    {
        MonoScript newScript = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));

        if (newScript != null)
        {
            Selection.activeObject = newScript;
            EditorGUIUtility.PingObject(newScript);
            return newScript;
        }

        return null;
    }

    public static bool GetScriptClassName(MonoScript targetScript, out string typeName)
    {
        typeName = "";
        Type targetType = targetScript.GetClass();
        if (targetType != null)
        {
            typeName = targetType.ToString();
            return true;
        }

        if (targetType == null)
            targetType = TypeExtensions.GetTypeByName(targetScript.name);

        if (targetType == null)
        {
            Debug.LogError($"[{nameof(FileEditing)}] Cannot get class from selected object, it might be Generic: " + targetScript?.name);
            return false;
        }

        typeName = TypeExtensions.TrimGenericContextFromTypeName(targetType);
        return true;
    }

    public static bool ValidateScriptType(string scriptName)
    {
        Type typeToFind = TypeExtensions.GetTypeByName(scriptName);
        bool validType = typeToFind == null;

        return validType;
    }


    public static DefaultAsset DefaultFolderAsset(string folderLocation)
    {
        if (!CheckForDirectory(folderLocation)) return null;

        // remove last fowardslash if it was left on the location 
        Char fowardslash = "/".ToCharArray()[0];
        if (folderLocation.LastIndexOf(fowardslash) == folderLocation.Length - 1)
        {
            folderLocation = folderLocation.Remove(folderLocation.LastIndexOf(fowardslash));
        }
        return (DefaultAsset)AssetDatabase.LoadAssetAtPath(folderLocation, typeof(DefaultAsset));
    }


    public static bool CheckForDirectory(string directoryPath)
    {
        if (!DirExists(directoryPath))
        {
            if (EditorUtility.DisplayDialog($"{directoryPath} does not exist", "Create new directory?", "OK", "Canel"))
            {
                CreateDir(directoryPath);
                return DirExists(directoryPath);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public static string GetAssetFolderPath(string assetPath)
    {
        int forwardSlashIndex = assetPath.LastIndexOf("/");
        if (forwardSlashIndex <= 0) return assetPath;
        string folderPath = assetPath.Remove(forwardSlashIndex, assetPath.Length - forwardSlashIndex);
        return folderPath;
    }

    internal static void CreateDir(string directory)
    {
        Directory.CreateDirectory(DataPath() + directory);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    internal static bool DirExists(string directoryPath)
    {
        return Directory.Exists(DataPath() + directoryPath);
    }

    public static string RemoveDataPath(string path)
    {
        return path.Replace(DataPath(), "");
    }

    public static string CombinePaths(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public static string DataPath()
    {
        return Application.dataPath.Substring(0, Application.dataPath.Length - 6);
    }


}