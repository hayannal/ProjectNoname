using System;
using System.Collections.Generic;
using System.Text;

namespace ClientSuspect
{
	/// <summary>
	/// Error codes returned by PlayFabAPIs
	/// </summary>
	public enum eClientSuspectCode
	{
		FastEndGame				= 100001,
		OneShotKillBoss			= 100002,
		InvalidMainCharacter	= 100003,
		InvalidSelectedChapter	= 100004,
		InvalidPowerLevel		= 100005,
		InvalidPp				= 100006,
		InvalidLimitBreakLevel	= 100007,
	}
}