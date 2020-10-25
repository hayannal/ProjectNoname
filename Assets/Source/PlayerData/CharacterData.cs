﻿using System.Collections;
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
	// 한계돌파와 초월로 나뉘면서 limitBreakPoint는 사라졌다. 대신 초월재료 개수와 초월레벨이 추가되었다.
	ObscuredInt _transcendPoint;
	ObscuredInt _transcendLevel;
	public int powerLevel { get { return _powerLevel; } set { _powerLevel = value; } }
	public int pp { get { return _pp; } set { _pp = value; } }
	public int limitBreakLevel { get { return _limitBreakLevel; } }
	public int transcendPoint { get { return _transcendPoint; } set { _transcendPoint = value; } }
	public int transcendLevel { get { return _transcendLevel; } }

	public static int TranscendMax = 3;

	List<ObscuredInt> _listStatPoint = new List<ObscuredInt>();
	public List<ObscuredInt> listStatPoint { get { return _listStatPoint; } }
	ObscuredInt _trainingValue;
	public int trainingValue { get { return _trainingValue; } }

	ObscuredInt _wingLookId;
	public int wingLookId { get { return _wingLookId; } }
	List<ObscuredInt> _listWingGradeId = new List<ObscuredInt>();
	public List<ObscuredInt> listWingGradeId { get { return _listWingGradeId; } }
	ObscuredInt _wingHide;
	public bool wingHide { get { return _wingHide == 1; } }

	public bool needLimitBreak
	{
		get
		{
			if (powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
				return false;
			PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel + 1);
			if (nextPowerLevelTableData == null)
				return false;
			if (limitBreakLevel < nextPowerLevelTableData.requiredLimitBreak)
				return true;
			return false;
		}
	}

	public int maxPowerLevelOfCurrentLimitBreak
	{
		get
		{
			int max = 0;
			int maxPowerLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel");
			for (int i = powerLevel; i <= maxPowerLevel; ++i)
			{
				PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(i);
				if (powerLevelTableData == null)
					continue;
				if (limitBreakLevel < powerLevelTableData.requiredLimitBreak)
					break;
				max = i;
			}
			return max;
		}
	}

	public int maxPpOfCurrentLimitBreak
	{
		get
		{
			PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(maxPowerLevelOfCurrentLimitBreak);
			if (powerLevelTableData == null)
				return 0;
			return powerLevelTableData.requiredAccumulatedPowerPoint;
		}
	}

	public int maxPp
	{
		get
		{
			int maxPowerLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel");
			PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(maxPowerLevel);
			if (powerLevelTableData == null)
				return 0;
			return powerLevelTableData.requiredAccumulatedPowerPoint;
		}
	}

	public int maxStatPoint
	{
		get
		{
			//int delta = powerLevel - maxPowerLevelWithoutLimitBreak;
			//if (delta < 0) delta = 0;
			//return delta;
			//
			// 한계돌파와 초월로 나뉘면서 스탯 포인트 역시 달라졌다.
			return transcendLevel * 5;
		}
	}

	public int remainStatPoint
	{
		get
		{
			int sumStatPoint = 0;
			for (int i = 0; i < _listStatPoint.Count; ++i)
				sumStatPoint += _listStatPoint[i];
			return maxStatPoint - sumStatPoint;
		}
	}

	#region Alarm
	public bool IsPlusAlarmState()
	{
		// CharacterInfoGrowthCanvas의 RefreshRequired 함수에서 핵심 코드들만 가져왔다.
		if (powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
			return false;

		PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel);
		PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel + 1);
		if (needLimitBreak)
		{
			// 한계돌파와 초월로 나뉘어지면서 한계돌파 대기때는 PlusAlarm이 뜨지 않기로 변경.
			return false;
		}
		else
		{
			int current = pp - powerLevelTableData.requiredAccumulatedPowerPoint;
			if (current < nextPowerLevelTableData.requiredPowerPoint)
				return false;
		}
		return true;
	}

	public bool IsAlarmState()
	{
		if (IsTranscendAlarmState() || IsPotentialAlarmState())
			return true;
		return false;
	}

	public bool IsTranscendAlarmState()
	{
		if (transcendPoint > transcendLevel)
			return true;
		return false;
	}

	public bool IsPotentialAlarmState()
	{
		// 캐릭터에는 잠재로 인해 찍을 포인트가 늘어날때만 진짜 Alarm을 표시한다.
		if (remainStatPoint > 0)
			return true;
		return false;
	}
	#endregion

	public static int FixedCharacterGroupCount = 4;

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

	public static bool IsUseLegendWeight(ActorTableData actorTableData)
	{
		return actorTableData.grade >= 2;
	}


	public void Initialize(Dictionary<string, int> characterStatistics, PlayFabApiManager.CharacterDataEntity1 dataObject)
	{
		int pow = 1;
		int pp = 0;
		int lb = 0;
		int tr = 0;
		int trp = 0;
		if (characterStatistics.ContainsKey("pow"))
			pow = characterStatistics["pow"];
		if (characterStatistics.ContainsKey("pp"))
			pp = characterStatistics["pp"];
		if (characterStatistics.ContainsKey("lb"))
			lb = characterStatistics["lb"];
		if (characterStatistics.ContainsKey("tr"))
			tr = characterStatistics["tr"];
		if (characterStatistics.ContainsKey("trp"))
			trp = characterStatistics["trp"];

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

		if (invalid == false)
		{
			// lbp보다 높거나 2단계 이상 차이나도 이상한거다. - lbp가 삭제되면서 필요없는 코드들 지운다.
			//if (lb > lbp)
			//	invalid = true;
			//if (lbp - lb >= 2)
			//	invalid = true;

			if (invalid == false)
			{
				// 원래는 자신의 limitBreak값과 같은게 기본이지만 10렙일땐 lb가 0일수도 1일수도 있다.
				if (lb != powerLevelTableData.requiredLimitBreak)
				{
					PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow + 1);
					if (nextPowerLevelTableData == null)
						invalid = true;
					else
					{
						if (lb != nextPowerLevelTableData.requiredLimitBreak)
							invalid = true;
					}
				}
				//if (invalid == false)
				//{
				//	// 위 절차를 lbp에 대해서도 해준다.
				//	if (lbp != powerLevelTableData.requiredLimitBreak)
				//	{
				//		PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow + 1);
				//		if (nextPowerLevelTableData == null)
				//			invalid = true;
				//		else
				//		{
				//			if (lbp != nextPowerLevelTableData.requiredLimitBreak)
				//				invalid = true;
				//		}
				//	}
				//}
			}
			if (invalid)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidLimitBreakLevel, false, lb);
		}

		if (invalid == false)
		{
			if (tr > trp)
				invalid = true;
			if (trp > TranscendMax)
				invalid = true;
			if (invalid)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTranscendLevel, false, tr);
		}

		// 추가 데이터. 잠재 염색 날개 등등.
		if (dataObject != null)
		{

		}

		_powerLevel = pow;
		_pp = pp;
		_limitBreakLevel = lb;
		_transcendLevel = tr;
		_transcendPoint = trp;

		// _powerLevel이 저장되고 나서야 파싱할 수 있다.
		_listStatPoint.Clear();
		if (maxStatPoint > 0)
		{
			if (_listStatPoint.Count == 0)
			{
				for (int i = 0; i < (int)CharacterInfoStatsCanvas.eStatsType.Amount; ++i)
					_listStatPoint.Add(0);
			}
			if (characterStatistics.ContainsKey("str"))
				_listStatPoint[0] = characterStatistics["str"];
			if (characterStatistics.ContainsKey("dex"))
				_listStatPoint[1] = characterStatistics["dex"];
			if (characterStatistics.ContainsKey("int"))
				_listStatPoint[2] = characterStatistics["int"];
			if (characterStatistics.ContainsKey("vit"))
				_listStatPoint[3] = characterStatistics["vit"];

			if (invalid == false)
			{
				int sumStatPoint = 0;
				for (int i = 0; i < _listStatPoint.Count; ++i)
					sumStatPoint += _listStatPoint[i];
				if (sumStatPoint > maxStatPoint)
				{
					PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidStatPoint, false, lb);
					invalid = true;
				}
			}
		}

		if (_transcendLevel >= 2)	/////ch 2
		{
			if (characterStatistics.ContainsKey("train"))
				_trainingValue = characterStatistics["train"];
			if (invalid == false && _trainingValue > CharacterInfoTrainingCanvas.TrainingMax)
			{
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTraining, false, lb);
				invalid = true;
			}
		}

		_listWingGradeId.Clear();
		if (_transcendLevel >= 3)	/////ch 3
		{
			if (_listWingGradeId.Count == 0)
			{
				for (int i = 0; i < (int)CharacterInfoWingCanvas.eStatsType.Amount; ++i)
					_listWingGradeId.Add(0);
			}

			if (characterStatistics.ContainsKey("wgLk"))
				_wingLookId = characterStatistics["wgLk"];
			if (characterStatistics.ContainsKey("wgGr0"))
				_listWingGradeId[0] = characterStatistics["wgGr0"];
			if (characterStatistics.ContainsKey("wgGr1"))
				_listWingGradeId[1] = characterStatistics["wgGr1"];
			if (characterStatistics.ContainsKey("wgGr2"))
				_listWingGradeId[2] = characterStatistics["wgGr2"];
			if (characterStatistics.ContainsKey("wgHd"))
				_wingHide = characterStatistics["wgHd"];
		}
	}

	public void OnPowerLevelUp()
	{
		_powerLevel += 1;

		// 캐릭터 데이터가 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}

	public void OnLimitBreak()
	{
		// 한계돌파는 레벨업을 스탯 변화 없이 레벨업을 할 수 있어지는거라서 재계산 안한다.
		_limitBreakLevel += 1;
	}

	public void OnTranscend()
	{
		// 초월 역시 새 메뉴가 확장되는거라 스탯을 재계산하지 않아도 된다.
		_transcendLevel += 1;
	}

	public void OnApplyStats(int strAddPoint, int dexAddPoint, int intAddPoint, int vitAddPoint)
	{
		if (_listStatPoint.Count == 0)
		{
			for (int i = 0; i < (int)CharacterInfoStatsCanvas.eStatsType.Amount; ++i)
				_listStatPoint.Add(0);
		}

		_listStatPoint[0] += strAddPoint;
		_listStatPoint[1] += dexAddPoint;
		_listStatPoint[2] += intAddPoint;
		_listStatPoint[3] += vitAddPoint;

		// powerLevel과 마찬가지고 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}

	public void OnResetStats()
	{
		if (_listStatPoint.Count == 0)
			return;

		for (int i = 0; i < _listStatPoint.Count; ++i)
			_listStatPoint[i] = 0;

		// powerLevel과 마찬가지고 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}

	public void OnTraining(int addTrainingPoint)
	{
		_trainingValue += addTrainingPoint;

		// powerLevel과 마찬가지고 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}

	public bool HasWing()
	{
		bool findGrade = false;
		for (int i = 0; i < _listWingGradeId.Count; ++i)
		{
			if (_listWingGradeId[i] > 0)
			{
				findGrade = true;
				break;
			}
		}
		if (findGrade == false)
			return false;

		if (_wingLookId == 0)
			return false;

		return true;
	}

	public void OnChangeWing(int changeType, int wingLookId, int gradeIndex0, int gradeIndex1, int gradeIndex2)
	{
		if (changeType == 0 || changeType == 1)
			_wingLookId = wingLookId;
		if (changeType == 0 || changeType == 2)
		{
			if (_listWingGradeId.Count == 0)
			{
				for (int i = 0; i < (int)CharacterInfoWingCanvas.eStatsType.Amount; ++i)
					_listWingGradeId.Add(0);
			}
			_listWingGradeId[0] = gradeIndex0;
			_listWingGradeId[1] = gradeIndex1;
			_listWingGradeId[2] = gradeIndex2;

			// powerLevel과 마찬가지고 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
			if (playerActor != null)
				playerActor.actorStatus.InitializeActorStatus();
		}
	}

	public void HideWing(bool hideState)
	{
		_wingHide = hideState ? 1 : 0;
	}
}