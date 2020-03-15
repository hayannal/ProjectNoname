using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ServerTime
{
    public static DateTime UtcNow
	{
		get
		{
			if (PlayerData.instance.clientOnly)
				return DateTime.UtcNow;
			return PlayFabApiManager.instance.GetServerUtcTime();
		}
	}
}
