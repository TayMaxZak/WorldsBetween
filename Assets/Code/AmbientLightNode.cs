using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AmbientLightNode
{
	[System.Serializable]
	public class AmbientPoint
	{
		public Vector3 direction;
		public float brightness;
		public float colorTemp;

		public AmbientPoint(Vector3 direction)
		{
			this.direction = direction;
			brightness = 0;
			colorTemp = 0;
		}
	}

	private static readonly Vector3Int[] directions = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0),
													new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)};

	private readonly int size;

	private readonly int surfaceArea;

	private readonly int volume;

	[SerializeField]
	private List<AmbientPoint> points;

	public AmbientLightNode(int dimension)
	{
		size = dimension;

		surfaceArea = size * size;

		volume = size * size * size;

		points = new List<AmbientPoint> {
			new AmbientPoint(directions[0]),
			new AmbientPoint(directions[1]),
			new AmbientPoint(directions[2]),
			new AmbientPoint(directions[3]),
			new AmbientPoint(directions[4]),
			new AmbientPoint(directions[5])
		};
	}

	public void Contribute(Vector3 normal, float brightness, float colorTemp)
	{
		foreach (AmbientPoint point in points)
		{
			// TODO: Instead of using surface normal as input, look at the difference in positions between the input surface and the ambient point (avoid obvious backlighting)
			float dotMult = Mathf.Clamp01(Vector3.Dot(point.direction, normal));

			point.brightness = 1 - (1 - point.brightness) * (1 - dotMult * brightness / surfaceArea);
			point.colorTemp += dotMult * colorTemp / surfaceArea;
		}
	}

	public ChunkMesh.LightingSample Retrieve(Vector3 normal)
	{
		float dotFactor = 0.0f;

		float brightness = 0;
		float colorTemp = 0;

		foreach (AmbientPoint point in points)
		{
			float dotMult = Mathf.Clamp01(Vector3.Dot(-point.direction, normal));

			brightness = 1 - (1 - brightness) * (1 - Mathf.Lerp(1, dotMult, dotFactor) * point.brightness);
			colorTemp += dotMult * point.colorTemp;
		}

		return new ChunkMesh.LightingSample(brightness, colorTemp);
	}
}
