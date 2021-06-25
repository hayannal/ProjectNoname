using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossBattleRefreshCanvas : MonoBehaviour
{
	public static BossBattleRefreshCanvas instance;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public GameObject priceOnIconImageObject;
	public GameObject priceOffIconImageObject;
	//public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshPrice();
	}

	int _price;
	void RefreshPrice()
	{
		// 가격
		int price = 1;
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.energy < price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		//priceGrayscaleEffect.enabled = disablePrice;
		priceOnIconImageObject.SetActive(!disablePrice);
		priceOffIconImageObject.SetActive(disablePrice);
		_price = price;
	}

	public void OnClickButton()
	{
		int nextId = PlayerData.instance.GetNextRandomBossId();
		PlayFabApiManager.instance.RequestRefreshBoss(nextId, 1, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_NewAppear"), 2.0f);
			BossBattleEnterCanvas.instance.gameObject.SetActive(false);
			BossBattleEnterCanvas.instance.gameObject.SetActive(true);
			gameObject.SetActive(false);
		});
	}
}