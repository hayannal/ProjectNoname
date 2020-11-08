using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SupportListCanvas : MonoBehaviour
{
	public static SupportListCanvas instance;

	public GameObject emptySupportObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SupportCanvasListItem>
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
	}

	void OnEnable()
	{
		RefreshGrid();

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickHomeButton()
	{
		gameObject.SetActive(false);
	}


	List<SupportCanvasListItem> _listSupportCanvasListItem = new List<SupportCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listSupportCanvasListItem.Count; ++i)
			_listSupportCanvasListItem[i].gameObject.SetActive(false);
		_listSupportCanvasListItem.Clear();

		List<SupportData.MySupportData> listMySupportData = SupportData.instance.listMySupportData;
		if (listMySupportData == null || listMySupportData.Count == 0)
		{
			emptySupportObject.SetActive(true);
			return;
		}

		for (int i = 0; i < listMySupportData.Count; ++i)
		{
			// 미처 삭제하지 못한 문의내역 중에 오래된 문의는 아예 표시하지 않기로 하려다가 클라는 rcdDat를 파싱하지 않기로 했었어서 그냥 패스하기로 한다.
			//

			SupportCanvasListItem supportCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			supportCanvasListItem.Initialize(i, listMySupportData[i]);
			_listSupportCanvasListItem.Add(supportCanvasListItem);
		}
	}

	public void OnClickListItem(SupportData.MySupportData data)
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SupportReadCanvas", () =>
		{
			SupportReadCanvas.instance.RefreshText(data);
		});
	}

	public void OnClickWriteButton()
	{
		gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("SupportWriteCanvas", null);
	}
}