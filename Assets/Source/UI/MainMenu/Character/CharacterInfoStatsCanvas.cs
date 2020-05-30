using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class CharacterInfoStatsCanvas : MonoBehaviour
{
	public static CharacterInfoStatsCanvas instance;

	public GameObject needGroupObject;
	public GameObject contentGroupObject;

	public Image resetButtonImage;
	public Text resetText;
	public Transform remainPointTextTransform;
	public Text remainPointValueText;
	public Transform[] statsTextTransformList;
	public Text[] statsValueTextList;
	public Image[] plusButtonImageList;
	public Color textGreenColor;
	public Image applyButtonImage;
	public Text applyButtonText;

	List<ObscuredInt> _listShowStatPoint = new List<ObscuredInt>();
	List<ObscuredInt> _listAddStatPoint = new List<ObscuredInt>();

	public enum eStatsType
	{
		Str,
		Dex,
		Int,
		Vit,

		Amount,
	}

	void Awake()
	{
		instance = this;

		for (int i = 0; i < statsTextTransformList.Length; ++i)
		{
			_listShowStatPoint.Add(0);
			_listAddStatPoint.Add(0);
		}
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
	int _maxStatPoint;
	int _remainStatPoint;
	CharacterData _characterData;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		_characterData = characterData;
		_maxStatPoint = characterData.maxStatPoint;
		_remainStatPoint = characterData.remainStatPoint;
		_sumUsePoint = 0;

		if (characterData.limitBreakLevel == 0)
		{
			needGroupObject.SetActive(true);
			contentGroupObject.SetActive(false);
			return;
		}
		needGroupObject.SetActive(false);
		contentGroupObject.SetActive(true);

		List<ObscuredInt> listStatPoint = characterData.listStatPoint;
		for (int i = 0; i < listStatPoint.Count; ++i)
			_sumUsePoint += listStatPoint[i];

		resetText.text = UIString.instance.GetString("GameUI_StatReset");
		resetText.color = (_sumUsePoint > 0) ? Color.white : Color.gray;
		resetButtonImage.color = (_sumUsePoint > 0) ? Color.white : Color.gray;

		remainPointValueText.text = _remainStatPoint.ToString();
		bool existRemainPoint = (_remainStatPoint > 0);

		for (int i = 0; i < statsValueTextList.Length; ++i)
		{
			int baseValue = 0;
			switch (i)
			{
				case 0: baseValue = actorTableData.baseStr; break;
				case 1: baseValue = actorTableData.baseDex; break;
				case 2: baseValue = actorTableData.baseInt; break;
				case 3: baseValue = actorTableData.baseVit; break;
			}
			if (i < listStatPoint.Count)
				baseValue += listStatPoint[i];
			_listShowStatPoint[i] = baseValue;
			_listAddStatPoint[i] = 0;
			statsValueTextList[i].text = baseValue.ToString();
			statsValueTextList[i].color = Color.white;
			plusButtonImageList[i].color = existRemainPoint ? Color.white : Color.gray;
		}

		applyButtonImage.color = ColorUtil.halfGray;
		applyButtonText.color = ColorUtil.halfGray;

		_actorId = actorId;
		_changed = false;
	}
	#endregion

	int _sumUsePoint = 0;
	public void OnClickResetButton()
	{
		if (_sumUsePoint == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NoAppliedPoints"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("StatsResetDiamond");
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ResetQuestion"), CurrencyData.eCurrencyType.Diamond, price, false, () =>
			{
				if (CurrencyData.instance.dia < price)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
					return;
				}
				PlayFabApiManager.instance.RequestResetCharacterStats(_characterData, price, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvApplyStatPoint(true);
				});
			});
		});
	}

	public void OnClickLeftPointButton()
	{
		string text = UIString.instance.GetString("GameUI_LeftPointsMore");
		if (_maxStatPoint > 0)
		{
			string totalPointText = UIString.instance.GetString("GameUI_TotalPoints", _maxStatPoint);
			text = string.Format("{0}\n\n{1}", text, totalPointText);
		}
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, remainPointTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickStatsDetailButton(int index)
	{
		string stringId = "";
		switch (index)
		{
			case 0: stringId = "GameUI_StrMore"; break;
			case 1: stringId = "GameUI_DexMore"; break;
			case 2: stringId = "GameUI_IntMore"; break;
			case 3: stringId = "GameUI_VitMore"; break;
		}
		string text = UIString.instance.GetString(stringId);
		CharacterData characterData = PlayerData.instance.GetCharacterData(_actorId);
		if (characterData != null)
		{
			List<ObscuredInt> listStatPoint = characterData.listStatPoint;
			if (index < listStatPoint.Count && listStatPoint[index] > 0)
			{
				string usePointText = UIString.instance.GetString("GameUI_AppliedPoints", listStatPoint[index]);
				text = string.Format("{0}\n\n{1}", text, usePointText);
			}
		}
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, statsTextTransformList[index], new Vector2(10.0f, -35.0f));
	}

	bool _changed = false;
	public void OnClickStatsPlusButton(int index)
	{
		// 한번 찍기 시작한 상태에서 0으로 될땐 따로 토스트 처리 하지 않고 비활성화 형태로만 보여준다.
		if (_changed && _remainStatPoint == 0)
			return;

		if (_remainStatPoint == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NoStatPoints"), 2.0f);
			return;
		}

		// 보이는 스탯용
		_listShowStatPoint[index] += 1;
		statsValueTextList[index].text = _listShowStatPoint[index].ToString();
		statsValueTextList[index].color = textGreenColor;

		// 패킷으로 보내는 스탯용
		_listAddStatPoint[index] += 1;

		_remainStatPoint -= 1;
		remainPointValueText.text = _remainStatPoint.ToString();
		if (_remainStatPoint == 0)
		{
			for (int i = 0; i < plusButtonImageList.Length; ++i)
				plusButtonImageList[i].color = Color.gray;
		}
		_changed = true;

		applyButtonImage.color = Color.white;
		applyButtonText.color = Color.white;

		// 패킷 전달용 - 서버부터 하고 처리
	}

	public void OnClickApplyButton()
	{
		if (_changed)
		{
			// 여기선 별도의 확인창이 없다. 바로 패킷 보내서 적용하면 된다.
			// 재화 소모가 없는거라 Set으로 보내면 위험하다. Add로 보내야한다.
			int strAddPoint = 0;
			int dexAddPoint = 0;
			int intAddPoint = 0;
			int vitAddPoint = 0;
			if (0 < _listAddStatPoint.Count) strAddPoint = _listAddStatPoint[0];
			if (1 < _listAddStatPoint.Count) dexAddPoint = _listAddStatPoint[1];
			if (2 < _listAddStatPoint.Count) intAddPoint = _listAddStatPoint[2];
			if (3 < _listAddStatPoint.Count) vitAddPoint = _listAddStatPoint[3];
			PlayFabApiManager.instance.RequestApplyCharacterStats(_characterData, strAddPoint, dexAddPoint, intAddPoint, vitAddPoint, () =>
			{
				OnRecvApplyStatPoint(false);
			});
			return;
		}

		if (_remainStatPoint == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NoStatPoints"), 2.0f);
			return;
		}

		if (_remainStatPoint > 0 && _changed == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_RaiseStatPoints"), 2.0f);
			return;
		}
	}

	void OnRecvApplyStatPoint(bool reset)
	{
		// 먼저 Refresh
		RefreshInfo();
		CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();

		// 알람 Refresh
		CharacterInfoCanvas.instance.RefreshAlarmObjectList();
		CharacterListCanvas.instance.RefreshAlarmList();
		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();

		ToastCanvas.instance.ShowToast(UIString.instance.GetString(reset ? "GameUI_ResetComplete" : "GameUI_StatComplete"), 2.0f);
	}
}