using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
//using MecanimStateDefine;
//using ActorStatusDefine;

public class ScreenJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public static ScreenJoystick instance;

	public Image joystickImage;
	public Image centerImage;
	public Image lineImage;

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
	}

	void LateUpdate()
	{
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList[i] = false;
	}

	public void OnDragAction(PointerEventData eventData)
	{
		#region VirtualAxis
		Vector3 newPos = Vector3.zero;
		newPos.x = (int)(eventData.position.x - eventData.pressPosition.x);
		newPos.y = (int)(eventData.position.y - eventData.pressPosition.y);
		//transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
		UpdateVirtualAxes(newPos);
		#endregion

		joystickImage.gameObject.SetActive(true);
		centerImage.gameObject.SetActive(true);
		lineImage.gameObject.SetActive(true);

		centerImage.transform.position = eventData.pressPosition;
		lineImage.transform.position = eventData.pressPosition;
		joystickImage.transform.position = eventData.position;

		lineImage.GetComponent<RectTransform>().sizeDelta = new Vector2(1.0f, Vector3.Distance(eventData.position, eventData.pressPosition) * (512.0f / Screen.height));
		Vector3 diff = eventData.position - eventData.pressPosition;
		diff.x = -diff.x;
		lineImage.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(diff.x, diff.y) * Mathf.Rad2Deg);		
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
		//transform.position = m_StartPos;
		UpdateVirtualAxes(Vector3.zero);
		#endregion

		joystickImage.gameObject.SetActive(false);
		centerImage.gameObject.SetActive(false);
		lineImage.gameObject.SetActive(false);

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