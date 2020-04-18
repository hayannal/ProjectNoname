using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class EquipEnhanceConfirmCanvas : MonoBehaviour
{
	public static EquipEnhanceConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem equipListItem;
	public Text mainStatusText;
	public Text increaseAtkText;
	public Text materialCountText;

	public GameObject standbyEffectPrefab;
	public GameObject successEffectPrefab;
	public GameObject failureEffectPrefab;
	public RectTransform toastBackImageRectTransform;
	public CanvasGroup processCanvasGroup;

	public GameObject successObject;
	public Text successCountText;
	public GameObject failureObject;
	public EquipCanvasListItem resultEquipListItem;
	public DOTweenAnimation enhanceTweenAnimation;
	public Text resultMainStatusText;
	public DOTweenAnimation resultMainStatusTweenAnimation;
	public Text addAtkText;
	public DOTweenAnimation addAtkTweenAnimation;
	
	public Button priceButton;
	public Text priceText;
	public Text exitText;
	public Graphic processGraphicElement;

	Color _defaultBackgroundColor;
	void Awake()
	{
		instance = this;
		_defaultBackgroundColor = backgroundImage.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
		canvasGroup.gameObject.SetActive(true);
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		backgroundImage.color = _defaultBackgroundColor;
		toastBackImageRectTransform.gameObject.SetActive(false);
		processCanvasGroup.gameObject.SetActive(false);
		processCanvasGroup.alpha = 0.0f;
		
		priceButton.gameObject.SetActive(true);
		exitText.gameObject.SetActive(false);
		
		_processed = false;
	}

	void OnDisable()
	{
		if (_processed)
		{
			StackCanvas.Pop(gameObject);
			_processed = false;
		}
	}

	void Update()
	{
		UpdateAtkText();
	}

	EquipData _equipData;
	int _price;
	public void ShowCanvas(bool show, EquipData equipData, string displayAttack, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_equipData = equipData;
		_price = price;
		equipListItem.Initialize(equipData, null);
		mainStatusText.text = displayAttack;

		int prevValue = 0;
		int.TryParse(displayAttack.Replace(",", ""), out prevValue);
		string nextDisplayString = "";
		equipData.GetMainStatusDisplayStringByEnhance(equipData.enhanceLevel + 1, ref nextDisplayString);
		int nextValue = 0;
		int.TryParse(nextDisplayString.Replace(",", ""), out nextValue);
		increaseAtkText.text = string.Format("+{0}", nextValue - prevValue);

		materialCountText.text = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData.Count.ToString();

		resultEquipListItem.Initialize(equipData, null);
		resultMainStatusText.text = displayAttack;

		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("EquipUI_EquipmentEnhanceMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		// 장비 관련해서는 클라가 처리해야한다.
		int enhanceLevel = _equipData.enhanceLevel;
		bool maxReached = false;
		int materialIndex = -1;
		int sumPrice = 0;
		int successCount = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		EnhanceTableData nextEnhanceTableData = TableDataManager.instance.FindEnhanceTableData(_equipData.cachedEquipTableData.innerGrade, enhanceLevel + 1);
		List<EquipData> listMultiSelectEquipData = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData;
		for (int i = 0; i < listMultiSelectEquipData.Count; ++i)
		{
			float probability = 0.0f;
			int price = 0;
			switch (listMultiSelectEquipData[i].cachedEquipTableData.innerGrade)
			{
				case 0: probability = nextEnhanceTableData.innerGradeZeroProb; price = innerGradeTableData.innerGradeZeroEnhanceGold; break;
				case 1: probability = nextEnhanceTableData.innerGradeOneProb; price = innerGradeTableData.innerGradeOneEnhanceGold; break;
				case 2: probability = nextEnhanceTableData.innerGradeTwoProb; price = innerGradeTableData.innerGradeTwoEnhanceGold; break;
				case 3: probability = nextEnhanceTableData.innerGradeThreeProb; price = innerGradeTableData.innerGradeThreeEnhanceGold; break;
				case 4: probability = nextEnhanceTableData.innerGradeFourProb; price = innerGradeTableData.innerGradeFourEnhanceGold; break;
				case 5: probability = nextEnhanceTableData.innerGradeFiveProb; price = innerGradeTableData.innerGradeFiveEnhanceGold; break;
				case 6: probability = nextEnhanceTableData.innerGradeSixProb; price = innerGradeTableData.innerGradeSixEnhanceGold; break;
			}
			sumPrice += price;
			materialIndex = i;
			if (probability > 0.0f && Random.value <= probability)
			{
				successCount += 1;
				enhanceLevel += 1;
				if (enhanceLevel >= innerGradeTableData.max)
				{
					maxReached = true;
					break;
				}
				nextEnhanceTableData = TableDataManager.instance.FindEnhanceTableData(_equipData.cachedEquipTableData.innerGrade, enhanceLevel + 1);
			}
		}

		// 일부만 소모된건지 체크 후 리스트 재설정
		bool returnForMax = false;
		if (maxReached && materialIndex < (listMultiSelectEquipData.Count - 1))
		{
			for (int i = listMultiSelectEquipData.Count - 1; i >= 0; --i)
			{
				if (i > materialIndex)
				{
					listMultiSelectEquipData.RemoveAt(i);
					returnForMax = true;
				}
			}
		}

		priceButton.gameObject.SetActive(false);

		// 선 이펙트가 있기 때문에 Process를 먼저 실행시킨다.
		Timing.RunCoroutine(EnhanceProcess(successCount, enhanceLevel, listMultiSelectEquipData, sumPrice, returnForMax));
	}

	bool _waitRecv = false;
	void OnRecvEnhance()
	{
		_waitRecv = false;
		EquipInfoGrowthCanvas.instance.currencySmallInfo.RefreshInfo();
	}

	float _standbyEffectWaitTime;
	float _currentAtk;
	float _addAtk;
	IEnumerator<float> EnhanceProcess(int successCount, int enhanceLevel, List<EquipData> listMultiSelectEquipData, int sumPrice, bool returnForMax)
	{
		bool enhanceResult = successCount > 0;

		// 인풋 차단
		backKeyButton.interactable = false;
		processGraphicElement.raycastTarget = true;

		// 배경 페이드
		DOTween.To(() => backgroundImage.color, x => backgroundImage.color = x, Color.clear, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		// 나머지 창들도 다 닫고
		StackCanvas.Push(gameObject);
		yield return Timing.WaitForSeconds(0.2f);
		canvasGroup.gameObject.SetActive(false);

		// 선이펙트
		_standbyEffectWaitTime = Time.time + 3.0f;
		BattleInstanceManager.instance.GetCachedObject(standbyEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);

		// 선이펙트와 동시에 패킷을 보낸다.
		_waitRecv = true;
		PlayFabApiManager.instance.RequestEnhance(_equipData, enhanceLevel, listMultiSelectEquipData, sumPrice, OnRecvEnhance);

		// 패킷 온다고 바로 처리하지 않고 원래 대기하려던 타임까지 기다린다.
		while (Time.time < _standbyEffectWaitTime)
			yield return Timing.WaitForOneFrame;

		// 아직까지도 패킷이 오지 않았다면 패킷 대기창을 띄운다.
		if (_waitRecv)
			WaitingNetworkCanvas.Show(true);
		while (_waitRecv)
			yield return Timing.WaitForOneFrame;
		WaitingNetworkCanvas.Show(false);

		// 결과에 따라 이펙트 출력
		BattleInstanceManager.instance.GetCachedObject(enhanceResult ? successEffectPrefab : failureEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.2f);

		successObject.SetActive(false);
		failureObject.SetActive(false);
		addAtkText.text = "";
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		if (enhanceResult)
		{
			successObject.SetActive(enhanceResult);
			successCountText.text = successCount.ToString();
			yield return Timing.WaitForSeconds(0.3f);

			float tweenDelay = 0.3f;
			resultEquipListItem.RefreshStatus();
			enhanceTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(tweenDelay);

			// 디스플레이용 공격력을 구하기 위해 강제로 Refresh를 호출한다.
			EquipEnhanceCanvas.instance.equipStatusInfo.RefreshStatus();
			string targetText = EquipEnhanceCanvas.instance.equipStatusInfo.mainStatusText.text;
			string currentText = mainStatusText.text;
			int currentValue = 0;
			int targetValue = 0;
			int.TryParse(currentText.Replace(",", ""), out currentValue);
			int.TryParse(targetText.Replace(",", ""), out targetValue);
			_addAtk = targetValue - currentValue;
			_currentAtk = currentValue;
			addAtkText.text = string.Format("+{0:N0}", _addAtk);
			addAtkTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(tweenDelay);

			_atkChangeSpeed = -_addAtk / atkChangeTime;
			_floatCurrentAtk = _addAtk;
			_updateAtkText = true;
			yield return Timing.WaitForSeconds(atkChangeTime);
			resultMainStatusTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(tweenDelay);

			string text = UIString.instance.GetString("GameUI_TouchToExit");
			if (returnForMax)
				text = string.Format("{0}\n\n<size=16>{1}</size>", text, UIString.instance.GetString("EquipUI_ReturnForMax"));
			exitText.SetLocalizedText(text);
		}
		else
		{
			failureObject.SetActive(true);
			yield return Timing.WaitForSeconds(0.3f);

			exitText.SetLocalizedText(UIString.instance.GetString("GameUI_TouchToExit"));
		}
		exitText.gameObject.SetActive(true);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;

		EquipListCanvas.instance.RefreshGrid(true, false);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		if (enhanceResult && TimeSpaceData.instance.IsEquipped(_equipData))
		{
			int positionIndex = _equipData.cachedEquipTableData.equipType;
			TimeSpaceGround.instance.timeSpaceAltarList[positionIndex].RefreshEnhanceInfo();
		}
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed == false)
		{
			gameObject.SetActive(false);
			return;
		}

		if (processCanvasGroup.alpha >= 1.0f)
			DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 0.0f, 0.1f).OnComplete(() => gameObject.SetActive(false));
		toastBackImageRectTransform.gameObject.SetActive(false);
		exitText.gameObject.SetActive(false);
	}

	const float atkChangeTime = 0.4f;
	float _atkChangeSpeed;
	float _floatCurrentAtk;
	int _lastAtk;
	bool _updateAtkText;
	void UpdateAtkText()
	{
		if (_updateAtkText == false)
			return;

		_floatCurrentAtk += _atkChangeSpeed * Time.deltaTime;
		int currentAtkInt = (int)_floatCurrentAtk;
		if (currentAtkInt <= 0)
		{
			currentAtkInt = 0;
			_updateAtkText = false;
		}
		if (currentAtkInt != _lastAtk)
		{
			_lastAtk = currentAtkInt;
			resultMainStatusText.text = (_currentAtk + (_addAtk - _lastAtk)).ToString("N0");
			if (_lastAtk > 0)
				addAtkText.text = string.Format("+{0:N0}", _lastAtk);
			else
				addAtkText.text = "";
		}
	}
}