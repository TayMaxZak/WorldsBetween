using System.Collections;
using System.Collections.Generic;
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
}
