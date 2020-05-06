using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

	GameObject _subPrefabObject;
	public void RefreshInfo()
	{
		// 먼저 팀레벨 패키지 테이블을 돌면서 구매하지 않은 첫번째 항목을 찾아야한다.
		// 그리고 이 항목의 레벨보다 현재 팀레벨이 높다면 보여주고 아니면 보여주지 않는다.
		bool show = true;

		if (!show)
		{
			gameObject.SetActive(false);
			return;
		}

		if (_subPrefabObject != null)
			_subPrefabObject.SetActive(false);
		_subPrefabObject = UIInstanceManager.instance.GetCachedObject(levelPackagePrefabList[0], iconImageRootTransform);

		bool kor = (OptionManager.instance.language == "KOR");
		priceTextTransform.anchoredPosition = new Vector2(kor ? 10.0f : 0.0f, 0.0f);
		wonText.gameObject.SetActive(kor);
		if (kor)
		{
			//priceText.text = shopDiamondTableData.kor.ToString("N0");
			wonText.SetLocalizedText(BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"));
		}
		else
		{
			//priceText.text = string.Format("$ {0:0.##}", shopDiamondTableData.eng);
			wonText.gameObject.SetActive(false);
		}

		RefreshLineImage();
		_updateRefreshLineImage = true;
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

	}
}