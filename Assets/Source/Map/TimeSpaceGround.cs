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

	void Awake()
	{
		instance = this;
	}
	
	void OnEnable()
	{
		translationEffectorTransform.parent = BattleInstanceManager.instance.playerActor.cachedTransform;
		translationEffectorTransform.localPosition = Vector2.zero;
		translationEffectorTransform.gameObject.SetActive(true);

		StageManager.instance.EnableEnvironmentSettingForUI(false);
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

		environmentSetting.gameObject.SetActive(false);
		StageManager.instance.EnableEnvironmentSettingForUI(true);

		translationEffectorTransform.gameObject.SetActive(false);

		TimeSpacePortal.instance.gameObject.SetActive(false);
		TimeSpacePortal.instance.gameObject.SetActive(true);

		BattleInstanceManager.instance.playerActor.cachedTransform.position = returnPosition;
		TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);
		CustomFollowCamera.instance.immediatelyUpdate = true;
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