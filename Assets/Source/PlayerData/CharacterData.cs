using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.DataModels;

public class CharacterData
{
	public ObscuredString actorId;
	public EntityKey entityKey;

	ObscuredInt _powerLevel;
	ObscuredInt _pp;
	ObscuredInt _limitBreakLevel;
	public int powerLevel { get { return _powerLevel; } set { _powerLevel = value; } }
	public int pp { get { return _pp; } }
	public int limitBreakLevel { get { return _limitBreakLevel; } }



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


	public void Initialize(Dictionary<string, int> CharacterStatistics, PlayFabApiManager.CharacterDataEntity1 dataObject)
	{
		int pow = 1;
		int pp = 0;
		int lb = 0;
		if (CharacterStatistics.ContainsKey("pow"))
			pow = CharacterStatistics["pow"];
		if (CharacterStatistics.ContainsKey("pp"))
			pp = CharacterStatistics["pp"];
		if (CharacterStatistics.ContainsKey("lb"))
			lb = CharacterStatistics["lb"];

		if (dataObject != null)
		{

		}

		_powerLevel = pow;
		_pp = pp;
		_limitBreakLevel = lb;
	}
}
