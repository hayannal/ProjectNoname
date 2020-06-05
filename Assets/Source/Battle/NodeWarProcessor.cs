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

		BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.identity;
		BattleInstanceManager.instance.playerActor.cachedTransform.position = Vector3.zero;
		CustomFollowCamera.instance.checkPlaneLeftRightQuad = false;
		CustomFollowCamera.instance.distanceToTarget += 8.0f;
		CustomFollowCamera.instance.followSpeed = 5.0f;
		CustomFollowCamera.instance.immediatelyUpdate = true;
	}

	public override void OnLoadedMap()
	{
		_mapLoaded = true;
	}

	public override void OnSelectedNodeWarLevel(int level)
	{
		Debug.LogFormat("Select Level = {0}", level);
	}
}
