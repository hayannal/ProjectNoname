using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkipIndicatorCanvas : ObjectIndicatorCanvas
{
	public static TutorialSkipIndicatorCanvas instance;

	public GameObject buttonRootObject;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);
	}

	public void OnClickSkipButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("Tutorial_SkipDesc"), () =>
		{
			// Yes 누를때 바로 이동하려면 이렇게 호출한담에 게이트 필라를 동작시키면 된다.
			StageManager.instance.playStage = StageManager.instance.GetCurrentMaxStage() - 1;
			StageManager.instance.GetNextStageInfo();

			// 재진입 위해서 만들어진 함수긴 한데 전투중에선 이거 호출해도 무방해서 그냥 따로 안만들고 호출하기로 한다.
			GatePillar.instance.EnterInProgressGame();

			// 맨몸으로 보낸다고 또 뭐라 할까봐
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("Atk", false, 0);
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("Atk", false, 0);
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);
		});
	}
}