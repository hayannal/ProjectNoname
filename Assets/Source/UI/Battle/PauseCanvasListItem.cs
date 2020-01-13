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
	public Image titleImage;
	public Text exclusiveText;
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

		gradient.color2 = levelPackInfo.colored ? LevelUpIndicatorButton.s_coloredGradientColor : Color.white;
		titleImage.color = levelPackInfo.colored ? LevelUpIndicatorButton.s_coloredTitleColor : Color.white;
		levelText.text = UIString.instance.GetString("GameUI_Lv", levelPackInfo.stackCount);
		exclusiveText.gameObject.SetActive(levelPackInfo.exclusive);

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
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.unscaledDeltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.unscaledDeltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}
