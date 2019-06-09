using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using DG.Tweening;
//using MecanimStateDefine;
//using ActorStatusDefine;

public class ScreenJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public static ScreenJoystick instance;

	public Transform joystickImageTransform;
	public Transform centerImageTransform;
	public Transform centerRotationImageTransform;
	public RectTransform lineImageRectTransform;

	public float centerRotationSlerpPower = 5.0f;
	public float lineStartOffset = 15.0f;
	public float lineEndOffset = 30.0f;

	public string horizontalAxisName = "Horizontal"; // The name given to the horizontal axis for the cross platform input
	public string verticalAxisName = "Vertical"; // The name given to the vertical axis for the cross platform input

	CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis; // Reference to the joystick in the cross platform input
	CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis; // Reference to the joystick in the cross platform input

	//ActionController _actionController = null;
	//ActorStatus _actorStatus = null;
	List<bool> _touchEventResultList = null;

	Dictionary<int, InputProcessor> _dicInputProcessor = new Dictionary<int, InputProcessor>();

	void Awake()
	{
		instance = this;

		_touchEventResultList = new List<bool>();
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList.Add(false);
	}

	void Start()
	{
		DOTween.Init();
	}

	#region VirtualAxis
	void OnEnable()
	{
		CreateVirtualAxes();
	}

	void OnDisable()
	{
		// remove the joysticks from the cross platform input
		m_HorizontalVirtualAxis.Remove();
		m_VerticalVirtualAxis.Remove();
	}

	void CreateVirtualAxes()
	{
		// create new axes based on axes to use
		m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
		CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
		m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
		CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
	}

	void UpdateVirtualAxes(Vector3 value)
	{
		m_HorizontalVirtualAxis.Update(value.x);
		m_VerticalVirtualAxis.Update(value.y);
	}

	#endregion

	InputProcessor GetInputProcessor(int pointerId)
	{
		if (_dicInputProcessor.ContainsKey(pointerId))
			return _dicInputProcessor[pointerId];

		InputProcessor inputProcessor = new InputProcessor();
		inputProcessor.tabAction = OnTab;
		inputProcessor.holdAction = OnHold;
		inputProcessor.dragAction = OnDragAction;
		inputProcessor.draggingAction = OnDragging;
		inputProcessor.endDragAction = OnEndDrag;
		inputProcessor.swipeAction = OnSwipe;
		inputProcessor.pressAction = OnPress;
		inputProcessor.releaseAction = OnRelease;
		_dicInputProcessor.Add(pointerId, inputProcessor);
		return inputProcessor;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnPointerDown(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnPointerUp(eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnBeginDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnDrag(eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnEndDrag(eventData);
	}

	//public void Initialize(ActionController actionController, ActorStatus actorStatus)
	//{
	//	_actionController = actionController;
	//	_actorStatus = actorStatus;
	//}

	void Update()
	{
		Dictionary<int, InputProcessor>.Enumerator e = _dicInputProcessor.GetEnumerator();
		while (e.MoveNext())
		{
			e.Current.Value.Update();
		}

		if (centerRotationImageTransform != null && centerRotationImageTransform.gameObject.activeSelf)
		{
			centerRotationImageTransform.rotation = Quaternion.Slerp(centerRotationImageTransform.rotation, lineImageRectTransform.rotation, Time.deltaTime * centerRotationSlerpPower);
		}
	}

	void LateUpdate()
	{
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList[i] = false;
	}

	int _lastDragPointerId = -1;
	public void OnDragAction(PointerEventData eventData)
	{
		// for multi touch
		if (eventData.pointerId < _lastDragPointerId)
			return;

		#region VirtualAxis
		Vector3 newPos = Vector3.zero;
		newPos.x = (int)(eventData.position.x - eventData.pressPosition.x);
		newPos.y = (int)(eventData.position.y - eventData.pressPosition.y);
		UpdateVirtualAxes(newPos);
		#endregion

		joystickImageTransform.gameObject.SetActive(true);
		centerImageTransform.gameObject.SetActive(true);
		if (centerRotationImageTransform != null) centerRotationImageTransform.gameObject.SetActive(true);

		centerImageTransform.transform.position = eventData.pressPosition;
		if (centerRotationImageTransform != null) centerRotationImageTransform.position = eventData.pressPosition;
		joystickImageTransform.transform.position = eventData.position;

		float lineLength = Vector3.Distance(eventData.position, eventData.pressPosition);
		lineLength = lineLength * (512.0f / Screen.height);
		if (lineStartOffset > 0.0f) lineLength -= lineStartOffset;
		if (lineEndOffset > 0.0f) lineLength -= lineEndOffset;
		if (lineLength > 0.0f)
		{
			lineImageRectTransform.gameObject.SetActive(true);
			lineImageRectTransform.sizeDelta = new Vector2(1.0f, lineLength);
			Vector2 diff = eventData.position - eventData.pressPosition;
			lineImageRectTransform.transform.position = eventData.pressPosition + new Vector2(diff.normalized.x * lineStartOffset * (Screen.height / 512.0f), diff.normalized.y * lineStartOffset * (Screen.height / 512.0f));
			diff.x = -diff.x;
			lineImageRectTransform.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(diff.x, diff.y) * Mathf.Rad2Deg);
		}

		_lastDragPointerId = eventData.pointerId;
	}

	public void OnDragging(Vector2 delta)
	{
		/*
		Vector3 direction = CameraManager.Instance.GetMovementAxis(delta.x, delta.y);
		direction.y = 0.0f;

		_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Drag);

		if (_actionController.mecanimState.IsState((int)eMecanimState.Move))
		{
			_movementController.MoveDirection(direction, _actorStatus.GetValue(eActorStatus.MoveSpeed));
			_movementController.RotateToDirection(direction, 15.0f, true);
		}
		*/


		//if (!MecanimStateUtil.IsMovable(_actionController.mecanimState))
		//	direction = Vector2.zero;
	}

	public void OnEndDrag()
	{
		#region VirtualAxis
		UpdateVirtualAxes(Vector3.zero);
		#endregion

		joystickImageTransform.gameObject.SetActive(false);
		centerImageTransform.gameObject.SetActive(false);
		if (centerRotationImageTransform != null) centerRotationImageTransform.gameObject.SetActive(false);
		lineImageRectTransform.gameObject.SetActive(false);

		// for multi touch
		if (Input.touchCount == 0)
			_lastDragPointerId = -1;

		/*
		//_movementBase.MoveDirection(Vector2.zero, 2.0f);
		if (_actionController.mecanimState.IsState((int)eMecanimState.Move))
		{
			_actionController.PlayActionByActionName("Idle", true);
		}
		*/
	}

	public void OnHold()
	{
		/*
		_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Hold);
		*/
		_touchEventResultList[(int)Control.eInputType.Hold] = true;
	}

	public void OnSwipe()
	{
		_touchEventResultList[(int)Control.eInputType.Swipe] = true;
	}

	public void OnTab()
	{
		/*
		_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Tab);
		*/
		_touchEventResultList[(int)Control.eInputType.Tab] = true;
	}

	public void OnPress()
	{
		//_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Press);
		_touchEventResultList[(int)Control.eInputType.Press] = true;
	}

	public void OnRelease()
	{
		//_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Release);
		_touchEventResultList[(int)Control.eInputType.Release] = true;
	}

	public bool CheckInput(Control.eInputType eInputType)
	{
		switch (eInputType)
		{
			case Control.eInputType.Drag:
			case Control.eInputType.Tab:
			case Control.eInputType.Hold:
			case Control.eInputType.Swipe:
			case Control.eInputType.Press:
			case Control.eInputType.Release:
				return _touchEventResultList[(int)eInputType];
		}
		return false;
	}
}