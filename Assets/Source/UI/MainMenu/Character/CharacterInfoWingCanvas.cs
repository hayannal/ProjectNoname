﻿using System;
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
	public GameObject spGainGroupObject;
	public Transform spGainTextTransform;
	public Text spGainValueText;

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

	public GameObject effectPrefab;

	public enum eStatsType
	{
		AttackSpeed,
		CriticalRate,
		CriticalDamage,
		SpGain,

		Amount,
	}

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_ignoreStartEvent = true;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	CharacterData _characterData;
	bool _hasWing;
	int _prevWingLookId;
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
		if (characterData.transcendLevel <= 2) /////ch 2
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
					case (int)eStatsType.SpGain:
						spGainGroupObject.SetActive(grade > 0);
						if (grade > 0)
						{
							WingPowerTableData wingPowerTableData = TableDataManager.instance.FindWingPowerTableData(i, grade);
							if (wingPowerTableData != null)
								spGainValueText.text = string.Format("<color=#{0}>{1}</color>", wingPowerTableData.colorDex, UIString.instance.GetString(wingPowerTableData.gradeName));
							_spGainWingPowerTableData = wingPowerTableData;
						}
						break;
				}
			}

			if (hideSwitch.isOn != characterData.wingHide)
			{
				_notUserSetting = true;
				hideSwitch.AnimateSwitch();
				_notUserSetting = false;
			}
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
		_prevWingLookId = characterData.wingLookId;
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

	WingPowerTableData _spGainWingPowerTableData;
	public void OnClickSpGainTextButton()
	{
		if (_spGainWingPowerTableData == null)
			return;

		string firstText = string.Format("{0} {1} : {2:0.##}", UIString.instance.GetString("GameUI_WingsSpGainMore"),
			UIString.instance.GetString(_spGainWingPowerTableData.gradeName), _spGainWingPowerTableData.value1);
		string secondText = GetGradeValueText(_spGainWingPowerTableData.wingType);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", firstText, secondText), 250, spGainTextTransform, new Vector2(30.0f, -35.0f));
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
			if (wingType == (int)eStatsType.SpGain)
				_stringBuilderGrade.AppendFormat("{0:0.##}", TableDataManager.instance.wingPowerTable.dataArray[i].value1);
			else
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

	// SwitchAnim 특성상 처음 Start호출될때 한번 이벤트를 강제로 발생시킨다. 이때 패킷 보내면 안되므로 플래그 하나를 만들어둔다.
	bool _ignoreStartEvent = false;
	// 옵션이 다른 캐릭터를 번갈아가면서 볼때 캐릭터에게 셋팅한대로 값을 로드해서 적용해야한다. 이때는 패킷을 보내면 안되므로 플래그를 하나 더 만들어서 관리한다.
	bool _notUserSetting = false;
	public void OnSwitchOnHide()
	{
		hideOnOffText.text = "ON";
		hideOnOffText.color = Color.white;

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		PlayFabApiManager.instance.RequestHideWing(_characterData, true, () =>
		{
			// 메뉴 안에서는 항상 보이기 때문에 호출할 필요 없다.
			//PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_characterData.actorId);
			//if (playerActor != null)
			//	playerActor.RefreshWingHide();

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_HideWingsOn"), 2.0f);
		});
	}

	public void OnSwitchOffHide()
	{
		hideOnOffText.text = "OFF";
		hideOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

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
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ChangeWingsLookConfirm"), CurrencyData.eCurrencyType.Diamond, _changeWingLookPrice, false, () =>
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
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ChangeWingsOptionsConfirm"), CurrencyData.eCurrencyType.Diamond, _changeWingOptionPrice, false, () =>
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
		int gradeIndex3 = 0;

		if (changeType == 0 || changeType == 1)
		{
			wingLookId = GetWingLookId();

			// 패킷 받고 교체할테니 미리 프리로딩을 걸어둔다.
			_nextWingPrefab = null;
			WingLookTableData wingLookTableData = TableDataManager.instance.FindWingLookTableData(wingLookId);
			if (wingLookTableData != null)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(wingLookTableData.prefabAddress, "Wing", (prefab) =>
				{
					_nextWingPrefab = prefab;
				});
			}
		}
		if (changeType == 0 || changeType == 2)
			GetWingPowerId(ref gradeIndex0, ref gradeIndex1, ref gradeIndex2, ref gradeIndex3);

		if (CheatingListener.detectedCheatTable)
			return;

		PlayFabApiManager.instance.RequestChangeWing(_characterData, changeType, wingLookId, gradeIndex0, gradeIndex1, gradeIndex2, gradeIndex3, price, () =>
		{
			ConfirmSpendCanvas.instance.gameObject.SetActive(false);
			CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
			Timing.RunCoroutine(ChangeWingProcess(changeType, wingLookId));
		});
	}

	GameObject _nextWingPrefab;
	IEnumerator<float> ChangeWingProcess(int changeType, int wingLookId)
	{
		// 인풋 차단
		CharacterInfoCanvas.instance.inputLockObject.SetActive(true);

		// 날개 교체는 이펙트 끝날때 바로 해야하니 이펙트가 로딩되었는지를 확인한다.
		if (changeType == 0 || changeType == 1)
		{
			while (_nextWingPrefab == null)
				yield return Timing.WaitForOneFrame;
		}

		// priceButton은 3개나 있기도 하고 골드가 다이아로 바뀌는거 같은 아이콘 변경은 없으니 그냥 둔다.
		//priceButtonObject.SetActive(false);

		// 인풋 막은 상태에서 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 사전 이펙트 끝나갈때쯤 화이트 페이드
		FadeCanvas.instance.FadeOut(0.3f, 0.85f);
		yield return Timing.WaitForSeconds(0.3f);

		// 화이트 페이드의 끝나는 시점에 날개 교체하면서 캔버스 갱신하고 툴팁 표시
		if (changeType == 0 || changeType == 1)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_characterData.actorId);
			if (playerActor != null)
				playerActor.RefreshWing();

			// 들고있을 필요는 없으니 null
			_nextWingPrefab = null;
		}

		RefreshInfo();
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_WingsChangeDone"), 2.0f);
		FadeCanvas.instance.FadeIn(1.5f);

		// 페이드 복구중 1초 지나면
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

			if (_hasWing && _prevWingLookId == TableDataManager.instance.wingLookTable.dataArray[i].wingLookId)
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
	List<int> _listSelectedType = null;
	void GetWingPowerId(ref int gradeIndex0, ref int gradeIndex1, ref int gradeIndex2, ref int gradeIndex3)
	{
		int optionCount = UnityEngine.Random.Range(1, 5);

		if (_listSelectedType == null)
			_listSelectedType = new List<int>();
		_listSelectedType.Clear();

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
				if (_listSelectedType.Contains(TableDataManager.instance.wingPowerTable.dataArray[i].wingType))
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
				case 3: gradeIndex3 = _listRandomWingPowerInfo[index].wingPowerTableData.grade; break;
			}
			_listSelectedType.Add(_listRandomWingPowerInfo[index].wingPowerTableData.wingType);
		}
	}
	#endregion
}