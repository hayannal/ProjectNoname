using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab.ClientModels;

public class InvasionResultCanvas : MonoBehaviour
{
	public static InvasionResultCanvas instance;

	public Text difficultyText;
	public Text resultText;
	public GameObject noRewardGroupObject;


	public GameObject ppGroupObject;
	public Slider ppLineSlider;
	public GameObject gainPpTextObject;
	public GameObject ppScrollViewObject;
	public GameObject ppContentItemPrefab;
	public RectTransform ppContentRootRectTransform;
	public GameObject levelUpPossibleTextObject;

	public GameObject currencyGroupObject;
	public DOTweenAnimation goldImageTweenAnimation;
	public Text goldValueText;
	public GameObject diaGroupObject;
	public DOTweenAnimation diaImageTweenAnimation;
	public Text diaValueText;

	public GameObject itemGroupObject;
	public Slider itemLineSlider;
	public GameObject gainItemTextObject;
	public GameObject itemScrollViewObject;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public GameObject exitGroupObject;

	public class CustomItemSubContainer : CachedItemHave<CharacterBoxResultListItem>
	{
	}
	CustomItemSubContainer _ppContainer = new CustomItemSubContainer();

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		ppContentItemPrefab.SetActive(false);
		contentItemPrefab.SetActive(false);
		smallStatusBackBlurImage.SetAsFirstSibling();
	}

	void OnEnable()
	{
		Time.timeScale = 0.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDestroy()
	{
		Time.timeScale = 1.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateGoldText();
		UpdateDiaText();
		UpdateEquipSmallStatusInfo();
	}

	bool _clear = false;
	List<ItemInstance> _listGrantItem;
	public void RefreshInfo(bool clear, int difficulty, string jsonItemGrantResults)
	{
		_clear = clear;

		difficultyText.text = ChangeInvasionDifficultyCanvasListItem.GetDifficultyText(InvasionEnterCanvas.instance.GetInvasionTableData().hard);
		resultText.text = UIString.instance.GetString(clear ? "GameUI_Success" : "GameUI_Failure");
		resultText.color = clear ? new Color(0.0f, 0.733f, 0.792f) : new Color(0.792f, 0.152f, 0.0f);

		if (jsonItemGrantResults != "")
			_listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		gameObject.SetActive(true);

		// NodeWarResultCanvas 때처럼 여기서 삭제하기로 한다.
		if (clear)
			RandomBoxScreenCanvas.instance.gameObject.SetActive(false);

		StartCoroutine(BgmProcess());
	}

	IEnumerator BgmProcess()
	{
		yield return new WaitForSecondsRealtime(_clear ? 0.1f : 1.5f);

		SoundManager.instance.PlaySFX(_clear ? "BattleWin" : "BattleLose");

		yield return new WaitForSecondsRealtime((_clear ? 12.0f : 11.0f) + 3.0f);

		SoundManager.instance.PlayBgm("BGM_BattleEnd", 3.0f);
	}

	public void OnEventSuccessResult()
	{
		if (_clear)
		{
			// 요일별로 보여줘야하는 탭이 다르다.
			bool usePp = false;
			bool useEquip = false;
			bool useCurrency = false;
			switch ((DayOfWeek)InvasionEnterCanvas.instance.GetInvasionTableData().dayWeek)
			{
				case DayOfWeek.Sunday: useCurrency = true; break;
				case DayOfWeek.Monday: useEquip = true; break;
				case DayOfWeek.Tuesday: usePp = true; break;
				case DayOfWeek.Wednesday: useCurrency = true; break;
				case DayOfWeek.Thursday: usePp = true; break;
				case DayOfWeek.Friday: useEquip = true; break;
				case DayOfWeek.Saturday: usePp = true; break;
			}

			if (usePp)
			{
				ppGroupObject.SetActive(true);
				StartCoroutine(PpProcess());
			}
			else if (useEquip)
			{
				itemGroupObject.SetActive(true);
				StartCoroutine(ItemProcess());
			}
			else if (useCurrency)
			{
				// 기본값으로 셋팅 후
				SetGoldRewardInfo();
				currencyGroupObject.SetActive(true);
			}
		}
		else
		{
			// 실패했다면 보상 없음 표시를 켜고
			noRewardGroupObject.SetActive(true);
		}
	}

	#region Gold
	int _goldRewardAmount;
	int _diaRewardAmount;
	void SetGoldRewardInfo()
	{
		_goldRewardAmount = DropManager.instance.GetLobbyGoldAmount();
		_diaRewardAmount = DropManager.instance.GetLobbyDiaAmount();
		diaGroupObject.SetActive(_diaRewardAmount > 0);
	}

	public void OnEventIncreaseGold()
	{
		_goldChangeRemainTime = goldChangeTime;
		_goldChangeSpeed = _goldRewardAmount / _goldChangeRemainTime;
		_currentGold = 0.0f;
		_updateGoldText = true;

		goldImageTweenAnimation.DOPlay();

		if (diaGroupObject.activeSelf)
		{
			_diaChangeRemainTime = diaChangeTime;
			_diaChangeSpeed = _diaRewardAmount / _diaChangeRemainTime;
			_currentDia = 0.0f;
			_updateDiaText = true;

			diaImageTweenAnimation.DOPlay();
		}

		StartCoroutine(GoldProcess());
	}

	IEnumerator GoldProcess()
	{
		yield return new WaitForSecondsRealtime(goldChangeTime);

		yield return new WaitForSecondsRealtime(0.2f);
		exitGroupObject.SetActive(true);
	}

	const float goldChangeTime = 0.4f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.unscaledDeltaTime;
		int currentGoldInt = (int)_currentGold;
		if (currentGoldInt >= _goldRewardAmount)
		{
			currentGoldInt = _goldRewardAmount;
			_updateGoldText = false;
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}

	const float diaChangeTime = 0.4f;
	float _diaChangeRemainTime;
	float _diaChangeSpeed;
	float _currentDia;
	int _lastDia;
	bool _updateDiaText;
	void UpdateDiaText()
	{
		if (_updateDiaText == false)
			return;

		_currentDia += _diaChangeSpeed * Time.unscaledDeltaTime;
		int currentDiaInt = (int)_currentDia;
		if (currentDiaInt >= _diaRewardAmount)
		{
			currentDiaInt = _diaRewardAmount;
			_updateDiaText = false;
		}
		if (currentDiaInt != _lastGold)
		{
			_lastDia = currentDiaInt;
			diaValueText.text = _lastDia.ToString("N0");
		}
	}
	#endregion

	#region Item
	IEnumerator ItemProcess()
	{
		DOTween.To(() => itemLineSlider.value, x => itemLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

		yield return new WaitForSecondsRealtime(0.3f);

		gainItemTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.1f);

		// 드랍매니저가 가지고 있는 아이템 리스트에는 아이디밖에 들어있지 않아서 정보를 구성할 수 없기 때문에 사용할 수 없다.
		// 결과패킷으로 받은 실제 아이템 리스트를 사용해야한다.
		if (_listGrantItem == null || _listGrantItem.Count == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		itemScrollViewObject.SetActive(true);
		for (int i = 0; i < _listGrantItem.Count; ++i)
		{
			EquipData newEquipData = new EquipData();
			newEquipData.equipId = _listGrantItem[i].ItemId;
			newEquipData.Initialize(_listGrantItem[i].CustomData);

			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
			yield return new WaitForSecondsRealtime(0.2f);
		}

		exitGroupObject.SetActive(true);
	}

	public void OnClickListItem(EquipData equipData)
	{
		equipSmallStatusInfo.RefreshInfo(equipData, false);
		equipSmallStatusInfo.gameObject.SetActive(false);
		equipSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	float _materialSmallStatusInfoShowRemainTime;
	void UpdateEquipSmallStatusInfo()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.unscaledDeltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				equipSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}
	#endregion

	#region Pp
	IEnumerator PpProcess()
	{
		DOTween.To(() => ppLineSlider.value, x => ppLineSlider.value = x, 1.0f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

		yield return new WaitForSecondsRealtime(0.3f);

		gainPpTextObject.SetActive(true);

		yield return new WaitForSecondsRealtime(0.1f);

		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		if (listPpInfo.Count == 0)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			exitGroupObject.SetActive(true);
			yield break;
		}

		ppScrollViewObject.SetActive(true);
		bool levelUpPossible = false;
		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			CharacterBoxResultListItem resultListItem = _ppContainer.GetCachedItem(ppContentItemPrefab, ppContentRootRectTransform);
			int powerLevel = 0;
			int transcendLevel = 0;
			bool showPlusAlarm = false;
			CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[i].actorId);
			if (characterData != null)
			{
				powerLevel = characterData.powerLevel;
				transcendLevel = characterData.transcendLevel;
				showPlusAlarm = characterData.IsPlusAlarmState();
			}
			resultListItem.characterListItem.Initialize(listPpInfo[i].actorId, powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
			resultListItem.characterListItem.ShowAlarm(false);
			if (showPlusAlarm)
			{
				resultListItem.characterListItem.ShowAlarm(true, true);
				levelUpPossible = true;
			}
			resultListItem.Initialize("", listPpInfo[i].add);
			yield return new WaitForSecondsRealtime(0.2f);
		}
		levelUpPossibleTextObject.SetActive(levelUpPossible);
		exitGroupObject.SetActive(true);
	}
	#endregion

	public void OnEventNoReward()
	{
		exitGroupObject.SetActive(true);
	}

	public void OnClickExitButton()
	{
		ContentsData.instance.readyToReopenInvasionEnterCanvas = true;
		SceneManager.LoadScene(0);
	}
}