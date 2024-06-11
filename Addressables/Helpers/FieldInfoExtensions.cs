using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

public static class FieldInfoExtensions
{
	#region Constants

	///<summary>
	///  Provides a FieldAttributes mask that matches any field with an attribute that prevents Unity from serializing it:
	///<list>
	///  <br/>- InitOnly (<see langword="readonly"/>)
	///  <br/>- Static (<see langword="static"/>)
	///  <br/>- Literal (<see langword="const"/>)
	///  <br/>- NotSerialized (<see cref="NonSerializedAttribute"/>)
	///</list>
	///</summary>
	///<remarks>
	///  Use the bitwise complement operator '<see langword="~"/>' to invert the mask and match fields which do not have these attributes
	///</remarks>
	public const FieldAttributes NON_SERIALIZABLE_MASK = FieldAttributes.NotSerialized | FieldAttributes.InitOnly |
														 FieldAttributes.Static		   | FieldAttributes.Literal;

	#endregion

	#region Fields

	// Cache the results of IsUnitySerializable since it could potentially be an expensive method and a field shouldn't suddenly become
	//  serializable until a new build, in which case the cache would be cleared
	private static readonly Dictionary<FieldInfo, bool> SerializableCache = new();
		 
	#endregion

	#region Methods

	///<summary>
	///  Checks if a field meets all the requirements to be serialized by Unity but doesn't validate whether its type is serializable or not
	///</summary>
	///<remarks>
	///  If you want to check if a field meets the serialization requirements AND its type is serializable then use <see cref="IsFullyUnitySerializable(FieldInfo)"/>
	///</remarks>
	public static bool IsUnitySerializable(this FieldInfo field)
	{
		if ((field.Attributes & NON_SERIALIZABLE_MASK) != 0)
			return false;

		// If the field meets the attribute requirements but isn't public or marked with [SerializeField] then it won't be serialized
		if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
			return false;

		return true;
	}

	///<summary>
	///  Checks if a field meets all of Unity's serialization requirements and whether its type is serializable or not
	///</summary>
	///<remarks>
	///  If you only care about whether the field's type is serializable and not the field itself then use <see cref="TypeExtensions.IsUnitySerializable(Type)"/>
	///</remarks>
	public static bool IsFullyUnitySerializable(this FieldInfo field)
	{
		if (SerializableCache.TryGetValue(field, out bool serializable))
			return serializable;

		serializable = field.IsUnitySerializable() && field.FieldType.IsUnitySerializable();

		SerializableCache.Add(field, serializable);
		return serializable;
	}

	#endregion
}
