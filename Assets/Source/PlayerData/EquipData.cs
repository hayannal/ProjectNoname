using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

public class EquipData
{
	public ObscuredString uniqueId;
	public ObscuredString equipId;

	ObscuredBool _isLock;
	ObscuredInt _enhanceLevel;
	ObscuredFloat _mainOption;
	public bool isLock { get { return _isLock; } }
	public float mainOption { get { return _mainOption; } }
	public int enhanceLevel { get { return _enhanceLevel; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredFloat _mainStatusValue = 0.0f;
	public float mainStatusValue { get { return _mainStatusValue; } }
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	// for AlarmObject
	public bool newEquip { get; set; }

	public class RandomOptionInfo
	{
		public int innerGrade;
		eActorStatus _statusType;
		public eActorStatus statusType
		{
			get
			{
				return _statusType;
			}
			set
			{
				_statusType = value;
				_cachedOptionTableData = null;
			}
		}
		public ObscuredFloat value;

		OptionTableData _cachedOptionTableData = null;
		public OptionTableData cachedOptionTableData
		{
			get
			{
				if (_cachedOptionTableData == null)
					_cachedOptionTableData = TableDataManager.instance.FindOptionTableData(statusType.ToString(), innerGrade);
				return _cachedOptionTableData;
			}
		}

		public float GetRandomStatusRatio()
		{
			float displayMin = 0.0f;
			float displayMax = 0.0f;
			float display = 0.0f;
			switch (statusType)
			{
				case eActorStatus.MaxHp:
					displayMin = ActorStatus.GetDisplayMaxHp(cachedOptionTableData.min);
					displayMax = ActorStatus.GetDisplayMaxHp(cachedOptionTableData.max);
					display = ActorStatus.GetDisplayMaxHp(value);
					return ((display - displayMin) / (displayMax - displayMin));
				case eActorStatus.Attack:
					displayMin = ActorStatus.GetDisplayAttack(cachedOptionTableData.min);
					displayMax = ActorStatus.GetDisplayAttack(cachedOptionTableData.max);
					display = ActorStatus.GetDisplayAttack(value);
					return ((display - displayMin) / (displayMax - displayMin));
				default:
					return ((value - cachedOptionTableData.min) / (cachedOptionTableData.max - cachedOptionTableData.min));
			}
		}
	}
	List<RandomOptionInfo> _listRandomOptionInfo;

	public int optionCount
	{
		get
		{
			if (_listRandomOptionInfo == null)
				return 0;
			return _listRandomOptionInfo.Count;
		}
	}
	ObscuredInt _transmuteRemainCount;
	public int transmuteRemainCount { get { return _transmuteRemainCount; } }

	public static string KeyMainOp = "mainOp";
	public static string KeyLock = "lock";
	public static string KeyEnhan = "enhan";
	public static string KeyRandomOp = "randOp";
	public static string KeyTransmuteRemainCount = "trsmtReCnt";

	public static bool IsUseLegendKey(EquipTableData equipTableData)
	{
		return equipTableData.grade >= 4;
	}

	public static bool IsUseNotStreakGacha(EquipTableData equipTableData)
	{
		return (equipTableData.innerGrade == 4 || equipTableData.innerGrade == 5);
	}

	public void Initialize(Dictionary<string, string> customData)
	{
		bool lockState = false;
		int enhan = 0;
		float mainOp = 0.0f;
		int trsmtCount = 0;
		if (customData.ContainsKey(KeyEnhan))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyEnhan], out intValue))
				enhan = intValue;
		}
		if (customData.ContainsKey(KeyLock))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLock], out intValue))
				lockState = (intValue == 1);
		}
		if (customData.ContainsKey(KeyMainOp))
		{
			float floatValue = 0;
			if (float.TryParse(customData[KeyMainOp], out floatValue))
				mainOp = floatValue;
		}
		bool invalidEquipOption = false;
		int invalidEquipOptionParam2 = 0;
		for (int i = 0; i < RandomOption.RandomOptionCountMax; ++i)
		{
			string optionKey = string.Format("{0}{1}", EquipData.KeyRandomOp, i);
			if (customData.ContainsKey(optionKey))
			{
				string optionValue = customData[optionKey];
				string[] split = optionValue.Split(':');
				if (split == null || split.Length != 2)
					continue;
				eActorStatus randomOptionType = eActorStatus.ExAmount;
				System.Enum.TryParse<eActorStatus>(split[0], out randomOptionType);
				float value = 0.0f;
				float.TryParse(split[1], out value);
				if (randomOptionType != eActorStatus.ExAmount)
				{
					if (_listRandomOptionInfo == null)
						_listRandomOptionInfo = new List<RandomOptionInfo>();

					RandomOptionInfo info = new RandomOptionInfo();
					info.innerGrade = cachedEquipTableData.innerGrade;
					info.statusType = randomOptionType;
					info.value = value;
					if (value < info.cachedOptionTableData.min || value > info.cachedOptionTableData.max)
					{
						invalidEquipOption = true;
						invalidEquipOptionParam2 = i;
					}
					_listRandomOptionInfo.Add(info);
				}
			}
		}

		if (customData.ContainsKey(KeyTransmuteRemainCount))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyTransmuteRemainCount], out intValue))
				trsmtCount = intValue;
		}

		// 데이터 검증
		// 메인옵부터 체크. 메인옵의 범위가 테이블 범위를 넘어섰다면
		if (mainOp < cachedEquipTableData.min || mainOp > cachedEquipTableData.max)
		{
			invalidEquipOption = true;
			invalidEquipOptionParam2 = 100;
			mainOp = cachedEquipTableData.min;
		}
		// 랜덤옵션 카운트가 붙일 수 있는 최대 랜덤옵션 개수보다 많다면
		if (optionCount > cachedEquipTableData.optionCount)
		{
			invalidEquipOption = true;
			invalidEquipOptionParam2 = 101;
		}
		if (invalidEquipOption)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipOption, false, invalidEquipOptionParam2);
		}

		// 타입에 따라 최대 강화레벨이 정해져있는데 그 범위를 넘어섰다면
		bool invalidEquipEnhance = false;
		if (enhan > 0)
		{
			InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(cachedEquipTableData.innerGrade);
			if (enhan > innerGradeTableData.max)
				invalidEquipEnhance = true;
		}
		if (invalidEquipEnhance)
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipEnhance, false, enhan);

		_isLock = lockState;
		_enhanceLevel = enhan;
		_mainOption = mainOp;
		_transmuteRemainCount = trsmtCount;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		// 자리별 보너스 같은건 사라졌지만 UI표기를 위해서라도 따로 가지고 있는게 편하다. 이 캐싱엔 강화 정보까지 포함되어있다.
		_mainStatusValue = _mainOption;
		if (_enhanceLevel > 0)
		{
			EnhanceTableData enhanceTableData = TableDataManager.instance.FindEnhanceTableData(cachedEquipTableData.innerGrade, _enhanceLevel);
			if (enhanceTableData != null)
				_mainStatusValue *= enhanceTableData.multi;
		}

		// 서브 옵션들을 돌면서 equipStatusList에 모아야한다. 같은 옵은 같은 옵션끼리.
		for (int i = 0; i < _equipStatusList.valueList.Length; ++i)
			_equipStatusList.valueList[i] = 0.0f;
	}

	public float GetMainStatusValueMin(int overrideEnhanceLevel = -1)
	{
		int selectedEnhanceLevel = _enhanceLevel;
		if (overrideEnhanceLevel != -1)
			selectedEnhanceLevel = overrideEnhanceLevel;

		float value = cachedEquipTableData.min;
		EnhanceTableData enhanceTableData = TableDataManager.instance.FindEnhanceTableData(cachedEquipTableData.innerGrade, selectedEnhanceLevel);
		if (enhanceTableData != null)
			value *= enhanceTableData.multi;
		return value;
	}

	public float GetMainStatusValueMax(int overrideEnhanceLevel = -1)
	{
		int selectedEnhanceLevel = _enhanceLevel;
		if (overrideEnhanceLevel != -1)
			selectedEnhanceLevel = overrideEnhanceLevel;

		float value = cachedEquipTableData.max;
		EnhanceTableData enhanceTableData = TableDataManager.instance.FindEnhanceTableData(cachedEquipTableData.innerGrade, selectedEnhanceLevel);
		if (enhanceTableData != null)
			value *= enhanceTableData.multi;
		return value;
	}

	public void GetMainStatusDisplayStringByEnhance(int targetEnhance, ref string targetDisplayString)
	{
		float value = _mainOption;
		EnhanceTableData enhanceTableData = TableDataManager.instance.FindEnhanceTableData(cachedEquipTableData.innerGrade, targetEnhance);
		if (enhanceTableData != null)
			value *= enhanceTableData.multi;
		bool fullGauge = (GetMainStatusRatio() == 1.0f);
		float displayValue = ActorStatus.GetDisplayAttack(value);
		string displayString = displayValue.ToString("N0");
		bool adjustValue = false;
		if (!fullGauge)
		{
			string maxDisplayString = ActorStatus.GetDisplayAttack(GetMainStatusValueMax(targetEnhance)).ToString("N0");
			if (displayString == maxDisplayString)
				adjustValue = true;
		}
		targetDisplayString = adjustValue ? (displayValue - 1.0f).ToString("N0") : displayString;
	}

	public void SetLock(bool lockState)
	{
		_isLock = lockState;
	}

	public void OnEnhance(int targetEnhanceLevel)
	{
		_enhanceLevel = targetEnhanceLevel;

		RefreshCachedStatus();

		// 장착된 장비였을 경우엔 TimeSpace에도 알려서 모든 캐릭터를 재계산해야하니 
		if (TimeSpaceData.instance.IsEquipped(this))
			TimeSpaceData.instance.OnChangedEquippedData();
	}

	public void OnAmplifyMain(string mainOptionString)
	{
		// 로그인했을때랑 동일하게 파싱되게 스트링에서 추출하는 형태로 처리한다.
		float floatValue = 0;
		if (float.TryParse(mainOptionString, out floatValue))
			_mainOption = floatValue;

		RefreshCachedStatus();

		if (TimeSpaceData.instance.IsEquipped(this))
			TimeSpaceData.instance.OnChangedEquippedData();
	}

	public void OnTransmute(int randomIndex, string randomOptionString, bool ignoreTransmuteRemainCount)
	{
		string[] split = randomOptionString.Split(':');
		eActorStatus randomOptionType = eActorStatus.ExAmount;
		System.Enum.TryParse<eActorStatus>(split[0], out randomOptionType);
		float value = 0.0f;
		float.TryParse(split[1], out value);
		if (randomOptionType == eActorStatus.ExAmount)
			return;

		if (ignoreTransmuteRemainCount == false)
		{
			_transmuteRemainCount -= 1;
			if (_transmuteRemainCount < 0)
			{
				// something wrong!
				Debug.LogError("OnTransmute Error. Transmute Remain Count is negative.");
				return;
			}
		}

		// 새로 추가되는건 불가능하기때문에 null일리 없다. 없으면 해킹일거다.
		RandomOptionInfo info = GetOption(randomIndex);
		info.statusType = randomOptionType;
		info.value = value;

		RefreshCachedStatus();

		if (TimeSpaceData.instance.IsEquipped(this))
			TimeSpaceData.instance.OnChangedEquippedData();
	}

	public float GetMainStatusRatio()
	{
		float min = GetMainStatusValueMin();
		float max = GetMainStatusValueMax();
		float displayMin = ActorStatus.GetDisplayAttack(min);
		float displayMax = ActorStatus.GetDisplayAttack(max);
		float display = ActorStatus.GetDisplayAttack(mainStatusValue);
		return ((display - displayMin) / (displayMax - displayMin));
	}

	public RandomOptionInfo GetOption(int index)
	{
		if (_listRandomOptionInfo != null && index < _listRandomOptionInfo.Count)
			return _listRandomOptionInfo[index];
		return null;
	}





	EquipTableData _cachedEquipTableData = null;
	public EquipTableData cachedEquipTableData
	{
		get
		{
			if (_cachedEquipTableData == null)
				_cachedEquipTableData = TableDataManager.instance.FindEquipTableData(equipId);
			return _cachedEquipTableData;
		}
	}
}
