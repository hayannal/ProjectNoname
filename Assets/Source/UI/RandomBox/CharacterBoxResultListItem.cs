using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBoxResultListItem : MonoBehaviour
{
	public SwapCanvasListItem characterListItem;
	public Text commentText;
	public Text ppText;

	public void Initialize(string stringId, int pp)
	{
		commentText.gameObject.SetActive(pp == 0);
		ppText.gameObject.SetActive(pp > 0);

		if (pp > 0)
			ppText.text = string.Format("+{0:N0}", pp);
		else
			commentText.SetLocalizedText(UIString.instance.GetString(stringId));
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