﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ExperienceCanvas : MonoBehaviour
{
	public static ExperienceCanvas instance;

	public GameObject inputLockObject;
	public RectTransform backButtonRectTransform;
	public RectTransform backButtonHideRectTransform;
	public Button backButton;

	public float positionTweenTime = 1.8f;
	public float rotationTweenTime = 0.5f;
	public float fovTweenTime = 1.7f;
	public Ease positionEase = Ease.OutQuad;
	public Ease rotationEase = Ease.OutQuad;
	public Ease fovEase = Ease.OutQuad;

	public GameObject experienceGroundPrefab;

	Vector2 _defaultBackButtonPosition;
	void Awake()
	{
		instance = this;
		_defaultBackButtonPosition = backButtonRectTransform.anchoredPosition;
	}

	void Start()
	{
		Instantiate<GameObject>(experienceGroundPrefab, CharacterInfoGround.instance.transform);
	}

	void OnEnable()
	{
		ChangeExperienceMode();
		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		if (StageManager.instance == null)
			return;

		ResetExperienceMode();
		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		if (inputLockObject.activeSelf)
			return;

		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	void ChangeExperienceMode()
	{
		inputLockObject.SetActive(true);

		_targetFov = CharacterListCanvas.instance.GetLastFov();
		_targetPosition = CharacterListCanvas.instance.rootOffsetPosition + CharacterListCanvas.instance.GetLastCameraOffset();
		_targetRotation = CharacterListCanvas.instance.GetLastCameraRotation();
		_tweenRemainTime = Mathf.Max(Mathf.Max(positionTweenTime, rotationTweenTime), fovTweenTime);

		CustomFollowCamera.instance.cachedTransform.DOMove(_targetPosition, positionTweenTime).SetEase(positionEase);
		CustomFollowCamera.instance.cachedTransform.DORotateQuaternion(_targetRotation, rotationTweenTime).SetEase(rotationEase);
		DOTween.To(() => UIInstanceManager.instance.GetCachedCameraMain().fieldOfView, x => UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = x, _targetFov, fovTweenTime).SetEase(fovEase);

		backButtonRectTransform.anchoredPosition = backButtonHideRectTransform.anchoredPosition;

		CharacterListCanvas.instance.ChangeExperience();
		if (ExperienceGround.instance != null)
			ExperienceGround.instance.gameObject.SetActive(true);

		CallAffectorValueAffector.OnEvent(CharacterListCanvas.instance.selectedPlayerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);

		SoundManager.instance.PlayBattleBgm(CharacterListCanvas.instance.selectedPlayerActor.actorId);
	}

	void ResetExperienceMode()
	{
		// reset sp
		PlayerActor playerActor = CharacterListCanvas.instance.selectedPlayerActor;
		if (playerActor != null)
			playerActor.actorStatus.AddSP(-playerActor.actorStatus.GetSP());

		BattleInstanceManager.instance.FinalizeAllSummonObject();
		CharacterListCanvas.instance.ResetExperience();
		if (ExperienceGround.instance != null)
			ExperienceGround.instance.gameObject.SetActive(false);
		SkillSlotCanvas.instance.HideSkillSlot();

		SoundManager.instance.PlayLobbyBgm();

		// 메릴 체험모드 끝나고 즉시 복구시키기 위해 호출
		Time.timeScale = 1.0f;

		// 특수한 어펙터 체크해서 삭제
		if (ChangeAttackStateAffector.CheckBulletRemovable(CharacterListCanvas.instance.selectedPlayerActor.affectorProcessor))
		{
			ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)CharacterListCanvas.instance.selectedPlayerActor.affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
			changeAttackStateAffector.finalized = true;
		}
	}



	float _targetFov;
	Vector3 _targetPosition;
	Quaternion _targetRotation;
	float _tweenRemainTime = 0.0f;
	void Update()
	{
		if (_tweenRemainTime > 0.0f)
		{
			// Tween으로 바꾸기 전에 사용하던 코드
			//CustomFollowCamera.instance.cachedTransform.position = Vector3.Lerp(CustomFollowCamera.instance.cachedTransform.position, _targetPosition, Time.deltaTime * 1.0f);
			//CustomFollowCamera.instance.cachedTransform.rotation = Quaternion.Slerp(CustomFollowCamera.instance.cachedTransform.rotation, _targetRotation, Time.deltaTime * 5.0f);
			//UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = Mathf.Lerp(UIInstanceManager.instance.GetCachedCameraMain().fieldOfView, _targetFov, Time.deltaTime * 2.0f);

			_tweenRemainTime -= Time.deltaTime;
			if (_tweenRemainTime <= 0.0f)
			{
				_tweenRemainTime = 0.0f;
				//CustomFollowCamera.instance.cachedTransform.position = _targetPosition;
				//CustomFollowCamera.instance.cachedTransform.rotation = _targetRotation;
				//UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _targetFov;

				backButtonRectTransform.DOAnchorPos(_defaultBackButtonPosition, 1.0f);

				SkillSlotCanvas.instance.InitializeSkillSlot(CharacterListCanvas.instance.selectedPlayerActor);
				CameraFovController.instance.enabled = true;
				CustomFollowCamera.instance.enabled = true;
				inputLockObject.SetActive(false);
			}
		}
	}
}