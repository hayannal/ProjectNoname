using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class EquipBoxConfirmCanvas : MonoBehaviour
{
	public static EquipBoxConfirmCanvas instance = null;

	public Text equipBoxNameText;
	public Image equipBoxAddImage;
	public Transform equipBoxAddImageTransform;
	public Text equipBoxAddText;
	public Image equipBoxImage;
	public RectTransform equipBoxImageRectTransform;
	public Text priceText;
	public GameObject buttonObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	void OnDisable()
	{
		TooltipCanvas.Hide();
	}

	bool _miniBox;
	int _price;
	public void ShowCanvas(bool show, bool miniBox, int price, string name, string addText, Sprite equipBoxSprite, Vector2 anchoredPosition, Vector2 sizeDelta)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_miniBox = miniBox;
		_price = price;

		// 하단 다이아 영역이 36이었는데 잘려나가면서 강제로 offset처리를 해줘야한다.
		anchoredPosition.y -= 36.0f * 0.5f;

		equipBoxNameText.SetLocalizedText(name);
		bool existAddText = !string.IsNullOrEmpty(addText);
		equipBoxAddImage.gameObject.SetActive(existAddText);
		if (existAddText) equipBoxAddText.text = addText;
		equipBoxImage.sprite = equipBoxSprite;
		equipBoxImageRectTransform.anchoredPosition = anchoredPosition;
		equipBoxImageRectTransform.sizeDelta = sizeDelta;
		priceText.text = price.ToString("N0");
	}

	Dictionary<int, float> _dicGradeWeight;
	public void OnClickInfoButton()
	{
		string detailText = UIString.instance.GetString(_miniBox ? "ShopUIMore_EquipmentBox1" : "ShopUIMore_EquipmentBox8");

		if (_dicGradeWeight == null)
			_dicGradeWeight = new Dictionary<int, float>();
		_dicGradeWeight.Clear();
		float notStreakAdjustWeight = TableDataManager.instance.FindNotStreakAdjustWeight(DropManager.instance.GetCurrentNotSteakCount());
		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			if (weight <= 0.0f)
				continue;

			if (EquipData.IsUseNotStreakGacha(TableDataManager.instance.equipTable.dataArray[i]))
				weight *= notStreakAdjustWeight;

			sumWeight += weight;

			if (_dicGradeWeight.ContainsKey(TableDataManager.instance.equipTable.dataArray[i].grade))
				_dicGradeWeight[TableDataManager.instance.equipTable.dataArray[i].grade] += weight;
			else
				_dicGradeWeight.Add(TableDataManager.instance.equipTable.dataArray[i].grade, weight);
		}
		string rateText = "";

		// 일반 장비의 확률은 1.0f - (1 ~ 4까지의 합산값) 으로 계산하기로 한다.
		float sumExceptZero = 0.0f;
		for (int i = 1; i < 5; ++i)
		{
			if (_dicGradeWeight.ContainsKey(i))
				sumExceptZero += _dicGradeWeight[i];
		}
		if (_dicGradeWeight.ContainsKey(0))
			_dicGradeWeight[0] = sumWeight - sumExceptZero;

		// 장비 Grade에는 Enum이 없다... 0 1 2 3 4 체크해서 보여주기로 한다.
		bool first = true;
		for (int i = 0; i < 5; ++i)
		{
			if (first)
				first = false;
			else
				rateText = string.Format("{0}\n", rateText);

			if (_dicGradeWeight.ContainsKey(i) == false)
				continue;

			string gradeText = UIString.instance.GetString(string.Format("ShopUI_EquipmentByGrade{0}", i));
			float resultRate = 0.0f;
			if (sumWeight > 0.0f)
				resultRate = _dicGradeWeight[i] / sumWeight;
			string addText = string.Format("{0} {1:0.####}%", gradeText, resultRate * 100.0f);
			rateText = string.Format("{0}{1}", rateText, addText);
		}

		string text = string.Format("{0}\n\n{1}", detailText, rateText);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 350, equipBoxAddImageTransform, new Vector2(0.0f, -20.0f));
	}

	DropProcessor _cachedDropProcessor;
	public void OnClickOkButton()
	{
		// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _miniBox ? "Wkdql" : "Wkdwkdql", "", true, true);
		if (_miniBox == false)
			_cachedDropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return;

		PlayFabApiManager.instance.RequestEquipBox(DropManager.instance.GetLobbyDropItemInfo(), _price, OnRecvEquipBox);

		// 패킷 보내고 먼저 버튼부터 하이드
		buttonObject.SetActive(false);
	}

	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

		// 연출은 연출대로 두고
		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
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

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출에 의해 캐시샵 가려질때 같이 하이드 시켜야한다.
			gameObject.SetActive(false);

			RandomBoxScreenCanvas.instance.SetInfo(count == 1 ? RandomBoxScreenCanvas.eBoxType.Equip1 : RandomBoxScreenCanvas.eBoxType.Equip8, _cachedDropProcessor, 0, () =>
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