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

	public void ApplyUIDragThreshold()
	{
		eventSystem.pixelDragThreshold = (int)(uiDragThresholdCM * Screen.dpi / inchToCm);
	}

	public void ResetUIDragThreshold()
	{
		eventSystem.pixelDragThreshold = _defaultPixelDragThreshold;
	}
}
