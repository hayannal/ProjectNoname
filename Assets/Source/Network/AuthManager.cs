#define Google
#define Facebook
#define Apple

using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
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
#if UNITY_IOS
#if Facebook
using Facebook.Unity;
#endif
#if Apple
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif
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
		Apple,
	}
	
	static string LAST_AUTH_KEY = "_account_last_type";
	static string GUEST_CUSTOM_ID_KEY = "_bjkeqevpzzrem";

	eAuthType _requestAuthType;
	string _customId;

	Action _onLinkSuccess;
	Action<bool, PlayFabErrorCode> _onLinkFailure;

#if UNITY_IOS
#if Apple
	IAppleAuthManager _appleAuthManager;
	static string APPLE_USER_ID_KEY = "_fibzqplraj";
	static string APPLE_IDENTITY_TOKEN_KEY = "_qplfjzvcix";
#endif

	void Awake()
	{
#if Apple
		// Creates a default JSON deserializer, to transform JSON Native responses to C# instances
		PayloadDeserializer deserializer = new PayloadDeserializer();
		// Creates an Apple Authentication manager with the deserializer
		_appleAuthManager = new AppleAuthManager(deserializer);

		// If at any point we receive a credentials revoked notification, we delete the stored User ID, and go back to login
		_appleAuthManager.SetCredentialsRevokedCallback(result =>
		{
			Debug.Log("Received revoked callback " + result);
			//this.SetupLoginMenuForSignInWithApple();
			//PlayerPrefs.DeleteKey(AppleUserIdKey);

			// 로그아웃처럼 처리해주면 되지 않을까
			DeleteCachedLastLoginInfo();
			PlayerData.instance.ResetData();
			SceneManager.LoadScene(0);
		});
#endif
	}

	void Start()
	{
#if Facebook
		if (!FB.IsInitialized)
		{
			// Initialize the Facebook SDK
			FB.Init(InitCallback, OnHideUnity);
		}
		else
		{
			// Already initialized, signal an app activation App Event
			FB.ActivateApp();
		}
#endif
	}

#if Facebook
	void InitCallback()
	{
		if (FB.IsInitialized)
		{
			// Signal an app activation App Event
			FB.ActivateApp();
			// Continue with Facebook SDK
			// ...
		}
		else
		{
			Debug.Log("Failed to Initialize the Facebook SDK");
		}
	}

	void OnHideUnity(bool isGameShown)
	{
		//if (!isGameShown)
		//{
		//	// Pause the game - we will need to hide
		//	Time.timeScale = 0;
		//}
		//else
		//{
		//	// Resume the game - we're getting focus again
		//	Time.timeScale = 1;
		//}
	}
#endif
#endif

	void Update()
	{
		UpdateRetryRemainTime();
		UpdateRetryUnlinkCustomId();

#if UNITY_IOS
#if Apple
		if (_appleAuthManager != null)
			_appleAuthManager.Update();
#endif
#endif
	}

#if Google
	//Google
	private string webClientId = "1044940954131-maditus5m72kc7vs7famr207mu3ug65j.apps.googleusercontent.com";
	private GoogleSignInConfiguration configuration;
	private GoogleSignInUser googleUser;
#endif

//#if UNITY_EDITOR
	// Add a menu item named "Do Something" to MyMenu in the menu bar.
	//[MenuItem("Tools/Network/Delete Cached Login Info")]
	public static void DeleteCachedLastLoginInfo()
	{
		ObscuredPrefs.DeleteKey(LAST_AUTH_KEY);
		ObscuredPrefs.DeleteKey(GUEST_CUSTOM_ID_KEY);
		Debug.Log("Delete Complete.");
	}
//#endif

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
				LoginWithGoogle(true);
				break;
#endif
#if UNITY_IOS
#if Facebook
			case eAuthType.Facebook:
				LoginWithFacebook();
				break;
#endif
#if Apple
			case eAuthType.Apple:
				LoginWithApple();
				break;
#endif
#endif
		}
	}

	static string GetDeviceUniqueIdentifier()
	{
#if !UNITY_EDITOR && UNITY_IOS
		// 먼저 키체인을 뒤져서 저장되어있는지 확인하고 있으면 불러온다.
		string jsonSavedInfo = KeyChain.BindGetKeyChainUser();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		Dictionary<string, string> dicSavedInfo = serializer.DeserializeObject<Dictionary<string, string>>(jsonSavedInfo);
		string savedIdentifier = "";
		if (dicSavedInfo.ContainsKey("uuid"))
			savedIdentifier = dicSavedInfo["uuid"];

		if (string.IsNullOrEmpty(savedIdentifier))
		{
			// 없으면 SystemInfo.deviceUniqueIdentifier를 통해서 하나 만든 후 세이브 해두고 리턴.
			savedIdentifier = SystemInfo.deviceUniqueIdentifier;
			KeyChain.BindSetKeyChainUser("_unknown", savedIdentifier);
		}
		Debug.LogFormat("GetDeviceUniqueIdentifier {0}", savedIdentifier);
		return savedIdentifier;
#endif

		return SystemInfo.deviceUniqueIdentifier;
	}

	public void RequestCreateGuestAccount()
	{
		_customId = GetDeviceUniqueIdentifier();
#if UNITY_EDITOR
		_customId = Guid.NewGuid().ToString();
#endif

		RequestLoginWithGuestId(_customId, true);
	}

	public bool IsCachedLastLoginInfo()
	{
		return ObscuredPrefs.HasKey(LAST_AUTH_KEY);
	}

	public eAuthType GetLastLoginType()
	{
		int lastLogin = ObscuredPrefs.GetInt(LAST_AUTH_KEY);
		return (eAuthType)lastLogin;
	}

	public static void ChangeLastAuthType(eAuthType authType)
	{
		ObscuredPrefs.SetInt(LAST_AUTH_KEY, (int)authType);
	}

	public static string GetLastGuestCustomId()
	{
		string customId = GetDeviceUniqueIdentifier();
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
		playerStatisticNames.Add("highestValue");
		playerStatisticNames.Add("nodClLv");
		playerStatisticNames.Add("guideQuestIndex");
		playerStatisticNames.Add("chaosFragment");
		playerStatisticNames.Add("analysisExp");
		playerStatisticNames.Add("pointShopPoint");
		playerStatisticNames.Add("chtRnkSus");
		GetPlayerCombinedInfoRequestParams parameters = new GetPlayerCombinedInfoRequestParams();
		parameters.GetCharacterList = true;
		parameters.GetPlayerStatistics = true;
		parameters.GetUserData = true;
		parameters.GetUserInventory = true;
		// 뛰어난 해커의 경우 로그인 정보로 오는 ReadOnlyData를 가져다가 패킷 스니핑에 쓸수도 있으니 ReadOnlyData 대신 InternalData를 사용하기로 한다.
		// 대신 InternalData이지만 보여주기 용으로 필요한 일퀘 완료 등의 상태값은
		// 같은 이름의 UserData로도 추가해서 로그인시 받아올 수 있게 한다. 대표적인 예가 SHcha. 앞에 Share의 약자인 "SH"를 써서 공용 변수인지 구분하기로 한다.
		//
		// 처음엔 ReadOnly안쓰고 Internal만 쓰려고 했는데
		// 뽑기 횟수 같이 클라가 연산해야하는건데 정보를 저장해야하는건 ReadOnly여야 안전해서 ReadOnly 데이터도 추가하기로 했다.
		parameters.GetUserReadOnlyData = true;
		parameters.GetUserVirtualCurrency = true;
		parameters.PlayerStatisticNames = playerStatisticNames;
		// 일일 상점 및 무료 상품은 서버에 올려진 데이터를 받아서 처리해야한다.
		parameters.GetTitleData = true;
		// 구글 로그인 연동시 CustomId를 해제해줘야해서 받아야한다.
		parameters.GetUserAccountInfo = true;
		// 이름때문에 받아야한다.
		parameters.GetPlayerProfile = true;
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

	void RequestLoginWithFacebook(string accessToken)
	{
		PlayFabApiManager.instance.StartTimeRecord("Login");
		_requestAuthType = eAuthType.Facebook;

		GetPlayerCombinedInfoRequestParams parameters = CreateLoginParameters();
		var request = new LoginWithFacebookRequest { AccessToken = accessToken, CreateAccount = false, InfoRequestParameters = parameters };
		PlayFabClientAPI.LoginWithFacebook(request, OnLoginSuccess, OnLoginFailure);
	}

	void RequestLoginWithApple(string identityToken)
	{
		PlayFabApiManager.instance.StartTimeRecord("Login");
		_requestAuthType = eAuthType.Apple;

		GetPlayerCombinedInfoRequestParams parameters = CreateLoginParameters();
		var request = new LoginWithAppleRequest { IdentityToken = identityToken, CreateAccount = false, InfoRequestParameters = parameters };
		PlayFabClientAPI.LoginWithApple(request, OnLoginSuccess, OnLoginFailure);
	}

	void OnLoginSuccess(PlayFab.ClientModels.LoginResult result)
	{
		PlayFabApiManager.instance.EndTimeRecord("Login");

		if (IsCachedLastLoginInfo() == false || _requestAuthType != GetLastLoginType())
		{
			ChangeLastAuthType(_requestAuthType);

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
		else if (error.Error == PlayFabErrorCode.InvalidGoogleToken || error.Error == PlayFabErrorCode.GoogleOAuthError)
		{
			// 혹시나 구글 로그인 관련해서 에러가 온다면 SignOut을 시켜놔야 Google SignIn 계정 선택창이라도 띄울 수 있게 된다.
			// 정상적인 경우에는 거의 발생하지 않을 일이다.
			if (_requestAuthType == eAuthType.Google)
			{
#if Google
				Debug.Log("Logout Google Sign-in by OAuth Error");
				GoogleSignIn.DefaultInstance.SignOut();
#endif
			}
		}
		else if (error.Error == PlayFabErrorCode.AccountNotFound)
		{
			if (GetLastLoginType() == eAuthType.Guest)
			{
				// 게스트인데 계정이 날아갔으면 불필요한 계정이라 날린걸수도 있으니 정보를 초기화 하고 다시 시도하도록 한다.
				DeleteCachedLastLoginInfo();
				SceneManager.LoadScene(0);
				return;
			}
		}

		//PlayFabApiManager.instance.HandleCommonError(error); 호출하는 대신
		// 로딩 구조 및 sortOrder를 바꿔야해서 직접 처리한다.
		StartCoroutine(RestartProcess(null, false, stringId));
	}

	// 여러 패킷이 동시에 실패하면 여러개의 RestartProcess가 만들어질 수도 있어서 플래그를 걸어서 체크하기로 한다.
	bool _restartProcessed = false;
	public IEnumerator RestartProcess(Action callback, bool useBigCanvas, string stringId = "SystemUI_DisconnectServer", params object[] arg)
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
		//yield return handleCommonCanvasGroup;
		while (handleCommonCanvasGroup.IsValid() && !handleCommonCanvasGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(handleCommonCanvasGroup.Result);

		Action action = () =>
		{
			_restartProcessed = false;
			Addressables.Release<GameObject>(handleCommonCanvasGroup);

			// 콜백이 있을땐 씬 재시작 호출하기 전에 콜백부터 처리
			if (callback != null)
				callback();

			SceneManager.LoadScene(0);
		};
		if (useBigCanvas)
		{
			OkBigCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(stringId, arg), () =>
			{
				action.Invoke();
			}, 100);
			yield break;
		}

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(stringId, arg), () =>
		{
			action.Invoke();
		}, 100);
	}

	public bool googleLinked { get; set; }
	public bool facebookLinked { get; set; }
	public bool appleLinked { get; set; }
	public void OnRecvAccountInfo(UserAccountInfo userAccountInfo)
	{
		googleLinked = facebookLinked = appleLinked = false;

		// 평소에는 할 필요 없는데 구글 로그인 연동 후에 CustomId가 해제되어있지 않다면 해제 처리를 해줘야한다.
		// 원래는 연동 즉시 보낼텐데 혹시 강종이나 다른 이슈로 처리 안될까봐 여기서 안전하게 한번 더 체크하는거다.
		if (userAccountInfo.GoogleInfo != null && userAccountInfo.CustomIdInfo != null)
		{
			SetNeedUnlinkCustomId();
			googleLinked = true;
		}
		if (userAccountInfo.FacebookInfo != null && userAccountInfo.CustomIdInfo != null)
		{
			SetNeedUnlinkCustomId();
			facebookLinked = true;
		}
		if (userAccountInfo.AppleAccountInfo != null && userAccountInfo.CustomIdInfo != null)
		{
			SetNeedUnlinkCustomId();
			appleLinked = true;
		}
	}

	public void SetNeedUnlinkCustomId()
	{
		needUnlinkCustomId = true;
		retryRequestUnlinkCustomId = true;
	}

	void RequestUnlinkCustomId()
	{
		// customId 가 사실 비워져있어도 되는게 서버에서 인자로 비워져있는채로 패킷이 날아오면 마지막으로 등록된 CustomId를 해제한다고 적혀있다.
		var request = new UnlinkCustomIDRequest { CustomId = "" };
		PlayFabClientAPI.UnlinkCustomID(request, OnUnlinkSuccess, OnUnlinkFailure);
	}

	void OnUnlinkSuccess(UnlinkCustomIDResult result)
	{
		needUnlinkCustomId = false;
	}

	void OnUnlinkFailure(PlayFabError error)
	{
		Debug.LogError(error.GenerateErrorReport());

		// 에러의 이유가 하필 NotLinked라면 이미 해제된거라고 봐야한다. 
		if (error.Error == PlayFabErrorCode.AccountNotLinked)
		{
			OnUnlinkSuccess(null);
			return;
		}

		// 왜 실패한거지? 다시 보내보자.
		retryRequestUnlinkCustomId = true;
	}




	float _retryLoginRemainTime;
	void UpdateRetryRemainTime()
	{
		if (_retryLoginRemainTime > 0.0f)
		{
			_retryLoginRemainTime -= Time.deltaTime;
			if (_retryLoginRemainTime <= 0.0f)
			{
				_retryLoginRemainTime = 0.0f;
				eAuthType lastAuthType = GetLastLoginType();
				switch (lastAuthType)
				{
#if Google
					case eAuthType.Google:
						// 재시도때는 계정이라도 바꿀 수 있게 해줘야하지 않을까. 우선 들어올 확률은 적으니 false로 해본다.
						LoginWithGoogle(false);
						break;
#endif
#if UNITY_IOS
#if Facebook
					case eAuthType.Facebook:
						LoginWithFacebook();
						break;
#endif
#if Apple
					case eAuthType.Apple:
						LoginWithApple();
						break;
#endif
#endif
				}
			}
		}
	}

	public bool needUnlinkCustomId { get; private set; }
	bool retryRequestUnlinkCustomId { get; set; }
	void UpdateRetryUnlinkCustomId()
	{
		if (retryRequestUnlinkCustomId)
		{
			Debug.Log("Retry RequestUnlinkCustomId");

			retryRequestUnlinkCustomId = false;
			RequestUnlinkCustomId();
		}
	}





	#region Link Packet
	void RequestLinkGoogle(string authCode)
	{
		var request = new LinkGoogleAccountRequest { ServerAuthCode = authCode };
		PlayFabClientAPI.LinkGoogleAccount(request, OnLinkGoogleSuccess, OnLinkGoogleFailure);
	}

	void OnLinkGoogleSuccess(LinkGoogleAccountResult result)
	{
		// 링크가 성공하면 다음 로그인부터 구글로 로그인하면 된다.
		ChangeLastAuthType(eAuthType.Google);

#if UNITY_EDITOR
		ObscuredPrefs.DeleteKey(GUEST_CUSTOM_ID_KEY);
#endif

		if (_onLinkSuccess != null)
			_onLinkSuccess();
	}

	void OnLinkGoogleFailure(PlayFabError error)
	{
		Debug.Log(error.Error.ToString());
		Debug.Log(error.ErrorMessage);

		if (_onLinkFailure != null)
			_onLinkFailure(false, error.Error);
	}

#if UNITY_IOS
	void RequestLinkFacebook(string accessToken)
	{
		var request = new LinkFacebookAccountRequest { AccessToken = accessToken };
		PlayFabClientAPI.LinkFacebookAccount(request, OnLinkFacebookSuccess, OnLinkFacebookFailure);
	}

	void OnLinkFacebookSuccess(LinkFacebookAccountResult result)
	{
		ChangeLastAuthType(eAuthType.Facebook);

#if UNITY_EDITOR
		ObscuredPrefs.DeleteKey(GUEST_CUSTOM_ID_KEY);
#endif

		if (_onLinkSuccess != null)
			_onLinkSuccess();
	}

	void OnLinkFacebookFailure(PlayFabError error)
	{
		Debug.Log(error.Error.ToString());
		Debug.Log(error.ErrorMessage);

		if (_onLinkFailure != null)
			_onLinkFailure(false, error.Error);
	}



	void RequestLinkApple(string identityToken)
	{
		var request = new LinkAppleRequest { IdentityToken = identityToken };
		PlayFabClientAPI.LinkApple(request, OnLinkAppleSuccess, OnLinkAppleFailure);
	}

	void OnLinkAppleSuccess(EmptyResult result)
	{
		ChangeLastAuthType(eAuthType.Apple);

#if UNITY_EDITOR
		ObscuredPrefs.DeleteKey(GUEST_CUSTOM_ID_KEY);
#endif

		if (_onLinkSuccess != null)
			_onLinkSuccess();
	}

	void OnLinkAppleFailure(PlayFabError error)
	{
		Debug.Log(error.Error.ToString());
		Debug.Log(error.ErrorMessage);

		if (_onLinkFailure != null)
			_onLinkFailure(false, error.Error);
	}
#endif
	#endregion






	bool _waitForLinkGoogle = false;
	public void LinkGoogleAccount(Action onLinkSuccess, Action<bool, PlayFabErrorCode> onLinkFailure)
	{
#if UNITY_EDITOR
		Debug.LogWarning("Google login cannot be launched from the editor.");
		return;
#endif

		// 로그인 할때도 아니고 연동을 할때라면 인풋 막는게 맞겠지
		WaitingNetworkCanvas.Show(true);

		_waitForLinkGoogle = true;
		_onLinkSuccess = onLinkSuccess;
		_onLinkFailure = onLinkFailure;

#if Google
		LoginWithGoogle(false);
#endif
	}

	public void LogoutWithGoogle(bool onlySignOut = false)
	{
#if Google
		GoogleSignIn.DefaultInstance.SignOut();
#endif

		if (onlySignOut)
			return;

		// 로그아웃시엔 PlayAfterInstallation처럼 첫로딩 후 튜토리얼 시작되야한다. 그러려면 전부다 로그아웃하고 게스트 아이디로 접속해야한다.
		DeleteCachedLastLoginInfo();
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}

	public void RestartWithGoogle()
	{
		ChangeLastAuthType(AuthManager.eAuthType.Google);
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}



	public string GetGoogleUserId()
	{
#if Google
		if (googleUser != null)
			return googleUser.Email;
#endif
		return "";
	}

#if Google
	void LoginWithGoogle(bool silentLogin)
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

		if (silentLogin)
			GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(GoogleLoginHandler);
		else
			GoogleSignIn.DefaultInstance.SignIn().ContinueWith(GoogleLoginHandler);
	}

	private void GoogleLoginHandler(Task<GoogleSignInUser> task)
	{
		if (task.IsFaulted)
		{
			// 예전엔 계정 선택을 안하고 뒤 누르거나 옆 눌러서 창을 닫으면 Canceled로 왔었는데 최신 빌드에서는 Faulted로 온다.
			Debug.Log("Login is faulted.");
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
			WaitingNetworkCanvas.Show(false);

			if (task.IsFaulted)
			{
				if (_onLinkFailure != null)
					_onLinkFailure(true, PlayFabErrorCode.Unknown);
			}
			else if (task.IsCanceled)
			{
				if (_onLinkFailure != null)
					_onLinkFailure(true, PlayFabErrorCode.Unknown);
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
				// 이제는 로그인이 Silent 형태로 바뀌면서 취소할 일도 없어졌다.
				// 구글 로그인이 거의 실패할 일이 없으나 아무 처리도 하지 않으면 넘어가질 않을테니 재시도 하게 한다.
				_retryLoginRemainTime = 0.2f;
			}
			else if (task.IsCanceled)
			{
				// 구글 로그인 시도중에 창의 테두리 부분을 무한정 클릭해서 취소를 시키는 경우가 있다.
				// 이 구글 로그인은 게임보다 더 앞에 나오는 창이기 때문에 UI로 막는다고 이 터치가 안먹히게 할 방법도 없다.
				// 이때가 링크할때면 상관없는데 유저 캔슬로 처리하면 되는데 하필 로그인하는 동안이면 게스트로 바꾸기도 애매하므로
				// 시간 조금 기다렸다가 다시 구글 로그인을 시도하는 형태로 구현해본다.
				_retryLoginRemainTime = 0.2f;
			}
			else
			{
				RequestLoginWithGoogle(googleUser.AuthCode);
			}
		}
	}



	// Sample
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
#endif












#if UNITY_IOS
	bool _waitForLinkFacebook = false;
	public void LinkFacebookAccount(Action onLinkSuccess, Action<bool, PlayFabErrorCode> onLinkFailure)
	{
#if UNITY_EDITOR
		Debug.LogWarning("Google login cannot be launched from the editor.");
		return;
#endif
		WaitingNetworkCanvas.Show(true);

		_waitForLinkFacebook = true;
		_onLinkSuccess = onLinkSuccess;
		_onLinkFailure = onLinkFailure;

#if Facebook
		LoginWithFacebook();
#endif
	}

	public void LogoutWithFacebook(bool onlySignOut = false)
	{
#if Facebook
		FB.LogOut();
#endif

		if (onlySignOut)
			return;

		// 로그아웃시에 하는 것도 구글과 비슷
		DeleteCachedLastLoginInfo();
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}

	public void RestartWithFacebook()
	{
		// 리스타트도 마찬가지
		ChangeLastAuthType(AuthManager.eAuthType.Facebook);
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}

	

#if Facebook
	void LoginWithFacebook()
	{
		if (!FB.IsInitialized)
		{
			Debug.Log("Facebook is not initialized.");

			// 앱 처음 켜서 들어가려고 할때 FB가 초기화 되어있지 않을 가능성이 높다. 대기시간 두고 조금 뒤에 다시 시도하게 한다.
			if (_waitForLinkFacebook == false)
				_retryLoginRemainTime = 0.2f;
			return;
		}

		if (_waitForLinkFacebook == false)
		{
			// 구글과 달리 페이스북은 초기화때 이미 로그인된 상태를 감지해서 받아오는 기능이 있다.
			// 그러니 로그인을 할때는 이미 로그인 되어있는지만 판단하면 된다.
			if (FB.IsLoggedIn && AccessToken.CurrentAccessToken != null)
			{
				Debug.Log("Facebook login already.");
				AuthCallback((ILoginResult)null);
				return;
			}
		}

		Debug.Log("Start facebook login.");
		FB.LogInWithPublishPermissions(new List<string>() { "public_profile", "email" }, AuthCallback);
	}

	private void AuthCallback(ILoginResult result)
	{
		if (FB.IsLoggedIn)
		{
			Debug.Log("Facebook login successed.");

			// AccessToken class will have session details
			var aToken = AccessToken.CurrentAccessToken;

			// Print current access token's User ID
			Debug.Log(aToken.UserId);

			//FB.API("me?fields=email,name", HttpMethod.GET, APICallBack);
		}
		else
		{
			Debug.Log("User cancelled login.");
		}

		if (_waitForLinkFacebook)
		{
			_waitForLinkFacebook = false;
			WaitingNetworkCanvas.Show(false);

			if (FB.IsLoggedIn)
			{
				RequestLinkFacebook(AccessToken.CurrentAccessToken.TokenString);
			}
			else
			{
				if (_onLinkFailure != null)
					_onLinkFailure(true, PlayFabErrorCode.Unknown);
			}
		}
		else
		{
			if (FB.IsLoggedIn)
			{
				RequestLoginWithFacebook(AccessToken.CurrentAccessToken.TokenString);
			}
			else
			{
				// 구글과 마찬가지
				_retryLoginRemainTime = 0.2f;
			}
		}
	}
#endif
#endif



#if UNITY_IOS
	bool _waitForLinkApple = false;
	public void LinkAppleAccount(Action onLinkSuccess, Action<bool, PlayFabErrorCode> onLinkFailure)
	{
#if UNITY_EDITOR
		Debug.LogWarning("Apple login cannot be launched from the editor.");
		return;
#endif
		WaitingNetworkCanvas.Show(true);

		_waitForLinkApple = true;
		_onLinkSuccess = onLinkSuccess;
		_onLinkFailure = onLinkFailure;

#if Apple
		LoginWithApple();
#endif
	}

	public void LogoutWithApple(bool onlySignOut = false)
	{
		// 애플은 코드에서 로그아웃 하는 방법이 없기 때문에
		// 여기서는 아무것도 하지 않는다.
		// 아마 셋팅창에서 로그아웃 할 수 없다는 메세지를 띄울 것이다.
		//FB.LogOut();

		if (onlySignOut)
			return;

		// 로그아웃시에 하는 것도 구글과 비슷
		ObscuredPrefs.DeleteKey(APPLE_USER_ID_KEY);
		ObscuredPrefs.DeleteKey(APPLE_IDENTITY_TOKEN_KEY);
		DeleteCachedLastLoginInfo();
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}

	public void RestartWithApple()
	{
		// 리스타트도 마찬가지
		ChangeLastAuthType(AuthManager.eAuthType.Apple);
		ClientSaveData.instance.OnEndGame();
		PlayerData.instance.ResetData();
		SceneManager.LoadScene(0);
	}



#if Apple
	void LoginWithApple()
	{
		if (_waitForLinkApple == false)
		{
			// 예제에서는 저장된 AppleUserId가 존재할 경우
			// CheckCredentialStatusForUserId 함수를 호출해서 정보만 얻고 넘어가라고 하지만
			// 이 방식으로 하면 로그인 패킷에 필요한 IdentityToken을 받아올 수가 없게된다.
			// 
			// 그렇다고 또 다른 제공 함수인 QuickLogin을 사용하면 기존의 silent 로그인 같은게 되는줄 알았는데
			// 사람들이 이슈에 적어둔 것도 그렇고 그냥 LoginWithAppleId함수와 다를바가 없이 암호 입력창이 뜬다고 한다.
			/*
			var quickLoginArgs = new AppleAuthQuickLoginArgs();

			// Quick login should succeed if the credential was authorized before and not revoked
			_appleAuthManager.QuickLogin(
				quickLoginArgs,
				AuthCallback,
				error =>
				{
					var authorizationErrorCode = error.GetAuthorizationErrorCode();
					Debug.LogWarning("Quick Login Failed " + authorizationErrorCode.ToString() + " " + error.ToString());
				});
			*/
			// 그래서 선택한 마지막 방법은
			// 플레이팹 Sign in with Apple 연동쪽에도 적혀있는 팁이긴 한데
			// identityToken의 만료시간을 체크하지 않도록 하고(서버에 Apple로그인 모듈 Addon할때 설정하는 옵션이 있다.)
			// 클라이언트 단에서 identityToken을 기억해놓고 쓰라는거다.
			// 대신 로그인 해둔걸 외부 셋팅메뉴에서 풀고올 수 있으니
			// CheckCredentialStatusForUserId로 

			string appleUserId = ObscuredPrefs.GetString(APPLE_USER_ID_KEY);

			_appleAuthManager.GetCredentialState(
				appleUserId,
				state =>
				{
					switch (state)
					{
						// If it's authorized, login with that user id
						case CredentialState.Authorized:
							string identityToken = ObscuredPrefs.GetString(APPLE_IDENTITY_TOKEN_KEY);
							RequestLoginWithApple(identityToken);
							return;

						// If it was revoked, or not found, we need a new sign in with apple attempt
						// Discard previous apple user id
						case CredentialState.Revoked:
						case CredentialState.NotFound:

							ObscuredPrefs.DeleteKey(APPLE_USER_ID_KEY);
							ObscuredPrefs.DeleteKey(APPLE_IDENTITY_TOKEN_KEY);
							DeleteCachedLastLoginInfo();
							PlayerData.instance.ResetData();
							SceneManager.LoadScene(0);
							return;
					}
				},
				error =>
				{
					var authorizationErrorCode = error.GetAuthorizationErrorCode();
					Debug.LogWarning("Error while trying to get credential state " + authorizationErrorCode.ToString() + " " + error.ToString());

					ObscuredPrefs.DeleteKey(APPLE_USER_ID_KEY);
					ObscuredPrefs.DeleteKey(APPLE_IDENTITY_TOKEN_KEY);
					DeleteCachedLastLoginInfo();
					PlayerData.instance.ResetData();
					SceneManager.LoadScene(0);
				});
			return;
		}


		Debug.Log("Start Apple login.");

		// LoginOptions.None으로 요청하면 불필요한 이메일 선택창을 건너뛸 수 있어서 유저가 좀 더 편한다.
		var loginArgs = new AppleAuthLoginArgs(LoginOptions.None);

		_appleAuthManager.LoginWithAppleId(
			loginArgs,
			AuthCallback,
			error =>
			{
				var authorizationErrorCode = error.GetAuthorizationErrorCode();
				Debug.LogWarning("Sign in with Apple failed " + authorizationErrorCode.ToString() + " " + error.ToString());

				if (_waitForLinkApple)
				{
					_waitForLinkApple = false;
					WaitingNetworkCanvas.Show(false);

					if (_onLinkFailure != null)
						_onLinkFailure(true, PlayFabErrorCode.Unknown);
				}
				else
				{
					// 구글과 마찬가지
					_retryLoginRemainTime = 0.2f;
				}
			});
	}

	private void AuthCallback(ICredential credential)
	{
		string appleUserId = "";
		ICredential receivedCredential = credential;
		if (receivedCredential == null)
		{
			Debug.Log("Apple credential is null.");
			return;
		}
		appleUserId = credential.User;

		var appleIdCredential = receivedCredential as IAppleIDCredential;
		var passwordCredential = receivedCredential as IPasswordCredential;
		
		if (_waitForLinkApple)
		{
			_waitForLinkApple = false;
			WaitingNetworkCanvas.Show(false);

			if (appleIdCredential != null)
			{
				var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length);
				RequestLinkApple(identityToken);

				// 애플은 다른 로그인과 달리 최초의 링크때 얻어둔 아이디와 identityToken을 저장해두고 다음번 로그인때 사용해야한다.
				ObscuredPrefs.SetString(APPLE_USER_ID_KEY, appleUserId);
				ObscuredPrefs.SetString(APPLE_IDENTITY_TOKEN_KEY, identityToken);
			}
			else
			{
				if (_onLinkFailure != null)
					_onLinkFailure(true, PlayFabErrorCode.Unknown);
			}
		}
		else
		{
			// 구글이나 페이스북과 달리 저장된 identityToken을 사용할거기 때문에 이쪽으로 들어오지 않는다.
			/*
			if (appleIdCredential != null)
			{
				var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length);
				RequestLoginWithApple(identityToken);
			}
			else
			{
				// 구글과 마찬가지
				_retryLoginRemainTime = 0.2f;
			}
			*/
		}
	}
#endif
#endif
}