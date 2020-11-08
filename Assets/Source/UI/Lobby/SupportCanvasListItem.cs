using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SupportCanvasListItem : MonoBehaviour
{
	public RectTransform offsetRootTransform;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Text titleText;

	public int index { get; set; }
	public SupportData.MySupportData mySupportData { get; set; }
	public void Initialize(int index, SupportData.MySupportData data)
	{
		this.index = index;
		mySupportData = data;

		// 타이틀은 data에서 첫번째 줄을 가져와서 보여준다.
		string str = "";
		if (string.IsNullOrEmpty(data.sid))
			str = data.body;
		else
			str = UIString.instance.GetString(data.sid);

		int lineIndex = str.IndexOfAny(new char[] { '\r', '\n' });
		string firstline = lineIndex == -1 ? str : str.Substring(0, lineIndex);
		titleText.SetLocalizedText(firstline);

		bool mySupport = (data.type == 0);
		if (mySupport)
		{
			offsetRootTransform.anchoredPosition = new Vector3(14.0f, 0.0f);
			blurImage.color = new Color(0.311f, 0.311f, 0.311f, 0.0f);
			gradient.color1 = new Color(0.160f, 0.160f, 0.160f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
		else
		{
			offsetRootTransform.anchoredPosition = new Vector3(-14.0f, 0.0f);
			blurImage.color = new Color(0.145f, 0.528f, 0.792f, 0.0f);
			gradient.color1 = new Color(0.224f, 0.582f, 0.896f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
	}

	public void OnClickButton()
	{
		SupportListCanvas.instance.OnClickListItem(mySupportData);
		SoundManager.instance.PlaySFX("GridOn");
	}
}