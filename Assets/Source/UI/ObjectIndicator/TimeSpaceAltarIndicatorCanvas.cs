using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSpaceAltarIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;
	public int positionIndex { get; set; }

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
	}

	public void OnClickEquipButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("EquipListCanvas", () =>
		{
			EquipListCanvas.instance.RefreshInfo(positionIndex);
		});
	}
}