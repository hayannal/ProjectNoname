using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SkillSlotIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public Image powerSourceIconImage;
	public GameObject activeAlarmObject;
	public Image activeSkillOutlineImage;
	public GameObject spGaugeObject;
	public Image spGaugeValueImage;
	public GameObject blinkObject;
	public DOTweenAnimation useTweenAnimation;

	public Image cooltimeImage;
	public Text cooltimeText;

	public bool movable;
	public bool cachingLastMovedPosition;

	Color _disableColor = new Color(0.0f, 0.0f, 0.0f, 0.65f);

	PlayerActor _playerActor;
	ActionController.ActionInfo _actionInfo = null;

	List<bool> _touchEventResultList = null;
	InputProcessor _inputProcessor = null;

	void Awake()
	{
		_touchEventResultList = new List<bool>();
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList.Add(false);
		_inputProcessor = new InputProcessor();
		_inputProcessor.tabAction = OnTab;
		_inputProcessor.holdAction = OnHold;
		_inputProcessor.draggingAction = OnDragging;
		_inputProcessor.endDragAction = OnEndDrag;
		_inputProcessor.swipeAction = OnSwipe;
		_inputProcessor.pressAction = OnPress;
		_inputProcessor.releaseAction = OnRelease;
	}

	void Start()
	{
		if (movable && cachingLastMovedPosition)
			LoadLastMovedPosition();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_inputProcessor.OnPointerDown(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_inputProcessor.OnPointerUp(eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		_inputProcessor.OnBeginDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		_inputProcessor.OnDrag(eventData);

		//OnDragAction(eventData);

		if (movable)
			OnDragIcon(eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		_inputProcessor.OnEndDrag(eventData);
	}

	public void Initialize(PlayerActor playerActor, ActionController.ActionInfo actionInfo)
	{
		if (_actionInfo != null)
		{
			// 필요한가?
			//if (_actionInfo.cooltimeInfo != null)
			//	_actionInfo.cooltimeInfo.cooltimeStartAction = _actionInfo.cooltimeInfo.cooltimeEndAction = null;
		}

		_playerActor = playerActor;
		_actionInfo = actionInfo;

		// 우선 쿨타임 나중에
		//if (actionInfo.cooltimeInfo != null)
		//{
		//	actionInfo.cooltimeInfo.cooltimeStartAction = OnStartCooltime;
		//	actionInfo.cooltimeInfo.cooltimeEndAction = OnEndCooltime;
		//	if (actionInfo.cooltimeInfo.CheckCooltime())
		//	{
		//		OnStartCooltime();
		//		UpdateCooltime();
		//	}
		//	else
		//		OnEndCooltime();
		//}

		useTweenAnimation.transform.localScale = Vector3.one;

		int playerPowerSourceIndex = 0;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(playerActor.actorId);
		if (actorTableData != null)
			playerPowerSourceIndex = actorTableData.powerSource;
		powerSourceIconImage.sprite = SkillSlotCanvas.instance.powerSourceIconSpriteList[playerPowerSourceIndex];
		OnChangedSP(playerActor, true);
	}

	void Update()
	{
		_inputProcessor.Update();
		UpdateCooltime();
	}

	void LateUpdate()
	{
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList[i] = false;
	}

	//public void OnDragAction(PointerEventData eventData)
	//{
	//	if (_playerActor.castingProcessor != null)
	//	{
	//		CastingObjectBase castingObjectBase = _playerActor.castingProcessor.GetCurrentCastingObject();
	//		if (castingObjectBase != null)
	//		{
	//			if (OnDragCastingControllerUI(eventData))
	//			{
	//				castingObjectBase.OnDragCastingPosition(_castingControllerPointImageTransform.anchoredPosition,
	//					_castingControllerBackgroundImageTransform.sizeDelta.x * 0.5f * _castingControllerBackgroundImageRectAdjustValue);
	//			}
	//		}
	//	}
	//}

	#region Drag Slot Icon
	void OnDragIcon(PointerEventData eventData)
	{
		//if (CheckThreshold(eventData) == false)
		//	return;

		cachedTransform.position = eventData.position;

		if (cachingLastMovedPosition)
			SaveLastMovedPosition();
	}

	const string LastMovedPositionXKey = "_LastMovedSkillSlotPositionX";
	const string LastMovedPositionYKey = "_LastMovedSkillSlotPositionY";
	void SaveLastMovedPosition()
	{
		PlayerPrefs.SetFloat(LastMovedPositionXKey, cachedTransform.localPosition.x);
		PlayerPrefs.SetFloat(LastMovedPositionYKey, cachedTransform.localPosition.y);
	}

	void LoadLastMovedPosition()
	{
		if (PlayerPrefs.HasKey(LastMovedPositionXKey) == false)
			return;

		Vector3 cachedPosition = cachedTransform.localPosition;
		cachedPosition.x = PlayerPrefs.GetFloat(LastMovedPositionXKey);
		cachedPosition.y = PlayerPrefs.GetFloat(LastMovedPositionYKey);
		cachedTransform.localPosition = cachedPosition;
	}
	#endregion

	public void OnDragging(Vector2 delta)
	{
	}

	public void OnEndDrag()
	{
	}

	public void OnHold()
	{
		if (_actionInfo.eInputType == Control.eInputType.Hold)
			PlayAction();

		_touchEventResultList[(int)Control.eInputType.Hold] = true;
	}

	public void OnSwipe()
	{
		if (_actionInfo.eInputType == Control.eInputType.Swipe)
			PlayAction();

		_touchEventResultList[(int)Control.eInputType.Swipe] = true;
	}

	public void OnTab(Vector2 position)
	{
		if (_actionInfo.eInputType == Control.eInputType.Tab)
			PlayAction();

		_touchEventResultList[(int)Control.eInputType.Tab] = true;
	}

	public void OnPress()
	{
		if (_actionInfo.eInputType == Control.eInputType.Press)
			PlayAction();

		_touchEventResultList[(int)Control.eInputType.Press] = true;
	}

	public void OnRelease()
	{
		if (_actionInfo.eInputType == Control.eInputType.Release)
			PlayAction();

		_touchEventResultList[(int)Control.eInputType.Release] = true;
	}

	public bool CheckInput(Control.eInputType eInputType)
	{
		switch (eInputType)
		{
			case Control.eInputType.Tab:
			case Control.eInputType.Hold:
			case Control.eInputType.Swipe:
			case Control.eInputType.Press:
			case Control.eInputType.Release:
				return _touchEventResultList[(int)eInputType];
		}
		return false;
	}

	void PlayAction()
	{
		if (_playerActor.actorStatus.IsDie())
			return;

		if (_playerActor.actionController.PlayActionByControl(_actionInfo.eControllerType, _actionInfo.eInputType))
			useTweenAnimation.DORestart();
	}

	/*
	#region Casting Controller UI
	GameObject _castingControllerUIObject = null;
	RectTransform _castingControllerBackgroundImageTransform = null;
	RectTransform _castingControllerPointImageTransform = null;
	bool _dragThresholdChecked = false;
	float _castingControllerBackgroundImageRectAdjustValue = 0.8f;
	void ShowCastingControllerUI(bool show)
	{
		if (_castingControllerUIObject == null)
		{
			_castingControllerUIObject = (GameObject)Instantiate(SkillSlotCanvas.instance.castingControllerPrefab, transform);
			_castingControllerBackgroundImageTransform = _castingControllerUIObject.GetComponent<RectTransform>();
			_castingControllerPointImageTransform = _castingControllerUIObject.transform.GetChild(0).GetComponent<RectTransform>();
		}
		_castingControllerPointImageTransform.anchoredPosition = Vector2.zero;
		_castingControllerUIObject.SetActive(show);
		_dragThresholdChecked = false;
	}

	bool OnDragCastingControllerUI(PointerEventData eventData)
	{
		//Debug.Log("Event pos : " + eventData.position.ToString());
		//Debug.Log("Unity Press Pos : " + eventData.pressPosition.ToString());

		Vector2 diff = eventData.position - eventData.pressPosition;
		float sqrtSize = _castingControllerPointImageTransform.sizeDelta.x * 0.5f * _castingControllerPointImageTransform.sizeDelta.x * 0.5f;
		if (_dragThresholdChecked == false && diff.sqrMagnitude < sqrtSize * (_castingControllerBackgroundImageRectAdjustValue - 0.1f))
			return false;
		_dragThresholdChecked = true;

		Vector2 localPoint = Vector2.zero;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_castingControllerBackgroundImageTransform, eventData.position, eventData.pressEventCamera, out localPoint);
		sqrtSize = _castingControllerBackgroundImageTransform.sizeDelta.x * 0.5f * _castingControllerBackgroundImageTransform.sizeDelta.x * 0.5f;
		if (localPoint.sqrMagnitude > sqrtSize * _castingControllerBackgroundImageRectAdjustValue * _castingControllerBackgroundImageRectAdjustValue)
			localPoint = localPoint.normalized * _castingControllerBackgroundImageTransform.sizeDelta.x * 0.5f * _castingControllerBackgroundImageRectAdjustValue;
		_castingControllerPointImageTransform.anchoredPosition = localPoint;

		return true;
	}
	#endregion
	*/

	#region Cooltime
	void OnStartCooltime()
	{
		cooltimeImage.gameObject.SetActive(true);
		_inputProcessor.enabled = false;
	}

	void OnEndCooltime()
	{
		cooltimeImage.gameObject.SetActive(false);
		_inputProcessor.enabled = true;
	}

	void UpdateCooltime()
	{
		if (!cooltimeImage.gameObject.activeSelf)
			return;

		// 쿨타임은 나중에
		//cooltimeImage.fillAmount = _actionInfo.cooltimeInfo.cooltimeRatio;
		//cooltimeText.text = _actionInfo.cooltimeInfo.cooltimeRatioText;
	}
	#endregion

	#region SP
	public void OnChangedSP(PlayerActor playerActor, bool ignoreBlink = false)
	{
		float spRatio = playerActor.actorStatus.GetSPRatio();
		if (spRatio >= 1.0f)
		{
			if (!spGaugeObject.activeSelf)
				ignoreBlink = true;
			
			// check active?
			if (_actionInfo != null)
			{
				activeAlarmObject.SetActive(true);
				activeSkillOutlineImage.gameObject.SetActive(true);
			}
			powerSourceIconImage.color = Color.white;
			spGaugeObject.SetActive(false);
			_inputProcessor.enabled = true;
		}
		else
		{
			// 사용할때도 반짝 해주려나
			if (!spGaugeObject.activeSelf)
				ignoreBlink = true;

			activeAlarmObject.SetActive(false);
			activeSkillOutlineImage.gameObject.SetActive(false);
			powerSourceIconImage.color = _disableColor;
			spGaugeObject.SetActive(true);
			spGaugeValueImage.fillAmount = spRatio;
			_inputProcessor.enabled = false;
		}
		if (!ignoreBlink)
		{
			blinkObject.SetActive(false);
			blinkObject.SetActive(true);
		}
	}
	#endregion



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
