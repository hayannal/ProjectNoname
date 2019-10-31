using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpIndicatorButton : MonoBehaviour
{
	public Image iconImage;
	public Image titleImage;
	public Image backgroundImage;
	public Coffee.UIExtensions.UIGradient uIGradient;
	public Text[] nameTextList;
	public Text[] descTextList;

	Color normalTitleColor = Color.white;
	Color normalGradientColor = Color.black;
	Color exclusiveTitleColor = new Color(1.0f, 0.5f, 0.0f);
	Color exclusiveGradientColor = new Color(171.0f / 255.0f, 90.0f / 255.0f, 27.0f / 255.0f);

	public void SetInfo()
	{

	}
}
