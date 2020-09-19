using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChaosPurifierConfirmCanvas : MonoBehaviour
{
	public static ChaosPurifierConfirmCanvas instance;

	public Transform subTitleTransform;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public GameObject challengeGatePillarSpawnEffectPrefab;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);
		RefreshInfo();
		buttonObject.SetActive(true);
	}

	void OnDisable()
	{
		CurrencySmallInfoCanvas.Show(false);
	}

	void RefreshInfo()
	{
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(PlayerData.instance.selectedChapter);
		if (chapterTableData == null)
			return;

		int price = chapterTableData.purifyGold;

		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.gold < price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
		_price = price;
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_ChaosPurifyMore"), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}

	int _price;
	public void OnClickButton()
	{
		if (PlayerData.instance.chaosMode == false)
			return;

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestPurifyChaos(_price, OnRecvPurify);

		buttonObject.SetActive(false);
	}

	void OnRecvPurify()
	{
		CurrencySmallInfoCanvas.RefreshInfo();

		// FullChaosSelectCanvas에서 하던거 비슷하게 따라하면 된다.
		ChaosPurifier.instance.gameObject.SetActive(false);
		GatePillar.instance.gameObject.SetActive(false);
		BattleInstanceManager.instance.GetCachedObject(StageManager.instance.challengeGatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		BattleInstanceManager.instance.GetCachedObject(challengeGatePillarSpawnEffectPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

		StageManager.instance.ChangeChallengeMode();
		gameObject.SetActive(false);
	}
}