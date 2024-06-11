using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

public static class ListExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddIfUnique<T>(this IList<T> source, T newEntry)
    {
        if (!source.Contains(newEntry))
        {
            source.Add(newEntry);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddSubRange<T>(this IList<T> source, IEnumerable<T> collection, int startIndex)
    {
        AddSubRange<T>(source, collection, startIndex, -1);
    }

    public static void AddSubRange<T>(this IList<T> source, IEnumerable<T> collection, int startIndex, int length)
    {
        if (length == 0) return;

        int currentIndex = 0;
        int copyCount = 0;
        using (IEnumerator<T> en = collection.GetEnumerator())
        {
            while (en.MoveNext() && (length < 0 || copyCount < length))
            {
                if (startIndex <= currentIndex)
                {
                    source.Add(en.Current);
                    copyCount++;
                }
                currentIndex++;
            }
        }
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] SubArray<T>(this T[] source, int index)
    {
        return SubArray<T>(source, index, source.Length - index);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] SubArray<T>(this T[] source, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(source, index, result, 0, length);
        return result;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T First<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        return source[0];
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Last<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        return source[source.Count - 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Pop<T>(this IList<T> source, int index)
    {
        if (source.Count <= index)
            return default(T);

        T value = source[index];
        source.RemoveAt(index);

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PopFirst<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        T value = source[0];
        source.RemoveAt(0);

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PopLast<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        T value = source[source.Count - 1];
        source.RemoveAt(source.Count - 1);

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PopRandom<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        int i = UnityEngine.Random.Range(0, source.Count);
        T value = source[i];
        source.RemoveAt(i);

        return value;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> Clone<T>(this List<T> source)
    {
        return new List<T>(source);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PickRandom<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default(T);

        return source[UnityEngine.Random.Range(0, source.Count)];
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int index = list.Count;
        while (index > 1)
        {
            index--;
            int newPos = UnityEngine.Random.Range(0, index + 1);
            T temp = list[newPos];
            list[newPos] = list[index];
            list[index] = temp;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SwitchPlaces<T>(this IList<T> list, int indexOne, int indexTwo)
    {
        if (indexOne < 0 || indexTwo < 0 || indexOne == indexTwo || indexOne >= list.Count || indexTwo >= list.Count)
            return;

        T temp = list[indexOne];
        list[indexOne] = list[indexTwo];
        list[indexTwo] = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ItemInDirection<T>(this List<T> items, T currentItem, int direction, T defaultValue)
    {
        int currentIndex = items.IndexOf(currentItem);

        if (currentIndex != -1)
        {
            int newIndex = currentIndex + direction;

            if (newIndex < 0) newIndex = items.Count - 1;
            else if (newIndex >= items.Count) newIndex = 0;

            return items[newIndex];
        }

        return defaultValue;
    }
}
