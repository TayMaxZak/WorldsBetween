using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockyNoiseModifier : NoiseModifier
{
	private float resampleScale;

	private int minDivide;
	private int maxDivide;

	private float diagAmount;

	public BlockyNoiseModifier(bool addOrSub, float strength, Vector3 scale, float resampleScale, int minDivide, int maxDivide, float diagAmount) : base(addOrSub, strength, scale)
	{
		this.resampleScale = resampleScale;

		this.minDivide = minDivide;
		this.maxDivide = maxDivide;

		this.diagAmount = diagAmount;
	}

	protected override Vector3 WarpPosition(Vector3 pos)
	{
		float divNoise = GetNoiseAt(Utils.Scale(pos, scale) * resampleScale);

		int div = (int)(minDivide + (divNoise * (maxDivide - minDivide)));

		float diag = diagAmount * Utils.SumAbs(pos) * (2 * divNoise - 1);

		pos = new Vector3(
			div * ((int)(pos.x + diag) / div),
			div * ((int)(pos.y + diag) / div),
			div * ((int)(pos.z + diag) / div)
		);

		return pos;
	}
}
