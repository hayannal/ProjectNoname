using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DelayedLoadingCanvas : MonoBehaviour
{
	public static DelayedLoadingCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.delayedLoadingCanvasPrefab).GetComponent<DelayedLoadingCanvas>();
			}
			return _instance;
		}
	}
	static DelayedLoadingCanvas _instance = null;

	public CanvasGroup objectCanvasGroup;

	void OnEnable()
	{
		objectCanvasGroup.alpha = 0.0f;
		objectCanvasGroup.gameObject.SetActive(false);
	}
}
