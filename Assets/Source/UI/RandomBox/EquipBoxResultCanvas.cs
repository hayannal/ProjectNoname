using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using MEC;

public class EquipBoxResultCanvas : MonoBehaviour
{
	public static EquipBoxResultCanvas instance;

	public GameObject goldDiaRectObject;
	public Text goldValueText;
	public Text diaValueText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	public GridLayoutGroup contentGridLayoutGroup;

	public GameObject exitGroupObject;

	public EquipListStatusInfo equipSmallStatusInfo;
	public RectTransform smallStatusBackBlurImage;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
		smallStatusBackBlurImage.SetAsFirstSibling();
	}

	void OnEnable()
	{
		exitGroupObject.SetActive(false);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		equipSmallStatusInfo.gameObject.SetActive(false);
		_materialSmallStatusInfoShowRemainTime = 0.0f;

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	void Update()
	{
		UpdateEquipSmallStatusInfo();
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	public void RefreshInfo(List<ItemInstance> listGrantItem, int addGold = 0, int addDia = 0)
	{
		bool goldDia = (addDia > 0) || (addDia > 0);
		goldDiaRectObject.SetActive(goldDia);
		if (goldDia)
		{
			goldValueText.text = addGold.ToString("N0");
			diaValueText.text = addDia.ToString("N0");
		}

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		_listEquipCanvasListItem.Clear();

		if (listGrantItem == null)
			return;

		// 이 타이밍이 가장 갱신하기 좋은 타이밍이다.
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.gameObject.activeSelf)
			TimeSpacePortal.instance.RefreshAlarmObject();

		contentGridLayoutGroup.childAlignment = (listGrantItem.Count == 1) ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
		Timing.RunCoroutine(ItemProcess(listGrantItem));
	}

	IEnumerator<float> ItemProcess(List<ItemInstance> listGrantItem)
	{
		for (int i = 0; i < listGrantItem.Count; ++i)
		{
			EquipData newEquipData = new EquipData();
			newEquipData.equipId = listGrantItem[i].ItemId;
			newEquipData.Initialize(listGrantItem[i].CustomData);

			// 스케일 초기화를 해줘야 안줄어드는 경우 없이 잘 트윈이 먹는다.
			contentItemPrefab.transform.localScale = Vector3.one;

			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(newEquipData, OnClickListItem);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
			yield return Timing.WaitForSeconds(0.2f);
		}

		exitGroupObject.SetActive(true);
	}

	public void OnClickListItem(EquipData equipData)
	{
		equipSmallStatusInfo.RefreshInfo(equipData, false);
		equipSmallStatusInfo.gameObject.SetActive(false);
		equipSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	float _materialSmallStatusInfoShowRemainTime;
	void UpdateEquipSmallStatusInfo()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.deltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				equipSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}