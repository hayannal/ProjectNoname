using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PlayerGaugeCanvas : MonoBehaviour
{
	public static PlayerGaugeCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.playerHPGaugePrefab).GetComponent<PlayerGaugeCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static PlayerGaugeCanvas _instance = null;

	public CanvasGroup canvasGroup;
	public GameObject offsetRootObject;
	public RectTransform widthRectTransform;
	public MOBAEnergyBar mobaEnergyBar;
	public Text levelText;

	float _defaultWidth;
	void Awake()
	{
		_defaultWidth = widthRectTransform.sizeDelta.x;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		canvasGroup.alpha = DEFAULT_CANVAS_GROUP_ALPHA;
	}

	void OnEnable()
	{
		_lateFillDelayRemainTime = 0.0f;
		_lateFillLerpStarted = false;
		_prevTargetPosition = -Vector3.up;
	}

	public void InitializeGauge(PlayerActor playerActor)
	{
		_offsetY = playerActor.gaugeOffsetY;
		//widthRectTransform.sizeDelta = new Vector2(monsterActor.monsterHpGaugeWidth * _defaultWidth, widthRectTransform.sizeDelta.y);
		mobaEnergyBar.MaxValue = playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHP);
		mobaEnergyBar.Value = playerActor.actorStatus.GetHP();
		_lastRatio = playerActor.actorStatus.GetHPRatio();
		_targetTransform = playerActor.cachedTransform;
		GetTargetHeight(_targetTransform);
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
				//UpdateGaugeRotation();
				_prevTargetPosition = _targetTransform.position;
			}
		}

		UpdateLateFill();
		UpdateAlpha();
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

	void UpdateGaugeRotation()
	{
		float rotateY = cachedTransform.position.x * 2.0f;
		cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);
	}

	float _lastRatio = 1.0f;
	public void OnChangedHP(PlayerActor playerActor)
	{
		mobaEnergyBar.MaxValue = playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHP);
		mobaEnergyBar.Value = playerActor.actorStatus.GetHP();
		float hpRatio = playerActor.actorStatus.GetHPRatio();

		if (_lastRatio < hpRatio)
		{
		}
		else
		{
			if (_lateFillLerpStarted == false && _lateFillDelayRemainTime == 0.0f)
				_lateFillDelayRemainTime = LateFillDelay;
		}

		_lastRatio = hpRatio;
	}

	const float LateFillDelay = 0.9f;
	float _lateFillDelayRemainTime = 0.0f;
	bool _lateFillLerpStarted = false;
	void UpdateLateFill()
	{
		if (_lateFillDelayRemainTime > 0.0f)
		{
			_lateFillDelayRemainTime -= Time.deltaTime;
			if (_lateFillDelayRemainTime <= 0.0f)
			{
				_lateFillDelayRemainTime = 0.0f;
				_lateFillLerpStarted = true;
			}
		}

		if (_lateFillLerpStarted == false)
			return;

		if (mobaEnergyBar.UpdateLerpDamage())
			_lateFillLerpStarted = false;
	}

	const float DEFAULT_CANVAS_GROUP_ALPHA = 0.78125f;
	float ALPHA_DELAY_TIME = 5.0f;
	float _alphaRemainTime = 0.0f;
	float ALPHA_FADE_TIME = 0.5f;
	float _alphaFadeRemainTime = 0.0f;
	void UpdateAlpha()
	{
		if (_lastRatio < 1.0f)
		{
			if (!offsetRootObject.activeSelf)
				offsetRootObject.SetActive(true);
			canvasGroup.alpha = DEFAULT_CANVAS_GROUP_ALPHA;
			_alphaRemainTime = 0.0f;
			return;
		}

		if (_alphaRemainTime == 0.0f && canvasGroup.alpha == DEFAULT_CANVAS_GROUP_ALPHA)
			_alphaRemainTime = ALPHA_DELAY_TIME;

		if (_alphaRemainTime > 0.0f)
		{
			_alphaRemainTime -= Time.deltaTime;
			if (_alphaRemainTime <= 0.0f)
			{
				_alphaRemainTime = 0.0f;
				_alphaFadeRemainTime = ALPHA_FADE_TIME;
			}
		}

		if (_alphaFadeRemainTime > 0.0f)
		{
			_alphaFadeRemainTime -= Time.deltaTime;
			if (_alphaFadeRemainTime <= 0.0f)
			{
				offsetRootObject.SetActive(false);
				_alphaFadeRemainTime = 0.0f;
			}
			canvasGroup.alpha = _alphaFadeRemainTime * (1.0f / ALPHA_FADE_TIME) * DEFAULT_CANVAS_GROUP_ALPHA;
		}
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
