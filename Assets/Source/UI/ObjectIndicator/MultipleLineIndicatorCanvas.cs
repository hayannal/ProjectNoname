using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleLineIndicatorCanvas : ObjectIndicatorCanvas
{
	public static MultipleLineIndicatorCanvas instance;
	public Text contextText;

	void Awake()
	{
		instance = this;
	}

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