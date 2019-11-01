using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpIndicatorButton : MonoBehaviour
{
	public Text nameTextList;
	public Image iconImage;
	public Coffee.UIExtensions.UIGradient gradient;

	Color normalGradientColor = Color.white;
	Color exclusiveGradientColor = new Color(1.0f, 0.5f, 0.0f);

	public void SetInfo(LevelPackTableData levelPackTableData)
	{
		nameTextList.SetLocalizedText(UIString.instance.GetString(levelPackTableData.nameId));
		gradient.color2 = levelPackTableData.exclusive ? exclusiveGradientColor : normalGradientColor;
	}
}
