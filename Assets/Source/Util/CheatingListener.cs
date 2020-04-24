using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientSuspect;
using MEC;

public class CheatingListener : MonoBehaviour
{
	public void OnDetect()
	{
		Application.Quit();
	}

	public static bool detectedCheatTable { get; private set; }
	public static void OnDetectCheatTable()
	{
		if (detectedCheatTable)
			return;

#if UNITY_EDITOR
		Debug.LogError("OnDetectCheatTable!!!!!!!!!");
		Debug.LogError("OnDetectCheatTable!!!!!!!!!");
#endif

		PlayFabApiManager.instance.RequestIncCliSus(eClientSuspectCode.CheatTable);

		Timing.RunCoroutine(DeleyedQuit(5.0f));

		detectedCheatTable = true;
	}

	static IEnumerator<float> DeleyedQuit(float delay)
	{
		yield return Timing.WaitForSeconds(delay);

		Application.Quit();
	}
}
