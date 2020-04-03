using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image equipIconImage;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;
	public Text enhanceLevelText;
	public GameObject selectObject;

	public EquipData equipData { get; set; }
	public void Initialize(EquipData equipData, Action<EquipData> clickCallback)
	{
		this.equipData = equipData;

		AddressableAssetLoadManager.GetAddressableSprite(equipData.cachedEquipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		if (equipData.enhanceLevel > 0)
			enhanceLevelText.text = string.Format("+{0}", equipData.enhanceLevel);
		else
			enhanceLevelText.text = "";

		switch (equipData.cachedEquipTableData.grade)
		{
			case 0:
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				blurImage.color = new Color(0.28f, 1.0f, 0.78f, 0.0f);
				gradient.color1 = new Color(0.0f, 1.0f, 0.7f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 1.0f, 0.51f);
				break;
			case 2:
				blurImage.color = new Color(0.28f, 0.78f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 3:
				blurImage.color = new Color(0.73f, 0.31f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.66f, 0.0f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.63f, 0.0f, 1.0f);
				break;
			case 4:
				blurImage.color = new Color(1.0f, 0.78f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}
		selectObject.SetActive(false);
		_clickAction = clickCallback;
	}

	Action<EquipData> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction != null)
			_clickAction.Invoke(equipData);
	}

	public void ShowSelectObject(bool show)
	{
		selectObject.SetActive(show);
	}

	void Update()
	{
		UpdateSelectPosition();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-13.0f, 8.0f);
		if (selectObject.activeSelf)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
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
}