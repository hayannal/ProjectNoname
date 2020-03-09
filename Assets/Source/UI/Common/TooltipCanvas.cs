using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using DG.Tweening;

public class TooltipCanvas : MonoBehaviour
{
	static TooltipCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.tooltipCanvasPrefab).GetComponent<TooltipCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static TooltipCanvas _instance = null;

	public enum eDirection
	{
		Bottom,
		Top,
		LeftBottom,
	}

	public static void Show(bool show, eDirection direction, string text, float textWidth, Transform targetTransform, Vector2 offset)
	{
		if (show)
		{
			instance.SetDirectionType(direction);
			instance.SetTooltipText(text, textWidth);
			instance.SetTextPosition(targetTransform, offset);
			instance.gameObject.SetActive(true);
			instance.PlayStartAnimation();
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

	private void OnEnable()
	{
		PlayStartAnimation();
	}

	public GameObject[] rootList;
	public Text[] tooltipTextList;
	public RectTransform[] tooltipTextRectTransform;

	void Update()
	{
		if (Input.GetMouseButtonUp(0))
			gameObject.SetActive(false);
	}

	int _currentIndex = 0;
	void SetDirectionType(eDirection direction)
	{
		_currentIndex = (int)direction;
		for (int i = 0; i < rootList.Length; ++i)
			rootList[i].SetActive(i == _currentIndex);
	}

	void SetTooltipText(string text, float textWidth)
	{
		tooltipTextRectTransform[_currentIndex].sizeDelta = new Vector2(textWidth, tooltipTextRectTransform[_currentIndex].sizeDelta.y);
		tooltipTextList[_currentIndex].SetLocalizedText(text);
	}

	void SetTextPosition(Transform targetTransform, Vector2 offset)
	{
		cachedRectTransform.SetParent(targetTransform.parent);
		cachedRectTransform.localScale = Vector3.one;
		cachedRectTransform.localRotation = Quaternion.identity;

		// 해상도 달라질때는 offset도 달라져야한다.
		if (cachedRectTransform.parent != null)
		{
			Canvas parentCanvas = cachedRectTransform.parent.GetComponentInParent<Canvas>();
			if (parentCanvas != null)
			{
				offset.x *= parentCanvas.transform.localScale.x;
				offset.y *= parentCanvas.transform.localScale.y;
			}
		}
		tooltipTextRectTransform[_currentIndex].position = targetTransform.position + new Vector3(offset.x, offset.y, 0.0f);
	}

	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}

	#region Animation
	public float downScale = 0.2f;
	public float startScale = 1.2f;
	public float startAnimationDuration = 0.3f;

	void PlayStartAnimation()
	{
		tooltipTextRectTransform[_currentIndex].localScale = new Vector3(downScale, downScale, 1.0f);
		tooltipTextRectTransform[_currentIndex].DOScale(startScale, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale);
	}

	void OnCompleteScale()
	{
		tooltipTextRectTransform[_currentIndex].DOScale(1.0f, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad);
	}
	#endregion
}