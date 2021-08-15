using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class LevelPackageInfo : MonoBehaviour
{
	public GameObject[] levelPackagePrefabList;
	public ScrollSnap scrollSnap;

	public LevelPackageBox levelPackageBoxReference;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<LevelPackageBox>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	List<int> _listShowIndex = new List<int>();
	List<LevelPackageBox> _listLevelPackageBoxListItem = new List<LevelPackageBox>();
	public void RefreshInfo()
	{
		if (contentItemPrefab.activeSelf)
			contentItemPrefab.SetActive(false);

		for (int i = 0; i < _listLevelPackageBoxListItem.Count; ++i)
			_listLevelPackageBoxListItem[i].gameObject.SetActive(false);
		_listLevelPackageBoxListItem.Clear();
		_listShowIndex.Clear();

		CheckUnprocessed();

		// 먼저 팀레벨 패키지 테이블을 돌면서 구매하지 않은 항목들을 찾아야한다.
		// 그리고 이 항목의 레벨보다 현재 팀레벨이 높다면 보여주고 아니면 보여주지 않는다.
		// 보여줄땐 제일 우측꺼를 보여주면 된다.
		for (int i = 0; i < TableDataManager.instance.shopLevelPackageTable.dataArray.Length; ++i)
		{
			int researchLevel = TableDataManager.instance.shopLevelPackageTable.dataArray[i].level;
			if (PlayerData.instance.researchLevel < researchLevel)
				continue;
			if (PlayerData.instance.IsPurchasedLevelPackage(researchLevel))
				continue;

			_listShowIndex.Add(i);
		}

		// 보여줄게 없다면 통째로 꺼두면 된다.
		if (_listShowIndex.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		for (int i = 0; i < _listShowIndex.Count; ++i)
		{
			LevelPackageBox levelPackageBox = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			levelPackageBox.RefreshInfo(TableDataManager.instance.shopLevelPackageTable.dataArray[_listShowIndex[i]]);
			_listLevelPackageBoxListItem.Add(levelPackageBox);
		}

		// 다 구성하고나서 이렇게 Setup 호출해주면 된다.
		scrollSnap.Setup();
		scrollSnap.GoToLastPanel();
		gameObject.SetActive(true);
	}


	#region Unprocessed
	public bool standbyUnprocessed { get; set; }
	void CheckUnprocessed()
	{
		standbyUnprocessed = false;

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
			standbyUnprocessed = true;
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingInventory"));
			return;
		}

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress"), () =>
		{
			WaitingNetworkCanvas.Show(true);

			// 하단 RetryPurchase와 마찬가지로 reference를 통해서 호출해준다.
			levelPackageBoxReference.DropLevelPackage(TableDataManager.instance.shopLevelPackageTable.dataArray[index]);
		});
	}
	#endregion


	public void RetryPurchase(Product product, ShopLevelPackageTableData shopLevelPackageTableData)
	{
		// 위에 CheckUnprocessed 처리도 그렇고 구매처리를 하는 LevelPackageBox 객체가 필요하다.
		// 그래서 인스턴스를 위해 꺼두고 사용하는 게임오브젝트에 붙어있는 스크립트에 접근해 호출하기로 했다.
		levelPackageBoxReference.RetryPurchase(product, shopLevelPackageTableData);
	}
}