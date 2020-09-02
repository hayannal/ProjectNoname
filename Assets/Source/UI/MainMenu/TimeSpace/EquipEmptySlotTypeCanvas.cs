using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipEmptySlotTypeCanvas : MonoBehaviour
{
	public static EquipEmptySlotTypeCanvas instance;

	public CanvasGroup canvasGroup;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
	}

	public void ShowInfo()
	{
	}
}