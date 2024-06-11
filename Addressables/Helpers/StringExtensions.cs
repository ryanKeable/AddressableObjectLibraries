using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.IO;

public static class StringExtensions
{
    [Pure]
    public static bool IsNotNullOrEmpty(this string source)
    {
        return !string.IsNullOrEmpty(source);
    }

    [Pure]
    public static bool StartsWithFast(this string source, string value)
    {
        if (source == null || value == null) 
            return false;

        if (value.Length > source.Length)
            return false;

        if (value.Length > 0)
        {
            for (int i = 0; i < value.Length; ++i)
            {
                if (source[i] != value[i])
                    return false;
            }
        }

        return true;
    }

    [Pure]
    public static bool EndsWithFast(this string source, string value)
    {
        if (source == null || value == null) 
            return false;

        if (value.Length > source.Length)
            return false;

        if (value.Length > 0)
        {
            for (int vi = 0, si = source.Length - value.Length; vi < value.Length; ++vi, ++si)
            {
                if (source[si] != value[vi])
                    return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0) return text;
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CaseInsensitiveContains(this string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
    {
        return text.IndexOf(value, stringComparison) >= 0;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream ToStream(this string str)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(str);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> AllIndexesOf(this string str, string value)
    {
        if (String.IsNullOrEmpty(value))
            throw new ArgumentException("the string to find may not be empty", "value");
        List<int> indexes = new List<int>();
        for (int index = 0; ; index += value.Length)
        {
            index = str.IndexOf(value, index);
            if (index == -1)
                return indexes;
            indexes.Add(index);
        }
    }
}
