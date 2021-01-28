using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarEnterAgainIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
	}

	public void OnClickEnterAgainButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("NodeWarAgainDiamond");
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_NodeWarAgainEnter"), CurrencyData.eCurrencyType.Diamond, price, true, () =>
			{
				if (CurrencyData.instance.dia < price)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
					return;
				}
				PlayFabApiManager.instance.RequestOpenAgainNodeWar(price, () =>
				{
					CurrencySmallInfoCanvas.RefreshInfo();
					NodeWarPortal.instance.RefreshRemainTime();
					NodeWarPortal.instance.HideIndicator();
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					gameObject.SetActive(false);
				});
			});
		});
	}
}