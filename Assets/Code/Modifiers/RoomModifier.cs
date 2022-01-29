using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomModifier : Modifier
{
	public enum RoomType
	{
		PedestalRoom,
		Skylight,
		Corridor,
		CornerRoom
	}
	public RoomType roomType;

	[HideInInspector]
	public Bounds bounds;

	public override ModifierOutput OutputAt(float x, float y, float z)
	{
		bool inside = bounds.Contains(new Vector3(x, y, z));

		bool addOrSub = false;

		switch (roomType)
		{
			case RoomType.PedestalRoom:
				{
					// Make pedestal in middle
					bool pedestalL = (y > bounds.min.y && y < bounds.min.y + 1) && (Mathf.Abs(x - bounds.center.x) < 3 && Mathf.Abs(z - bounds.center.z) < 3);
					bool pedestalH = (y > bounds.min.y + 1 && y < bounds.min.y + 2) && (Mathf.Abs(x - bounds.center.x) < 2 && Mathf.Abs(z - bounds.center.z) < 2);

					addOrSub = pedestalL || pedestalH;
				}
				break;
			case RoomType.Skylight:
				break;
			case RoomType.Corridor:
				{
					if (y > bounds.min.y && y < bounds.min.y + 1)
					{
						bool closeToWall = Mathf.Abs(x - bounds.min.x) < 1 || Mathf.Abs(x - bounds.max.x) < 1 || Mathf.Abs(z - bounds.min.z) < 1 || Mathf.Abs(z - bounds.max.z) < 1;

						// Stack rubble up
						if (SeedlessRandom.NextFloat() < (closeToWall ? 0.2f : 0.04f) && !World.GetBlockFor((int)x, (int)y - 1, (int)z).IsAir())
						{
							addOrSub = true;
						}
					}
				}
				break;
			case RoomType.CornerRoom:
				break;
			default:
				break;
		}

		return new ModifierOutput { passed = inside, addOrSub = addOrSub };
	}
}
