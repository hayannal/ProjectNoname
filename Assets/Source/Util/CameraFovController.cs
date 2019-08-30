using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraFovController : MonoBehaviour
{
	public float horizontalFov = 8.0f;

	Camera _mainCamera;
	int _lastWidth;
	int _lastHeight;

	void Awake()
	{
		_mainCamera = GetComponent<Camera>();
	}

	void Start()
	{
		RefreshFov();
	}

	public void RefreshFov()
	{
		_mainCamera.fieldOfView = horizontalFov / _mainCamera.aspect;
		_lastWidth = Screen.width;
		_lastHeight = Screen.height;
	}

	void Update()
	{
		if (_mainCamera == null)
			return;

		if (_lastWidth != Screen.width || _lastHeight != Screen.height)
			RefreshFov();
	}
}
