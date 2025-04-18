﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour
{
	public enum eTeamID
	{
		DefaultAlly = 0,
		DefaultMonster = 1,
	}

	public enum eTeamCheckFilter
	{
		Enemy,
		Ally,
		Any,
		//Alliance,
	}

	public enum eTeamLayer
	{
		TEAM0_HITOBJECT_LAYER,
		TEAM1_HITOBJECT_LAYER,
		TEAM0_ACTOR_LAYER,
		TEAM1_ACTOR_LAYER,
		TeamLayer_Amount,
	}
	public static int TEAM0_HITOBJECT_LAYER;
	public static int TEAM1_HITOBJECT_LAYER;
	public static int TEAM0_ACTOR_LAYER;
	public static int TEAM1_ACTOR_LAYER;

	int _teamId;
	public int teamId { get { return _teamId; } }

	public void SetTeamId(int teamId, bool applyChangeLayer = true, GameObject targetObject = null, eTeamLayer teamLayerType = eTeamLayer.TEAM0_HITOBJECT_LAYER, bool recursive = true)
	{
		_teamId = teamId;

		if (applyChangeLayer == false)
			return;

		SetTeamLayer(targetObject, teamLayerType, recursive);
	}

	public static void SetTeamLayer(GameObject targetObject, eTeamLayer teamLayerType, bool recursive = true, bool overrideLayer = false)
	{
		int layer = 0;
		switch (teamLayerType)
		{
			case eTeamLayer.TEAM0_HITOBJECT_LAYER:
				if (TEAM0_HITOBJECT_LAYER == 0) TEAM0_HITOBJECT_LAYER = LayerMask.NameToLayer("Team0HitObject");
				layer = TEAM0_HITOBJECT_LAYER;
				break;
			case eTeamLayer.TEAM1_HITOBJECT_LAYER:
				if (TEAM1_HITOBJECT_LAYER == 0) TEAM1_HITOBJECT_LAYER = LayerMask.NameToLayer("Team1HitObject");
				layer = TEAM1_HITOBJECT_LAYER;
				break;
			case eTeamLayer.TEAM0_ACTOR_LAYER:
				if (TEAM0_ACTOR_LAYER == 0) TEAM0_ACTOR_LAYER = LayerMask.NameToLayer("Team0Actor");
				layer = TEAM0_ACTOR_LAYER;
				break;
			case eTeamLayer.TEAM1_ACTOR_LAYER:
				if (TEAM1_ACTOR_LAYER == 0) TEAM1_ACTOR_LAYER = LayerMask.NameToLayer("Team1Actor");
				layer = TEAM1_ACTOR_LAYER;
				break;
		}

		if (overrideLayer == false && targetObject.layer != 0)
			return;

		if (recursive)
			ObjectUtil.ChangeLayer(targetObject, layer);
		else
			targetObject.layer = layer;
	}

	public static bool CheckTeamFilter(int teamId, Collider target, eTeamCheckFilter filter, bool nullIsEnemy = true)
	{
		if (filter == eTeamCheckFilter.Any)
			return true;
		Team targetTeam = BattleInstanceManager.instance.GetTeamFromCollider(target);
		if (targetTeam == null)
		{
			if (nullIsEnemy && filter == eTeamCheckFilter.Enemy)
				return true;
			return false;
		}

		return CheckTeamFilter(teamId, targetTeam.teamId, filter);
	}

	public static bool CheckTeamFilter(int teamId, int targetTeamId, eTeamCheckFilter filter)
	{
		eTeamCheckFilter filterResult = eTeamCheckFilter.Any;
		if (targetTeamId == teamId)
			filterResult = eTeamCheckFilter.Ally;
		else
			filterResult = eTeamCheckFilter.Enemy;
		if (filterResult == filter)
			return true;
		return false;
	}




	// team-group..
	//

	// Ally process
}
