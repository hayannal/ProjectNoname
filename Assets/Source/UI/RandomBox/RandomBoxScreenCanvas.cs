using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using DG.Tweening;

public class RandomBoxScreenCanvas : MonoBehaviour
{
	public static RandomBoxScreenCanvas instance;

	public GameObject boxPrefab;

	void Awake()
	{
		instance = this;
	}

	bool _isShowGatePillarIndicator;
	void OnEnable()
	{
		// 스택 중에는 알아서 최상위 스택만 얹어도 알아서 중간 캔버스들이 날아갈텐데
		// 로비에서 누를때는 스택이 없으므로 직접 호출해야한다.
		LobbyCanvas.instance.OnEnterMainMenu(true);

		// 게이트필라의 인디케이터가 보이고 있다면 사라지게 하고
		_isShowGatePillarIndicator = GatePillar.instance.IsShowIndicatorCanvas();
		if (_isShowGatePillarIndicator)
			GatePillar.instance.HideIndicatorCanvas(true);

		// 캐시상점에서 열땐 이게 있어야하는데 로비에서 오리진 상자 열땐 이게 없어야한다.
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf && StackCanvas.IsInStack(DotMainMenuCanvas.instance.gameObject))
		{
			// DotMainMenuCanvas는 특이하게도 메뉴 스택중에 닫히지 않는다.
			// 그러기때문에 뽑기 연출을 할때 gameObject는 끄지 않은채 안보이게 하는 방법이 필요하다.
			DotMainMenuCanvas.instance.HideCanvas(true);
			StackCanvas.Push(gameObject);
		}
	}

	void OnDisable()
	{
		LobbyCanvas.instance.OnEnterMainMenu(false);

		// 게이트필라의 인디케이터를 하이드 시켰다면 다시 복구해줘야한다.
		if (_isShowGatePillarIndicator)
			GatePillar.instance.HideIndicatorCanvas(false);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf && StackCanvas.IsInStack(DotMainMenuCanvas.instance.gameObject))
		{
			DotMainMenuCanvas.instance.HideCanvas(false);
			StackCanvas.Pop(gameObject);
		}
	}

	DropProcessor _dropProcessor;
	bool _originBox;
	System.Action _completeAction;
	public void SetInfo(DropProcessor dropProcessor, bool originBox, System.Action completeAction = null)
	{
		_dropProcessor = dropProcessor;
		_originBox = originBox;
		_completeAction = completeAction;

		Timing.RunCoroutine(OpenDropProcess());
	}

	public void OnClickBackground()
	{
		_waitTouch = false;
	}

	bool _waitTouch = false;
	RandomBoxAnimator _randomBoxAnimator;
	IEnumerator<float> OpenDropProcess()
	{
		Vector3 targetPosition = Vector3.zero;
		if (_originBox)
		{
			targetPosition = TreasureChest.instance.transform.position;

			// 오리진 박스일땐 TreasureChest부터 숨기고 떨어뜨려야한다.
		}
		else if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			targetPosition = TimeSpaceGround.instance.cachedTransform.position;

			// 시공간이면 그라운드 영역을 넓혀야한다.
			TimeSpaceGround.instance.objectScaleEffectorDeformer.cachedEffector.TweenDistance(15.0f, 0.8f);
			yield return Timing.WaitForSeconds(0.8f);
		}

		// 플레이어가 상자 떨어질 자리에 너무 가까이에 있다면 아래로 내려준다
		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - targetPosition;
		diff.y = 0.0f;
		if (diff.magnitude < 1.5f)
		{
			Vector3 movePosition = targetPosition - new Vector3(0.0f, 0.0f, 2.0f);
			BattleInstanceManager.instance.playerActor.baseCharacterController.enabled = false;
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.LookRotation(movePosition - BattleInstanceManager.instance.playerActor.cachedTransform.position);
			float moveDistance = Vector3.Distance(BattleInstanceManager.instance.playerActor.cachedTransform.position, movePosition);
			float time = moveDistance / BattleInstanceManager.instance.playerActor.baseCharacterController.speed;
			BattleInstanceManager.instance.playerActor.cachedTransform.DOMove(movePosition, time).SetEase(Ease.Linear);
			BattleInstanceManager.instance.playerActor.actionController.PlayActionByActionName("Move");
			yield return Timing.WaitForSeconds(time);
			BattleInstanceManager.instance.playerActor.actionController.PlayActionByActionName("Idle");
			BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.LookRotation(targetPosition - BattleInstanceManager.instance.playerActor.cachedTransform.position);
			BattleInstanceManager.instance.playerActor.baseCharacterController.enabled = true;
		}

		// 상자를 소환
		if (_randomBoxAnimator == null)
			_randomBoxAnimator = Instantiate<GameObject>(boxPrefab).GetComponent<RandomBoxAnimator>();
		else
			_randomBoxAnimator.gameObject.SetActive(true);
		_randomBoxAnimator.cachedTransform.position = targetPosition;
		yield return Timing.WaitForSeconds(1.5f);

		// 터치 이펙트를 소환

		// 터치를 알리고 기다린다
		_waitTouch = true;
		while (_waitTouch)
			yield return Timing.WaitForOneFrame;

		// 터치가 오면 상자 연출을 보여주고
		_randomBoxAnimator.punchScaleTweenAnimation.DOPause();
		_randomBoxAnimator.openAnimator.enabled = true;
		yield return Timing.WaitForSeconds(0.8f);
		_randomBoxAnimator.gameObject.SetActive(false);

		// 드랍프로세서를 작동
		_dropProcessor.cachedTransform.position = _randomBoxAnimator.cachedTransform.position;
		_dropProcessor.StartDrop();

		// 드랍프로세서는 드랍이 끝나면 알아서 active false로 바뀔테니 그때를 기다린다.
		while (_dropProcessor.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;

		// 회수를 알리려고 했는데 onAfterBattle true로 생성하니 알아서 흡수된다.
		while (DropManager.instance.IsExistAcquirableDropObject())
			yield return Timing.WaitForSeconds(0.1f);

		// 마지막 드랍이 들어오고나서 0.5초 대기
		yield return Timing.WaitForSeconds(0.5f);

		// 나머지 창들을 복구해야한다.
		if (_originBox)
		{
			// 오리진 박스 숨긴거 복구
		}
		else if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			// 시공간이면 Effector 영역 넓힌거 복구
			TimeSpaceGround.instance.objectScaleEffectorDeformer.cachedEffector.ResetTweenDistance();
		}

		// 획득 결과 캔버스를 띄우면 된다.
		// 각자 패킷처리하는 곳에서 할테니 competeAction을 실행시키면 될거다.
		if (_completeAction != null)
			_completeAction();
	}
}