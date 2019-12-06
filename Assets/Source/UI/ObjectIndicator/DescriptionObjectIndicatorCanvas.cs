using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DescriptionObjectIndicatorCanvas : ObjectIndicatorCanvas
{
	public Text contextText;

	// Start is called before the first frame update
	void Start()
    {
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
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
