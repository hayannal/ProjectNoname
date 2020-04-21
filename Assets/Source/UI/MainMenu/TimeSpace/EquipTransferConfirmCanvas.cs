using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;
using CodeStage.AntiCheat.ObscuredTypes;

public class EquipTransferConfirmCanvas : MonoBehaviour
{
	public static EquipTransferConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem materialEquipListItem;
	public Text materialMainStatusText;
	public Image materialMainStatusFillImage;
	public EquipCanvasListItem prevEquipListItem;
	public EquipCanvasListItem nextEquipListItem;
	public Text mainStatusText;
	public Image mainStatusFillImage;

	public GameObject standbyEffectPrefab;
	public GameObject successEffectPrefab;
	public RectTransform toastBackImageRectTransform;
	public CanvasGroup processCanvasGroup;

	public GameObject successObject;
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

	public static int GetTransferResult(EquipData equipData, EquipData materialEquipData)
	{
		// 1. 이전해줄 템의 등급과 강화 단계로 Transfer Rate * Same Sum 을 얻어둔다.
		// 2. 이전받을 템의 시작위치를 구해야하니 등급과 템으로 Data를 다시 구해야하는데..
		// 3. 이전해줄 템의 등급에 따라 흰템이면 innerGradeZeroAmount 녹템이면 innerGradeOneAmount 이렇게 어느 컬럼을 읽을지 결정해서 아래로 내려가면서
		// 4. 누적된 값을 가지고 적용할 최대수치를 찾으면 된다.
		int enhanceLevel = equipData.enhanceLevel;
		TransferTableData materialTransferTableData = TableDataManager.instance.FindTransferTableData(materialEquipData.cachedEquipTableData.innerGrade, materialEquipData.enhanceLevel);
		if (materialTransferTableData == null)
			return enhanceLevel;
		float materialTransferValue = materialTransferTableData.transferRate * materialTransferTableData.sameSum;
		float currentValue = 0.0f;
		while (true)
		{
			// 이전하고 나니 여기가 null? 리턴. 클릭이 안된다. 재료.
			TransferTableData transferTableData = TableDataManager.instance.FindTransferTableData(equipData.cachedEquipTableData.innerGrade, enhanceLevel + 1);
			if (transferTableData == null)
			{
				// 맥스에 도달하면 테이블이 없다.
				break;
			}
			float amount = 0.0f;
			switch (materialEquipData.cachedEquipTableData.innerGrade)
			{
				case 0: amount = transferTableData.innerGradeZeroAmount; break;
				case 1: amount = transferTableData.innerGradeOneAmount; break;
				case 2: amount = transferTableData.innerGradeTwoAmount; break;
				case 3: amount = transferTableData.innerGradeThreeAmount; break;
				case 4: amount = transferTableData.innerGradeFourAmount; break;
				case 5: amount = transferTableData.innerGradeFiveAmount; break;
				case 6: amount = transferTableData.innerGradeSixAmount; break;
			}
			currentValue += amount;
			if (currentValue > materialTransferValue)
				break;
			enhanceLevel += 1;
		}
		return enhanceLevel;
	}

	EquipData _equipData;
	ObscuredInt _price;
	ObscuredInt _targetEnhanceLevel;
	public void ShowCanvas(bool show, EquipData equipData, string displayAttack, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;
		if (EquipInfoGrowthCanvas.instance.selectedEquipData == null)
			return;

		_equipData = equipData;
		_price = price;
		
		EquipData materialEquipData = EquipInfoGrowthCanvas.instance.selectedEquipData;
		materialEquipListItem.Initialize(materialEquipData, null);
		if (TimeSpaceData.instance.IsEquipped(materialEquipData))
			materialEquipListItem.equippedText.gameObject.SetActive(true);
		string materialDisplayString = "";
		materialEquipData.GetMainStatusDisplayStringByEnhance(materialEquipData.enhanceLevel, ref materialDisplayString);
		materialMainStatusText.text = materialDisplayString;
		materialMainStatusFillImage.fillAmount = materialEquipData.GetMainStatusRatio();
		materialMainStatusFillImage.color = EquipListStatusInfo.GetGaugeColor(materialMainStatusFillImage.fillAmount == 1.0f);

		prevEquipListItem.Initialize(equipData, null);
		nextEquipListItem.Initialize(equipData, null);
		mainStatusFillImage.fillAmount = equipData.GetMainStatusRatio();
		mainStatusFillImage.color = EquipListStatusInfo.GetGaugeColor(mainStatusFillImage.fillAmount == 1.0f);

		// 강화와 달리 여기서 미리 계산해야한다.
		int enhanceLevel = GetTransferResult(equipData, materialEquipData);

		// 결과로 나온 enhanceLevel이 최종 이전값이다. _targetEnhanceLevel에 저장해두고 결과때 쓰기로 한다.
		_targetEnhanceLevel = enhanceLevel;

		// equipData를 바꾸진 않으니 UI에서만 새 값으로 교체한다.
		if (enhanceLevel > 0)
			nextEquipListItem.enhanceLevelText.text = string.Format("+{0}", enhanceLevel);
		else
			nextEquipListItem.enhanceLevelText.text = "";

		// 차이를 구해서 최종결과값에 적어두고 결과창에 있는 addAtk에도 설정해둔다.
		int prevValue = 0;
		int.TryParse(displayAttack.Replace(",", ""), out prevValue);
		string nextDisplayString = "";
		equipData.GetMainStatusDisplayStringByEnhance(enhanceLevel, ref nextDisplayString);
		int nextValue = 0;
		int.TryParse(nextDisplayString.Replace(",", ""), out nextValue);
		mainStatusText.text = nextDisplayString;

		resultEquipListItem.Initialize(equipData, null);
		resultMainStatusText.text = displayAttack;
		_currentAtk = prevValue;
		_addAtk = nextValue - prevValue;
		addAtkText.text = string.Format("+{0:N0}", addAtkText);

		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("EquipUI_TransferEnhanceMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		// 이미 최종 enhance를 구해놨으니 바로 쓰면된다.
		priceButton.gameObject.SetActive(false);

		// 선 이펙트가 있기 때문에 Process를 먼저 실행시킨다.
		Timing.RunCoroutine(TransferProcess(_targetEnhanceLevel, EquipInfoGrowthCanvas.instance.selectedEquipData, _price));
	}

	bool _waitRecv = false;
	void OnRecvTransfer()
	{
		_waitRecv = false;
		EquipInfoGrowthCanvas.instance.currencySmallInfo.RefreshInfo();
	}

	float _standbyEffectWaitTime;
	float _currentAtk;
	float _addAtk;
	IEnumerator<float> TransferProcess(int enhanceLevel, EquipData materialEquipData, int sumPrice)
	{
		// 인풋 차단
		backKeyButton.interactable = false;
		processGraphicElement.raycastTarget = true;

		// 배경 페이드
		DOTween.To(() => backgroundImage.color, x => backgroundImage.color = x, Color.clear, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		// 제단 이펙트 작게
		EquipInfoGround.instance.ScaleDownGradeParticle(true);
		// 나머지 창들도 다 닫고
		StackCanvas.Push(gameObject);
		yield return Timing.WaitForSeconds(0.2f);
		canvasGroup.gameObject.SetActive(false);

		// 선이펙트
		_standbyEffectWaitTime = Time.time + 1.75f;
		BattleInstanceManager.instance.GetCachedObject(standbyEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);

		// 선이펙트와 동시에 패킷을 보낸다.
		_waitRecv = true;
		bool needEquip = TimeSpaceData.instance.IsEquipped(materialEquipData);
		PlayFabApiManager.instance.RequestTransfer(_equipData, enhanceLevel, materialEquipData, sumPrice, needEquip, OnRecvTransfer);

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
		BattleInstanceManager.instance.GetCachedObject(successEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.2f);

		successObject.SetActive(false);
		addAtkText.gameObject.SetActive(false);
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		successObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		float tweenDelay = 0.3f;
		resultEquipListItem.RefreshStatus();
		enhanceTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// 디스플레이용 공격력은 이미 구해져있으니 켜기만 하면 된다.
		addAtkText.gameObject.SetActive(true);
		addAtkTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		_atkChangeSpeed = -_addAtk / atkChangeTime;
		_floatCurrentAtk = _addAtk;
		_lastAtk = -1;
		_updateAtkText = true;
		yield return Timing.WaitForSeconds(atkChangeTime);
		resultMainStatusTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		exitText.gameObject.SetActive(true);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;

		// 이펙트 복구
		EquipInfoGround.instance.ScaleDownGradeParticle(false);

		EquipListCanvas.instance.RefreshGrid(true, false);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		if (TimeSpaceData.instance.IsEquipped(_equipData))
		{
			int positionIndex = _equipData.cachedEquipTableData.equipType;
			if (needEquip)
			{
				// 장착중이던 재료에게 이전받아서 자동장착 된거라면 오브젝트 자체를 갱신
				TimeSpaceGround.instance.timeSpaceAltarList[positionIndex].RefreshEquipObject();
			}
			else
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