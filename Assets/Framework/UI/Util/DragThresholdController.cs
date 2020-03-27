using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragThresholdController : MonoBehaviour {

	public static DragThresholdController instance;

	private const float inchToCm = 2.54f;

	[SerializeField]
	private EventSystem eventSystem = null;

	[SerializeField]
	private float uiDragThresholdCM = 0.5f;
	//For drag Threshold

	int _defaultPixelDragThreshold;
	void Awake()
	{
		instance = this;
		_defaultPixelDragThreshold = eventSystem.pixelDragThreshold;
	}

	int _uiDragThresholdRefCount = 0;
	public void ApplyUIDragThreshold()
	{
		++_uiDragThresholdRefCount;
		if (_uiDragThresholdRefCount == 1)
			eventSystem.pixelDragThreshold = (int)(uiDragThresholdCM * Screen.dpi / inchToCm);
	}

	public void ResetUIDragThreshold()
	{
		--_uiDragThresholdRefCount;
		if (_uiDragThresholdRefCount < 0)
		{
			Debug.LogError("Invalid RefCount : Reference count is negative.");
			_uiDragThresholdRefCount = 0;
		}
		if (_uiDragThresholdRefCount == 0)
			eventSystem.pixelDragThreshold = _defaultPixelDragThreshold;
	}

	#region Global Input Lock
	public void EnableEventSystem(bool enable)
	{
		eventSystem.enabled = enable;
	}

	public bool IsEnableEventSystem() { return eventSystem.enabled; }
	#endregion
}
