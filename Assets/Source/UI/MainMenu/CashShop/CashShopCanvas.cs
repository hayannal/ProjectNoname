using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		LobbyCanvas.Home();
	}




	public void OnClickEquipBox1()
	{
		int equipBoxPrice = 30;
		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 1", () =>
		{
			// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
			_dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdql", "", true, true);
			PlayFabApiManager.instance.RequestEquipBox(_dropProcessor, equipBoxPrice, OnRecvEquipBox);
		});
	}

	public void OnClickEquipBox8()
	{
		int equipBoxPrice = 200;
		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 8", () =>
		{
			_dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdwkdql", "", true, true);
			PlayFabApiManager.instance.RequestEquipBox(_dropProcessor, equipBoxPrice, OnRecvEquipBox);
		});
	}

	DropProcessor _dropProcessor;
	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		currencySmallInfo.RefreshInfo();

		// 연출은 연출대로 두고
		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = null;
		if (itemGrantString != "")
		{
			listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
			for (int i = 0; i < listGrantItem.Count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(_dropProcessor, false, () =>
			{
				// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
				// 여기서는 장비 그리드를 띄운다.
				// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}
}