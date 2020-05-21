using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContentsManager
{
	// 해당 챕터에 도착하면 열린다. Chaos만 제외.
	public enum eOpenContentsByChapter
	{
		Chapter = 2,
		Research = 3,
		Chaos = 4,
		//Annihilation = 5,
		EquipOption = 6,
		SecondDailyBox = 8,
	}

	public enum eOpenContentsByChapterStage
	{
		TimeSpace,
	}
	const int TimeSpaceChpater = 2;
	const int TimeSpaceStage = 10;

	public static bool IsTutorialChapter()
	{
		if (PlayerData.instance.highestPlayChapter == 0)
			return true;
		return false;
	}

	public static bool IsOpen(eOpenContentsByChapter content)
	{
		if (PlayerData.instance.highestPlayChapter >= (int)content)
			return true;
		return false;
	}

	public static bool IsOpen(eOpenContentsByChapterStage content)
	{
		switch (content)
		{
			// 여기 이건 챕터랑 스테이지 둘다 체크해야해서 enum값으로 안하고 이렇게 처리해둔다.
			case eOpenContentsByChapterStage.TimeSpace:
				if (PlayerData.instance.highestPlayChapter > TimeSpaceChpater || (PlayerData.instance.highestPlayChapter == TimeSpaceChpater && PlayerData.instance.highestClearStage >= TimeSpaceStage))
					return true;
				break;
		}
		return false;
	}



	// for Event
	public static bool IsPlayable(eOpenContentsByChapterStage content, int chapter, int prevStage, int stage)
	{
		switch (content)
		{
			case eOpenContentsByChapterStage.TimeSpace:
				if (chapter != TimeSpaceChpater)
					return false;
				if (prevStage < TimeSpaceStage && stage >= TimeSpaceStage)
					return true;
				return false;
		}
		return false;
	}

	public static bool IsDropChapterStage(int chapter, int stage)
	{
		if (chapter == TimeSpaceChpater && stage == TimeSpaceStage)
			return true;
		return false;
	}
}