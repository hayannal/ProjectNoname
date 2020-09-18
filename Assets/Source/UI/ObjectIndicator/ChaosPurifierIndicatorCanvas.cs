using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaosPurifierIndicatorCanvas : ObjectIndicatorCanvas
{
	public GameObject buttonRootObject;

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

	public void OnClickPurifyButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("NodeWarBoostInfoCanvas", null);


		//PlayFabApiManager.instance.RequestOpenDailyBox((serverFailure) =>
		{
			if (true)
			{
				// 뭔가 잘못된건데 응답을 할 필요가 있을까.
			}
			else
			{
				// 연출 및 보상 처리.

				// TreasureChest는 숨겨도 하단 일퀘 갱신은 즉시 보여준다.
				//DailyBoxGaugeCanvas.instance.RefreshGauge();

				//UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
				//{
				//	RandomBoxScreenCanvas.instance.SetInfo(useSecond ? RandomBoxScreenCanvas.eBoxType.Origin_Big : RandomBoxScreenCanvas.eBoxType.Origin, dropProcessor, 0, 0, () =>
				//	{
				//		CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetLimitBreakPointInfo(), CharacterBoxConfirmCanvas.OnResult);
				//	});
				//});
			}
		}//);
	}
}