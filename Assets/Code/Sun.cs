using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	public DirectionalLightSource lightSource;

	public void Init()
	{
		lightSource = new DirectionalLightSource(lightSource.brightness, lightSource.colorTemp, transform.forward);
	}
}
