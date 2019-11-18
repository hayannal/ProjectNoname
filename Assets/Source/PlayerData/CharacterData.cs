using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData
{
	public string actorId;
	public int powerLevel;



	public static string GetAddressByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return actorTableData.prefabAddress;
	}
}
