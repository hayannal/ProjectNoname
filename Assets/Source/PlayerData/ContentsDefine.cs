using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContentsManager
{
	public enum eOpenContentsByChapter
	{
		Chapter = 2,
		Research = 3
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
}