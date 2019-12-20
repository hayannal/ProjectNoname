using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputProcessor
{
	public Action<Vector2> tabAction;
	public Action doubleTabAction;
	public Action endDragAction;
	public Action holdAction;
	public Action swipeAction;
	public Action<Vector2> draggingAction;
	public Action pressAction;
	public Action releaseAction;
	
	bool _pressed = false;
	float _pressedTime = 0.0f;
	Vector2 _pressPosition = Vector2.zero;
	Vector2 _position = Vector2.zero;
	bool _beginDrag = false;
	bool _beginHold = false;
	float _lastTabTime;
	const float _doubleTabInterval = 0.25f;

	bool _enabled = true;
	public bool enabled { set { _enabled = value; } }

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!_enabled) return;
		
		_pressed = true;
		_pressedTime = Time.time;
		_pressPosition = eventData.pressPosition;
		_position = eventData.position;
		_beginDrag = false;
		_beginHold = false;

		if (pressAction != null)
			pressAction();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!_enabled) return;

		if (releaseAction != null)
			releaseAction();
		
		if (_beginDrag == false && _beginHold == false)
		{
			if (tabAction != null)
				tabAction(eventData.position);

			if (Time.time - _lastTabTime < _doubleTabInterval)
			{
				// 더블탭을 탭과 완벽하게 구분해서 지원하는게 아니라서 탭 이벤트와 함께 발생된다.
				if (doubleTabAction != null)
					doubleTabAction();
			}
			_lastTabTime = Time.time;
		}

		if (_beginDrag == true)
		{
			if (endDragAction != null)
				endDragAction();
		}

		_pressed = false;
		_beginDrag = false;
		_beginHold = false;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!_enabled) return;

		_beginDrag = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!_enabled) return;

		_position = eventData.position;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_enabled) return;

		_beginDrag = false;

		if (eventData.delta.sqrMagnitude < 2.0f * 2.0f)
			return;

		if (swipeAction != null)
			swipeAction();
	}

	public void Update()
	{
		if (!_enabled) return;
		if (!_pressed) return;

		if (Time.time - _pressedTime > 0.5f && _beginDrag == false && _beginHold == false)
		{
			if (holdAction != null)
				holdAction();
		}

		if (_beginDrag)
		{
			if (draggingAction != null)
				draggingAction(_position - _pressPosition);
		}
	}
}
