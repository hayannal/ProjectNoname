using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIndicatorCanvas : ObjectIndicatorCanvas
{
	public Button buttonGroup;

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = BattleInstanceManager.instance.GetCachedCameraMain();

		InitializeTarget(targetTransform);
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	// Update is called once per frame
	void Update()
	{
		UpdateObjectIndicator();
	}
}
