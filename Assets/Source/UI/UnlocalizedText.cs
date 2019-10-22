using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnlocalizedText : MonoBehaviour
{
	Text _text;

	// Start is called before the first frame update
	void Start()
	{
		_text = GetComponent<Text>();
		RefreshFont();
	}

	void RefreshFont()
	{
		if (_text == null)
			return;

#if UNITY_EDITOR
		if (UIString.instance.IsDoneLoadAsyncFont() == false)
		{
			_text.fontStyle = FontStyle.Bold;
			return;
		}
#endif

		_text.font = UIString.instance.GetUnlocalizedFont();
		_text.fontStyle = UIString.instance.useSystemUnlocalizedFont ? FontStyle.Bold : FontStyle.Normal;
	}
}