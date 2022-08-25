using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSource
{
	public struct ColorFalloff
	{
		public Color colorClose;
		public Color colorFar;

		public ColorFalloff(Color colorClose, Color colorFar)
		{
			this.colorClose = colorClose;
			this.colorFar = colorFar;
		}
	}

	public ColorFalloff lightColor = new ColorFalloff(Color.white, Color.white);
	public Vector3Int pos;
	public float brightness = 1; // Multiplier on color
	public float spread = 1; // How far the light goes
	public float noise = 0; // Randomness of light

	public LightSource(Vector3Int pos, ColorFalloff lightColor)
	{
		this.pos = pos;
		this.lightColor = lightColor;
	}

	public Color GetLightColor(float falloff)
	{
		return Color.Lerp(lightColor.colorClose, lightColor.colorFar, 1 - falloff);
	}

	public static ColorFalloff colorWhite = new ColorFalloff(new Color(1, 1f, 1f), new Color(0.3f, 0.3f, 1.0f));

	public static ColorFalloff colorOrange = new ColorFalloff(new Color(1, 0.6f, 0.4f), new Color(1.0f, 0.1f, 0.0f));
	public static ColorFalloff colorGold = new ColorFalloff(new Color(1, 0.85f, 0.4f), new Color(1.0f, 0.2f, 0.0f));

	public static ColorFalloff colorBlue = new ColorFalloff(new Color(0.4f, 0.6f, 1), new Color(0.0f, 0.1f, 1.0f));
	public static ColorFalloff colorCyan = new ColorFalloff(new Color(0.4f, 0.85f, 1), new Color(0.0f, 0.2f, 1.0f));

	public static ColorFalloff colorPink = new ColorFalloff(new Color(1, 0.5f, 0.8f), new Color(1.0f, 0.25f, 0.6f));
	public static ColorFalloff colorPurple = new ColorFalloff(new Color(1, 0.2f, 0.5f), new Color(0.1f, 0.0f, 1.0f));
}
