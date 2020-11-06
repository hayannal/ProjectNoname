using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermsCanvas : MonoBehaviour
{
	public static TermsCanvas instance = null;

	public StringTermsTable stringTermsTable;

	public GameObject homeButtonObject;
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
		// 홈버튼 안보이는 이용약관 확인창에서는 그냥 닫기만 하면 된다.
		gameObject.SetActive(false);
		if (_showHomeButton)
			UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickHomeButton()
	{
		gameObject.SetActive(false);
	}

	bool _showTerms;
	int _page;
	bool _showHomeButton;
	public void RefreshInfo(bool terms, bool showHomeButton)
	{
		_showTerms = terms;
		_showHomeButton = showHomeButton;

		homeButtonObject.SetActive(showHomeButton);
		groupNameText.SetLocalizedText(UIString.instance.GetString(terms ? "GameUI_TermsOfService" : "GameUI_PrivacyPolicy"));

		_page = 1;
		pageText.text = _page.ToString();
		RefreshPageText();
	}

	public void OnClickLeftButton()
	{
		if (_page == 2)
		{
			_page = 1;
			pageText.text = _page.ToString();
			RefreshPageText();
		}
	}

	public void OnClickRightButton()
	{
		if (_page == 1)
		{
			_page = 2;
			pageText.text = _page.ToString();
			RefreshPageText();
		}
	}

	void RefreshPageText()
	{
		string pageStringId = "";
		if (_showTerms)
		{
			if (_page == 1) pageStringId = "GameUI_TermsOfServiceFullOne";
			else pageStringId = "GameUI_TermsOfServiceFullTwo";
		}
		else
		{
			if (_page == 1) pageStringId = "GameUI_PrivacyPolicyFullOne";
			else pageStringId = "GameUI_PrivacyPolicyFullTwo";
		}
		contentText.text = FindTermsString(pageStringId);
	}

	string FindTermsString(string id)
	{
		for (int i = 0; i < stringTermsTable.dataArray.Length; ++i)
		{
			if (stringTermsTable.dataArray[i].id == id)
			{
				if (OptionManager.instance.language == "KOR")
					return stringTermsTable.dataArray[i].kor;
				return stringTermsTable.dataArray[i].eng;
			}
		}
		return "";
	}
}