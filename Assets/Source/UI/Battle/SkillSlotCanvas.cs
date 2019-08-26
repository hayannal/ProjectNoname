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

	CustomItemContainer _container = new CustomItemContainer();

	public void InitializeSkillSlot(PlayerActor playerActor)
	{
		ActionController.ActionInfo actionInfo = playerActor.actionController.GetActionInfoByControllerType(Control.eControllerType.UltimateSkillSlot);
		if (actionInfo != null)
		{
			SkillSlotIcon skillSlotIcon = _container.GetCachedItem(skillSlotIconPrefab, ultimateSkillSlotTransform);
			skillSlotIcon.Initialize(playerActor, actionInfo);
		}
	}
}

public class CustomItemContainer : CachedItemHave<SkillSlotIcon>
{
}