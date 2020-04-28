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


	DropProcessor _cachedDropProcessor;
	int _repeatRemainCount;
	public void OnClickCharacterBox()
	{
		// 오리진이나 장비 뽑기와 달리 연속뽑기가 있다.
		// 첫번째 연출에서는 뽑기상자를 터치해서 열지만 두번째부터는 자동으로 패킷 보내면서 굴려져야한다.

		int characterBoxPrice = 50;
		_repeatRemainCount = 2;
		YesNoCanvas.instance.ShowCanvas(true, "confirm", "character box repeat", () =>
		{
			// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflr", "", true, true);
			PlayFabApiManager.instance.RequestCharacterBox(characterBoxPrice, OnRecvCharacterBox);
		});
	}

	void OnRecvCharacterBox(bool serverFailure)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		currencySmallInfo.RefreshInfo();

		// 최초 1회는 굴린거니까 1을 차감해둔다.
		_repeatRemainCount -= 1;
		
		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// repeatRemainCount를 0으로 보내면 오리진 박스처럼 한번 굴려진 결과가 바로 결과창에 보이게 된다.
			// 하지만 이 값을 1 이상으로 보내면 내부적으로 n회 돌린 후 누적해서 보여주게 된다.
			RandomBoxScreenCanvas.instance.SetInfo(_cachedDropProcessor, false, _repeatRemainCount, () =>
			{
				// 결과창은 각 패킷이 자신의 Response에 맞춰서 보여줘야한다.
				// 결과창을 닫을때 RandomBoxScreenCanvas도 같이 닫아주면 알아서 시작점인 CashShopCanvas로 돌아오게 될거다.
				UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxResultCanvas", () =>
				{
					CharacterBoxResultCanvas.instance.RefreshInfo(false);
				});
			});
		});
	}

	public void OnClickEquipBox1()
	{
		int equipBoxPrice = 30;
		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 1", () =>
		{
			// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdql", "", true, true);
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), equipBoxPrice, OnRecvEquipBox);
		});
	}

	public void OnClickEquipBox8()
	{
		int equipBoxPrice = 200;
		YesNoCanvas.instance.ShowCanvas(true, "confirm", "equip box 8", () =>
		{
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wkdwkdql", "", true, true);
			PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), equipBoxPrice, OnRecvEquipBox);
		});
	}

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
			RandomBoxScreenCanvas.instance.SetInfo(_cachedDropProcessor, false, 0, () =>
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