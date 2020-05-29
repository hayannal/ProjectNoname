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
		InvalidEquipType		= 100008,
		InvalidEquipOption		= 100009,
		InvalidEquipEnhance		= 100010,
		InvalidLegendKey		= 100011,
		CheatTable				= 100012,
		InvalidTotalPp			= 100013,
		InvalidResearchLevel	= 100014,
		InvalidStatPoint		= 100015,
	}
}