using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
	public RectTransform foregroundTransform;
	public Text menuText;
	public RectTransform selectBarImageTransform;
	public bool isOn { get; set; }

	void Start()
	{
		if (menuText != null)
			menuText.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	}

	void Update()
	{
		UpdateSelectPosition();
		UpdateTextAlpha();
		UpdateSelectBarImageTransform();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-8.0f, 8.0f);
		if (isOn)
		{
			if (foregroundTransform.anchoredPosition != selectOffset)
			{
				foregroundTransform.anchoredPosition = Vector2.Lerp(foregroundTransform.anchoredPosition, selectOffset, Time.deltaTime * 15.0f);
				Vector2 diff = foregroundTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					foregroundTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (foregroundTransform.anchoredPosition != Vector2.zero)
			{
				foregroundTransform.anchoredPosition = Vector2.Lerp(foregroundTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 15.0f);
				Vector2 diff = foregroundTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					foregroundTransform.anchoredPosition = Vector2.zero;
			}
		}
	}

	void UpdateTextAlpha()
	{
		if (menuText == null)
			return;

		Color transparentColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		if (isOn)
		{
			if (menuText.color != Color.white)
			{
				menuText.color = Color.Lerp(menuText.color, Color.white, Time.deltaTime * 20.0f);
				float diff = 1.0f - menuText.color.a;
				if (diff < 0.04f)
					menuText.color = Color.white;
			}
		}
		else
		{
			if (menuText.color != transparentColor)
			{
				menuText.color = Color.Lerp(menuText.color, transparentColor, Time.deltaTime * 20.0f);
				float diff = menuText.color.a;
				if (diff < 0.04f)
					menuText.color = transparentColor;
			}
		}
	}

	void UpdateSelectBarImageTransform()
	{
		if (selectBarImageTransform == null)
			return;

		if (isOn)
		{
			if (selectBarImageTransform.localScale.x != 1.0f)
			{
				selectBarImageTransform.localScale = new Vector3(selectBarImageTransform.localScale.x + Time.deltaTime * 6.0f, 1.0f, 1.0f);
				//selectBarImageTransform.localScale = Vector3.Lerp(selectBarImageTransform.localScale, Vector3.one, Time.deltaTime * 15.0f);
				float diff = 1.0f - selectBarImageTransform.localScale.x;
				if (diff < 0.01f)
					selectBarImageTransform.localScale = Vector3.one;
			}
		}
		else
		{
			if (selectBarImageTransform.localScale.x != 0.0f)
			{
				selectBarImageTransform.localScale = new Vector3(selectBarImageTransform.localScale.x - Time.deltaTime * 6.0f, 1.0f, 1.0f);
				//selectBarImageTransform.localScale = Vector3.Lerp(selectBarImageTransform.localScale, new Vector3(0.0f, 1.0f, 1.0f), Time.deltaTime * 15.0f);
				if (selectBarImageTransform.localScale.x < 0.01f)
					selectBarImageTransform.localScale = new Vector3(0.0f, 1.0f, 1.0f);
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