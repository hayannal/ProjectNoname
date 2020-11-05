using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermsCanvas : MonoBehaviour
{
	public static TermsCanvas instance = null;

	public StringTermsTable stringTermsTable;

	public Text groupNameText;
	public Text contentText;

	public GameObject pageGroupObject;
	public Text pageText;

	void Awake()
	{
		instance = this;
	}

	public void OnClickBackButton()
	{
		// StackCanvas를 사용하진 않지만 백버튼인척 하기 위해 이렇게 처리한다.
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickHomeButton()
	{
		gameObject.SetActive(false);
	}

	int _page;
	public void RefreshInfo(bool terms)
	{
		groupNameText.SetLocalizedText(UIString.instance.GetString(terms ? "GameUI_TermsOfService" : "GameUI_PrivacyPolicy"));
		if (terms)
		{
			_page = 1;
			pageText.text = _page.ToString();
			contentText.text = FindTermsString("GameUI_TermsOfServiceFullOne");
			pageGroupObject.SetActive(true);
		}
		else
		{
			pageGroupObject.SetActive(false);
			contentText.text = FindTermsString("GameUI_PrivacyPolicyFull");
		}
	}

	public void OnClickLeftButton()
	{
		if (_page == 2)
		{
			_page = 1;
			pageText.text = _page.ToString();
			contentText.text = FindTermsString("GameUI_TermsOfServiceFullOne");
		}
	}

	public void OnClickRightButton()
	{
		if (_page == 1)
		{
			_page = 2;
			pageText.text = _page.ToString();
			contentText.text = FindTermsString("GameUI_TermsOfServiceFullTwo");
		}
	}

	string FindTermsString(string id)
	{
		for (int i = 0; i < stringTermsTable.dataArray.Length; ++i)
		{
			if (stringTermsTable.dataArray[i].id == id)
				return stringTermsTable.dataArray[i].eng;
		}
		return "";
	}
}