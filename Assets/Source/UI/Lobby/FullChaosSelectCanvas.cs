using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullChaosSelectCanvas : MonoBehaviour
{
	public static FullChaosSelectCanvas instance;

	public Transform subTitleTextTransform;

	void Awake()
	{
		instance = this;
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_ChaosPopMore"), 300, subTitleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickChallengeButton()
	{
		int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (PlayerData.instance.selectedChapter >= chapterLimit)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotChallengeForUpdate"), 1.0f);
			return;
		}

		PlayFabApiManager.instance.RequestSelectFullChaos(true, () =>
		{
			// 알아서 인디케이터도 삭제될테니 GatePillar만 교체
			GatePillar.instance.gameObject.SetActive(false);
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.challengeGatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

			// 가장 중요한 맵 재구축. 씬 이동 없이 해야한다.
			StageManager.instance.ChangeChallengeMode();
			gameObject.SetActive(false);
		});
	}

	public void OnClickRevertButton()
	{
		PlayFabApiManager.instance.RequestSelectFullChaos(false, () =>
		{
			// 바로 공용 보상 팝업창 띄우면 된다.
			// 이게 만약 고정보상이라면 서버에서도 이미 처리했을거고
			// 랜덤보상이라면 아예 요청할때부터 보내서 서버 갱신하고 받는 처리 하면 될거다.
			// 우선은 OkCanvas로 느낌만 내본다.
			GatePillar.instance.RefreshPurify();
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ExitGame"));
			gameObject.SetActive(false);
		});
	}
}