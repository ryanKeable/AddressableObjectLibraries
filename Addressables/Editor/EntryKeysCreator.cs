using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MightyAddressables;

public static class EntryKeysCreator<TLibrary, TEntry> where TLibrary : AddressableDataLibrary<TLibrary, TEntry> where TEntry : UnityEngine.Object
{
    private const string Indent = "    ";
    private const string ConstPrefix = "public const string";
    private const string Comment = "//";

    public static void AddEntryKey(string entryName)
    {
        List<string> contents = EntryKeysContent().ToList();
        if (contents == null) return;

        foreach (string line in contents)
        {
            if (line.Contains(entryName))
            {
                LogInternal($"{EntryKeysClassName} already contains the guid for key: {entryName}");
                return;
            }
        }

        InsertEntryKeyId(ref contents, entryName);
        LogInternal($"{entryName.ToUpper()} has been added as a key to {EntryKeysClassName}");

        SaveContents(contents.ToArray());
    }

    public static bool CheckForUnusedOrMissingKeysOrSummaries(out bool scriptExists, string[] entryNames)
    {
        scriptExists = true;
        if (AddressableDataLibraryEditor<TLibrary, TEntry>.instance == null) return false;

        if (!CheckForKeysScript(out string[] contents))
        {
            scriptExists = false;
            return true;
        }

        if (CheckForEntriesWithNoMatch(entryNames, contents))
        {
            return true;
        }

        if (CheckForKeyIdentifiersWithNoMatch(entryNames, contents))
        {
            return true;
        }

        if (CheckForKeyContentsWithNoMatch(entryNames, contents))
        {
            return true;
        }

        return false;
    }

    public static void UpdateEntryKeys(string[] names)
    {
        if (HasEntries(names) == false) return;

        List<string> contents = EntryKeysContent().ToList();

        if (contents == null) return;

        contents.Clear();
        contents.AddRange(EntryKeysScriptFramework());

        for (int i = 0; i < names.Length; i++)
        {
            InsertEntryKeyId(ref contents, names[i]);
        }

        SaveContents(contents.ToArray());
    }

    public static List<string> GetEntryKeys()
    {
        if (!CheckForKeysScript(out string[] contents))
        {
            return null;
        }

        List<string> keys = new();
        foreach (string line in contents)
        {
            string identifier = ExtractKeyIdentifierFromLine(line);
            if (!string.IsNullOrEmpty(identifier)) keys.Add(identifier);
        }

        return keys;
    }

    private static void InsertEntryKeyId(ref List<string> contents, string entryName)
    {
        InsertEntryKeySummary(ref contents, entryName);
        string keyWithIdenfitier = $"{Indent}{ConstPrefix} {entryName} = \"{entryName}\";";

        contents.Insert(contents.Count - 1, keyWithIdenfitier);
        contents.Insert(contents.Count - 1, ""); // insert a space
    }

    private static void InsertEntryKeySummary(ref List<string> contents, string entryName)
    {
        contents.Insert(contents.Count - 1, $"{Indent}{Comment} <summary>");
        contents.Insert(contents.Count - 1, $"{Indent}{Comment} {typeof(TLibrary).Name} key for {typeof(TEntry).Name} with name {entryName}");
        contents.Insert(contents.Count - 1, $"{Indent}{Comment} </summary>");
    }

    private static bool CheckForKeyIdentifiersWithNoMatch(string[] identifiers, string[] contents)
    {
        if (identifiers.Length == 0 || contents == null)
            return true;

        foreach (string line in contents)
        {
            string identifier = ExtractKeyIdentifierFromLine(line);

            if (identifier.IsNotNullOrEmpty())
            {
                bool identifierHasNoMatch = true;

                for (int i = 0; i < identifiers.Length; i++)
                {
                    // this identifier has a match
                    if (identifier == identifiers[i])
                    {
                        identifierHasNoMatch = false;
                        break;
                    }
                }

                if (identifierHasNoMatch) return true;
            }
        }

        return false;
    }

    private static bool CheckForKeyContentsWithNoMatch(string[] keyContents, string[] contents)
    {
        if (keyContents.Length == 0 || contents == null)
            return true;

        foreach (string line in contents)
        {
            string keyContent = ExtractKeyContentFromLine(line);
            if (keyContent.IsNotNullOrEmpty())
            {
                bool contentHasAMatch = false;

                for (int i = 0; i < keyContents.Length; i++)
                {
                    if (keyContent == keyContents[i]) //this identifier has a match
                    {
                        contentHasAMatch = true;
                        break;
                    }
                }

                if (!contentHasAMatch) return true;
            }
        }

        return false;
    }

    private static bool CheckForEntriesWithNoMatch(string[] entries, string[] contents)
    {
        if (entries.Length == 0 || contents == null)
            return true;

        foreach (string entry in entries)
        {
            bool entryHasNoMatch = true;

            for (int i = 0; i < contents.Length; i++)
            {
                string identifier = ExtractKeyIdentifierFromLine(contents[i]);
                string keyContent = ExtractKeyContentFromLine(contents[i]);

                if (identifier.IsNotNullOrEmpty() && keyContent.IsNotNullOrEmpty())
                {
                    if (entry == identifier && entry == keyContent) //this identifier has a match
                    {
                        entryHasNoMatch = false;
                        break;
                    }
                }
            }

            if (entryHasNoMatch) return true;
        }

        return false;
    }

    private static string[] EntryKeysContent()
    {
        CheckForKeysScript(out string[] contents);
        return contents;
    }

    private static bool CheckForKeysScript(out string[] contents)
    {
        if (!File.Exists(EntryKeyScriptPath))
        {
            if (EditorUtility.DisplayDialog($"{EntryKeysClassName} does not exist", $"Create new {EntryKeysClassName} script?", "OK", "Canel"))
            {
                CreateNetKeysScript(out contents);
                return false;
            }
            else
            {
                LogInternalError($"There is no {EntryKeysClassName} script at: {EntryKeyScriptDirectory}. Could not append keys");
                contents = null;
                return false;
            }
        }
        contents = File.ReadAllLines(EntryKeyScriptPath);
        return true;
    }

    private static void CreateNetKeysScript(out string[] contents)
    {
        contents = EntryKeysScriptFramework();
        FileEditing.CreateNewScript(EntryKeyScriptDirectory, EntryKeysClassName, contents);
        LogInternal($"A {EntryKeysClassName} script has been created at {EntryKeyScriptPath}");
    }

    private static string[] EntryKeysScriptFramework()
    {
        List<string> contents = new List<string>()
        {
            $"public static class {EntryKeysClassName}",
            "{",
            $"",
            "}",
        };

        return contents.ToArray();
    }

    private static string ExtractKeyIdentifierFromLine(string line)
    {
        if (line.Contains(ConstPrefix))
        {
            // Line will look like this and we want to  get the Identifier to test against - "public const string KeyIdentifier = \"KeyContents\";
            // We remove the prefix, trim whitespace and then split the string at the '=' to get the two halves
            string[] lineParts = line.Replace(ConstPrefix, string.Empty).Trim().Split('=');
            // KeyIdentifier is the first half with no whitespace
            return lineParts[0].Trim();
        }

        return string.Empty;
    }

    private static string ExtractKeyContentFromLine(string line)
    {
        if (line.Contains(ConstPrefix))
        {
            // Line will look like this and we want to  get the KeyContents to test against - "public const string KeyIdentifier = \"KeyContents\";
            // We remove the prefix, trim whitespace and then split the string at the '=' to get the two halves
            string[] lineParts = line.Replace(ConstPrefix, string.Empty).Trim().Split('=');
            // KeyContent is the second half with no whitespace, " or ; symbols
            lineParts[1] = lineParts[1].Replace("\"", string.Empty);
            lineParts[1] = lineParts[1].Replace(";", string.Empty);
            return lineParts[1].Trim();
        }

        return string.Empty;
    }

    private static List<string> ExtractSummariesFromContents(string[] contents)
    {
        List<string> summaries = new List<string>();

        for (int i = 0; i < contents.Length; i++)
        {
            string line = contents[i].TrimStart();

            if (line.StartsWith(Comment) && line.Contains("<summary>"))
            {
                //look for the following line
                int nextLine = i + 1;
                if (nextLine < contents.Length)
                {
                    string summaryLine = contents[nextLine].TrimStart();
                    if (summaryLine.StartsWith(Comment))
                    {
                        summaries.Add(summaryLine.Replace(Comment, string.Empty).TrimStart());
                    }
                }
            }
        }

        return summaries;
    }

    private static void SaveContents(string[] contents)
    {
        File.WriteAllLines(EntryKeyScriptPath, contents);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool HasEntries(string[] names)
    {
        return names.Length > 0;
    }

    private static string EntryKey
    {
        get => typeof(TEntry).Name;
    }

    private static string EntryKeysClassName
    {
        get => typeof(TLibrary).Name + "Keys";
    }

    private static string EntryKeyScriptDirectory
    {
        get => $"Assets/Project/Scripts/AddressableLibraryKeys/";
    }

    private static string EntryKeyScriptPath
    {
        get => $"{EntryKeyScriptDirectory}/{EntryKeysClassName}.cs";
    }

    private static void LogInternal(string log)
    {
        Debug.Log($"[{nameof(EntryKeysCreator<TLibrary, TEntry>)}] {log}");
    }

    private static void LogInternalError(string log)
    {
        Debug.LogError($"[{nameof(EntryKeysCreator<TLibrary, TEntry>)}] {log}");
    }

}