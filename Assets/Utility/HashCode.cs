#if UNITY_EDITOR
#define HASHCODE_DEBUG
#endif

using System.Collections.Generic;

/// <summary>
/// Equality comparer for the HashCode class. Pass Instance to the constructor of a dictionary or any other generic container to avoid boxing in comparisons.
/// </summary>
public class HashCodeEqualityComparer : EqualityComparer<HashCode>
{
	public static readonly HashCodeEqualityComparer Instance = new HashCodeEqualityComparer();

	public override bool Equals(HashCode x, HashCode y)
	{
		return x.AsUInt32() == y.AsUInt32();
	}

	public override int GetHashCode(HashCode obj)
	{
		return obj.GetHashCode();
	}
}

public struct HashCode
{
	public static readonly HashCode invalid = new HashCode(0);

#if HASHCODE_DEBUG
	private string debugString;
#endif
	private uint hashCode;

	public HashCode(string input)
	{
		hashCode = 0;
#if HASHCODE_DEBUG
		debugString = null;
#endif
		if (input != null)
		{
			SetFromString(input);
		}
	}

	public HashCode(uint hashedValue)
	{
		hashCode = hashedValue;
#if HASHCODE_DEBUG
		debugString = "N/A";
#endif
	}

	private void SetFromString(string input)
	{
		hashCode = Dbj2Hash(input);
#if HASHCODE_DEBUG
		debugString = input;
#endif
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}

		HashCode otherHash = (HashCode)obj;
		return otherHash.hashCode == hashCode;
	}

	public static bool operator ==(HashCode lhs, HashCode rhs)
	{
		return lhs.hashCode == rhs.hashCode;
	}

	public static bool operator !=(HashCode lhs, HashCode rhs)
	{
		return !(lhs == rhs);
	}

	public static bool operator ==(HashCode lhs, uint rhs)
	{
		return lhs.AsUInt32() == rhs;
	}

	public static bool operator !=(HashCode lhs, uint rhs)
	{
		return !(lhs == rhs);
	}

	public override int GetHashCode()
	{
		return hashCode.GetHashCode();
	}

	private uint Dbj2Hash(string @string)
	{
		uint hash = 5381;

		foreach (char c in @string)
		{
			hash = ((hash << 5) + hash) + c; /* hash * 33 + c */
		}

		return hash;
	}

	public uint GetHashedValue()
	{
		return hashCode;
	}

	public override string ToString()
	{
#if HASHCODE_DEBUG
		return string.Format("[HashCode:{0} => {1}]", hashCode, debugString);
#else
		return string.Format("[HashCode:{0}]", hashCode);
#endif
	}

	public uint AsUInt32()
	{
		return hashCode;
	}
}