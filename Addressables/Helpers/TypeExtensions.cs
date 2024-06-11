using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Object = UnityEngine.Object;

public static class TypeExtensions
{
    #region Types

    private class SerTest<T> : Component
    {
        public T Value;
    }

    #endregion

    #region Constants

    // Unity cares not for your accessibility modifies
    public const BindingFlags ALL_INSTANCE_FIELDS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<Type, string> PrimitiveNames = new() {
        { typeof(  byte),   "byte" },
        { typeof( sbyte),  "sbyte" },
        { typeof( short),  "short" },
        { typeof(ushort), "ushort" },
        { typeof(   int),    "int" },
        { typeof(  uint),   "uint" },
        { typeof(  long),   "long" },
        { typeof( ulong),  "ulong" },

        { typeof(  float),   "float" },
        { typeof( double),  "double" },
        { typeof(decimal), "decimal" },

        { typeof(  char),   "char" },
        { typeof(string), "string" },

        { typeof(bool), "bool" },

        { typeof( nint),  "nint" },
        { typeof(nuint), "nuint" },

        { typeof(object), "object" },
        { typeof(  void),   "void" },
    };

    #endregion

    #region Fields

    // Cache the results of IsUnitySerializable since there's no reason a type's serialization status should change during runtime
    private static readonly Dictionary<Type, bool> SerializableCache = new();

    #endregion

    #region Methods

    public static string GetDisplayName(this Type type)
    {
        if (type == null)
            return "null";

        if (PrimitiveNames.TryGetValue(type, out string primitive))
            return primitive;

        string name = type.Name;

        if (type.IsArray)
        {
            StringBuilder builder = new(type.GetElementType().GetDisplayName());

            builder.Append('[')
                   .Append(',', type.GetArrayRank() - 1)
                   .Append(']');

            name = builder.ToString();
        }
        else if (type.IsGenericType)
        {
            StringBuilder builder = new();

            int index = name.IndexOf('`');
            if (index < 1) return name;

            builder.Append(name, 0, index)
                       .Append('<');

            Type[] typeArgs = type.GetGenericArguments();

            for (int i = 0; i < typeArgs.Length; ++i)
            {
                if (i != 0)
                    builder.Append(", ");

                builder.Append(typeArgs[i].GetDisplayName());
            }

            name = builder.Append('>').ToString();
        }
        else if (type.IsPointer)
        {
            name = type.GetElementType().GetDisplayName() + "*";
        }

        return name;
    }

    private static List<FieldInfo> GetAllInstanceFields(Type type)
    {
        List<FieldInfo> fields = new();

        while (type != null)
        {
            FieldInfo[] fieldInfo = type.GetFields(ALL_INSTANCE_FIELDS);

            for (int f = 0; f < fieldInfo.Length; ++f)
            {
                if (fieldInfo[f].DeclaringType == type)
                    fields.Add(fieldInfo[f]);
            }

            type = type.BaseType;
        }

        return fields;
    }

    ///<summary>
    ///  Retrieves all instance fields from a type that meet the requirements to be serialized by Unity but doesn't validate if their types
    ///  are serializable or not
    ///</summary>
    ///<remarks>
    ///  If you want to retrieve all instance fields that meet the serialization requirements AND have serializable types then use <see cref="GetFullySerializableFields(Type)"/>
    ///</remarks>
    public static FieldInfo[] GetSerializableFields(this Type type)
    {
        List<FieldInfo> fields = GetAllInstanceFields(type);
        List<FieldInfo> serializable = new(fields.Count);

        for (int f = 0; f < fields.Count; ++f)
        {
            if (fields[f].IsUnitySerializable())
                serializable.Add(fields[f]);
        }

        return serializable.ToArray();
    }

    ///<summary>
    ///  Retrieves all instance fields from a type that meet the requirements to be serialized by Unity but doesn't validate if their types
    ///  are serializable or not
    ///</summary>
    ///<remarks>
    ///  If you want to retrieve all instance fields that meet the serialization requirements but don't care about their types then use <see cref="GetSerializableFields(Type)"/>
    ///</remarks>
    public static FieldInfo[] GetFullySerializableFields(this Type type)
    {
        List<FieldInfo> fields = GetAllInstanceFields(type);
        List<FieldInfo> serializable = new(fields.Count);

        for (int f = 0; f < fields.Count; ++f)
        {
            if (fields[f].IsFullyUnitySerializable())
                serializable.Add(fields[f]);
        }

        return serializable.ToArray();
    }

#if false//UNITY_EDITOR

	///<summary>
	///  Checks whether a type can be serialized by Unity using a more accurate editor-only method that leverages SerializedObject
	///</summary>
	public static bool IsUnitySerializable(this Type type)
	{
		if (SerializableCache.TryGetValue(type, out bool serializable))
			return serializable;

		Type testType = typeof(SerTest<>).MakeGenericType(type);

		GameObject instance = new("SerializationTester") {
			hideFlags = HideFlags.HideAndDontSave
		};

		Component component = instance.AddComponent(testType);
		SerializedObject serialized = new(component);

		serializable = serialized.FindProperty("Value") != null;

		Object.Destroy(instance);

		SerializableCache.Add(type, serializable);
		return serializable;
	}

#else

    ///<summary>
    ///  Checks whether a type meets the criteria that would allow it to be serialized by Unity
    ///</summary>
    ///<remarks>
    ///  This method is NOT fully comprehensive, some edge cases have probably been missed
    ///</remarks>
    public static bool IsUnitySerializable(this Type type)
    {
        if (SerializableCache.TryGetValue(type, out bool serializable))
            return serializable;

        serializable = InternalUnitySerializable(type);

        SerializableCache.Add(type, serializable);
        return serializable;
    }

    ///<summary>
    ///  Checks if <paramref name="type"/> matches any known serializable types (i.e. built-in C# types, built-in Unity types, etc.) or meets
    ///  any conditions that would conclusively determine if it can be serialized
    ///</summary>
    private static bool InternalUnitySerializable(Type type)
    {
        // Static types can't be serialized and are defined as 'sealed abstract' in the IL
        if (type.IsAbstract && type.IsSealed)
            return false;

        // Unity can't serialize an interface as they have no fields nor can they be constructed
        if (type.IsInterface)
            return false;

        // Pointers also cannot be serialized
        if (type.IsPointer)
            return false;

        // Unity is capable of serializing all C# built-in types
        if (type.IsPrimitive)
            return true;
        if (type.IsEnum)
            return true;
        if (type == typeof(string))
            return true;

        // A generic type definition is only the template for a generic type, so it's inherently impossible to serialize as its type parameters are unknown
        if (type.IsGenericTypeDefinition)
            return false;

        // The only collections Unity serializes are arrays and lists, with the condition that they are not multi-dimensional or
        //  nested collections (i.e. List<T[]>, List<T>[], T[][], List<List<T>>, etc.)
        if (type.IsArray)
        {
            if (type.GetArrayRank() > 1)
                return false;

            Type elementType = type.GetElementType();

            if (elementType.IsArray || IsList(elementType))
                return false;

            return InternalUnitySerializable(elementType);
        }

        if (IsList(type))
        {
            Type elementType = type.GenericTypeArguments[0];

            if (elementType.IsArray || IsList(elementType))
                return false;

            return InternalUnitySerializable(elementType);
        }

        // Any type deriving from UnityEngine.Object can be serialized, albeit as a reference to an instance of the Object, but serialized nonetheless
        if (type == typeof(Object) || type.IsSubclassOf(typeof(Object)))
            return true;

        if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(ExposedReference<>))
            return true;

        // UnityEngine built-in types are also supported of course (I've probably missed a bunch, I'll add them as they come up)
        if (type == typeof(Vector2) || type == typeof(Vector2Int) ||
            type == typeof(Vector3) || type == typeof(Vector3Int) ||
            type == typeof(Vector4) || type == typeof(Quaternion) ||
            type == typeof(Color) || type == typeof(Color32) ||
            type == typeof(Bounds) || type == typeof(BoundsInt) ||
            type == typeof(Rect) || type == typeof(RectInt) ||
            type == typeof(Hash128) || type == typeof(LayerMask) ||
            type == typeof(RangeInt) || type == typeof(Matrix4x4))
            return true;

        // UnityEvents are also serializable but wouldn't be picked up by later checks
        if (type == typeof(UnityEventBase) || type.IsSubclassOf(typeof(UnityEventBase)))
            return true;

        // At this point the type has passed all conclusive checks (that I'm aware of, I may have missed some)
        // Unity will serialize any type with the [Serializable] attribute even if it contains no serializable fields, so
        //  we can just check for that and move on
        return type.GetCustomAttribute<SerializableAttribute>() != null;
    }

    private static bool IsList(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    /// <summary>
    /// Returns the first Type in all current domain assemblies that matches the input.
    /// </summary>
    ///<remarks>
    ///  this van ber very slow so might need to re-look at approach or use selectively
    ///</remarks>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Type GetTypeByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type == null) continue;
                if (type.Name == name)
                    return type;
                else if (type.IsGenericType && type.Name.StartsWith(name) && TrimGenericContextFromTypeName(type) == name)
                    return type;
            }
        }

        return null;
    }
    
    public static string TrimGenericContextFromTypeName(Type type)
    {
        if (type.IsGenericType && type.Name.Contains('`'))
        {
            int index = type.Name.IndexOf('`');
            int remainingChar = type.Name.Length - index;
            return type.Name.Remove(index, remainingChar);
        }

        return type.Name;
    }

#endif

    public static bool IsMonobehaviour<T>() { return typeof(T).BaseType == typeof(MonoBehaviour) || typeof(T).IsSubclassOf(typeof(MonoBehaviour)); }
    public static bool IsComponentType<T>() { return typeof(T).BaseType == typeof(Component) || typeof(T).IsSubclassOf(typeof(Component)); }
    public static bool IsSubClassOfSO<T>() { return typeof(T).IsSubclassOf(typeof(ScriptableObject)); }
    public static bool IsGameObject<T>() { return typeof(T) == typeof(GameObject); }

    #endregion
}
