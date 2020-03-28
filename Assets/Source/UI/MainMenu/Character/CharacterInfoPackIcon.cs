using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPackIcon : MonoBehaviour
{
	public Image iconImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image titleImage;
	public Text exclusiveText;

	public void Initialize(LevelPackTableData levelPackTableData, int level)
	{
		AddressableAssetLoadManager.GetAddressableSprite(levelPackTableData.iconAddress, "Icon", (sprite) =>
		{
			iconImage.sprite = null;
			iconImage.sprite = sprite;
		});

		gradient.color2 = levelPackTableData.colored ? LevelUpIndicatorButton.s_coloredGradientColor : Color.white;
		titleImage.color = levelPackTableData.colored ? LevelUpIndicatorButton.s_coloredTitleColor : Color.white;
		exclusiveText.text = UIString.instance.GetString("GameUI_Lv", level);
	}
}