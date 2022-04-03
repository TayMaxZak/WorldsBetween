using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSource
{
	public Color lightColor = Color.white;
	public Vector3Int pos;
	public float intensity = 1;
	public float noise = 0;

	public static Color colorCyan = new Color(0.1f, 0.45f, 1);
	public static Color colorOrange = new Color(1, 0.2f, 0);
	public static Color colorGreen = new Color(0, 1f, 0.6f);

	public static Color colorBlue = new Color(0.05f, 0f, 1);
	public static Color colorRed = new Color(1, 0, 0.05f);
	public static Color colorGold = new Color(1, 0.8f, 0);
}
