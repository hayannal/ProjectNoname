using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class CurrencySmallInfoCanvas : MonoBehaviour
{
	static CurrencySmallInfoCanvas _instance = null;

	static int s_refCount = 0;
	public static void Show(bool show)
	{
		if (show)
		{
			if (IsShow())
			{
				if (_instance._reserveHide)
				{
					_instance._reserveHide = false;
					_instance.gameObject.SetActive(false);
					Show(true);
					return;
				}

				++s_refCount;
				return;
			}
			UIInstanceManager.instance.ShowCanvasAsync("CurrencySmallInfoCanvas", null);
			++s_refCount;
		}
		else
		{
			if (_instance == null)
				return;
			--s_refCount;
			if (s_refCount <= 0)
				_instance.HideWithTween();
		}
	}

	public static bool IsShow()
	{
		if (_instance != null && _instance.gameObject.activeSelf)
			return true;
		return false;
	}

	public static void RefreshInfo()
	{
		if (IsShow())
			_instance.InternalRefreshInfo();
	}

	public Text diamondText;
	public Text goldText;
	public Transform diamondIconTransform;
	public Transform goldIconTransform;
	public DOTweenAnimation moveTweenAnimation;

	void Awake()
	{
		_instance = this;
	}

	void OnEnable()
	{
		InternalRefreshInfo();
	}

	public void InternalRefreshInfo()
	{
		diamondText.text = CurrencyData.instance.dia.ToString();
		goldText.text = CurrencyData.instance.gold.ToString();
	}

	bool _reserveHide = false;
	void HideWithTween()
	{
		if (_reserveHide)
			return;

		_reserveHide = true;
		moveTweenAnimation.DOPlayBackwards();
		Timing.RunCoroutine(DelayedDisable(moveTweenAnimation.duration));
	}

	IEnumerator<float> DelayedDisable(float delay)
	{
		yield return Timing.WaitForSeconds(delay);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		if (_reserveHide)
		{
			gameObject.SetActive(false);
			_reserveHide = false;
		}
	}

	public void OnClickDiamondButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.LeftBottom, UIString.instance.GetString("GameUI_ChaosModeDesc"), 200, diamondIconTransform, new Vector2(-40.0f, 0.0f));
	}

	public void OnClickGoldButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.LeftBottom, UIString.instance.GetString("GameUI_ChaosModeDesc"), 200, goldIconTransform, new Vector2(-40.0f, 0.0f));
	}
}