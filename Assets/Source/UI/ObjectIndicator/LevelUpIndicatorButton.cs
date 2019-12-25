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

	public static Color s_exclusiveGradientColor = new Color(1.0f, 0.5f, 0.0f);

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
		gradient.color2 = levelPackTableData.exclusive ? s_exclusiveGradientColor : Color.white;
		levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", level);
	}

	public void OnClickButton()
	{
		LevelUpIndicatorCanvas.OnSelectLevelPack(_id);
	}
}
