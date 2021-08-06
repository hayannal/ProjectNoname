using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class AnalysisData : MonoBehaviour
{
	public static AnalysisData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("AnalysisData")).AddComponent<AnalysisData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static AnalysisData _instance = null;



	// 초단위로 저장되는 경험치다. 이걸 받아와서 레벨을 구한다.
	public ObscuredInt analysisExp { get; private set; }
	public ObscuredInt analysisLevel { get; private set; }

	// 최초로 한번 분석을 시작하기 전까진 false로 되어있다.
	public ObscuredBool analysisStarted { get; set; }
	public DateTime analysisStartedTime { get; private set; }
	public DateTime analysisCompleteTime { get; private set; }

	void Update()
	{
		UpdateRemainTime();
	}

	public void OnRecvAnalysisData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		analysisExp = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "analysisExp")
			{
				analysisExp = playerStatistics[i].Value;
				break;
			}
		}

		// 경험치를 받는 곳에서 미리 레벨을 계산해둔다. 연구와 달리 1부터 시작하는 구조다.
		analysisLevel = 0;
		RefreshAnalysisLevel();

		analysisStarted = false;
		if (userReadOnlyData.ContainsKey("anlyStrDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["anlyStrDat"].Value) == false)
				OnRecvAnalysisStartInfo(userReadOnlyData["anlyStrDat"].Value);
		}
	}

	public void OnRecvAnalysisStartInfo(string lastAnalysisStartTimeString)
	{
		DateTime lastAnalysisStartTime = new DateTime();
		if (DateTime.TryParse(lastAnalysisStartTimeString, out lastAnalysisStartTime))
		{
			DateTime universalTime = lastAnalysisStartTime.ToUniversalTime();
			analysisStarted = true;
			analysisStartedTime = universalTime;

			AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
			if (analysisTableData != null)
			{
				analysisCompleteTime = analysisStartedTime + TimeSpan.FromSeconds(analysisTableData.maxTime);
				if (ServerTime.UtcNow < analysisCompleteTime)
					_needUpdate = true;
			}
		}
	}

	public void AddExp(int addExp)
	{
		if (addExp == 0)
			return;

		// 일반적인 분석 후 경험치 쌓이는 곳에서 호출된다.
		analysisExp += addExp;
		RefreshAnalysisLevel();
	}

	public void OnLevelUp(int targetLevel)
	{
		// 강제 레벨업 하는 곳에서 호출된다.
		AnalysisTableData targetAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(targetLevel);
		if (targetAnalysisTableData == null)
			return;

		analysisExp = targetAnalysisTableData.requiredAccumulatedTime;
		RefreshAnalysisLevel();
	}

	void RefreshAnalysisLevel()
	{
		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		for (int i = 0; i < TableDataManager.instance.analysisTable.dataArray.Length; ++i)
		{
			if (analysisExp < TableDataManager.instance.analysisTable.dataArray[i].requiredAccumulatedTime)
			{
				analysisLevel = TableDataManager.instance.analysisTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.analysisTable.dataArray[i].level >= maxLevel)
			{
				analysisLevel = maxLevel;
				break;
			}
		}
	}
	
	#region Notification
	const int AnalysisNotificationId = 10002;
	public void ReserveAnalysisNotification()
	{
		if (analysisStarted == false)
			return;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData == null)
			return;
		if (ServerTime.UtcNow > analysisCompleteTime)
			return;

		TimeSpan remainTime = analysisCompleteTime - ServerTime.UtcNow;
		DateTime deliveryTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(remainTime.TotalSeconds);
		MobileNotificationWrapper.instance.SendNotification(AnalysisNotificationId, UIString.instance.GetString("SystemUI_AnalysisFullTitle"), UIString.instance.GetString("SystemUI_AnalysisFullBody"),
			deliveryTime, null, true, "my_custom_icon_id", "my_custom_large_icon_id");
	}

	public void CancelAnalysisNotification()
	{
		MobileNotificationWrapper.instance.CancelPendingNotificationItem(AnalysisNotificationId);
	}
	#endregion

	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		// 로그인 후 분석창 열지 않은 상태에서 완료시 로비 및 DotMainMenu에 알람표시를 하려면 여기서 시간 다됐는지 체크해야만 한다.
		if (analysisStarted == false)
			return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < analysisCompleteTime)
		{
		}
		else
		{
			_needUpdate = false;

			// 로비 Alarm 확인
			if (DotMainMenuCanvas.instance != null)
				DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
			else if (LobbyCanvas.instance != null)
			{
				if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
					LobbyCanvas.instance.RefreshAlarmObject(DotMainMenuCanvas.eButtonType.Research, true);
			}
		}
	}


	int GetRemainAnalysisKey()
	{
		// legend key와 달리 테이블에 따라 감소수치가 달라진다.
		int defaultDecreaseValue = 30;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData != null)
			defaultDecreaseValue = analysisTableData.keySubtract;
		return CurrencyData.instance.analysisKey - DropManager.instance.droppedAnalysisOriginCount * defaultDecreaseValue;
	}



	const string OriginDropId = "Qnstjrdhfl";
	const string DiamondDropId = "Qnstjrqhtjr";
	const string GoldDropId = "Qnstjrrhfem";

	ObscuredInt _cachedSecond = 0;
	ObscuredInt _cachedResultGold = 0;
	ObscuredInt _cachedDropEnergy = 0;
	ObscuredBool _cachedReverted = false;
	public int cachedExpSecond { get { return _cachedSecond; } }
	public int cachedRandomGold { get { return _cachedResultGold; } }
	public int cachedDropEnergy { get { return _cachedDropEnergy; } }
	List<DropProcessor> _listCachedDropProcessor = new List<DropProcessor>();
	public void PrepareAnalysis()
	{
		// UI 막혔을텐데 어떻게 호출한거지
		if (analysisStarted == false)
			return;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData == null)
			return;

		TimeSpan diffTime = ServerTime.UtcNow - analysisStartedTime;
		int totalSeconds = Mathf.Min((int)diffTime.TotalSeconds, analysisTableData.maxTime);
		_cachedSecond = totalSeconds;
		Debug.LogFormat("Analysis Time = {0}", totalSeconds);

		// 쌓아둔 초로 하나씩 체크해봐야한다.
		// 제일 먼저 goldPerTime
		// 시간당 골드로 적혀있으니 초로 변환해서 계산하면 된다.
		float goldPerSec = analysisTableData.goldPerTime / 60.0f / 60.0f;
		float maxGold = goldPerSec * totalSeconds;
		_cachedResultGold = (int)UnityEngine.Random.Range(0.0f, maxGold);
		if (_cachedResultGold < 1)
			_cachedResultGold = 1;

		// period 가 있는 것들은 조금 다르게 처리한다.
		// 쌓아둔 초를 가지고 몇번이나 시도할 수 있는지 판단하면 된다.
		// 이 값이 1보다 크다면 그 횟수만큼 여러번 굴릴 수 있다는거고 1보다 작으면 확률로 굴리게 되는거다.
		float originRate = (float)totalSeconds / analysisTableData.originPeriod;
		int originDropCount = (int)originRate;
		float originDropRate = originRate - originDropCount;

		// 이제 1보다 작은 경우를 굴린담에 통과하면 드랍을 굴려야하는데
		// 한가지 처리해야할게 남아있다. 바로 legendKey와 비슷한 일을 하는 analysisKey다.
		// 이게 점점 줄어들수록 이 확률 역시 줄어들어야한다.
		if (originDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= originDropRate)
			{
				// 확률 검사를 통과하면 dropCount를 1회 올린다.
				++originDropCount;
			}
		}
		for (int i = 0; i < originDropCount; ++i)
		{
			float adjustWeight = 0.0f;
			AnalysisKeyTableData analysisKeyTableData = TableDataManager.instance.FindAnalysisKeyTableData(GetRemainAnalysisKey());
			if (analysisKeyTableData != null)
				adjustWeight = analysisKeyTableData.adjustWeight;
			// adjustWeight 검증
			if (adjustWeight > 1.0f)
				CheatingListener.OnDetectCheatTable();
			if (adjustWeight <= 0.0f)
				continue;
			if (UnityEngine.Random.value > adjustWeight)
				continue;

			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, OriginDropId, "", true, true);
			_listCachedDropProcessor.Add(dropProcessor);
		}

		

		// 다음은 에너지인데 에너지 역시 드랍없이 직접 계산하는 형태다.
		_cachedDropEnergy = 0;
		float energyRate = (float)totalSeconds / analysisTableData.energyPeriod;
		int energyDropCount = (int)energyRate;
		float energyDropRate = energyRate - energyDropCount;
		if (energyDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= energyDropRate)
				++energyDropCount;
		}
		for (int i = 0; i < energyDropCount; ++i)
		{
			if (UnityEngine.Random.value < 0.5f)
				continue;
			if (CurrencyData.instance.energy + _cachedDropEnergy >= 30)
			{
				if (UnityEngine.Random.value < 0.4f)
					continue;
			}
			if (CurrencyData.instance.energy + _cachedDropEnergy >= 40)
			{
				if (UnityEngine.Random.value < 0.3f)
					continue;
			}
			if (CurrencyData.instance.energy + _cachedDropEnergy >= 50)
			{
				if (UnityEngine.Random.value < 0.2f)
					continue;
			}
			if (CurrencyData.instance.energy + _cachedDropEnergy >= 60)
			{
				if (UnityEngine.Random.value < 0.1f)
					continue;
			}

			// 4 ~ 6 랜덤 범위
			_cachedDropEnergy += UnityEngine.Random.Range(3, 8);
		}

		// 대신 에너지는 한가지 예외처리가 있는데, 최대치를 넘으면 골드로 환원해줘야한다. 환원 골드는 랜덤골드에 포함시켜둔다.
		// 하려다가 안하기로 한다.
		/*
		_cachedReverted = false;
		int revertAmount = 0;
		if (_cachedDropEnergy > 0 && CurrencyData.instance.energy + _cachedDropEnergy > CurrencyData.instance.energyMax)
		{
			int remainSpace = CurrencyData.instance.energyMax - CurrencyData.instance.energy;
			if (remainSpace > 0)
			{
				revertAmount = _cachedDropEnergy - remainSpace;
				_cachedDropEnergy = remainSpace;
			}
			else
			{
				revertAmount = _cachedDropEnergy;
				_cachedDropEnergy = 0;
			}
			if (revertAmount > 0)
			{
				_cachedResultGold += revertAmount * 100;
				_cachedReverted = true;
			}
		}
		*/

		

		// 다음 두개 다이아와 골드는 확률계산 후 드랍아이디를 통해서 구하는 잭팟 개념이다.
		float bigDiaRate = (float)totalSeconds / analysisTableData.diamondPeriod;
		int bigDiaDropCount = (int)bigDiaRate;
		float bigDiaDropRate = bigDiaRate - bigDiaDropCount;
		if (bigDiaDropRate > 0.0f)
		{
			// 대성공 다이아 골드는 보정같은거 없이 그냥 돌리면 된다.
			if (UnityEngine.Random.value <= bigDiaDropRate)
				++bigDiaDropCount;
		}
		for (int i = 0; i < bigDiaDropCount; ++i)
		{
			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, DiamondDropId, "", true, true);
			_listCachedDropProcessor.Add(dropProcessor);
		}
		// 제대로 뽑혔다면 로비 Dia에 누적되어있을거다. 이걸 전달하면 된다.
		//DropManager.instance.AddLobbyDia


		// 잭팟 골드도 계산해둔다.
		float bigGoldRate = (float)totalSeconds / analysisTableData.goldPeriod;
		int bigGoldDropCount = (int)bigGoldRate;
		float bigGoldDropRate = bigGoldRate - bigGoldDropCount;
		if (bigGoldDropRate > 0.0f)
		{
			// 대성공 다이아 골드는 보정같은거 없이 그냥 돌리면 된다.
			if (UnityEngine.Random.value <= bigGoldDropRate)
				++bigGoldDropCount;
		}
		for (int i = 0; i < bigGoldDropCount; ++i)
		{
			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, GoldDropId, "", true, true);
			_listCachedDropProcessor.Add(dropProcessor);
		}
		// 제대로 뽑혔다면 로비 Dia에 누적되어있을거다. 이걸 전달하면 된다.
		//DropManager.instance.AddLobbyDia


		// 이렇게 계산된 second를 그냥 보내면 안되고 최고레벨 검사는 해놓고 보내야한다.
		AnalysisTableData maxAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		int maxAnalysisExp = maxAnalysisTableData.requiredAccumulatedTime;
		if (analysisExp + _cachedSecond > maxAnalysisExp)
			_cachedSecond = maxAnalysisExp - analysisExp;

		// 패킷 전달한 준비는 끝.
	}

	public void ClearCachedInfo()
	{
		_cachedSecond = 0;
		_cachedResultGold = 0;
		_cachedDropEnergy = 0;
		_cachedReverted = false;

		// 드랍 연출 할것도 아니니 그냥 꺼두면 된다.
		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
			_listCachedDropProcessor[i].gameObject.SetActive(false);

		// Drop으로 인해 누적되어있는 정보 역시 초기화 시켜둔다.
		DropManager.instance.ClearLobbyDropInfo();
	}
}