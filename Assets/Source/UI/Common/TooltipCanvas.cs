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
		CharacterInfo,
	}

	public static void Show(bool show, eDirection direction, string text, float textWidth, Transform targetTransform, Vector2 offset)
	{
		if (show)
		{
			if (_instance != null && _instance.gameObject != null && _instance.gameObject.activeSelf)
			{
				// 같을때는 Ignore하면 안된다. 그래서 인자를 전달한다.
				_instance.CheckIgnoreHideFrameCount(direction, text, textWidth, targetTransform, offset);
			}

			instance.SetDirectionType(direction);
			instance.SetTooltipText(text, textWidth);
			instance.SetTextPosition(targetTransform, offset);
			instance.gameObject.SetActive(true);
			instance.RefreshSortingOrder();
			instance.PlayStartAnimation();
		}
		else
		{
			if (_instance == null)
				return;
			_instance.gameObject.SetActive(false);
		}
	}

	public static void Hide()
	{
		if (_instance == null)
			return;
		_instance.gameObject.SetActive(false);
	}

	Canvas _canvas;
	void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}

	void OnEnable()
	{
		PlayStartAnimation();
	}

	void RefreshSortingOrder()
	{
		// 카메라 공간쪽에 보여진채로 오버레이쪽에 보여지면 이상하게 안보인다. 아래 라인을 호출하면 보이길래 추가해둔다.
		_canvas.sortingOrder = _canvas.sortingOrder;
	}

	public GameObject[] rootList;
	public Text[] tooltipTextList;
	public RectTransform[] tooltipTextRectTransform;

	public void CheckIgnoreHideFrameCount(eDirection direction, string text, float textWidth, Transform targetTransform, Vector2 offset)
	{
		bool differentTooltip = false;
		// 다 검사하기엔 너무 많아서..
		if ((int)direction != _currentIndex || cachedRectTransform.parent != targetTransform.parent)
			differentTooltip = true;

		if (differentTooltip)
			_ignoreFrameCount = 1;
	}

	int _ignoreFrameCount;
	void Update()
	{
		if (_ignoreFrameCount > 0)
		{
			--_ignoreFrameCount;
			return;
		}

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
		tooltipTextRectTransform[_currentIndex].DOScale(startScale, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale).SetUpdate(true);
	}

	void OnCompleteScale()
	{
		tooltipTextRectTransform[_currentIndex].DOScale(1.0f, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
	}
	#endregion
}