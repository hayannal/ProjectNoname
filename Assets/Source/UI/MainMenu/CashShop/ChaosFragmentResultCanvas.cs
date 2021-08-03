using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class ChaosFragmentResultCanvas : MonoBehaviour
{
	public static ChaosFragmentResultCanvas instance;

	public RectTransform toastBackImageRectTransform;
	public CharacterBoxResultListItem characterBoxResultListItem;
	public GameObject exitObject;
	public GameObject levelUpPossibleTextObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		toastBackImageRectTransform.gameObject.SetActive(false);
		characterBoxResultListItem.gameObject.SetActive(false);
		exitObject.SetActive(false);

		Timing.RunCoroutine(PpProcess());
	}

	IEnumerator<float> PpProcess()
	{
		_processed = true;

		// 슬롯을 갱신해야한다.
		CashShopCanvas.instance.dailyShopChaosInfo.gameObject.SetActive(false);
		CashShopCanvas.instance.dailyShopChaosInfo.gameObject.SetActive(true);

		// 이때 DotMainMenu꺼도 같이 해둔다.
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.RefreshCashShopAlarmObject();

		// 0.15초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.15f);
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		if (listPpInfo.Count == 0)
		{
			yield return Timing.WaitForSeconds(0.2f);
			exitObject.SetActive(true);
			yield break;
		}


		// 컨텐츠로 얻은 pp는 별도로 추가해야한다.
		PlayerData.instance.ppContentsAddCount += listPpInfo[0].add;


		// 0번꺼 가져와서 셋팅한다.
		bool levelUpPossible = false;
		int powerLevel = 0;
		int transcendLevel = 0;
		bool showPlusAlarm = false;
		CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[0].actorId);
		if (characterData != null)
		{
			powerLevel = characterData.powerLevel;
			transcendLevel = characterData.transcendLevel;
			showPlusAlarm = characterData.IsPlusAlarmState();
		}
		characterBoxResultListItem.characterListItem.Initialize(listPpInfo[0].actorId, powerLevel, SwapCanvasListItem.GetPowerLevelColorState(characterData), transcendLevel, 0, null, null, null);
		characterBoxResultListItem.characterListItem.ShowAlarm(false);
		if (showPlusAlarm)
		{
			characterBoxResultListItem.characterListItem.ShowAlarm(true, true);
			levelUpPossible = true;
		}
		characterBoxResultListItem.Initialize("", listPpInfo[0].add);
		characterBoxResultListItem.gameObject.SetActive(true);

		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);
		levelUpPossibleTextObject.SetActive(levelUpPossible);

		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		DropManager.instance.ClearLobbyDropInfo();

		_processed = false;
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed)
			return;

		toastBackImageRectTransform.gameObject.SetActive(false);
		characterBoxResultListItem.gameObject.SetActive(false);
		exitObject.SetActive(false);
		levelUpPossibleTextObject.SetActive(false);
		gameObject.SetActive(false);
	}
}