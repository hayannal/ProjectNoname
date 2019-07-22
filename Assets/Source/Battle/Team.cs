using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour
{
	public int teamID { get; set; }

	public enum eTeamID
	{
		DefaultArmy = 0,
		DefaultMonster = 1,
	}

	public enum eTeamCheckFilter
	{
		Enemy,
		Ally,
		Any,
		//Alliance,
	}

	static public bool CheckTeamFilter(int teamID, Collider target, eTeamCheckFilter filter, bool nullIsEnemy = true)
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

		eTeamCheckFilter filterResult = eTeamCheckFilter.Any;
		if (targetTeam.teamID == teamID)
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
