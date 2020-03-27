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

	public bool needLimitBreak
	{
		get
		{
			PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel + 1);
			if (nextPowerLevelTableData == null)
				return false;
			if (limitBreakLevel < nextPowerLevelTableData.requiredLimitBreak)
				return true;
			return false;
		}
	}

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

		// 검증
		bool invalid = false;
		if (pow > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidPowerLevel, false, pow);
			invalid = true;
		}

		PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow);
		if (invalid == false && pp < powerLevelTableData.requiredAccumulatedPowerPoint)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidPp, false, pp);
			invalid = true;
		}

		if (invalid == false && lb != powerLevelTableData.requiredLimitBreak)
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidLimitBreakLevel, false, lb);

		// 추가 데이터. 잠재 염색 날개 등등.
		if (dataObject != null)
		{

		}

		_powerLevel = pow;
		_pp = pp;
		_limitBreakLevel = lb;
	}

	public void OnPowerLevelUp()
	{
		_powerLevel += 1;

		// 캐릭터 데이터가 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}
}