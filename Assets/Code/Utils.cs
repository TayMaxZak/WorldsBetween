using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Utils
{
	public static Color colorOrange = new Color(1, 0.5f, 0);
	public static Color colorYellow = new Color(1.0f, 0.95f, 0.1f);
	public static Color colorGreen = new Color(0.3f, 1f, 0.15f);
	public static Color colorCyan = new Color(0.1f, 0.75f, 1);
	public static Color colorBlue = new Color(0, 0.25f, 1);
	public static Color colorPurple = new Color(0.4f, 0, 1);

	public static Color colorDarkGrayBlue = new Color(0.1f, 0.125f, 0.2f);


	public static int DistSquared(int xa, int ya, int za, int xb, int yb, int zb)
	{
		return (xa - xb) * (xa - xb) + (ya - yb) * (ya - yb) + (za - zb) * (za - zb);
	}

	public static int DistSquared(Vector3Int a, Vector3Int b)
	{
		return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
	}

	public static int DistManhattan(int xa, int ya, int za, int xb, int yb, int zb)
	{
		return Mathf.Abs(xa - xb) + Mathf.Abs(ya - yb) + Mathf.Abs(za - zb);
	}

	public static Vector3 Scale(Vector3 a, Vector3 b)
	{
		a.Scale(b);
		return a;
	}

	public static Vector3Int Scale(Vector3Int a, Vector3Int b)
	{
		a.Scale(b);
		return a;
	}

	public static int MaxAbs(Vector3Int a)
	{
		return Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
	}

	public static float MaxAbs(Vector3 a)
	{
		return Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
	}

	public static int Sum(Vector3Int a)
	{
		return (a.x) + (a.y) + (a.z);
	}

	public static float Sum(Vector3 a)
	{
		return (a.x) + (a.y) + (a.z);
	}

	public static int AbsSum(Vector3Int a)
	{
		return Mathf.Abs(a.x) + Mathf.Abs(a.y) + Mathf.Abs(a.z);
	}

	public static float SoftSign(float val)
	{
		return Mathf.Clamp(val * 10000, -1, 1);
	}

	public static int ToInt(float val)
	{
		return Mathf.FloorToInt(val);
	}
}

public static class SeedDecoder
{
	public static char CharOfValue(byte i)
	{
		// Digit characters
		if (i >= ValueOfChar('0') && i <= ValueOfChar('9'))
		{
			return (char)(i + '0');
		}
		// Letter characters
		else if (i >= ValueOfChar('A') && i <= ValueOfChar('Z'))
		{
			return (char)(i + 'A' - 10);
		}
		// Invalid character
		else
		{
			Debug.LogWarning("Invalid value " + i + " cannot be displayed as character");

			return '#';
		}
	}

	private static int ValueOfChar(char c)
	{
		// Digit characters
		if (char.IsDigit(c))
		{
			return c - '0';
		}
		// Letter characters
		else if (char.IsLetter(c))
		{
			return c - 'A' + 10;
		}
		// Invalid character
		else
		{
			Debug.LogWarning("Invalid character " + c + " included in seed string");

			return -1;
		}
	}

	public static long StringToLong(string seedAsString)
	{
		int customBase = 36; // 10 digits + 26 letters

		int powerAtPlace = 1; // Exponent increases at each place right to left
		long seedAsNumber = 0;

		for (int i = seedAsString.Length - 1; i >= 0; i--)
		{
			int charVal = ValueOfChar(seedAsString[i]);

			// Input char must be valid in custom base
			if (charVal < 0 || charVal >= customBase)
				return -1;

			// Value of char given its place
			seedAsNumber += charVal * powerAtPlace;
			// Next place has higher power
			powerAtPlace *= customBase;
		}

		return seedAsNumber;
	}

	public static string LongToString(long seedAsNumber)
	{
		int customBase = 36; // 10 digits + 26 letters

		string seedAsString = "";

		int i = 9;
		while (seedAsNumber > 0 && i > 0)
		{
			i--;

			seedAsString = CharOfValue((byte)(seedAsNumber % customBase)) + seedAsString;

			seedAsNumber /= customBase;
		}

		return seedAsString;
	}
}

namespace ExtensionMethods
{
	public static class ComponentExtensions
	{
		public static T GetCopyOf<T>(this Component comp, T other) where T : Component
		{
			Type type = comp.GetType();

			if (type != other.GetType()) return null; // Type mis-match

			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;

			// Prevent copying deprecated properties
			var pinfos = from property in type.GetProperties(flags)
						 where !property.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute))
						 select property;

			foreach (var pinfo in pinfos)
			{
				if (pinfo.CanWrite)
				{
					try
					{
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific
				}
			}
			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos)
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}
			return comp as T;
		}

		public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
		{
			return go.AddComponent<T>().GetCopyOf(toAdd) as T;
		}
	}
}
