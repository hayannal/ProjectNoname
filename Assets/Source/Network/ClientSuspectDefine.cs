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
		InvalidTranscendLevel	= 100008,
		InvalidEquipType		= 100009,
		InvalidEquipOption		= 100010,
		InvalidEquipEnhance		= 100011,
		InvalidLegendKey		= 100012,
		CheatTable				= 100013,
		InvalidTotalPp			= 100014,
		InvalidResearchLevel	= 100015,
		InvalidStatPoint		= 100016,
		InvalidTraining			= 100017,
		FastNodeWar				= 100018,
	}
}