using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpIndicatorButton : MonoBehaviour
{
	public Image iconImage;
	public Image titleImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text[] nameTextList;
	public Text[] descTextList;

	Color normalTitleColor = Color.white;
	Color normalGradientColor = Color.black;
	Color exclusiveTitleColor = new Color(1.0f, 0.5f, 0.0f);
	Color exclusiveGradientColor = new Color(171.0f / 255.0f, 90.0f / 255.0f, 27.0f / 255.0f);

	public void SetInfo(LevelPackTableData levelPackTableData)
	{
		titleImage.color = levelPackTableData.exclusive ? exclusiveTitleColor : normalTitleColor;
		gradient.color1 = levelPackTableData.exclusive ? exclusiveGradientColor : normalGradientColor;

		for (int i = 0; i < nameTextList.Length; ++i)
			nameTextList[i].SetLocalizedText(UIString.instance.GetString(levelPackTableData.nameId));
		for (int i = 0; i < descTextList.Length; ++i)
			descTextList[i].SetLocalizedText(UIString.instance.GetString(levelPackTableData.descriptionId));
	}
}
