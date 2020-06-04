using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarProcessor : BattleModeProcessorBase
{
	bool _mapLoaded = false;

	public override void Update()
	{
	}

	public override void OnStartBattle()
	{
		base.OnStartBattle();

		CustomFollowCamera.instance.checkPlaneLeftRightQuad = false;
		CustomFollowCamera.instance.distanceToTarget += 4.0f;
		CustomFollowCamera.instance.followSpeed = 5.0f;
		CustomFollowCamera.instance.immediatelyUpdate = true;
	}

	public override void OnLoadedMap()
	{
		_mapLoaded = true;
	}
}
