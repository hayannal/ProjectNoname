﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// RFX4_PerPlatformSettings 와 달리 씬 오브젝트도 쓸 수 있게 만들었다.
[ExecuteInEditMode]
public class NeedGrabObject : MonoBehaviour
{
	void OnEnable()
	{
		IncreaseRefCount();
	}

	void OnDisable()
	{
		DecreaseRefCount();
	}

	void IncreaseRefCount()
	{
		if (CustomRenderer.instance != null)
			++CustomRenderer.instance.needGrab_refCount;
	}

	void DecreaseRefCount()
	{
		if (CustomRenderer.instance != null)
			--CustomRenderer.instance.needGrab_refCount;
	}
}
