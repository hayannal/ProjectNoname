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
	public float moveDragThresholdLength = 25.0f;

	public string horizontalAxisName = "Horizontal"; // The name given to the horizontal axis for the cross platform input
	public string verticalAxisName = "Vertical"; // The name given to the vertical axis for the cross platform input

	CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis; // Reference to the joystick in the cross platform input
	CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis; // Reference to the joystick in the cross platform input

	List<bool> _touchEventResultList = null;
	Dictionary<int, InputProcessor> _dicInputProcessor = new Dictionary<int, InputProcessor>();

	void Awake()
	{
		instance = this;

		_touchEventResultList = new List<bool>();
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList.Add(false);
	}

	float _canvasMatchWidthOrHeightSize = 1024.0f;
	float _lineLengthRatio;
	float _lineOffsetRatio;
	void Start()
	{
		CanvasScaler parentCanvasScaler = GetComponentInParent<CanvasScaler>();
		if (parentCanvasScaler == null)
			return;

		if (parentCanvasScaler.matchWidthOrHeight == 0.0f)
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.x;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.width;
			_lineOffsetRatio = Screen.width / _canvasMatchWidthOrHeightSize;
		}
		else
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.y;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.height;
			_lineOffsetRatio = Screen.height / _canvasMatchWidthOrHeightSize;
		}
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
		inputProcessor.doubleTabAction = OnDoubleTab;
		inputProcessor.holdAction = OnHold;
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

		OnBeginDragJoystick(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnDrag(eventData);

		OnDragJoystick(eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		GetInputProcessor(eventData.pointerId).OnEndDrag(eventData);

		OnEndDragJoystick(eventData);
	}

	void Update()
	{
		Dictionary<int, InputProcessor>.Enumerator e = _dicInputProcessor.GetEnumerator();
		while (e.MoveNext())
		{
			e.Current.Value.Update();
		}

		if (centerRotationImageTransform != null && centerRotationImageTransform.gameObject.activeSelf)
		{
			centerRotationImageTransform.rotation = Quaternion.Slerp(centerRotationImageTransform.rotation, _lastDirectionQuaternion, Time.deltaTime * centerRotationSlerpPower);
		}
	}

	void LateUpdate()
	{
		for (int i = 0; i < (int)Control.eInputType.Amount; ++i)
			_touchEventResultList[i] = false;

		// 간혹 렉이 발생하면서 터치가 이미 끝났는데 계속 이동하는 경우가 있다.
		// 이때를 디버깅해서 보니 _lastDragPointerId 가 -1로 설정되어있었다. 이벤트 순서가 꼬인거 아닐까..
		// 아무튼 정상적인 상황에서는 동작하면 안되니 최대한 조건을 타이트하게 건다.
		if (_draggingCount > 0 && joystickImageTransform.gameObject.activeSelf && _lastDragPointerId == -1)
		{
#if UNITY_EDITOR
			bool noInput = (Input.anyKey == false);
#else
			bool noInput = (Input.touchCount == 0);
#endif
			if (noInput)
			{
				#region VirtualAxis
				UpdateVirtualAxes(Vector3.zero);
				#endregion

				HideJoystick();
				_draggingCount = 0;
			}
		}
	}

	void OnBeginDragJoystick(PointerEventData eventData)
	{
		// for multi touch
		++_draggingCount;
	}

	// for multi touch
	int _lastDragPointerId = -1;
	int _draggingCount = 0;
	Quaternion _lastDirectionQuaternion;
	void OnDragJoystick(PointerEventData eventData)
	{
		if (_lastDragPointerId == -1 && CheckThreshold(eventData) == false)
			return;

		// for multi touch
		if (_lastDragPointerId != -1 && eventData.pointerId < _lastDragPointerId)
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

		centerImageTransform.position = eventData.pressPosition;
		if (centerRotationImageTransform != null) centerRotationImageTransform.position = eventData.pressPosition;
		joystickImageTransform.position = eventData.position;

		Vector2 diff = eventData.position - eventData.pressPosition;
		_lastDirectionQuaternion = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);

		float lineLength = Vector3.Distance(eventData.position, eventData.pressPosition);
		lineLength = lineLength * _lineLengthRatio;
		if (lineStartOffset > 0.0f) lineLength -= lineStartOffset;
		if (lineEndOffset > 0.0f) lineLength -= lineEndOffset;
		if (lineLength > 0.0f)
		{
			lineImageRectTransform.gameObject.SetActive(true);
			lineImageRectTransform.sizeDelta = new Vector2(1.0f, lineLength);
			lineImageRectTransform.position = eventData.pressPosition + diff.normalized * lineStartOffset * _lineOffsetRatio;
			lineImageRectTransform.rotation = _lastDirectionQuaternion;
		}
		else
		{
			lineImageRectTransform.gameObject.SetActive(false);
		}

		// for multi touch
		_lastDragPointerId = eventData.pointerId;
	}

	void OnEndDragJoystick(PointerEventData eventData)
	{
		if (eventData.pointerId >= _lastDragPointerId)
		{
			#region VirtualAxis
			UpdateVirtualAxes(Vector3.zero);
			#endregion

			HideJoystick();
		}

		// for multi touch
		--_draggingCount;
		if (_draggingCount == 0)
			_lastDragPointerId = -1;
	}

	void HideJoystick()
	{
		joystickImageTransform.gameObject.SetActive(false);
		centerImageTransform.gameObject.SetActive(false);
		if (centerRotationImageTransform != null) centerRotationImageTransform.gameObject.SetActive(false);
		lineImageRectTransform.gameObject.SetActive(false);
	}

	bool CheckThreshold(PointerEventData eventData)
	{
		float lineLength = Vector3.Distance(eventData.position, eventData.pressPosition);
		lineLength = lineLength * _lineLengthRatio;
		if (lineLength < moveDragThresholdLength)
			return false;
		return true;
	}

	public void OnDragging(Vector2 delta)
	{
		//Vector3 direction = CameraManager.Instance.GetMovementAxis(delta.x, delta.y);
		//direction.y = 0.0f;

		//_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Drag);

		//if (_actionController.mecanimState.IsState((int)eMecanimState.Move))
		//{
		//	_movementController.MoveDirection(direction, _actorStatus.GetValue(eActorStatus.MoveSpeed));
		//	_movementController.RotateToDirection(direction, 15.0f, true);
		//}
	}

	public void OnEndDrag()
	{

	}

	public void OnHold()
	{
		_touchEventResultList[(int)Control.eInputType.Hold] = true;
	}

	public void OnSwipe()
	{
		_touchEventResultList[(int)Control.eInputType.Swipe] = true;
	}

	public Vector2 tabPosition { get; private set; }
	public void OnTab(Vector2 position)
	{
		//_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Tab);
		_touchEventResultList[(int)Control.eInputType.Tab] = true;
		tabPosition = position;
	}

	public void OnDoubleTab()
	{
		_touchEventResultList[(int)Control.eInputType.DoubleTab] = true;
	}

	public void OnPress()
	{
		////_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Press);
		_touchEventResultList[(int)Control.eInputType.Press] = true;
	}

	public void OnRelease()
	{
		////_actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Release);
		_touchEventResultList[(int)Control.eInputType.Release] = true;
	}

	public bool CheckInput(Control.eInputType eInputType)
	{
		switch (eInputType)
		{
			case Control.eInputType.Tab:
			case Control.eInputType.DoubleTab:
			case Control.eInputType.Hold:
			case Control.eInputType.Swipe:
			case Control.eInputType.Press:
			case Control.eInputType.Release:
				return _touchEventResultList[(int)eInputType];
		}
		return false;
	}
}