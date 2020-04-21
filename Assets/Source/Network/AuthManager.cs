//#define Google

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
#if Google
using Google;
using System.Threading.Tasks;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AuthManager : MonoBehaviour
{
	public static AuthManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("AuthManager")).AddComponent<AuthManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static AuthManager _instance = null;

	public enum eAuthType
	{
		Guest = 1,
		Google,
		Facebook,
	}
	
	static string LAST_AUTH_KEY = "_account_last_type";
	static string GUEST_CUSTOM_ID_KEY = "_bjkeqevpzzrem";

	eAuthType _requestAuthType;
	string _customId;

	Action _onLinkSuccess;
	Action<bool> _onLinkFailure;

#if Google
	//Google
	public string webClientId = "393852258340-lgd54aqt5h364sb0jp5um8d8vqraqm8b.apps.googleusercontent.com";
	private GoogleSignInConfiguration configuration;
	private GoogleSignInUser googleUser;
#endif

#if UNITY_EDITOR
	// Add a menu item named "Do Something" to MyMenu in the menu bar.
	//[MenuItem("Tools/Network/Delete Cached Login Info")]
	public static void DeleteCachedLastLoginInfo()
	{
		ObscuredPrefs.DeleteKey(LAST_AUTH_KEY);
		ObscuredPrefs.DeleteKey(GUEST_CUSTOM_ID_KEY);
		Debug.Log("Delete Complete.");
	}
#endif

	#region Helper Function
	public void LoginWithLastLoginType()
	{
		eAuthType lastAuthType = GetLastLoginType();
		switch (lastAuthType)
		{
			case eAuthType.Guest:
				RequestLoginWithGuestId(GetLastGuestCustomId(), false);
				break;
#if Google
			case eAuthType.Google:
				LoginWithGoogle();
				break;
#endif
		}
	}

	public void RequestCreateGuestAccount()
	{
		_customId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_IOS || UNITY_IPHONE
#endif
#if UNITY_EDITOR
		_customId = Guid.NewGuid().ToString();
#endif
		
		RequestLoginWithGuestId(_customId, true);
	}

#if Google
	bool _waitForLinkGoogle = false;
	public void LinkGoogleAccount(Action onLinkSuccess, Action<bool> onLinkFailure)
	{
		_waitForLinkGoogle = true;
		_onLinkSuccess = onLinkSuccess;
		_onLinkFailure = onLinkFailure;

		LoginWithGoogle();
	}

	public void Logout()
	{
		eAuthType lastAuthType = GetLastLoginType();
		switch (lastAuthType)
		{
			case eAuthType.Google:
				GoogleSignIn.DefaultInstance.SignOut();

				// 로그아웃시엔 PlayAfterInstallation처럼 첫로딩 후 튜토리얼 시작되야한다.
				ClearCachedLastLoginInfo();
				SceneManager.LoadScene(0);
				break;
		}
	}
#endif
	#endregion

	public bool IsCachedLastLoginInfo()
	{
		return ObscuredPrefs.HasKey(LAST_AUTH_KEY);
	}

	public eAuthType GetLastLoginType()
	{
		int lastLogin = ObscuredPrefs.GetInt(LAST_AUTH_KEY);
		return (eAuthType)lastLogin;
	}

	public static string GetLastGuestCustomId()
	{
		string customId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_IOS || UNITY_IPHONE
#endif
#if UNITY_EDITOR
		customId = ObscuredPrefs.GetString(GUEST_CUSTOM_ID_KEY);
#endif
		return customId;
	}

	public static void SetGuestCustomId(string id)
	{
#if UNITY_EDITOR
		ObscuredPrefs.SetString(GUEST_CUSTOM_ID_KEY, id);
#endif
	}


	GetPlayerCombinedInfoRequestParams CreateLoginParameters()
	{
		List<string> playerStatisticNames = new List<string>();
		playerStatisticNames.Add("highestPlayChapter");
		playerStatisticNames.Add("highestClearStage");
		GetPlayerCombinedInfoRequestParams parameters = new GetPlayerCombinedInfoRequestParams();
		parameters.GetCharacterList = true;
		parameters.GetPlayerStatistics = true;
		parameters.GetUserData = true;
		parameters.GetUserInventory = true;
		// 뛰어난 해커의 경우 로그인 정보로 오는 ReadOnlyData를 가져다가 패킷 스니핑에 쓸수도 있으니 ReadOnlyData 대신 InternalData를 사용하기로 한다.
		// 대신 InternalData이지만 보여주기 용으로 필요한 일퀘 완료 등의 상태값은
		// 같은 이름의 UserData로도 추가해서 로그인시 받아올 수 있게 한다. 대표적인 예가 SHlstBxDat. 앞에 Share의 약자인 "SH"를 써서 공용 변수인지 구분하기로 한다.
		parameters.GetUserReadOnlyData = false;
		parameters.GetUserVirtualCurrency = true;
		parameters.PlayerStatisticNames = playerStatisticNames;
		return parameters;
	}

	void RequestLoginWithGuestId(string customId, bool createAccount)
	{
		PlayFabApiManager.instance.StartTimeRecord("Login");
		_requestAuthType = eAuthType.Guest;

		GetPlayerCombinedInfoRequestParams parameters = CreateLoginParameters();
		var request = new LoginWithCustomIDRequest { CustomId = customId, CreateAccount = createAccount, InfoRequestParameters = parameters };
		PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
	}

	void RequestLoginWithGoogle(string authCode)
	{
		PlayFabApiManager.instance.StartTimeRecord("Login");
		_requestAuthType = eAuthType.Google;

		GetPlayerCombinedInfoRequestParams parameters = CreateLoginParameters();
		var request = new LoginWithGoogleAccountRequest { ServerAuthCode = authCode, CreateAccount = false, InfoRequestParameters = parameters };
		PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginSuccess, OnLoginFailure);
	}

	void OnLoginSuccess(LoginResult result)
	{
		PlayFabApiManager.instance.EndTimeRecord("Login");

		if (IsCachedLastLoginInfo() == false || _requestAuthType != GetLastLoginType())
		{
			ObscuredPrefs.SetInt(LAST_AUTH_KEY, (int)_requestAuthType);

#if UNITY_EDITOR
			if (_requestAuthType == eAuthType.Guest)
				SetGuestCustomId(_customId);
#endif
		}

		Debug.LogFormat("Login Successed! PlayFabId : {0}", result.PlayFabId);
		PlayFabApiManager.instance.OnRecvLoginResult(result);
	}

	void OnLoginFailure(PlayFabError error)
	{
		PlayFabApiManager.instance.EndTimeRecord("Login");
		Debug.LogError(error.GenerateErrorReport());

		string stringId = "SystemUI_DisconnectServer";
		if (error.Error == PlayFabErrorCode.AccountBanned)
			stringId = "SystemUI_Banned";

		//PlayFabApiManager.instance.HandleCommonError(error); 호출하는 대신
		// 로딩 구조 및 sortOrder를 바꿔야해서 직접 처리한다.
		StartCoroutine(RestartProcess(stringId));
	}

	// 여러 패킷이 동시에 실패하면 여러개의 RestartProcess가 만들어질 수도 있어서 플래그를 걸어서 체크하기로 한다.
	bool _restartProcessed = false;
	public IEnumerator RestartProcess(string stringId = "SystemUI_DisconnectServer")
	{
		if (_restartProcessed)
			yield break;
		_restartProcessed = true;

		// 로딩시간을 조금이라도 빠르게 줄이기 위해 MainSceneBuiler 초기화 부분에서 로딩이 끝나는걸 확인하지 않은채 넘겼기때문에
		// 여기서 대신 기다려야한다.
		while (UIString.instance.IsDoneLoadAsyncStringData() == false)
			yield return null;
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;

		// 이땐 로딩 속도를 위해 commonCanvasGroup도 로딩하지 않은 상태라서 직접 로드해서 보여줘야한다.
		AsyncOperationHandle<GameObject> handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");
		while (!handleCommonCanvasGroup.IsDone) yield return null;
		Instantiate<GameObject>(handleCommonCanvasGroup.Result);
		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(stringId), () =>
		{
			_restartProcessed = false;
			Addressables.Release<GameObject>(handleCommonCanvasGroup);
			SceneManager.LoadScene(0);
		}, 100);
	}


	#region Link
	void RequestLinkGoogle(string authCode)
	{
		var request = new LinkGoogleAccountRequest { ServerAuthCode = authCode };
		PlayFabClientAPI.LinkGoogleAccount(request, OnLinkGoogleSuccess, OnLinkGoogleFailure);
	}

	void OnLinkGoogleSuccess(LinkGoogleAccountResult result)
	{
		PlayerPrefs.SetInt(LAST_AUTH_KEY, (int)eAuthType.Google);

		// 링크에 성공하면 저장되어있던 CustomId를 날려서 로그아웃 하더라도 그 계정에 접속하지 못하게 한다.
		PlayerPrefs.SetString(GUEST_CUSTOM_ID_KEY, "");

#if UNITY_EDITOR
		Debug.Log("Link Success");
#endif

		if (_onLinkSuccess != null)
			_onLinkSuccess();
	}

	private void OnLinkGoogleFailure(PlayFabError error)
	{
#if UNITY_EDITOR
		Debug.Log("Link Fail : " + error.ErrorMessage);
#endif

		if (_onLinkFailure != null)
			_onLinkFailure(false);
	}
#endregion


#if Google
#region GoogleLogin
	public void LoginWithGoogle()
	{
		//Setup for Google
		if (configuration == null)
		{
			configuration = new GoogleSignInConfiguration
			{
				WebClientId = webClientId,
				RequestIdToken = true
			};
		}

		GoogleSignIn.Configuration = configuration;
		GoogleSignIn.Configuration.UseGameSignIn = false;
		GoogleSignIn.Configuration.RequestIdToken = true;
		GoogleSignIn.Configuration.RequestAuthCode = true;
		GoogleSignIn.Configuration.RequestEmail = true;

		Debug.Log("Start login normal.");

		GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
			GoogleLoginHandler);
	}

	private void GoogleLoginHandler(Task<GoogleSignInUser> task)
	{
		if (task.IsFaulted)
		{
			Debug.Log("Login is faulted.");

			using (IEnumerator<System.Exception> enumerator =
				task.Exception.InnerExceptions.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					GoogleSignIn.SignInException error =
						(GoogleSignIn.SignInException)enumerator.Current;
					Debug.Log("Got Error: " + error.Status + " " + error.Message);
					Debug.Log("Got Error: " + error.InnerException.ToString());
				}
				else
				{
					Debug.Log("Got Unexpected Exception?!?" + task.Exception);
				}
			}
		}
		else if (task.IsCanceled)
		{
			Debug.Log("Login is canceled.");
		}
		else
		{
			googleUser = task.Result;
			Debug.Log("Login successed. Welcome " + googleUser.DisplayName);
			Debug.Log("Server Auth Code " + googleUser.AuthCode);
		}

		if (_waitForLinkGoogle)
		{
			_waitForLinkGoogle = false;
			if (task.IsFaulted)
			{
				if (_onLinkFailure != null)
					_onLinkFailure(false);
			}
			else if (task.IsCanceled)
			{
				if (_onLinkFailure != null)
					_onLinkFailure(true);
			}
			else
			{
				RequestLinkGoogle(googleUser.AuthCode);
			}
		}
		else
		{
			if (task.IsFaulted)
			{
				if (_onLoginFailure != null)
					_onLoginFailure(false);
			}
			else if (task.IsCanceled)
			{
				if (_onLoginFailure != null)
					_onLoginFailure(true);
			}
			else
			{
				RequestLoginWithGoogle(googleUser.AuthCode);
			}
		}
	}

	private bool IsGoogleInit()
	{
		if (configuration == null)
		{
			return false;
		}

		return true;
	}

	public void BtnGoogle_ManualInit_Click()
	{
		if (IsGoogleInit())
		{
			Debug.Log("Google is initialized.");
			return;
		}

		Debug.Log("Manual Init.");

		//Setup for Google
		configuration = new GoogleSignInConfiguration
		{
			WebClientId = webClientId,
			RequestIdToken = true
		};
	}

	public void BtnGoogle_IdToken_Click()
	{
		string idToken = googleUser.IdToken;
		Debug.Log(String.Format("Id Token: {0}.", idToken));
	}

	public void BtnGoogle_AuthCode_Click()
	{
		string authCode = googleUser.AuthCode;
		Debug.Log(String.Format("Auth Code: {0}.", authCode));
	}
#endregion
#endif





}
