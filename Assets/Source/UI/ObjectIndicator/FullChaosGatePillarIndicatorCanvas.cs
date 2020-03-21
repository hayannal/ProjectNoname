using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullChaosGatePillarIndicatorCanvas : ObjectIndicatorCanvas
{
	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		InitializeTarget(targetTransform);
	}
}