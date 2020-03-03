using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WaitingNetworkCanvas : MonoBehaviour
{
	static WaitingNetworkCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.waitingNetworkCanvasPrefab).GetComponent<WaitingNetworkCanvas>();
			}
			return _instance;
		}
	}
	static WaitingNetworkCanvas _instance = null;

	public static void Show(bool show)
	{
		if (show)
		{
			if (IsShow())
				return;
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

	public static bool IsShow()
	{
		if (_instance != null && _instance.gameObject.activeSelf)
			return true;
		return false;
	}

	public CanvasGroup objectCanvasGroup;

	void OnEnable()
	{
		objectCanvasGroup.alpha = 0.0f;
		objectCanvasGroup.gameObject.SetActive(false);
	}
}