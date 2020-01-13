using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpIndicatorButton : MonoBehaviour
{
	public Text nameTextList;
	public Image iconImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text levelText;
	public Image titleImage;
	public Text exclusiveText;

	public static Color s_coloredGradientColor = new Color(1.0f, 0.5f, 0.0f);
	public static Color s_coloredTitleColor = new Color(1.0f, 0.627f, 0.25f);

	string _id;
	public void SetInfo(LevelPackTableData levelPackTableData, int level)
	{
		AddressableAssetLoadManager.GetAddressableSprite(levelPackTableData.iconAddress, "Icon", (sprite) =>
		{
			iconImage.sprite = null;
			iconImage.sprite = sprite;
		});

		_id = levelPackTableData.levelPackId;
		nameTextList.SetLocalizedText(UIString.instance.GetString(levelPackTableData.nameId));
		gradient.color2 = levelPackTableData.colored ? s_coloredGradientColor : Color.white;
		titleImage.color = levelPackTableData.colored ? s_coloredTitleColor : Color.white;
		levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", level);
		exclusiveText.gameObject.SetActive(levelPackTableData.exclusive);
	}

	public void OnClickButton()
	{
		if (exclusiveText.gameObject.activeSelf)
		{
			LevelUpIndicatorCanvas.OnClickExclusiveCloseButton();
			return;
		}

		LevelUpIndicatorCanvas.OnSelectLevelPack(_id);
	}
}
