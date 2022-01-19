using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	public DirectionalLightSource lightSource;

	public Bounds sourcePoints;

	public void Init()
	{
		lightSource = new DirectionalLightSource(lightSource.brightness, lightSource.colorTemp, transform.forward);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorYellow;
		Gizmos.DrawWireCube(sourcePoints.center, sourcePoints.size);
	}
}
