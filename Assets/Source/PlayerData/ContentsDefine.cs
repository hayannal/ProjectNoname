using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContentsManager
{
	public enum eOpenContentsByChapter
	{
		Chapter = 2,
		Research = 3,
		Chaos = 4,
		//Annihilation = 5,
	}

	public enum eOpenContentsByResearchLevel
	{
		SecondDailyBox = 5,
	}

	public enum eOpenContensByChapterStage
	{
		TimeSpace,
		EquipBox,
	}

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

	public static bool IsOpen(eOpenContentsByResearchLevel content)
	{
		//if (PlayerData.instance.researchLevel >= (int)content)
		//	return true;
		return false;
	}

	public static bool IsOpen(eOpenContensByChapterStage content)
	{
		switch (content)
		{
			// 여기 이건 챕터랑 스테이지 둘다 체크해야해서 enum값으로 안하고 이렇게 처리해둔다.
			case eOpenContensByChapterStage.TimeSpace:
				if (PlayerData.instance.highestPlayChapter > 2 || (PlayerData.instance.highestPlayChapter == 2 && PlayerData.instance.highestClearStage > 10))
					return true;
				break;
			case eOpenContensByChapterStage.EquipBox:
				if (PlayerData.instance.highestPlayChapter > 3 || (PlayerData.instance.highestPlayChapter == 3 && PlayerData.instance.highestClearStage > 10))
					return true;
				break;
		}
		return false;
	}

	// for Event
	public static bool IsPlayable(eOpenContensByChapterStage content, int chapter, int prevStage, int stage)
	{
		switch (content)
		{
			case eOpenContensByChapterStage.TimeSpace:
				if (chapter != 2)
					return false;
				if (prevStage < 10 && stage >= 10)
					return true;
				return false;
		}
		return false;
	}
}