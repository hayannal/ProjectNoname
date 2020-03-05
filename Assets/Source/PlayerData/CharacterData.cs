using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.DataModels;

public class CharacterData
{
	public ObscuredString actorId;
	public ObscuredInt powerLevel;
	public EntityKey entityKey;



	public static string GetAddressByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return actorTableData.prefabAddress;
	}

	public static string GetNameByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return UIString.instance.GetString(actorTableData.nameId);
	}
}
