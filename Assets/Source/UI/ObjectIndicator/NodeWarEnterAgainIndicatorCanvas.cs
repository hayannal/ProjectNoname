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

		// 거의 이럴일은 없겠지만
		// 창을 띄워둔채로 다음날이 되어서 nodeWarCleared가 해제된다면
		// 인디케이터가 비활성화 될텐데 이 타이밍에 켜놨던 ConfirmSpendCanvas도 같이 꺼주면
		// 다음날 되서 패킷 날리는 일을 막을 수 있다.
		//
		// 사실 이건 NodeWarEnterAgainConfirmCanvas를 별도로 만들면 거기서 처리하면 되는건데 중복해서 만드는 느낌이라 안만드려다보니
		// 이런 식으로 할 수 밖에 없었던거다.
		if (_isShowConfirmSpendCanvas)
		{
			_isShowConfirmSpendCanvas = false;
			ConfirmSpendCanvas.instance.gameObject.SetActive(false);
			return;
		}
	}

	void Update()
	{
		UpdateObjectIndicator();

		// 클로즈 이벤트를 받을 수 없으니 할 수 없이 매프레임 감지해야한다.
		if (_isShowConfirmSpendCanvas && ConfirmSpendCanvas.instance != null && ConfirmSpendCanvas.instance.gameObject.activeSelf == false)
			_isShowConfirmSpendCanvas = false;
	}

	bool _isShowConfirmSpendCanvas = false;
	public void OnClickEnterAgainButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			_isShowConfirmSpendCanvas = true;

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
					gameObject.SetActive(false);
				});
			});
		});
	}
}