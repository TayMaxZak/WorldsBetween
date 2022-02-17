using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockyNoiseModifier : NoiseModifier
{
	private float divideScale;

	private int minDivide;
	private int maxDivide;

	private float diagStrength;
	private Vector3 diagScale;

	public BlockyNoiseModifier(bool addOrSub, float strength, Vector3 scale, float divideScale, int minDivide, int maxDivide, float diagStrength, Vector3 diagScale) : base(addOrSub, strength, scale)
	{
		this.divideScale = divideScale;

		this.minDivide = minDivide;
		this.maxDivide = maxDivide;

		this.diagStrength = diagStrength;
		this.diagScale = diagScale;
	}

	protected override Vector3 WarpPosition(Vector3 pos)
	{
		float divNoise = GetNoiseAt(Utils.Scale(pos, scale) * divideScale);
		int div = (int)(minDivide + (divNoise * (maxDivide - minDivide)));

		float diagNoise = GetNoiseAt(Utils.Scale(Utils.Scale(pos, diagScale), scale));
		float diag = diagStrength * Utils.Sum(pos) * (2 * diagNoise - 1);

		pos = new Vector3(
			div * ((int)(pos.x + diag) / div),
			div * ((int)(pos.y + diag) / div),
			div * ((int)(pos.z + diag) / div)
		);

		return pos;
	}
}
