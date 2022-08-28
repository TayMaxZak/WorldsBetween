using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flashlight : Item
{
	[Header("")] // Separate from Item fields

	public bool on;

	//public float maxIntensity = 1;

	//public float flickerAmount = 0.1f;
	//private Timer flickerTimer = new Timer(0.2f);

	//private float curIntensity;
	//private float lastIntensity;

	public override void Use(UseHow useHow)
	{
		base.Use(useHow);

		on = !on;

		UpdateLight(on);
	}

	// Turn off when put away
	public override void Unequip()
	{
		base.Unequip();

		UpdateLight(false);
	}

	public override void Update()
	{
		base.Update();

		if (!equipped || !on)
			return;

		//flickerTimer.Increment(Time.deltaTime);
		//if (flickerTimer.Expired())
		//{
		//	flickerTimer.Reset();

		//	lastIntensity = curIntensity;
		//	curIntensity = SeedlessRandom.NextFloatInRange(maxIntensity - flickerAmount, maxIntensity);
		//}

		//Player.Instance.flashlight.intensity = Mathf.Lerp(curIntensity, lastIntensity, flickerTimer.currentTime);
	}

	private void UpdateLight(bool display)
	{
		if (display)
		{
			Player.Instance.flashlight.enabled = true;
		}
		else
		{
			Player.Instance.flashlight.enabled = false;
		}
	}
}
