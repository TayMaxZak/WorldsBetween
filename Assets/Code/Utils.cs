using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
	public static Color colorOrange = new Color(1, 0.5f, 0);
	public static Color colorBlue = new Color(0, 0.25f, 1);

	public static Color colorDarkGrayBlue = new Color(0.1f, 0.125f, 0.2f);

	public static int DistanceSqr(int xa, int ya, int za, int xb, int yb, int zb)
	{
		//return (int)Vector3.SqrMagnitude(new Vector3(xa - xb, ya - yb, za - zb));
		return (xa - xb) * (xa - xb) + (ya - yb) * (ya - yb) + (za - zb) * (za - zb);
	}
}
