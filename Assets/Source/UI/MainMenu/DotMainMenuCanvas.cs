using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DotMainMenuCanvas : MonoBehaviour
{
	public static DotMainMenuCanvas instance;

	public Transform targetTransform;
	public Transform scaleAniRootTransform;
	public CanvasGroup canvasGroup;

	public enum eButtonType
	{
		Shop,
		Character,
		Chapter,
		Research,
		Mail,
	}
	public Transform[] mainMenuButtonTransformList;

	int[][] MenuIndexList =
	{
		new int[] { 0 },
		new int[] { 0, 1 },
		new int[] { 0, 1, 4 },
		new int[] { 2, 0, 1, 4 },
		new int[] { 2, 0, 1, 3, 4 },
	};

	float[] StartAngleList =
	{
		180.0f,
		0.0f,
		60.0f,
		0.0f,
		0.0f,
	};

	float[] RadiusList =
	{
		1.5f,
		1.5f,
		1.5f,
		1.75f,
		2.0f,
	};

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		Initialize(targetTransform);
	}

	const float AnimationTime = 0.2f;
	const float RotateAnimationValue = 90.0f;
	const Ease startEaseType = Ease.Linear;
	int _elementCount = 0;
	void Initialize(Transform t)
	{
		if (t == null)
			return;

		_prevTargetTransform = targetTransform = t;

		scaleAniRootTransform.DOComplete(false);
		scaleAniRootTransform.localScale = Vector3.zero;
		scaleAniRootTransform.DOScale(1.0f, AnimationTime).SetEase(startEaseType);
		scaleAniRootTransform.localRotation = Quaternion.Euler(0.0f, -RotateAnimationValue, 0.0f);
		scaleAniRootTransform.DORotate(new Vector3(0.0f, RotateAnimationValue, 0.0f), AnimationTime, RotateMode.LocalAxisAdd).SetEase(startEaseType);
		canvasGroup.alpha = 0.0f;
		canvasGroup.DOFade(1.0f, AnimationTime).SetEase(Ease.InQuad);

		// initialize menu content
		_elementCount = 3;
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Research))
			_elementCount = 5;
		else if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chapter))
			_elementCount = 4;

		// initialize flag
		_targetPrevPosition = targetTransform.position;
		_adjustTargetDirection = Vector3.zero;
		_immediatelyUpdate = true;

		// initialize listTransform
		if (_listElementTransform.Count == _elementCount)
			return;

		for (int i = 0; i < _listElementTransform.Count; ++i)
			_listElementTransform[i].gameObject.SetActive(false);
		_listElementTransform.Clear();

		int[] indexList = MenuIndexList[_elementCount - 1];
		for (int i = 0; i < indexList.Length; ++i)
		{
			Transform buttonTransform = mainMenuButtonTransformList[indexList[i]];
			buttonTransform.gameObject.SetActive(true);
			_listElementTransform.Add(buttonTransform);
		}

		_listElementAngle.Clear();
		_listTargetPosition.Clear();
		for (int i = 0; i < _elementCount; ++i)
		{
			int angle = 360 / _elementCount;
			_listElementAngle.Add(i * angle + StartAngleList[_elementCount - 1]);
			_listTargetPosition.Add(Vector3.zero);
		}
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject, true, OnPushStack, OnPopStack);
		EnvironmentSetting.SetGlobalLightIntensityRatio(0.3f, 0.0f);
		Initialize(targetTransform);
	}

	void OnDisable()
	{
		EnvironmentSetting.ResetGlobalLightIntensityRatio();
		_reservedHide = false;
		StackCanvas.Pop(gameObject);
	}

	void OnPushStack()
	{
		EnvironmentSetting.SetGlobalLightIntensityRatio(0.3f, 0.0f, 100.0f);
	}

	void OnPopStack()
	{
		EnvironmentSetting.ResetGlobalLightIntensityRatio(100.0f);
	}

	#region Gacha
	// 원래는 없었다가 가차 하면서 생긴 함수. 임시로 캔버스를 하이드 시키는 기능이다.
	// 사실 하이드로 하려고 했는데 메시오브젝트까지 엮여있어서 차라리 스케일을 줄이는거로 해본다.
	public void HideCanvas(bool hide)
	{
		cachedTransform.localScale = hide ? Vector3.zero : Vector3.one;
	}
	#endregion

	// Update is called once per frame
	Transform _prevTargetTransform;
	void Update()
    {
		// 예외상황이 하나 생겼는데 가차 뽑을때 하이드 시켜놓고 걸어가게 된다.
		// 이때만큼은 플레이어를 따라가야하므로 하이드상태일때는 리턴하지 않게 한다.
		if (cachedTransform.localScale.x != 0.0f && StackCanvas.IsInStack(gameObject))
			return;

		if (_prevTargetTransform != targetTransform && targetTransform != null)
		{
			Initialize(targetTransform);
		}

		UpdateRootPosition();
		UpdateElementRotation();
		UpdateElementPosition();
	}

	// 메인캐릭터 교체할땐 DotMainMenu가 열려있는채로 해야해서 Prev까지 덮어야했다. 이렇게 해야 열리는 애니가 나오지 않으면서 새 캐릭터에 적용된다.
	public void ForceSetTargetTransform(Transform newTransform)
	{
		_prevTargetTransform = targetTransform = newTransform;
	}

	Vector3 _targetPrevPosition;
	Vector3 _adjustTargetDirection;
	Vector3 _lastAdjustTargetDirection = Vector3.up;
	List<float> _listElementAngle = new List<float>();
	List<Vector3> _listTargetPosition = new List<Vector3>();
	// 이동방향의 반대편으로 얼마나 영향받을지 비율값 설정
	float _adjustDirectionRatio = 0.2f;
	void UpdateRootPosition()
	{
		if (targetTransform == null)
			return;

		if (_targetPrevPosition != targetTransform.position)
		{
			_adjustTargetDirection = -targetTransform.forward;
			_targetPrevPosition = targetTransform.position;
		}
		else
		{
			_adjustTargetDirection = Vector3.zero;
		}

		// 움직임의 방향에 따라 타겟 위치를 새로 계산한다.
		if (_lastAdjustTargetDirection != _adjustTargetDirection || _immediatelyUpdate)
		{
			for (int i = 0; i < _listTargetPosition.Count; ++i)
			{
				Vector3 result = Quaternion.Euler(0.0f, _listElementAngle[i], 0.0f) * Vector3.forward;
				_listTargetPosition[i] = result;

				if (_adjustTargetDirection != Vector3.zero)
					_listTargetPosition[i] = Vector3.Slerp(_listTargetPosition[i], _adjustTargetDirection, _adjustDirectionRatio);

				_listTargetPosition[i] *= RadiusList[_elementCount - 1];
			}
			_lastAdjustTargetDirection = _adjustTargetDirection;
		}

		// 루트 포지션 자체는 lerp를 사용하지 않는다.
		cachedTransform.position = targetTransform.position;
	}

	List<Transform> _listElementTransform = new List<Transform>();
	void UpdateElementRotation()
	{
		for (int i = 0; i < _listElementTransform.Count; ++i)
			_listElementTransform[i].rotation = Quaternion.identity;
	}

	float _movingSlerpSpeed = 0.8f;
	float _slerpSpeed = 1.2f;
	bool _immediatelyUpdate;
	void UpdateElementPosition()
	{
		if (_immediatelyUpdate)
		{
			for (int i = 0; i < _listElementTransform.Count; ++i)
				_listElementTransform[i].localPosition = _listTargetPosition[i];
			_immediatelyUpdate = false;
			return;
		}

		if (scaleAniRootTransform.localScale.x < 1.0f)
			return;

		for (int i = 0; i < _listElementTransform.Count; ++i)
			_listElementTransform[i].localPosition = Vector3.Slerp(_listElementTransform[i].localPosition, _listTargetPosition[i], Time.deltaTime * (_adjustTargetDirection == Vector3.zero ? _slerpSpeed : _movingSlerpSpeed));
	}

	bool _reservedHide = false;
	public void ToggleShow()
	{
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
			return;
		}

		if (_reservedHide)
		{
			// 하이드 애니 나오는 도중에 다시 켜는 처리.
			//scaleAniRootTransform.DOComplete(false);
			float showRemainTime = AnimationTime * (1.0f - scaleAniRootTransform.localScale.x);
			scaleAniRootTransform.DOScale(1.0f, showRemainTime).SetEase(startEaseType);
			scaleAniRootTransform.DORotate(new Vector3(0.0f, RotateAnimationValue * (1.0f - scaleAniRootTransform.localScale.x), 0.0f), showRemainTime, RotateMode.LocalAxisAdd).SetEase(startEaseType);
			canvasGroup.DOFade(1.0f, showRemainTime).SetEase(Ease.InQuad);
			_reservedHide = false;
			return;
		}

		// complete을 호출하면 마지막 지점으로 가니 일부러 하지 않는다.
		//scaleAniRootTransform.DOComplete(false);
		float remainTime = AnimationTime * scaleAniRootTransform.localScale.x;
		scaleAniRootTransform.DOScale(0.0f, remainTime).SetEase(Ease.OutQuad).OnComplete(OnCompleteScaleZero);
		scaleAniRootTransform.DORotate(new Vector3(0.0f, -RotateAnimationValue * scaleAniRootTransform.localScale.x, 0.0f), remainTime, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);
		canvasGroup.DOFade(0.0f, remainTime).SetEase(Ease.InQuad);
		_reservedHide = true;
	}

	void OnCompleteScaleZero()
	{
		if (_reservedHide)
			gameObject.SetActive(false);
	}

	#region Button Event
	public void OnClickBackButton()
	{
		if (_reservedHide == false)
			ToggleShow();
	}

	public void OnClickShopButton()
	{
		if (_reservedHide)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("CashShopCanvas", null);
	}

	public void OnClickCharacterButton()
	{
		// 닫히는 도중에 메뉴를 열어버리면 새 메뉴가 열리고나서 OnCompleteScaleZero가 호출되면서 StackCanvas가 꼬이게 된다.
		// (최상단 스택을 닫지 않고 사이에 있는걸 닫아버렸기 때문)
		// 그렇다고 닫히는 도중에 메뉴 열때 닫히던 DotMainMenu를 그대로 멈춰두기도 애매하니 선택권은 두개다.
		// 닫히는 애니를 취소하고 도로 열어두면서 메뉴를 열던가
		// 아니면 닫히는건 그대로 두되 메뉴를 열지 말던가. 선택은 후자다.
		if (_reservedHide)
			return;

		// DotMainMenu는 열려있는채로 냅두고 다른 창을 연다.
		// 대신 화면 어둡게 한건 풀어야한다. - 이건 Stack구조에서 알아서 호출해준다.
		//gameObject.SetActive(false);
		UIInstanceManager.instance.ShowCanvasAsync("CharacterListCanvas", null);
	}

	public void OnClickChapterButton()
	{
		if (_reservedHide)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("ChapterCanvas", null);
	}

	public void OnClickResearchButton()
	{
		if (_reservedHide)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("ResearchCanvas", null);
	}

	public void OnClickMailButton()
	{
		if (_reservedHide)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("MailCanvas", null);
	}
	#endregion




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
