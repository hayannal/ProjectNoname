using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image iconImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text levelText;
	public GameObject selectObject;

	public SkillProcessor.LevelPackInfo levelPackInfo { get; private set; }
	public void Initialize(SkillProcessor.LevelPackInfo levelPackInfo)
	{
		AddressableAssetLoadManager.GetAddressableSprite(levelPackInfo.iconAddress, "Icon", (sprite) =>
		{
			iconImage.sprite = null;
			iconImage.sprite = sprite;
		});

		this.levelPackInfo = levelPackInfo;

		gradient.color2 = levelPackInfo.exclusive ? LevelUpIndicatorButton.s_exclusiveGradientColor : Color.white;
		levelText.text = UIString.instance.GetString("GameUI_Lv", levelPackInfo.stackCount);

		selectObject.SetActive(false);
	}

	public void OnClickButton()
	{
		PauseCanvas.instance.OnClickListItem(levelPackInfo);
		selectObject.SetActive(true);
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
}
