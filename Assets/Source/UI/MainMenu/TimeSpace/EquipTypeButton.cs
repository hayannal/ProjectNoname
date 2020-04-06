using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipTypeButton : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image backgroundImage;
	public int positionIndex;

	public bool selected { get; set; }
	void Update()
	{
		UpdateSelectPosition();
		UpdateSelectImage();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-4.0f, 3.0f);
		if (selected)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 8.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 8.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}

	void UpdateSelectImage()
	{
		Color selectColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		Color deselectColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
		if (selected)
		{
			if (backgroundImage.color.a != selectColor.a)
			{
				backgroundImage.color = Color.Lerp(backgroundImage.color, selectColor, Time.deltaTime * 5.0f);
				float diff = Mathf.Abs(backgroundImage.color.a - selectColor.a);
				if (diff < 0.01f)
					backgroundImage.color = selectColor;
			}
		}
		else
		{
			if (backgroundImage.color.a != deselectColor.a)
			{
				backgroundImage.color = Color.Lerp(backgroundImage.color, deselectColor, Time.deltaTime * 5.0f);
				float diff = Mathf.Abs(backgroundImage.color.a - deselectColor.a);
				if (diff < 0.01f)
					backgroundImage.color = deselectColor;
			}
		}
	}
}