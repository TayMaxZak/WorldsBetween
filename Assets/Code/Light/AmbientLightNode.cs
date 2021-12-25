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

	private readonly int nodeSize;

	private readonly int surfaceArea;

	private readonly int volume;

	private Vector3Int centerPos;

	private AmbientLightNode[,,] neighborArray;

	[SerializeField]
	private List<AmbientPoint> points;

	public AmbientLightNode(Vector3Int center, int dimension)
	{
		nodeSize = dimension;

		surfaceArea = nodeSize * nodeSize;

		volume = nodeSize * nodeSize * nodeSize;

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

	public LightingSample Retrieve(Vector3Int position)
	{
		return Retrieve(position, Vector3Int.one);
	}

	public LightingSample Retrieve(Vector3Int position, Vector3 normal)
	{
		float dotFactor = 0.0f;

		float brightness = 0;
		float colorTemp = 0;

		int count = 0;
		int buffer = 0;
		for (int x = 0 + buffer; x < 3 - buffer; x++)
		{
			for (int y = 0 + buffer; y < 3 - buffer; y++)
			{
				for (int z = 0 + buffer; z < 3 - buffer; z++)
				{
					AmbientLightNode amb = neighborArray[x, y, z];

					if (amb == null)
					{
						amb = World.GetChunkFor(centerPos.x + (x - 1) * nodeSize, centerPos.y + (y - 1) * nodeSize, centerPos.z + (z - 1) * nodeSize)?.GetAmbientLightNode();
					}

					// Still null?
					// TODO: Stricter requireAdjacents requirements for this genStage
					// TODO: How to handle neighboring chunks not existing
					if (amb == null)
						continue; // amb = this;
					else
						neighborArray[x, y, z] = amb;

					count++;

					float dist = Mathf.Sqrt(Utils.DistSquared(position.x, position.y, position.z, amb.centerPos.x, amb.centerPos.y, amb.centerPos.z));

					float contribution = 1 - (dist / nodeSize);

					// Too far
					if (contribution <= 0)
						continue;

					foreach (AmbientPoint point in amb.points)
					{
						float dotMult = Mathf.Clamp01(Vector3.Dot(-point.direction, normal));

						brightness += Mathf.Lerp(1, dotMult, dotFactor) * contribution * point.brightness;
					}
				}
			}
		}

		return new LightingSample(brightness, colorTemp);
	}
}
