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

	private Vector3Int centerPos;

	private AmbientLightNode[,,] neighborArray;

	[SerializeField]
	private List<AmbientPoint> points;

	public AmbientLightNode(Vector3Int center, int dimension)
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

		centerPos = center;

		neighborArray = new AmbientLightNode[3, 3, 3];
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

	public LightingSample Retrieve(Vector3Int position, Vector3 normal)
	{
		float dotFactor = 0.0f;

		float brightness = 0;
		float colorTemp = 0;

		int count = 0;
		int buffer = 1;
		for (int x = 0 + buffer; x < 3 - buffer; x++)
		{
			for (int y = 0 + buffer; y < 3 - buffer; y++)
			{
				for (int z = 0 + buffer; z < 3 - buffer; z++)
				{
					AmbientLightNode amb = neighborArray[x, y, z];

					if (amb == null)
					{
						amb = World.GetChunkFor(centerPos.x + (x - 1) * size, centerPos.y + (y - 1) * size, centerPos.z + (z - 1) * size)?.GetAmbientLightNode();
					}

					// Still null?
					// TODO: Stricter requireAdjacents requirements for this genStage
					// TODO: How to handle neighboring chunks not existing :(
					if (amb == null)
						continue; // amb = this;
					else
						neighborArray[x, y, z] = amb;

					Vector3 dif = new Vector3((float)(amb.centerPos.x - position.x) / size, (float)(amb.centerPos.y - position.y) / size, (float)(amb.centerPos.z - position.z) / size);
					dif = new Vector3(Mathf.Abs(dif.x), Mathf.Abs(dif.y), Mathf.Abs(dif.z));

					// Too far (further than one chunk size away)
					if (dif.x > 1 || dif.y > 1 || dif.z > 1)
						continue;

					count++;

					float neighborMult = ((1 - dif.x) + (1 - dif.y) + (1 - dif.z)) / 8;

					foreach (AmbientPoint point in amb.points)
					{
						float dotMult = Mathf.Clamp01(Vector3.Dot(-point.direction, normal));

						brightness += Mathf.Lerp(1, dotMult, dotFactor) * neighborMult * point.brightness;
						colorTemp += dotMult * neighborMult * point.colorTemp;
					}
				}
			}
		}

		return new LightingSample(brightness, colorTemp);
	}
}
