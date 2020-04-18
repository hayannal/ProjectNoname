using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SelectLanguageButton : MonoBehaviour
{
	public string languageId = "KOR";
	public string languageName { get; set; }
	public Text buttonText;
	Button _button;

	bool _started = false;
	void Start()
	{
		_button = GetComponent<Button>();
		RefreshButton();
		_started = true;
	}

	void OnEnable()
	{
		if (_started)
			RefreshButton();
	}

	void RefreshButton()
	{
		_button.interactable = !(OptionManager.instance.language == languageId);

		if (OptionManager.instance.language == languageId)		
			buttonText.SetLocalizedText(languageName);
		else
		{
			LanguageTableData languageTableData = TableDataManager.instance.FindLanguageTableData(languageId);
			if (languageTableData.useUnlocalized)
			{
				buttonText.font = UIString.instance.GetUnlocalizedFont();
				buttonText.fontStyle = UIString.instance.useSystemUnlocalizedFont ? FontStyle.Bold : FontStyle.Normal;
				buttonText.text = languageName;
			}
			else
			{
				// 나머지는 자기 폰트에서 뜨면 자신거로 아니면 시스템 폰트가 사용되게 처리해둔다.
				buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
				buttonText.fontStyle = FontStyle.Bold;
				buttonText.text = languageName;
			}
		}
	}

	public void OnClickButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_Confirm"), UIString.instance.GetString("GameUI_ChangeLanguageDesc"), () => {

			// 이렇게 하면 순간 localizedText들이 해당 언어로 변하는게 보이고나서 씬이 전환된다.
			//OptionManager.instance.language = region;
			//OptionManager.instance.SaveLanguagePlayerPref();
			//SceneManager.LoadScene(0);

			// 그래서 먼저 playerPref 저장만 하고 씬로드를 호출한 뒤 옵션을 바꾸려고 했는데 이 순서로 호출해도
			// 여전히 해당 언어로 변하고 나서 씬이 전환된다.
			//OptionManager.instance.SaveLanguagePlayerPref(region);
			//SceneManager.LoadScene(0);
			//OptionManager.instance.language = region;

			// 그래서 아예 OptionManager에서 폰트 및 LocalizedText Region변환 하는 코드를 주석처리하고
			// 씬로드를 통해서만 변하게 해둔다.
			// 이게 SceneManager.LoadScene함수 뒤에 호출된다고 mainSceneBulider가 다 초기화하고 호출되는게 아니라서
			// OptionManager.instance.language와 localizedText를 구분할 수 밖에 없는거다.
			if (SettingCanvas.instance != null)
				SettingCanvas.instance.SaveOption();
			OptionManager.instance.language = languageId;
			OptionManager.instance.SaveLanguagePlayerPref();
			SceneManager.LoadScene(0);
		});
	}
}
