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



	public ObscuredInt analysisLevel { get; set; }

	// 최초로 한번 분석을 시작하기 전까진 false로 되어있다.
	public ObscuredBool analysisStarted { get; set; }
	public DateTime analysisStartedTime { get; private set; }
	public DateTime analysisCompleteTime { get; private set; }

	void Update()
	{
		UpdateRemainTime();
	}

	public void OnRecvAnalysisData(Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		// 연구와 달리 1부터 시작하는 구조다.
		analysisLevel = 1;
		if (userReadOnlyData.ContainsKey("anlyLv"))
		{
			int intValue = 1;
			if (int.TryParse(userReadOnlyData["anlyLv"].Value, out intValue))
				analysisLevel = intValue;
		}

		analysisStarted = false;
		if (userReadOnlyData.ContainsKey("anlyStrDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["anlyStrDat"].Value) == false)
				OnRecvAnalysisStartInfo(userReadOnlyData["anlyStrDat"].Value);
		}

		if (analysisStarted)
		{
			AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
			if (analysisTableData != null)
			{
				analysisCompleteTime = analysisStartedTime + TimeSpan.FromMinutes(analysisTableData.maxTime);
				if (ServerTime.UtcNow < analysisCompleteTime)
					_needUpdate = true;
			}
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
		}
	}

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

			}
		}
	}
}