using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotificationSamples;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

// 매니저라고 이름 짓기엔 이미 GameNotificationsManager 클래스가 있어서 Wrapper 클래스로 지어둔다.
// Framework/MobileNotificationWrapper 폴더에 있는 파일들은 다 Sample에 있는거 수정없이 가져온거니 혹시 버전업할거라면 통째로 바꿔치기 하면 될거다.
// 이건 그 Sample을 쓰기 편하게 만든 헬퍼 클래스다.
// GameObject.FindObjectOfType<GameNotificationsManager>(); 함수 써서 찾기 싫어서 아예 씬에다가 등록해서 쓰기로 한다.
// 다른 매니저들과 달리 항상 존재해야하기 때문에 로그인해서 생성하는 형태도 아니고 씬에 두고 앱 시작할때 생성되게 한다.
public class MobileNotificationWrapper : MonoBehaviour
{
	public static MobileNotificationWrapper instance;

	public GameNotificationsManager manager;

	public const string ChannelId = "game_channel0";

	void Awake()
	{
		// 코드를 보니 GameNotificationsManager 클래스는 삭제되면 안되는 구조라 DontDestroyOnLoad를 걸어야하는데
		// 어차피 건드릴거라면 Wrapper에 작업하는게 나아서 여기에다 처리해둔다.
		// 씬에 존재하는 오브젝트이지만 싱글톤 처리를 해야하니 이런식으로 처리하면 될거다.
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Start()
	{
		// 채널은 하나만 쓴다.
		var c1 = new GameNotificationChannel(ChannelId, "Energy Charge Channel", "Gate Stone");
		manager.Initialize(c1);
	}

	// id를 같이 쓰는 Noti들은 그룹으로 묶여서 여러개의 알람이 와도 하나만 노출되게 해준다. 종류별로 묶어쓰면 된다.
	// deliveryTime이 null이면 즉시 반영이라서 백그라운드 다운로드 후 알림으로 쓸수 있을까 했는데
	// 어차피 백그라운드 상태에서는 즉시 반영으로 코딩해놔도 코드가 돌지 않기 때문에 Noti가 뜨지 않았다. 그래서 그냥 패스하기로 한다.
	public void SendNotification(int id, string title, string body, DateTime deliveryTime, int? badgeNumber = null, bool reschedule = false, string smallIcon = null, string largeIcon = null)
	{
		IGameNotification notification = manager.CreateNotification();

		if (notification == null)
		{
			return;
		}

		notification.Id = id;
		notification.Title = title;
		notification.Body = body;
		notification.Group = ChannelId;
		//if (deliveryTime != null)
		//{
			notification.DeliveryTime = deliveryTime;
		//}
		notification.SmallIcon = smallIcon;
		notification.LargeIcon = largeIcon;
		// 안드로이드에서는 누르면 사라지는게 디폴트이므로 체크해둔다.
		notification.ShouldAutoCancel = true;
		// 안드로이드에서는 시간 표시 나오는게 기본이므로 시간 표시 켜둔다.
		notification.ShowTimestamp = true;
		// 혹시 번역이 길어질거 대비해서 BigText로 해둔다. 짧게 나올땐 기본 스타일이랑 똑같이 나오니 문제없을거다.
		notification.BigTextStyle = true;
		if (badgeNumber != null)
		{
			notification.BadgeNumber = badgeNumber;
		}

		PendingNotification notificationToDisplay = manager.ScheduleNotification(notification);
		notificationToDisplay.Reschedule = reschedule;
	}

	public void CancelPendingNotificationItem(int id)
	{
		manager.CancelNotification(id);
	}

#if UNITY_IOS
	public void CheckAuthorization(Action nextAction = null)
	{
		StartCoroutine(RequestAuthorization(nextAction));
	}

	IEnumerator RequestAuthorization(Action nextAction)
	{
		var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
		using (var req = new AuthorizationRequest(authorizationOption, true))
		{
			while (!req.IsFinished)
			{
				yield return null;
			};

			string res = "\n RequestAuthorization:";
			res += "\n finished: " + req.IsFinished;
			res += "\n granted :  " + req.Granted;
			res += "\n error:  " + req.Error;
			res += "\n deviceToken:  " + req.DeviceToken;
			Debug.Log(res);
		}

		yield return null;

		// 권한요청을 거절했는지 검사같은거는 뭐로 해야하나.

		if (nextAction != null)
			nextAction();
	}
#endif
}