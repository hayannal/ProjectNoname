using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
	public RectTransform foregroundTransform;
	public Text menuText;
	public bool isOn { get; set; }

	void Start()
	{
		menuText.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	}

	void Update()
	{
		UpdateSelectPosition();
		UpdateTextAlpha();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-10.0f, 10.0f);
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