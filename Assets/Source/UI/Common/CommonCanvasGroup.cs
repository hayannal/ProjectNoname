using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonCanvasGroup : MonoBehaviour
{
	public static CommonCanvasGroup instance = null;

	public GameObject toastCanvasPrefab;
	public GameObject fullscreenYesNoCanvasPrefab;
	public GameObject yesNoCanvasPrefab;
	public GameObject okCanvasPrefab;
	public GameObject delayedLoadingCanvasPrefab;

	void Awake()
	{
		instance = this;
	}
}
