using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Michsky.UI.Hexart;

public class SettingCanvas : MonoBehaviour
{
	public static SettingCanvas instance = null;

	public Slider systemVolumeSlider;
	public Text systemVolumeText;
	public Slider bgmVolumeSlider;
	public Text bgmVolumeText;
	public SwitchAnim doubleTabSwitch;
	public Text doubleTabOnOffText;
	public SwitchAnim lockIconSwitch;
	public Text lockIconOnOffText;

	public Text languageButtonText;
	public Text accountButtonText;
	public Slider frameRateSlider;
	public Text frameRateText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.lobbyOptionButton.gameObject.SetActive(false);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		LoadOption();
	}

	void OnDisable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.lobbyOptionButton.gameObject.SetActive(true);
	}

	public void OnClickHomeButton()
	{
		SaveOption();
		gameObject.SetActive(false);
	}


	#region GameOption
	void LoadOption()
	{
		systemVolumeSlider.value = OptionManager.instance.systemVolume;
		bgmVolumeSlider.value = OptionManager.instance.bgmVolume;
		doubleTabSwitch.isOn = (OptionManager.instance.useDoubleTab == 1);
		lockIconSwitch.isOn = (OptionManager.instance.lockIcon == 1);

		LoadLanguage();
		RefreshAccount();
		frameRateSlider.value = OptionManager.instance.frame;
	}

	public void SaveOption()
	{
		OptionManager.instance.SavePlayerPrefs();
	}

	public void OnValueChangedSystem(float value)
	{
		OptionManager.instance.systemVolume = value;
		systemVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnValueChangedBgm(float value)
	{
		OptionManager.instance.bgmVolume = value;
		bgmVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnSwitchOnDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 1;
		doubleTabOnOffText.text = "ON";
		doubleTabOnOffText.color = Color.white;
	}

	public void OnSwitchOffDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 0;
		doubleTabOnOffText.text = "OFF";
		doubleTabOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}

	public void OnSwitchOnLockIcon()
	{
		OptionManager.instance.lockIcon = 1;
		lockIconOnOffText.text = "ON";
		lockIconOnOffText.color = Color.white;
	}

	public void OnSwitchOffLockIcon()
	{
		OptionManager.instance.lockIcon = 0;
		lockIconOnOffText.text = "OFF";
		lockIconOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}
	#endregion

	#region Language
	void LoadLanguage()
	{
		LanguageTableData languageTableData = UIString.instance.FindLanguageTableData(OptionManager.instance.language);
		if (languageTableData != null)
			languageButtonText.SetLocalizedText(languageTableData.languageName);
	}

	public void OnClickLanguageButton()
	{
		if (PlayerData.instance.lobbyDownloadState)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("SelectLanguageCanvas", null);
	}
	#endregion

	#region Auth
	void RefreshAccount()
	{
		AuthManager.eAuthType lastAuthType = AuthManager.instance.GetLastLoginType();
		switch (lastAuthType)
		{
			case AuthManager.eAuthType.Guest:
				accountButtonText.SetLocalizedText(UIString.instance.GetString("GameUI_SignIn"));
				break;
			case AuthManager.eAuthType.Google:
				accountButtonText.SetLocalizedText(UIString.instance.GetString("GameUI_LogOut"));
				break;
		}
	}

	public void OnClickGoogleButton()
	{
		// 구현부 없이 그냥 호출하면 아무일 안일어나는 소셜 함수다.
		// 게다가 구글플레이서비스나 iOS 게임센터에 연결되는 형태니 Sign-in 과는 상관없으므로 사용하지 않는다.
		//Social.localUser.Authenticate((bool success) =>
		//{
		//	Debug.Log("1111");
		//});

		AuthManager.eAuthType lastAuthType = AuthManager.instance.GetLastLoginType();
		switch (lastAuthType)
		{
			case AuthManager.eAuthType.Guest:
				// 게스트 상태면 로그인을 해야한다.
				AuthManager.instance.LinkGoogleAccount(() =>
				{
					// 성공시에는 딱히 별다른 메세지 처리도 하지 않고 버튼 텍스트만 바꾸면 된다.
					RefreshAccount();

					// 그렇지만 따로 표시는 안해도 커스텀 아이디를 삭제해주는 처리가 필요하다.
					AuthManager.instance.SetNeedUnlinkCustomId();

				}, (cancel, failure) =>
				{
					// 유저의 캔슬이라면 아무것도 띄우지 않는다.
					if (cancel)
						return;

					// 구글의 로그인이 실패하는건데 이 경우에도 아무것도 띄우지 않는다.
					if (failure == PlayFab.PlayFabErrorCode.Unknown)
						return;

					// 실패의 이유가 이미 연동시킨 구글계정이라 안되는거라면 해당 계정을 로드할지 물어봐야한다.
					// 참고로 PlayFab.PlayFabErrorCode.AccountAlreadyLinked 오류는 이미 해당 계정이 이미 구글에 링크된 상태일때 나오는 에러인데
					// 현재 스탭에서는 구글로그인 연동 상태라면 로그아웃만 가능하므로 로그인을 시도할 수 없다. 그러니 저 경우는 아예 빼두기로 한다.
					if (failure == PlayFab.PlayFabErrorCode.LinkedAccountAlreadyClaimed)
					{
						YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_SignInAlready"), () =>
						{
							// 이미 구글로그인은 되어있는 상태지만 Last AuthType는 CustomId인 상태다. 그러니 Last AuthType을 바꾸고 씬을 재시작시켜야한다.
							AuthManager.instance.RestartWithGoogle();
						}, () =>
						{
							// 아니오를 눌렀을 경우엔 게스트 상태로 남아야하므로 방금 연결하려고 했던 구글 계정에서 로그아웃만 해두면 된다.
							AuthManager.instance.LogoutWithGoogle(true);
						});
					}
				});
				break;
			case AuthManager.eAuthType.Google:
				// 구글 연동할때 CustomId 계정 연결된걸 해제하는 처리를 해야하는데
				// 혹시라도 실패했다면 계속해서 Unlink 재시도 처리를 하고있을거다.
				// 이 상황에서는 로그아웃 하면 안되므로 예외처리 하기로 한다.
				if (AuthManager.instance.needUnlinkCustomId)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_LogOutException"), 2.0f);
					return;
				}

				// 이미 연동되어있는 상태라면 확인창을 띄우고 로그아웃을 해야한다.
				YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_LogOutConfirm"), () =>
				{
					AuthManager.instance.LogoutWithGoogle();
				});
				break;
		}
	}
	#endregion

	#region Frame Rate
	public void OnValueChangedFrameRate(float value)
	{
		int intValue = Mathf.RoundToInt(value);
		OptionManager.instance.frame = intValue;
		frameRateText.text = OptionManager.instance.GetTargetFrameRateText(intValue).ToString();
	}
	#endregion

	#region System
	#endregion
}