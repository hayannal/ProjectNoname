using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PlayerIgnoreEvadeCanvas : MonoBehaviour
{
	public static PlayerIgnoreEvadeCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.playerIgnoreEvadeCanvasPrefab).GetComponent<PlayerIgnoreEvadeCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static PlayerIgnoreEvadeCanvas _instance = null;

	public Text percentText;
	
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		_prevTargetPosition = -Vector3.up;
	}

	public void ShowIgnoreEvade(bool show, PlayerActor playerActor)
	{
		if (!show)
		{
			gameObject.SetActive(false);
			return;
		}

		gameObject.SetActive(true);

		if (_targetTransform == playerActor.cachedTransform)
		{
			UpdateGaugePosition();
			return;
		}

		_offsetY = playerActor.gaugeOffsetY;
		_targetTransform = playerActor.cachedTransform;
		GetTargetHeight(_targetTransform);
	}

	public void SetPercent(float rate)
	{
		percentText.text = string.Format("{0:N0}%", (int)(rate * 100.0f));
	}

	// Update is called once per frame
	Vector3 _prevTargetPosition = -Vector3.up;
	void Update()
	{
		if (_targetTransform != null)
		{
			if (_targetTransform.position != _prevTargetPosition)
			{
				UpdateGaugePosition();
				_prevTargetPosition = _targetTransform.position;
			}
		}

		// 아무래도 이 캔버스가 나와있는 상태에서 플레이어가 죽을 경우(혹은 아예 게임오브젝트가 꺼질 경우) 자동으로 Disable 하는 처리를 여기서 해야할거 같다.
		// 패시브 어펙터가 Finalize하기도 전에 삭제될 경우를 대비해서다.
		//
	}

	void GetTargetHeight(Transform t)
	{
		Collider collider = t.GetComponentInChildren<Collider>();
		if (collider == null)
			return;

		_targetHeight = ColliderUtil.GetHeight(collider);
	}

	Transform _targetTransform;
	float _targetHeight;
	float _offsetY;
	void UpdateGaugePosition()
	{
		Vector3 desiredPosition = _targetTransform.position;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += _offsetY;
		cachedTransform.position = desiredPosition;
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}