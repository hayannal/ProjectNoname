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
	public void Initialize(int index)
	{
		this.index = index;

		titleText.SetLocalizedText("");
		bool mySupport = false;
		if (mySupport)
		{
			offsetRootTransform.anchoredPosition = new Vector3(14.0f, 0.0f);
			blurImage.color = new Color(0.192f, 0.866f, 0.819f, 0.0f);
			gradient.color1 = new Color(0.117f, 0.914f, 0.914f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
		else
		{
			offsetRootTransform.anchoredPosition = new Vector3(-14.0f, 0.0f);
			blurImage.color = new Color(0.323f, 0.623f, 0.443f, 0.0f);
			gradient.color1 = new Color(0.111f, 0.411f, 0.094f);
			gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
		}
	}

	public void OnClickButton()
	{
		SupportListCanvas.instance.OnClickListItem(index);
		SoundManager.instance.PlaySFX("GridOn");
	}


	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}