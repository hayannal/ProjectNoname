using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelPackageImagePrefabInfo : MonoBehaviour
{
	public int teamLevel;
	public Text goldText;
	public Text diaText;
	public Text energyText;
	public Text equipBoxKeyText;
	public Text legendEquipKeyText;

	void Start()
	{
		ShopLevelPackageTableData shopLevelPackageTableData = TableDataManager.instance.FindShopLevelPackageTableData(teamLevel);
		if (shopLevelPackageTableData == null)
			return;

		if (goldText != null)
			goldText.text = shopLevelPackageTableData.buyingGold.ToString("N0");
		if (diaText != null)
			diaText.text = shopLevelPackageTableData.buyingGems.ToString("N0");
		if (energyText != null)
			energyText.text = shopLevelPackageTableData.buyingEnergy.ToString("N0");
		if (equipBoxKeyText != null)
			equipBoxKeyText.text = string.Format("x {0}", shopLevelPackageTableData.buyingEquipKey);
		if (legendEquipKeyText != null)
			legendEquipKeyText.text = string.Format("x {0}", shopLevelPackageTableData.buyingLegendEquipKey);
	}
}