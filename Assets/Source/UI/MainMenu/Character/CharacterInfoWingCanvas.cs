using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using MEC;

public class CharacterInfoWingCanvas : MonoBehaviour
{
	public static CharacterInfoWingCanvas instance;

	public GameObject needGroupObject;
	public GameObject contentGroupObject;

	public Transform wingOptionTextTransform;

	public GameObject noWingGroupObject;
	public GameObject wingStatusGroupObject;
	public GameObject attackSpeedGroupObject;
	public Transform attackSpeedTextTransform;
	public Text attackSpeedValueText;
	public GameObject criticalRateGroupObject;
	public Transform criticalRateTextTransform;
	public Text criticalRateValueText;
	public GameObject criticalDamageGroupObject;
	public Transform criticalDamageTextTransform;
	public Text criticalDamageValueText;

	public GameObject switchGroupObject;
	public SwitchAnim hideSwitch;
	public Text hideOnOffText;

	public Text changeWingText;
	public Transform changeWingTextTransform;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Transform changeLookTextTransform;
	public Image lookPriceButtonImage;
	public Text lookPriceText;
	public Coffee.UIExtensions.UIEffect lookPriceGrayscaleEffect;

	public Transform changeOptionTextTransform;
	public Image optionPriceButtonImage;
	public Text optionPriceText;
	public Coffee.UIExtensions.UIEffect optionPriceGrayscaleEffect;

	public enum eStatsType
	{
		AttackSpeed,
		CriticalRate,
		CriticalDamage,

		Amount,
	}

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	CharacterData _characterData;
	bool _hasWing;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		_actorId = actorId;
		_characterData = characterData;
		if (characterData.limitBreakLevel <= 2)
		{
			needGroupObject.SetActive(true);
			contentGroupObject.SetActive(false);
			return;
		}
		needGroupObject.SetActive(false);
		contentGroupObject.SetActive(true);

		// 먼저 처음 날개 붙이는건지 확인
		bool hasWing = characterData.HasWing();
		noWingGroupObject.SetActive(!hasWing);
		wingStatusGroupObject.SetActive(hasWing);
		switchGroupObject.SetActive(hasWing);

		if (hasWing)
		{
			List<ObscuredInt> listWingGradeId = characterData.listWingGradeId;
			for (int i = 0; i < listWingGradeId.Count; ++i)
			{
				int grade = listWingGradeId[i];
				switch (i)
				{
					case (int)eStatsType.AttackSpeed:
						attackSpeedGroupObject.SetActive(grade > 0);
						if (grade > 0)
						{
							WingPowerTableData wingPowerTableData = TableDataManager.instance.FindWingPowerTableData(i, grade);
							if (wingPowerTableData != null)
								attackSpeedValueText.text = string.Format("<color=#{0}>{1}</color>", wingPowerTableData.colorDex, UIString.instance.GetString(wingPowerTableData.gradeName));
							_attackSpeedWingPowerTableData = wingPowerTableData;
						}
						break;
					case (int)eStatsType.CriticalRate:
						criticalRateGroupObject.SetActive(grade > 0);
						if (grade > 0)
						{
							WingPowerTableData wingPowerTableData = TableDataManager.instance.FindWingPowerTableData(i, grade);
							if (wingPowerTableData != null)
								criticalRateValueText.text = string.Format("<color=#{0}>{1}</color>", wingPowerTableData.colorDex, UIString.instance.GetString(wingPowerTableData.gradeName));
							_criticalRateWingPowerTableData = wingPowerTableData;
						}
						break;
					case (int)eStatsType.CriticalDamage:
						criticalDamageGroupObject.SetActive(grade > 0);
						if (grade > 0)
						{
							WingPowerTableData wingPowerTableData = TableDataManager.instance.FindWingPowerTableData(i, grade);
							if (wingPowerTableData != null)
								criticalDamageValueText.text = string.Format("<color=#{0}>{1}</color>", wingPowerTableData.colorDex, UIString.instance.GetString(wingPowerTableData.gradeName));
							_criticalDamageWingPowerTableData = wingPowerTableData;
						}
						break;
				}
			}

			hideSwitch.isOn = characterData.wingHide;
		}

		changeWingText.SetLocalizedText(UIString.instance.GetString(hasWing ? "GameUI_ChangeWings" : "GameUI_CreateWings"));
		int requiredDia = 0;
		_changeWingPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsChange");
		priceText.text = requiredDia.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < requiredDia);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		_changeWingLookPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsLook");
		lookPriceText.text = requiredDia.ToString("N0");
		disablePrice = (hasWing == false || CurrencyData.instance.dia < requiredDia);
		lookPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		lookPriceText.color = !disablePrice ? Color.white : Color.gray;
		lookPriceGrayscaleEffect.enabled = disablePrice;

		_changeWingOptionPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsAbility");
		optionPriceText.text = requiredDia.ToString("N0");
		disablePrice = (hasWing == false || CurrencyData.instance.dia < requiredDia);
		optionPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		optionPriceText.color = !disablePrice ? Color.white : Color.gray;
		optionPriceGrayscaleEffect.enabled = disablePrice;

		_hasWing = hasWing;
	}
	#endregion

	WingPowerTableData _attackSpeedWingPowerTableData;
	public void OnClickAttackSpeedTextButton()
	{
		if (_attackSpeedWingPowerTableData == null)
			return;

		string firstText = string.Format("{0} {1} : {2:0.##}%", UIString.instance.GetString("GameUI_WingsAttackSpeedMore"),
			UIString.instance.GetString(_attackSpeedWingPowerTableData.gradeName), _attackSpeedWingPowerTableData.value1 * 100.0f);
		string secondText = GetGradeValueText(_attackSpeedWingPowerTableData.wingType);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", firstText, secondText), 250, attackSpeedTextTransform, new Vector2(30.0f, -35.0f));
	}

	WingPowerTableData _criticalRateWingPowerTableData;
	public void OnClickCriticalRateTextButton()
	{
		if (_criticalRateWingPowerTableData == null)
			return;

		string firstText = string.Format("{0} {1} : {2:0.##}%", UIString.instance.GetString("GameUI_WingsCriticalChanceMore"),
			UIString.instance.GetString(_criticalRateWingPowerTableData.gradeName), _criticalRateWingPowerTableData.value1 * 100.0f);
		string secondText = GetGradeValueText(_criticalRateWingPowerTableData.wingType);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", firstText, secondText), 250, criticalRateTextTransform, new Vector2(30.0f, -35.0f));
	}

	WingPowerTableData _criticalDamageWingPowerTableData;
	public void OnClickCriticalDamageTextButton()
	{
		if (_criticalDamageWingPowerTableData == null)
			return;

		string firstText = string.Format("{0} {1} : {2:0.##}%", UIString.instance.GetString("GameUI_WingsCriticalDamageMore"),
			UIString.instance.GetString(_criticalDamageWingPowerTableData.gradeName), _criticalDamageWingPowerTableData.value1 * 100.0f);
		string secondText = GetGradeValueText(_criticalDamageWingPowerTableData.wingType);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", firstText, secondText), 250, criticalDamageTextTransform, new Vector2(30.0f, -35.0f));
	}

	StringBuilder _stringBuilderGrade = new StringBuilder();
	string GetGradeValueText(int wingType)
	{
		int threeCount = 0;
		_stringBuilderGrade.Remove(0, _stringBuilderGrade.Length);
		for (int i = 0; i < TableDataManager.instance.wingPowerTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.wingPowerTable.dataArray[i].wingType != wingType)
				continue;

			_stringBuilderGrade.Append(UIString.instance.GetString(TableDataManager.instance.wingPowerTable.dataArray[i].gradeName));
			_stringBuilderGrade.Append(" : ");
			_stringBuilderGrade.AppendFormat("{0:0.##}%", TableDataManager.instance.wingPowerTable.dataArray[i].value1 * 100.0f);
			if (threeCount < 2)
			{
				_stringBuilderGrade.Append(" / ");
				threeCount += 1;
			}
			else
			{
				_stringBuilderGrade.Append("\n");
				threeCount = 0;
			}
		}
		int lastEnterIndex = _stringBuilderGrade.ToString().LastIndexOf("\n");
		if (lastEnterIndex == _stringBuilderGrade.Length - 1) _stringBuilderGrade.Remove(_stringBuilderGrade.Length - 1, 1);
		return _stringBuilderGrade.ToString();
	}

	bool _notUserSetting = false;
	public void OnSwitchOnHide()
	{
		hideOnOffText.text = "ON";
		hideOnOffText.color = Color.white;

		if (_notUserSetting)
			return;

		PlayFabApiManager.instance.RequestHideWing(_characterData, true, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_HideWingsOn"), 2.0f);
		});
	}

	public void OnSwitchOffHide()
	{
		hideOnOffText.text = "OFF";
		hideOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;

		PlayFabApiManager.instance.RequestHideWing(_characterData, false, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_HideWingsOff"), 2.0f);
		});
	}

	public void OnClickWingOptionTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("GameUI_WingsOptionsMore"), 250, wingOptionTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickChangeWingTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("GameUI_ChangeWingsMore"), 250, changeWingTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickChangeWingLookTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("GameUI_ChangeWingsLookMore"), 250, changeLookTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickChangeWingOptionTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("GameUI_ChangeWingsOptionsMore"), 250, changeOptionTextTransform, new Vector2(10.0f, -35.0f));
	}


	int _changeWingPrice;
	public void OnClickChangeButton()
	{
		if (CurrencyData.instance.dia < _changeWingPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		string stringId = _hasWing ? "GameUI_ChangeWingsConfirm" : "GameUI_CreateWingsConfirm";
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(stringId), CurrencyData.eCurrencyType.Diamond, _changeWingPrice, false, () =>
			{
				RequestChangeWing(0, _changeWingPrice);
			});
		});
	}

	int _changeWingLookPrice;
	public void OnClickChangeLookButton()
	{
		if (_hasWing == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_WingsCreateFirst"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _changeWingLookPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ChangeWingsLookConfirm"), CurrencyData.eCurrencyType.Diamond, _changeWingPrice, false, () =>
			{
				RequestChangeWing(1, _changeWingLookPrice);
			});
		});
	}

	int _changeWingOptionPrice;
	public void OnClickChangeOptionButton()
	{
		if (_hasWing == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_WingsCreateFirst"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _changeWingOptionPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ChangeWingsOptionsConfirm"), CurrencyData.eCurrencyType.Diamond, _changeWingPrice, false, () =>
			{
				RequestChangeWing(2, _changeWingOptionPrice);
			});
		});
	}

	void RequestChangeWing(int changeType, int price)
	{
		int wingLookId = 0;
		int gradeIndex0 = 0;
		int gradeIndex1 = 0;
		int gradeIndex2 = 0;

		if (changeType == 0 || changeType == 1)
			wingLookId = GetWingLookId();
		if (changeType == 0 || changeType == 2)
			GetWingPowerId(ref gradeIndex0, ref gradeIndex1, ref gradeIndex2);

		if (CheatingListener.detectedCheatTable)
			return;

		PlayFabApiManager.instance.RequestChangeWing(_characterData, changeType, wingLookId, gradeIndex0, gradeIndex1, gradeIndex2, price, () =>
		{
			ConfirmSpendCanvas.instance.gameObject.SetActive(false);
			OnRecvChangeWing();
		});
	}

	void OnRecvChangeWing()
	{
		CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
		Timing.RunCoroutine(ChangeWingProcess());
	}

	IEnumerator<float> ChangeWingProcess()
	{
		// 인풋 차단
		CharacterInfoCanvas.instance.inputLockObject.SetActive(true);

		// Refresh
		RefreshInfo();
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_WingsChangeDone"), 2.0f);
		yield return Timing.WaitForSeconds(1.0f);

		// 인풋 복구
		CharacterInfoCanvas.instance.inputLockObject.SetActive(false);
	}




	#region Drop
	class RandomWingLookInfo
	{
		public WingLookTableData wingLookTableData;
		public float sumWeight;
	}
	List<RandomWingLookInfo> _listRandomWingLookInfo = null;
	int GetWingLookId()
	{
		if (_listRandomWingLookInfo == null)
			_listRandomWingLookInfo = new List<RandomWingLookInfo>();
		_listRandomWingLookInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.wingLookTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.wingLookTable.dataArray[i].weight;
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomWingLookInfo newInfo = new RandomWingLookInfo();
			newInfo.wingLookTableData = TableDataManager.instance.wingLookTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomWingLookInfo.Add(newInfo);
		}

		if (_listRandomWingLookInfo.Count == 0)
			return 0;

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, sumWeight);
		for (int i = 0; i < _listRandomWingLookInfo.Count; ++i)
		{
			if (random <= _listRandomWingLookInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return 0;

		return _listRandomWingLookInfo[index].wingLookTableData.wingLookId;
	}

	class RandomWingPowerInfo
	{
		public WingPowerTableData wingPowerTableData;
		public float sumWeight;
	}
	List<RandomWingPowerInfo> _listRandomWingPowerInfo = null;
	void GetWingPowerId(ref int gradeIndex0, ref int gradeIndex1, ref int gradeIndex2)
	{
		// 먼저 수량을 굴려본다.
		int optionCount = UnityEngine.Random.Range(1, 4);
		if (optionCount == 3)
			GetWingPowerIdAllCount(ref gradeIndex0, ref gradeIndex1, ref gradeIndex2);
		else
			GetWingPowerIdPart(optionCount, ref gradeIndex0, ref gradeIndex1, ref gradeIndex2);
	}

	void GetWingPowerIdAllCount(ref int gradeIndex0, ref int gradeIndex1, ref int gradeIndex2)
	{
		for (int j = 0; j < 3; ++j)
		{
			if (_listRandomWingPowerInfo == null)
				_listRandomWingPowerInfo = new List<RandomWingPowerInfo>();
			_listRandomWingPowerInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.wingPowerTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.wingPowerTable.dataArray[i].weight;
				if (weight <= 0.0f)
					continue;

				// 자기 타입에 맞는거만 넣고 랜덤을 돌린다.
				if (j != TableDataManager.instance.wingPowerTable.dataArray[i].wingType)
					continue;

				// weight 검증
				if (TableDataManager.instance.wingPowerTable.dataArray[i].grade >= 16 && weight > 1.0f)
					CheatingListener.OnDetectCheatTable();

				sumWeight += weight;
				RandomWingPowerInfo newInfo = new RandomWingPowerInfo();
				newInfo.wingPowerTableData = TableDataManager.instance.wingPowerTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomWingPowerInfo.Add(newInfo);
			}

			if (_listRandomWingPowerInfo.Count == 0)
				continue;

			int index = -1;
			float random = UnityEngine.Random.Range(0.0f, sumWeight);
			for (int i = 0; i < _listRandomWingPowerInfo.Count; ++i)
			{
				if (random <= _listRandomWingPowerInfo[i].sumWeight)
				{
					index = i;
					break;
				}
			}
			if (index == -1)
				continue;

			switch (j)
			{
				case 0: gradeIndex0 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
				case 1: gradeIndex1 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
				case 2: gradeIndex2 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
			}
		}
	}

	void GetWingPowerIdPart(int optionCount, ref int gradeIndex0, ref int gradeIndex1, ref int gradeIndex2)
	{
		int _selectedType = -1;
		for (int j = 0; j < optionCount; ++j)
		{
			if (_listRandomWingPowerInfo == null)
				_listRandomWingPowerInfo = new List<RandomWingPowerInfo>();
			_listRandomWingPowerInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.wingPowerTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.wingPowerTable.dataArray[i].weight;
				if (weight <= 0.0f)
					continue;

				// 한번 선택한 타입은 제외하고 랜덤 목록에 넣는다. 처음엔 다 들어간다.
				if (_selectedType == TableDataManager.instance.wingPowerTable.dataArray[i].wingType)
					continue;

				// weight 검증
				if (TableDataManager.instance.wingPowerTable.dataArray[i].grade >= 16 && weight > 1.0f)
					CheatingListener.OnDetectCheatTable();

				sumWeight += weight;
				RandomWingPowerInfo newInfo = new RandomWingPowerInfo();
				newInfo.wingPowerTableData = TableDataManager.instance.wingPowerTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomWingPowerInfo.Add(newInfo);
			}

			if (_listRandomWingPowerInfo.Count == 0)
				continue;

			int index = -1;
			float random = UnityEngine.Random.Range(0.0f, sumWeight);
			for (int i = 0; i < _listRandomWingPowerInfo.Count; ++i)
			{
				if (random <= _listRandomWingPowerInfo[i].sumWeight)
				{
					index = i;
					break;
				}
			}
			if (index == -1)
				continue;

			switch (_listRandomWingPowerInfo[index].wingPowerTableData.wingType)
			{
				case 0: gradeIndex0 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
				case 1: gradeIndex1 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
				case 2: gradeIndex2 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
			}
			_selectedType = _listRandomWingPowerInfo[index].wingPowerTableData.wingType;
		}
	}
	#endregion
}