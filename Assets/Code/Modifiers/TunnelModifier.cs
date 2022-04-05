using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TunnelModifier : Modifier
{
	public Vector3 posA = Vector3.zero;
	public Vector3 posB = Vector3.zero;
	public float radius = 1;

	public float radiusNoiseAmt = 0;
	public Vector3 radiusNoiseScale = Vector3.one;

	public Vector3 offsetNoiseAmt = Vector3.one;
	public Vector3 offsetNoiseScale = Vector3.one;


	private Vector3 randomOffsetR = Vector3.zero;

	private Vector3 randomOffsetX = Vector3.zero;
	private Vector3 randomOffsetY = Vector3.zero;
	private Vector3 randomOffsetZ = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public TunnelModifier(float radius, Vector3 posA, Vector3 posB, float radiusNoiseAmt, Vector3 radiusNoiseScale, Vector3 offsetNoiseAmt, Vector3 offsetNoiseScale)
	{
		this.radius = radius;
		this.posA = posA;
		this.posB = posB;

		this.radiusNoiseAmt = radiusNoiseAmt;
		this.radiusNoiseScale = radiusNoiseScale;

		this.offsetNoiseAmt = offsetNoiseAmt;
		this.offsetNoiseScale = offsetNoiseScale;
	}

	public override bool Init()
	{
		base.Init();

		SeedNoise(ref randomOffsetR);
		SeedNoise(ref randomOffsetX);
		SeedNoise(ref randomOffsetY);
		SeedNoise(ref randomOffsetZ);

		return true;
	}

	protected void SeedNoise(ref Vector3 offset)
	{
		float offsetAmount = 999999;

		offset = new Vector3(
			Random.value + (int)(Random.value * offsetAmount),
			Random.value + (int)(Random.value * offsetAmount),
			Random.value + (int)(Random.value * offsetAmount)
		);
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyTunnel;

		ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual void ApplyTunnel(Vector3Int pos, Chunk chunk)
	{
		float distanceSqr = GetSqrDistanceAt(WarpPosition(pos), posA, posB);

		float noise = GetNoiseAt(GetClosestLinePoint(pos), radiusNoiseScale, randomOffsetR);
		float radiusMult = Mathf.Lerp(
			1 / radiusNoiseAmt,
			radiusNoiseAmt,
			noise * noise
		);

		float modifiedRadius = radius * radiusMult;

		if (distanceSqr < modifiedRadius * modifiedRadius)
		{
			World.SetBlock(pos.x, pos.y, pos.z, BlockList.EMPTY);
		}
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		Vector3 linePos = GetClosestLinePoint(pos);

		pos = new Vector3(
			pos.x + offsetNoiseAmt.x * (2 * GetNoiseAt(linePos, offsetNoiseScale, randomOffsetX) - 1),
			pos.y + offsetNoiseAmt.y * (2 * GetNoiseAt(linePos, offsetNoiseScale, randomOffsetY) - 1),
			pos.z + offsetNoiseAmt.z * (2 * GetNoiseAt(linePos, offsetNoiseScale, randomOffsetZ) - 1)
		);

		return pos;
	}

	protected float GetLineTValue(Vector3 testPoint)
	{
		float lineLengthSqr = Vector3.SqrMagnitude(posA - posB);
		if (lineLengthSqr == 0)
			return 0;

		float t = ((testPoint.x - posA.x) * (posB.x - posA.x) + (testPoint.y - posA.y) * (posB.y - posA.y) + (testPoint.z - posA.z) * (posB.z - posA.z)) / lineLengthSqr;
		t = Mathf.Clamp01(t);

		return t;
	}

	protected Vector3 GetClosestLinePoint(Vector3 testPoint)
	{
		// t value along line length
		float t = GetLineTValue(testPoint);

		return new Vector3(posA.x + t * (posB.x - posA.x), posA.y + t * (posB.y - posA.y), posA.z + t * (posB.z - posA.z));
	}

	protected float GetSqrDistanceAt(Vector3 testPoint, Vector3 lineA, Vector3 lineB)
	{
		return Vector3.SqrMagnitude(testPoint - GetClosestLinePoint(testPoint));
	}

	protected float GetNoiseAt(Vector3 pos, Vector3 scale, Vector3 offset)
	{
		float x = (pos.x + 0.5f) * scale.x + offset.x;
		float y = (pos.y + 0.5f) * scale.y + offset.y;
		float z = (pos.z + 0.5f) * scale.z + offset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01(1.33f * (xPlane + yPlane + zPlane) / 3f);
		//float noise = Mathf.Clamp01((zPlane));

		return noise;
	}
}
