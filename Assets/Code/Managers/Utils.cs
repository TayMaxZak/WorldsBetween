using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Utils
{
	public static Color colorOrange = new Color(1, 0.5f, 0);
	public static Color colorBlue = new Color(0, 0.25f, 1);

	public static Color colorDarkGrayBlue = new Color(0.1f, 0.125f, 0.2f);
	public static Color colorYellow = new Color(1.0f, 0.95f, 0.1f);

	public static int DistSquared(int xa, int ya, int za, int xb, int yb, int zb)
	{
		return (xa - xb) * (xa - xb) + (ya - yb) * (ya - yb) + (za - zb) * (za - zb);
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

	public static float SoftSign(float val)
	{
		return Mathf.Clamp(val * 10000, -1, 1);
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
