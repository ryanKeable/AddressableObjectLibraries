using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

public static class SerializedPropertyExtensions
{
	#region Constants

	private const BindingFlags SERIALIZED_FIELD_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	#endregion

	#region Methods

	public static Type GetValueType(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		switch (property.propertyType)
		{
			case SerializedPropertyType.AnimationCurve: return typeof(AnimationCurve);
			case SerializedPropertyType.Boolean:        return typeof(bool);
			case SerializedPropertyType.Bounds:         return typeof(Bounds);
			case SerializedPropertyType.BoundsInt:      return typeof(BoundsInt);
			case SerializedPropertyType.Character:      return typeof(char);
			case SerializedPropertyType.Color:          return typeof(Color);
			case SerializedPropertyType.Float:          return typeof(float);
			case SerializedPropertyType.Gradient:       return typeof(Gradient);
			case SerializedPropertyType.Hash128:        return typeof(Hash128);
			case SerializedPropertyType.Integer:        return typeof(int);
			case SerializedPropertyType.LayerMask:      return typeof(LayerMask);
			case SerializedPropertyType.Quaternion:     return typeof(Quaternion);
			case SerializedPropertyType.Rect:           return typeof(Rect);
			case SerializedPropertyType.RectInt:        return typeof(RectInt);
			case SerializedPropertyType.String:         return typeof(string);
			case SerializedPropertyType.Vector2:        return typeof(Vector2);
			case SerializedPropertyType.Vector2Int:     return typeof(Vector2Int);
			case SerializedPropertyType.Vector3:        return typeof(Vector3);
			case SerializedPropertyType.Vector3Int:     return typeof(Vector3Int);
			case SerializedPropertyType.Vector4:        return typeof(Vector4);
			
			default:
				return GetField(property)?.FieldType;
		}
	}

	// For some reason 'SerializedProperty.isArray' returns true if it's a string, probably because "string = char[]" but it's really unhelpful and
	//  inaccruate in C#, hence this method
	public static bool IsRealArray(this SerializedProperty property)
	{
		if (property == null)
			throw new NullReferenceException(nameof(property));

		return property.isArray && property.propertyType != SerializedPropertyType.String;
	}

	public static bool IsArrayElement(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		string path = property.propertyPath;

		if (path[^1] == ']')
		{
			int lastSeparator = path.LastIndexOf('.');
			return lastSeparator > 0 && path[lastSeparator..].StartsWith(".data[", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	public static int GetArrayIndex(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		string path = property.propertyPath;
		int lastSeparator = path.LastIndexOf('.');

		if (lastSeparator == -1)
			return -1;

		int start = path.IndexOf('[', lastSeparator);

		if (start == -1)
			return -1;

		int end = path.IndexOf(']', start);

		if (end == -1)
			return -1;

		string indexStr = path[(start + 1)..end];
		return int.TryParse(indexStr, out int index) ? index : -1;
	}

	private static string GetParentPath(string path)
	{
		if (string.IsNullOrEmpty(path))
			return string.Empty;

		int last = path.LastIndexOf('.');

		if (last > 0 && path[^1] == ']')
			last = path.LastIndexOf('.', last - 1);

		if (last == -1)
			return null;

		return path[..last];
	}

	public static SerializedProperty GetParent(this SerializedProperty property, int levels = 1)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		for (int i = 0; i < levels; ++i)
		{
			string path = property.propertyPath;
			int    last = path.LastIndexOf('.');

			property = property.serializedObject.FindProperty(path[..last]);

			if (property == null)
				break;
		}

		return property;
	}

	public static SerializedProperty GetParent(this SerializedProperty property, string name)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		if (!string.IsNullOrEmpty(name))
		{
			string path = property.propertyPath;
			int index = path.LastIndexOf(name);

			if (index > -1)
			{
				int end = path.IndexOf('.', index);
				return property.serializedObject.FindProperty(path[..end]);
			}
		}

		return null;
	}

	public static int CountSurfaceChildren(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		SerializedProperty child = property.Copy();
		int children = 0;

		if (child.NextVisible(true))
		{
			do
			{
				if (!child.IsChildOf(property))
					return children;

				if (child.IsSurfaceChildOf(property))
					++children;
			}
			while (child.NextVisible(false));
		}

		return children;
	}

	public static bool IsChildOf(this SerializedProperty property, SerializedProperty parent)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));
		if (parent == null)
			throw new ArgumentNullException(nameof(parent));

		return property.propertyPath.StartsWith(parent.propertyPath);
	}

	public static bool IsSurfaceChildOf(this SerializedProperty property, SerializedProperty parent)
	{
		// If the last period in the child's path is just after the end of the parent's path then it's a surface child
		return IsChildOf(property, parent) && property.propertyPath.LastIndexOf('.') == parent.propertyPath.Length;
	}

	public static void SetValue(this SerializedProperty property, object value)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		FieldInfo field = GetField(property, out object parent);
		field.SetValue(parent, value);
	}

	public static bool TrySetValue(this SerializedProperty property, object value)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		try
		{
			SetValue(property, value);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static T GetValue<T>(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		object val = GetValue(property);
		return val == null ? default : (T)val;
	}

	public static object GetValue(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));
		
		FieldInfo field = GetField(property, out object parent);

		if (field == null)
			return parent;

		if (parent == null)
			return null;

		return field.GetValue(parent);
	}

	public static FieldInfo GetField(this SerializedProperty property)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		return GetField(property, out _);
	}

	public static FieldInfo GetField(this SerializedProperty property, out object parent)
	{
		if (property == null)
			throw new ArgumentNullException(nameof(property));

		string[] paths = property.propertyPath.Split('.');

		object parentObj = property.serializedObject.targetObject;
		Type type = parentObj.GetType();
		FieldInfo field = null;

		for (int i = 0; i < paths.Length; ++i)
		{
			if (parentObj != null && field != null)
				parentObj = field.GetValue(parentObj);

			if (paths[i] != "Array")
			{
				field = type?.GetField(paths[i], SERIALIZED_FIELD_FLAGS);

				if (field == null)
					break;
				
				type = field.FieldType;
			}
			else
			{
				string dataStr = paths[++i];
				dataStr = dataStr[(dataStr.IndexOf('[') + 1)..dataStr.IndexOf(']')];

				field = null;

				if (int.TryParse(dataStr, out int index))
				{
					if (type.IsArray)
					{
						type = type.GetElementType();

						if (parentObj is Array arr)
							parentObj = index >= arr.Length ? null : arr.GetValue(index);
					}
					else if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
					{
						type = type.GenericTypeArguments[0];

						if (parentObj is IList list)
							parentObj = index >= list.Count ? null : list[index];
					}
				}
			}
		}

		parent = parentObj;
		return field;
	}

    public static void SetDefault(this SerializedProperty prop, bool enterChildren = true)
	{
		switch (prop.propertyType)
		{
			// Primitive properties (any property with an accessor for its value)
			case SerializedPropertyType.AnimationCurve:   prop.animationCurveValue   = default; break;
			case SerializedPropertyType.Boolean:          prop.boolValue             = default; break;
			case SerializedPropertyType.Bounds:           prop.boundsValue           = default; break;
			case SerializedPropertyType.BoundsInt:        prop.boundsIntValue        = default; break;
			case SerializedPropertyType.Color:            prop.colorValue            = default; break;
			case SerializedPropertyType.ExposedReference: prop.exposedReferenceValue = default; break;
			case SerializedPropertyType.Float:            prop.floatValue            = default; break;
			case SerializedPropertyType.Hash128:          prop.hash128Value          = default; break;
			case SerializedPropertyType.ManagedReference: prop.managedReferenceValue = default; break;
			case SerializedPropertyType.ObjectReference:  prop.objectReferenceValue  = default; break;
			case SerializedPropertyType.Quaternion:       prop.quaternionValue       = default; break;
			case SerializedPropertyType.Rect:             prop.rectValue             = default; break;
			case SerializedPropertyType.RectInt:          prop.rectIntValue          = default; break;
			case SerializedPropertyType.String:           prop.stringValue           = default; break;
			case SerializedPropertyType.Vector2:          prop.vector2Value          = default; break;
			case SerializedPropertyType.Vector2Int:       prop.vector2IntValue       = default; break;
			case SerializedPropertyType.Vector3:          prop.vector3Value          = default; break;
			case SerializedPropertyType.Vector3Int:       prop.vector3IntValue       = default; break;
			case SerializedPropertyType.Vector4:          prop.vector4Value          = default; break;

			// Complex properties (Properties that encapsulate more than a direct value)
			case SerializedPropertyType.ArraySize: 
				prop.arraySize = 0; 
				break;
			case SerializedPropertyType.Enum:
				prop.enumValueIndex = 0;
				prop.enumValueFlag  = 0;
				break;
			case SerializedPropertyType.Character:
			case SerializedPropertyType.Integer:
			case SerializedPropertyType.LayerMask: 
				prop.intValue = default; 
				break;
			// Container properties (serializable types that contain serialized fields, e.g. Gradients/Generics/etc.)
			default:
				if (enterChildren)
				{
					SerializedProperty child = prop.Copy();

					while (child.Next(true))
					{
						// If we've moved out of the children of this property then exit
						if (!child.propertyPath.StartsWith(prop.propertyPath))
							break;

						// Pass 'false' so that we don't recursively enter children and unnecessarily default a property multiple times
						child.SetDefault(false);
					}
				}

				break;
		}
	}

	public static float GetHeight(this SerializedProperty prop)
	{
		return prop != null ? EditorGUI.GetPropertyHeight(prop) : 0.0f;
	}


	/// <summary>
	/// Get a Gradient from a SerializedProperty of a Gradient
	/// </summary>
	/// <param name="gradientProperty"></param>
	/// <returns></returns>
	public static Gradient GetGradient(SerializedProperty gradientProperty)
	{
		System.Reflection.PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty("gradientValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (propertyInfo == null) { return null; }
		else { return propertyInfo.GetValue(gradientProperty, null) as Gradient; }
	}


	public static void SetValue<T>(this SerializedProperty prop, string propIdentifier, T value)
	{
		System.Reflection.PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(propIdentifier, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (propertyInfo == null) { return; }
		else { propertyInfo.SetValue(prop, value); }
	}



	#endregion
}
