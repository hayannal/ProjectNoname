using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Extension methods must be defined in a static class
public static class TextExtension
{
	// This is the extension method.
	// The first parameter takes the "this" modifier
	// and specifies the type for which the method is defined.
	public static void SetText(this Text textComponent, string text)
	{
		//UIString.instance.CheckFont(textComponent);
		textComponent.text = text;
	}
}

public class LocalizedText : MonoBehaviour {

    public string uiStringKey;
    Text _text;

	void Start()
	{
        _text = GetComponent<Text>();
		RefreshText();
	}

	void RefreshText()
	{
		if (_text == null)
			return;
		
		if (string.IsNullOrEmpty(uiStringKey) == false)
			_text.SetText(UIString.instance.GetString(uiStringKey));
	}






	void Awake()
	{
		LocalizedText.Push(this);
	}

	void OnDestroy()
	{
		LocalizedText.Pop(this);
	}

#region Manager
	static List<LocalizedText> s_localizedTextList;
	public static void Push(LocalizedText localizedText)
	{
		if (s_localizedTextList == null)
			s_localizedTextList = new List<LocalizedText>();

		s_localizedTextList.Add(localizedText);
	}

	public static void Pop(LocalizedText localizedText)
	{
		if (s_localizedTextList == null)
			return;

		s_localizedTextList.Remove(localizedText);
	}

	public static void OnChangeRegion()
	{
		if (s_localizedTextList == null)
			return;

		for (int i = 0; i < s_localizedTextList.Count; ++i)
			s_localizedTextList[i].RefreshText();
	}
#endregion
}
