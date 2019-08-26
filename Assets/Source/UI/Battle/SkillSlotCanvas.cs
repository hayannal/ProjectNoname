using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotCanvas : MonoBehaviour
{
	public static SkillSlotCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.skillSlotCanvasPrefab).GetComponent<SkillSlotCanvas>();
			}
			return _instance;
		}
	}
	static SkillSlotCanvas _instance = null;

	public GameObject skillSlotIconPrefab;
	//public GameObject castingControllerPrefab;
	public Transform ultimateSkillSlotTransform;
	public Sprite[] powerSourceIconSpriteList;

	CustomItemContainer _container = new CustomItemContainer();

	SkillSlotIcon _ultimateSkillSlotIcon;
	public void InitializeSkillSlot(PlayerActor playerActor)
	{
		if (_ultimateSkillSlotIcon != null)
			_ultimateSkillSlotIcon.gameObject.SetActive(false);

		ActionController.ActionInfo actionInfo = playerActor.actionController.GetActionInfoByName("Ultimate");
		if (actionInfo != null)
		{
			_ultimateSkillSlotIcon = _container.GetCachedItem(skillSlotIconPrefab, ultimateSkillSlotTransform);
			_ultimateSkillSlotIcon.Initialize(playerActor, actionInfo);
		}
	}

	public void OnChangedSP(PlayerActor playerActor)
	{
		if (_ultimateSkillSlotIcon != null)
			_ultimateSkillSlotIcon.OnChangedSP(playerActor);
	}
}

public class CustomItemContainer : CachedItemHave<SkillSlotIcon>
{
}