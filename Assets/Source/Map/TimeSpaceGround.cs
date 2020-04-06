using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSpaceGround : MonoBehaviour
{
	public static TimeSpaceGround instance;

	public Transform translationEffectorTransform;
	public EnvironmentSetting environmentSetting;
	public Vector3 teleportPosition;
	public Vector3 returnPosition;
	public ObjectTransformEffectorDeformer objectTransformEffectorDeformer;
	public ObjectScaleEffectorDeformer objectScaleEffectorDeformer;
	public TimeSpaceAltar[] timeSpaceAltarList;

	void Awake()
	{
		instance = this;
	}

	GameObject _prevEnvironmentSettingObject;
	void OnEnable()
	{
		translationEffectorTransform.parent = BattleInstanceManager.instance.playerActor.cachedTransform;
		translationEffectorTransform.localPosition = Vector2.zero;
		translationEffectorTransform.gameObject.SetActive(true);

		// 먼저 이렇게 Disable 처리하고 자신의 환경셋팅을 켜야한다.
		// 이래야 제대로 임시 환경셋팅에 등록된다.
		_prevEnvironmentSettingObject = StageManager.instance.DisableCurrentEnvironmentSetting();
		environmentSetting.gameObject.SetActive(true);

		// teleport
		BattleInstanceManager.instance.playerActor.cachedTransform.position = cachedTransform.position + teleportPosition;
		TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);
		CustomFollowCamera.instance.immediatelyUpdate = true;
	}

	void OnDisable()
	{
		if (StageManager.instance == null)
			return;
		if (translationEffectorTransform == null)
			return;
		if (BattleInstanceManager.instance.playerActor == null || BattleInstanceManager.instance.playerActor.gameObject == null)
			return;

		// 되돌아갈때도 새로 셋팅한 환경값을 끄고나서 예전 환경값을 켜는 형태다.
		environmentSetting.gameObject.SetActive(false);
		_prevEnvironmentSettingObject.SetActive(true);

		translationEffectorTransform.gameObject.SetActive(false);

		TimeSpacePortal.instance.gameObject.SetActive(false);
		TimeSpacePortal.instance.gameObject.SetActive(true);

		BattleInstanceManager.instance.playerActor.cachedTransform.position = returnPosition;
		TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);
		CustomFollowCamera.instance.immediatelyUpdate = true;
	}

	public void EnableObjectDeformer(bool enable)
	{
		objectTransformEffectorDeformer.enabled = enable;
		objectScaleEffectorDeformer.enabled = enable;
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