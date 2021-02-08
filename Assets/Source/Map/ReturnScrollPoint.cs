using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnScrollPoint : MonoBehaviour
{
	public static ReturnScrollPoint instance;

	public GameObject onObject;
	public GameObject offObject;

	// 경우의 수는 4가지다.
	// 1. 구매하지 않은 경우 - 기본적으로는 아무것도 뜨지 않으나 가까이 다가가면 세이브 버튼형 인디케이터가 뜬다. 누르면 확인창이 뜨는데 Save를 못하게 되어있다.
	// 2. 구매한 경우 - 세이브 라는 버튼형 인디케이터가 뜬다. 누르면 확인창이 뜨고 Save할 수 있다.
	// 3. 구매한채로 다음번 파워소스에 도달하는 경우 - 텍스트형 인디케이터를 띄워서 세이브 포인트가 갱신됨을 알린다.
	// 4. 그러다가 죽어서 부활한 경우 - 회색의 offObject를 보여주면서 가까이 다가가면 토스트로 이미 부활했음을 알려준다.
	enum eReturnScrollState
	{
		NotPurchase = 0,
		Standby = 1,
		Saved = 2,
		Used = 3,
	}
	eReturnScrollState _currentType = eReturnScrollState.NotPurchase;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		//if (ClientSaveData.instance.IsLoadingInProgressGame())
		//{
		//}
		//else
		//{
		//}

		if (StageManager.instance.returnScrollUsed)
		{
			onObject.SetActive(false);
			offObject.SetActive(true);
			_currentType = eReturnScrollState.Used;
		}
		else
		{
			onObject.SetActive(true);
			offObject.SetActive(false);

			if (CurrencyData.instance.returnScroll > 0)
			{
				if (StageManager.instance.IsSavedReturnScrollPoint())
				{
					_currentType = eReturnScrollState.Saved;
				}
				else
				{
					_currentType = eReturnScrollState.Standby;
				}
			}
			else
			{
				// 무과금 유저 아무것도 뜨지 않는다.
				_currentType = eReturnScrollState.NotPurchase;
			}
		}

		switch (_currentType)
		{
			case eReturnScrollState.Standby:
				_indicatorShowRemainTime = 0.2f;
				break;
			case eReturnScrollState.Saved:
				StageManager.instance.SaveReturnScrollPoint();
				SetTextIndicatorShowRemainTime(0.2f);
				break;
		}
	}

	public void SetTextIndicatorShowRemainTime(float remainTime)
	{
		_textIndicatorShowRemainTime = remainTime;
	}

	void OnDisable()
	{
		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
		_spawnedIndicator = false;
	}

	bool _spawnedIndicator;
	void OnTriggerEnter(Collider other)
	{
		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		if (_currentType == eReturnScrollState.Used)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_LoadOnlyOne"), 2.0f);
			return;
		}
		else if (_currentType == eReturnScrollState.NotPurchase)
		{
			if (_spawnedIndicator)
				return;

			_indicatorShowRemainTime = 0.1f;
		}
	}

	float _indicatorShowRemainTime;
	float _textIndicatorShowRemainTime;
	void Update()
	{
		if (_indicatorShowRemainTime > 0.0f)
		{
			_indicatorShowRemainTime -= Time.deltaTime;
			if (_indicatorShowRemainTime <= 0.0f)
			{
				_indicatorShowRemainTime = 0.0f;
				ShowIndicator();
			}
		}

		if (_textIndicatorShowRemainTime > 0.0f)
		{
			_textIndicatorShowRemainTime -= Time.deltaTime;
			if (_textIndicatorShowRemainTime <= 0.0f)
			{
				_textIndicatorShowRemainTime = 0.0f;
				ShowIndicator(true);
			}
		}
	}

	ObjectIndicatorCanvas _objectIndicatorCanvas;
	public void ShowIndicator(bool textIndicator = false)
	{
		AddressableAssetLoadManager.GetAddressableGameObject(textIndicator ? "ReturnScrollPointTextIndicator" : "ReturnScrollPointIndicator", "Canvas", (prefab) =>
		{
			// 로딩하는 중간에 맵이동시 다음맵으로 넘어가서 인디케이터가 뜨는걸 방지.
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeSelf == false) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
		});

		_spawnedIndicator = true;
	}
}