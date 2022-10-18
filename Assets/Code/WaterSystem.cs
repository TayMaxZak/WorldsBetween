using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterSystem : MonoBehaviour
{
	[SerializeField]
	private Sound enterWaterSound;

	[SerializeField]
	private BoxCollider boxCollider;

	[SerializeField]
	private Volume volume;

	private Timer enterWaterTimer = new Timer(0.5f, 0);
	private bool inWater = false;

	private float waterMuffleStrength = 0;

	void Update()
	{
		if (!Player.Instance || !GameManager.IsFinishedLoading())
			return;

		enterWaterTimer.Increment(Time.deltaTime);

		float playerEyesY = Player.Instance.head.transform.position.y;
		float waterSurfaceY = boxCollider.bounds.max.y;

		if (playerEyesY < waterSurfaceY)
		{
			if (!inWater && enterWaterTimer.Expired())
			{
				enterWaterTimer.Reset();

				AudioManager.PlaySound(enterWaterSound, transform.position);
			}

			inWater = true;
		}
		else
		{
			inWater = false;
		}

		float depth = Mathf.Clamp01(((waterSurfaceY + volume.blendDistance) - playerEyesY) / volume.blendDistance);
		waterMuffleStrength = Mathf.Lerp(waterMuffleStrength, depth, Time.deltaTime * 2.5f);

		if (!GameManager.IsFinishingLevel())
			AudioManager.SetWorldEffectsLowpass(1 - waterMuffleStrength * 0.65f);
	}
}
