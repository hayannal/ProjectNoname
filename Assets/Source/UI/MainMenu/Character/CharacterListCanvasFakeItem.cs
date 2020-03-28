using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterListCanvasFakeItem : MonoBehaviour
{
	public GameObject lineObject;

    public void Initialize(bool showLine)
	{
		lineObject.SetActive(showLine);
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
