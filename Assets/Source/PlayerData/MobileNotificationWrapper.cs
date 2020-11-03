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
public class MobileNotificationWrapper : MonoBehaviour
{
	public static MobileNotificationWrapper instance;

	public GameNotificationsManager manager;

	public const string ChannelId = "game_channel0";

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		// 채널은 하나만 쓴다.
		var c1 = new GameNotificationChannel(ChannelId, "Default Game Channel", "Generic notifications");
		manager.Initialize(c1);
	}

	// id를 같이 쓰는 Noti들은 그룹으로 묶여서 여러개의 알람이 와도 하나만 노출되게 해준다. 종류별로 묶어쓰면 된다.
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
		notification.DeliveryTime = deliveryTime;
		notification.SmallIcon = smallIcon;
		notification.LargeIcon = largeIcon;
		notification.ShouldAutoCancel = true;
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
	IEnumerator RequestAuthorization()
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
	}
#endif
}