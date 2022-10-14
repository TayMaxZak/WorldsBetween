using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLight
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

	public ColorFalloff colorFalloff = new ColorFalloff(Color.white, Color.white); // Blends between colors by distance
	public Vector3Int blockPos; // Block pos; actual world position is + 0.5,0.5,0.5
	private float brightness = 1; // Multiplier on color
	private float spread = 1; // How far the light goes
	private float noise = 0; // Randomness of light

	private float flickerAmt = 0;

	public BlockLight(Vector3Int blockPos, ColorFalloff colorFalloff, float flickerAmt = 0)
	{
		this.blockPos = blockPos;
		this.colorFalloff = colorFalloff;
		this.flickerAmt = flickerAmt;
	}

	public Color GetLightColor(float lightStrength)
	{
		Color toReturn = Color.Lerp(colorFalloff.colorClose, colorFalloff.colorFar, 1 - lightStrength);
		toReturn.a = flickerAmt; // Replace alpha channel with custom info
		return toReturn;
	}

	public static ColorFalloff colorWhite = new ColorFalloff(new Color(1, 1f, 1f), new Color(0.4f, 0.6f, 1.0f));

	public static ColorFalloff colorOrange = new ColorFalloff(new Color(1, 0.6f, 0.4f), new Color(1.0f, 0.1f, 0.0f));
	public static ColorFalloff colorGold = new ColorFalloff(new Color(1, 0.85f, 0.4f), new Color(1.0f, 0.2f, 0.0f));

	public static ColorFalloff colorBlue = new ColorFalloff(new Color(0.4f, 0.6f, 1), new Color(0.0f, 0.1f, 1.0f));
	public static ColorFalloff colorCyan = new ColorFalloff(new Color(0.4f, 0.85f, 1), new Color(0.0f, 0.2f, 1.0f));

	public static ColorFalloff colorPink = new ColorFalloff(new Color(1, 0.5f, 0.8f), new Color(1.0f, 0.25f, 0.6f));
	public static ColorFalloff colorPurple = new ColorFalloff(new Color(1, 0.2f, 0.5f), new Color(0.1f, 0.0f, 1.0f));
}
