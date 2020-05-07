using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class LevelPackageInfo : MonoBehaviour
{
	public GameObject[] levelPackagePrefabList;

	public Transform iconImageRootTransform;
	public Text valueXText;
	public Text priceText;
	public RectTransform priceTextTransform;
	public Text wonText;
	public Text prevPriceText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;
	public Text nameText;

	ShopLevelPackageTableData _shopLevelPackageTableData;
	GameObject _subPrefabObject;
	public void RefreshInfo()
	{
		CheckUnprocessed();

		// 먼저 팀레벨 패키지 테이블을 돌면서 구매하지 않은 첫번째 항목을 찾아야한다.
		// 그리고 이 항목의 레벨보다 현재 팀레벨이 높다면 보여주고 아니면 보여주지 않는다.
		int index = -1;
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			int researchLevel = TableDataManager.instance.shopLevelPackageTable.dataArray[i].level;
			//if (PlayerData.instance.researchLevel < researchLevel)
			//	continue;
			if (PlayerData.instance.IsPurchasedLevelPackage(researchLevel))
				continue;

			index = i;
			break;
		}
		if (index == -1)
		{
			gameObject.SetActive(false);
			return;
		}

		ShopLevelPackageTableData shopLevelPackageTableData = TableDataManager.instance.shopLevelPackageTable.dataArray[index];

		if (_subPrefabObject != null)
			_subPrefabObject.SetActive(false);
		int imagePrefabIndex = FindImagePrefabIndex(shopLevelPackageTableData.imagePrefab);
		_subPrefabObject = UIInstanceManager.instance.GetCachedObject(levelPackagePrefabList[imagePrefabIndex], iconImageRootTransform);

		valueXText.text = string.Format("{0}x", shopLevelPackageTableData.times);

		bool kor = (OptionManager.instance.language == "KOR");
		priceTextTransform.anchoredPosition = new Vector2(kor ? 10.0f : 0.0f, 0.0f);
		wonText.gameObject.SetActive(kor);
		if (kor)
		{
			prevPriceText.text = shopLevelPackageTableData.beforeKor.ToString("N0");
			priceText.text = shopLevelPackageTableData.kor.ToString("N0");
			wonText.SetLocalizedText(BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"));
		}
		else
		{
			prevPriceText.text = string.Format("$ {0:0.##}", shopLevelPackageTableData.beforEng);
			priceText.text = string.Format("$ {0:0.##}", shopLevelPackageTableData.eng);
			wonText.gameObject.SetActive(false);
		}
		nameText.SetLocalizedText(UIString.instance.GetString(shopLevelPackageTableData.boxName));

		RefreshLineImage();
		_updateRefreshLineImage = true;
		_shopLevelPackageTableData = shopLevelPackageTableData;
	}

	int FindImagePrefabIndex(string name)
	{
		for (int i = 0; i < levelPackagePrefabList.Length; ++i)
		{
			if (levelPackagePrefabList[i].name == name)
				return i;
		}
		return 0;
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
	}

	bool _updateRefreshLineImage;
	void Update()
	{
		if (_updateRefreshLineImage)
		{
			RefreshLineImage();
			_updateRefreshLineImage = false;
		}
	}

	public void OnClickButton()
	{
		if (_standbyUnprocessed)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingInventory"));
			return;
		}

		if ((_shopLevelPackageTableData.buyingEquipKey > 0 || _shopLevelPackageTableData.buyingLegendEquipKey > 0) && TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetwork(OnResponse);
	}

	public void OnResponse()
	{
		// 통신이 되는 상황이면 나머지 스텝을 진행하면 되는데 인풋차단부터 걸어놔야 안전하다.
		WaitingNetworkCanvas.Show(true);

		// 원래라면 IAP 결제를 진행해야하는데 차후에 붙이기로 했으니 성공했다고 가정하고 Validate패킷 대신 일반 구매 패킷으로 처리해본다.
		PlayFabApiManager.instance.RequestValidateLevelPackage(_shopLevelPackageTableData.serverItemId, _shopLevelPackageTableData, () =>
		{
			// 서버에서 구매 ok가 떨어지면 아마 Confirm부터 해야할텐데 지금은 일반 함수로 구현하는거니 패스하고
			//ConfirmPurchase

			// 여기서부턴 연출을 시작해야한다.
			DropLevelPackage();
		});
	}

	DropProcessor _cachedDropProcessor;
	void DropLevelPackage()
	{
		// 레벨 패키지에 뭐가 들어있냐에 따라 굴리는 로직이 달라진다.
		// 크게 나누자면 장비가 있고 없고의 차이다.
		// 하지만 둘다한테 필요한게 있는데 바로 레벨패키지 목록 갱신이다.
		List<int> listResult = PlayerData.instance.AddLevelPackage(_shopLevelPackageTableData.level);
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonLevelPackageList = serializer.SerializeObject(listResult);

		if (_shopLevelPackageTableData.buyingEquipKey > 0 || _shopLevelPackageTableData.buyingLegendEquipKey > 0)
		{
			// 이땐 드랍을 굴리는데
			// 특이한 점은 showingDrop말고 실제 드랍이 껴있을테니 그것도 함께 굴려야한다는거다.
			// showingDrop에는 골드 다이아가 들어있을테고 그건 패킷으로 보내지 않고 무시하지만
			// equipKeyDrop에 있는 장비는 실제로 뽑아야할 장비가 들어있으므로 패킷으로 보내서 itemInstanceId를 받아야 클라에서도 템을 생성할 수 있게된다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _shopLevelPackageTableData.showingDrop, _shopLevelPackageTableData.equipKeyDrop, true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);
			if (CheatingListener.detectedCheatTable)
				return;

			// 장비가 있을땐 EquipBox 패킷을 보내며 결과로 오면 드랍연출을 보여준다.
			// 만약 이거하기전에 클라가 강종되면 키가 남아있는채로 끝날거고
			// 다음 재접에서 캐시샵을 열때 미처리된게 있다면서 이 로직을 다시 수행하면 된다.
			// 그리고 레벨패키지 리스트 갱신을 위해 별도 패킷을 또 한번 날릴바엔 장비뽑기에 실어서 보내는게 나아서 추가인자로 넘기기로 한다.
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), 0, _shopLevelPackageTableData.buyingEquipKey, _shopLevelPackageTableData.buyingLegendEquipKey, jsonLevelPackageList, OnRecvEquipBox);
		}
		else
		{
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _shopLevelPackageTableData.showingDrop, "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);

			// 장비가 없을땐 연출 상자 보여주면 끝이긴 한데 그전에 레벨 패키지를 샀음을 서버에 기록해놔야한다.
			// 사실 이거 하기전에 클라를 종료시키더라도 이미 패키지로 산 재화는 다 들어갔기 때문에 복구같은건 할 필요 없다.
			// 단지 패키지를 한번 더 살 수 있게 되는건데 이건 허용하기로 한다.
			PlayFabApiManager.instance.RequestUpdateLevelPackageList(jsonLevelPackageList, OnRecvComplete);
		}
	}

	void OnRecvComplete()
	{
		OnRandomBoxScreen(null);
	}

	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		// 아이콘의 프리로드를 진행한다.
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

		OnRandomBoxScreen(listGrantItem);
	}

	void OnRandomBoxScreen(List<ItemInstance> listGrantItem)
	{
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			WaitingNetworkCanvas.Show(false);

			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Dia4_6, _cachedDropProcessor, 0, () =>
			{
				// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
				DropManager.instance.ClearLobbyDropInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
				CurrencyData.instance.OnRecvRefillEnergy(_shopLevelPackageTableData.buyingEnergy);

				if (listGrantItem == null)
				{
					// 장비가 들어있지 않은 레벨패키지는 공용 재화창을 사용하고
					UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
					{
						CurrencyBoxResultCanvas.instance.RefreshInfo(_shopLevelPackageTableData.buyingGold, _shopLevelPackageTableData.buyingGems);
					});
				}
				else
				{
					// 장비가 들어있는 레벨패키지는 장비창꺼를 같이 쓴다.
					UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
					{
						EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem, _shopLevelPackageTableData.buyingGold, _shopLevelPackageTableData.buyingGems);
					});
				}
			});
		});
	}

	#region Unprocessed
	bool _standbyUnprocessed = false;
	void CheckUnprocessed()
	{
		_standbyUnprocessed = false;

		if (CurrencyData.instance.equipBoxKey == 0 && CurrencyData.instance.legendEquipKey == 0)
			return;

		// 굴리지 않은 장비를 포함한 레벨팩이 남았다는거다. 어떤 패키지였는지 찾아야한다.
		int index = -1;
		int subIndex = -1;
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.shopLevelPackageTable.dataArray[i].buyingEquipKey != CurrencyData.instance.equipBoxKey)
				continue;
			if (TableDataManager.instance.shopLevelPackageTable.dataArray[i].buyingLegendEquipKey != CurrencyData.instance.legendEquipKey)
				continue;

			int researchLevel = TableDataManager.instance.shopLevelPackageTable.dataArray[i].level;
			//if (PlayerData.instance.researchLevel < researchLevel)
			//	continue;
			if (PlayerData.instance.IsPurchasedLevelPackage(researchLevel))
			{
				if (subIndex == -1)
					subIndex = i;
				continue;
			}

			index = i;
			break;
		}
		if (index == -1)
		{
			// 분명 디비에 키가 남아있는데 못찾는 경우다. 이럴수가 있나..
			// 이럴때 대비해서 우선 subIndex라도 한번 더 쓰게 한다.
			index = subIndex;
		}
		// subIndex까지 쓰고도 -1이면 에이 모르겠다. 넘어가자.
		if (index == -1)
			return;		

		// 진행할 수 있는지를 판단해야한다.
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			_standbyUnprocessed = true;
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingInventory"));
			return;
		}

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress"), () =>
		{
			WaitingNetworkCanvas.Show(true);
			_shopLevelPackageTableData = TableDataManager.instance.shopLevelPackageTable.dataArray[index];
			DropLevelPackage();
		});
	}
	#endregion
}