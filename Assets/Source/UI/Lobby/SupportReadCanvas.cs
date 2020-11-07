using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SupportReadCanvas : MonoBehaviour
{
	public static SupportReadCanvas instance;

	public Text bodyText;

	void Awake()
	{
		instance = this;
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SupportListCanvas", null);
	}

	public void OnClickHomeButton()
	{
		gameObject.SetActive(false);
	}

	public void RefreshText(string value)
	{
		bodyText.SetLocalizedText(value);
	}
}