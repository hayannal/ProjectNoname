using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialLinkAccountCanvas : MonoBehaviour
{
	public static TutorialLinkAccountCanvas instance = null;

	public GameObject buttonObject;

	void Awake()
	{
		instance = this;
	}

	// 거의 SettingCanvas에서 하던거와 비슷하다. 대신 로그아웃이 없고 연동하기만 있다.
	public void OnClickLinkAccountButton()
	{
		// 이땐 항상 게스트 상태다.
		AuthManager.instance.LinkGoogleAccount(() =>
		{
			AuthManager.instance.SetNeedUnlinkCustomId();

			// 성공시에는 메세지 표시라도 해야하나.
			gameObject.SetActive(false);

		}, (cancel, failure) =>
		{
			if (cancel)
				return;
			if (failure == PlayFab.PlayFabErrorCode.Unknown)
				return;

			// 계정을 불러올 수 있다면
			if (failure == PlayFab.PlayFabErrorCode.LinkedAccountAlreadyClaimed)
			{
				YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_SignInAlready"), () =>
				{
					AuthManager.instance.RestartWithGoogle();
				}, () =>
				{
					AuthManager.instance.LogoutWithGoogle(true);
				});
			}
		});
	}
}