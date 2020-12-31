using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class FullChaosSelectCanvas : MonoBehaviour
{
	public static FullChaosSelectCanvas instance;

	public Transform subTitleTextTransform;
	public Text chapterText;
	public GameObject challengeGatePillarSpawnEffectPrefab;

	void Awake()
	{
		instance = this;
	}

	int _price;
	void OnEnable()
	{
		string romanNumberString = UIString.instance.GetString(string.Format("GameUI_RomanNumber{0}", PlayerData.instance.selectedChapter));
		chapterText.text = UIString.instance.GetString("GameUI_MenuChapter", romanNumberString);
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

		PlayFabApiManager.instance.RequestSelectFullChaos(() =>
		{
			// 알아서 인디케이터도 삭제될테니 GatePillar만 교체
			GatePillar.instance.gameObject.SetActive(false);
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.challengeGatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
			BattleInstanceManager.instance.GetCachedObject(challengeGatePillarSpawnEffectPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

			// 가장 중요한 맵 재구축. 씬 이동 없이 해야한다.
			StageManager.instance.ChangeChallengeMode();
			gameObject.SetActive(false);
		});
	}

	DropProcessor _cachedDropProcessor;
	public void OnClickRevertButton()
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(PlayerData.instance.selectedChapter);
		if (chapterTableData == null)
			return;

		// 이제 환원도 드랍을 굴리는 방식으로 바뀌게 되었다.
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, chapterTableData.revertDropId, "", true, true);
		_cachedDropProcessor.AdjustDropRange(3.2f);
		if (CheatingListener.detectedCheatTable)
			return;

		// 다른 드랍들 처리했을때와 달리 아무것도 드랍되지 않을때가 있기 때문에 백업 드랍을 처리해야하는지 확인해야한다. 백업 드랍은 비어있지 않는다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();
		if (addGold == 0 && addDia == 0 && listDropItemId.Count == 0)
		{
			_cachedDropProcessor.gameObject.SetActive(false);
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, chapterTableData.noneRevertDropId, "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.2f);
			if (CheatingListener.detectedCheatTable)
				return;
		}

		PlayFabApiManager.instance.RequestSelectFullChaosRevert(OnRecvRevert);
	}

	void OnRecvRevert(string itemGrantString)
	{
		// 연출은 연출대로 두고
		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = null;
		int count = 0;
		if (itemGrantString != "")
		{
			listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
			count = listGrantItem.Count;
			for (int i = 0; i < count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();

		// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
		DropManager.instance.ClearLobbyDropInfo();

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출이 시작될때 같이 하이드 시켜야한다.
			gameObject.SetActive(false);

			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Revert, _cachedDropProcessor, 0, 0, () =>
			{
				if (listGrantItem == null)
				{
					// 보상으로 장비가 나오지 않았다면 공용 재화창을 사용하고
					UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
					{
						CurrencyBoxResultCanvas.instance.RefreshInfo(addGold, addDia);

						// 결과창을 보여주는 타이밍에 게이트 필라도 갱신해둔다.
						GatePillar.instance.RefreshPurify();
					});
				}
				else
				{
					// 장비가 나왔다면 장비결과창을 쓴다.
					UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
					{
						EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem, addGold, addDia);

						// 결과창을 보여주는 타이밍에 게이트 필라도 갱신해둔다.
						GatePillar.instance.RefreshPurify();
					});
				}
			});
		});
	}
}