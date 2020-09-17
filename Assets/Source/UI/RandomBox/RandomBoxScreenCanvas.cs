﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using DG.Tweening;

public class RandomBoxScreenCanvas : MonoBehaviour
{
	public static RandomBoxScreenCanvas instance;

	public enum eBoxType
	{
		Origin,
		Character,
		Equip1,
		Equip8,
		Dia1_3,
		Dia4_6,
		Gold,
		Origin_Big,
		NodeWar,
	}

	public GameObject[] boxPrefabList;
	public GameObject boxOpenEffectPrefab;
	public GameObject repeatButtonGroupObject;

	void Awake()
	{
		instance = this;
	}

	bool _isShowGatePillarIndicator;
	bool _isShowTreasureChestIndicator;
	void OnEnable()
	{
		repeatButtonGroupObject.SetActive(false);

		// 스택 중에는 알아서 최상위 스택만 얹어도 알아서 중간 캔버스들이 날아갈텐데
		// 로비에서 누를때는 스택이 없으므로 직접 호출해야한다.
		LobbyCanvas.instance.OnEnterMainMenu(true);

		// 게이트필라의 인디케이터가 보이고 있다면 사라지게 하고
		_isShowGatePillarIndicator = GatePillar.instance.IsShowIndicatorCanvas();
		if (_isShowGatePillarIndicator)
			GatePillar.instance.HideIndicatorCanvas(true);

		_isShowTreasureChestIndicator = TreasureChest.instance.IsShowIndicatorCanvas();
		if (_isShowTreasureChestIndicator)
			TreasureChest.instance.HideIndicatorCanvas(true);

		// 캐시상점에서 열땐 이게 있어야하는데 로비에서 오리진 상자 열땐 이게 없어야한다.
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf && StackCanvas.IsInStack(DotMainMenuCanvas.instance.gameObject))
		{
			// DotMainMenuCanvas는 특이하게도 메뉴 스택중에 닫히지 않는다.
			// 그러기때문에 뽑기 연출을 할때 gameObject는 끄지 않은채 안보이게 하는 방법이 필요하다.
			DotMainMenuCanvas.instance.HideCanvas(true);
			StackCanvas.Push(gameObject);
		}
		else if (CashShopCanvas.instance != null && CashShopCanvas.instance.gameObject.activeSelf)
		{
			// 로비에서 TreasureChest Indicator 통해서 상점을 열었을때는 DotMainMenuCanvas 변경없이 StackCanvas 처리를 한다.
			StackCanvas.Push(gameObject);
		}
	}

	void OnDisable()
	{
		LobbyCanvas.instance.OnEnterMainMenu(false);

		// 게이트필라의 인디케이터를 하이드 시켰다면 다시 복구해줘야한다.
		if (_isShowGatePillarIndicator)
			GatePillar.instance.HideIndicatorCanvas(false);

		if (_isShowTreasureChestIndicator)
			TreasureChest.instance.HideIndicatorCanvas(false);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf && StackCanvas.IsInStack(DotMainMenuCanvas.instance.gameObject))
		{
			DotMainMenuCanvas.instance.HideCanvas(false);
			StackCanvas.Pop(gameObject);
		}
		else if (CashShopCanvas.instance != null && CashShopCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(CashShopCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
	}

	eBoxType _boxType;
	DropProcessor _dropProcessor;
	int _repeatRemainCount;
	int _repeatPrice;
	System.Action _completeAction;
	
	public void SetInfo(eBoxType boxType, DropProcessor dropProcessor, int repeatRemainCount, int repeatPrice, System.Action completeAction = null)
	{
		_dropProcessor = dropProcessor;
		_boxType = boxType;
		_repeatRemainCount = repeatRemainCount;
		_repeatPrice = repeatPrice;
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
		if (_boxType == eBoxType.Origin || _boxType == eBoxType.Origin_Big)
			targetPosition = TreasureChest.instance.transform.position;
		else if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			targetPosition = TimeSpaceGround.instance.cachedTransform.position;
		else if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			targetPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(0.0f, 0.0f, 2.0f);

		// 플레이어가 상자 떨어질 자리에 너무 가까이에 있다면 아래로 내려준다
		bool needMove = false;
		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - targetPosition;
		diff.y = 0.0f;
		if (diff.magnitude < 1.5f)
			needMove = true;
		else
		{
			// 이번엔 반대로 movePosition 과의 거리가 얼마나 먼지를 체크
			Vector3 movePosition = targetPosition - new Vector3(0.0f, 0.0f, 2.0f);
			diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - movePosition;
			diff.y = 0.0f;
			if (diff.magnitude > 3.5f)
			{
				// 너무 멀다고 판단되면 직선거리로 거리를 좁혀준 후 나머지를 이동시킨다.
				BattleInstanceManager.instance.playerActor.cachedTransform.position = movePosition + diff.normalized * 2.5f;
				TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 5);
				CustomFollowCamera.instance.immediatelyUpdate = true;
				needMove = true;
			}
			else if (diff.magnitude > 1.0f)
				needMove = true;
		}
		if (needMove)
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
			BattleInstanceManager.instance.playerActor.baseCharacterController.enabled = true;
		}
		// 이동하지 않아도 항상 쳐다보게는 하기
		BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.LookRotation(targetPosition - BattleInstanceManager.instance.playerActor.cachedTransform.position);

		if (_boxType == eBoxType.Origin || _boxType == eBoxType.Origin_Big)
		{
			// 오리진 박스일땐 TreasureChest부터 숨기고 떨어뜨려야한다.
			TreasureChest.instance.ActivateEffect(false);
			yield return Timing.WaitForSeconds(0.6f);
		}
		else if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			// 시공간이면 그라운드 영역을 넓혀야한다.
			TimeSpaceGround.instance.objectScaleEffectorDeformer.cachedEffector.TweenDistance(15.0f, 0.8f);
			yield return Timing.WaitForSeconds(0.2f);
		}

		// 상자를 소환
		_randomBoxAnimator = BattleInstanceManager.instance.GetCachedRandomBoxAnimator(boxPrefabList[(int)_boxType], targetPosition, Quaternion.identity);
		yield return Timing.WaitForSeconds(0.8f);

		// 터치 이펙트를 소환
		_randomBoxAnimator.touchCanvasObject.SetActive(true);

		// 터치를 알리고 기다린다
		_waitTouch = true;
		while (_waitTouch)
			yield return Timing.WaitForOneFrame;

		// 첫번째 터치가 입력되는 동시에 남은 횟수를 판단해서 반복횟수가 남았다면 캔슬버튼을 보여준다.
		if (_repeatRemainCount > 0)
			repeatButtonGroupObject.SetActive(true);

		// 터치가 오면 상자 연출을 보여주고
		_randomBoxAnimator.touchCanvasObject.SetActive(false);
		_randomBoxAnimator.punchScaleTweenAnimation.DOPause();
		_randomBoxAnimator.openAnimator.enabled = true;
		_randomBoxAnimator.disableObjectComponent.enabled = true;
		BattleInstanceManager.instance.GetCachedObject(boxOpenEffectPrefab, _randomBoxAnimator.cachedTransform.position, Quaternion.identity);

		// 박스 애니가 둘로 나눠져있는 상자들 때문에 예외처리 한다.
		if (_randomBoxAnimator.internalOpenAnimator != null)
		{
			yield return Timing.WaitForSeconds(_randomBoxAnimator.internalOpenDelay);
			_randomBoxAnimator.internalOpenAnimator.enabled = true;
			yield return Timing.WaitForSeconds(0.8f - _randomBoxAnimator.internalOpenDelay);
		}
		else
			yield return Timing.WaitForSeconds(0.8f);

		// 거의 그럴일은 없겠지만 DropObjectGroup이 로딩되어있지 않다면 기다린다.
		while (MainSceneBuilder.instance.IsDoneLateInitialized(true) == false)
			yield return Timing.WaitForOneFrame;

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

		// 반복횟수가 설정되어있는 상태라면 첫번째 뽑기 이후부터 자동으로 해줘야한다.
		if (_repeatRemainCount > 0)
		{
			// 최초로 반복을 시작
			InitializeRepeat();
			yield break;
		}

		ResetObject();

		// 섬멸전 보상이라면 타임스케일 0으로 만들거라서 조금만 더 기다리기로 한다.
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			yield return Timing.WaitForSeconds(0.8f);

		// 획득 결과 캔버스를 띄우면 된다.
		// 각자 패킷처리하는 곳에서 할테니 completeAction을 실행시키면 될거다.
		if (_completeAction != null)
			_completeAction();
	}

	void ResetObject()
	{
		// 나머지 창들을 복구해야한다.
		if (_boxType == eBoxType.Origin || _boxType == eBoxType.Origin_Big)
		{
			// 오리진 박스 숨긴거 복구
			TreasureChest.instance.ActivateEffect(true);
		}
		else if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			// 시공간이면 Effector 영역 넓힌거 복구
			TimeSpaceGround.instance.objectScaleEffectorDeformer.cachedEffector.ResetTweenDistance();
		}
	}

	#region Repeat CharacterBox
	void InitializeRepeat()
	{
		// 가장 먼저 첫번째로 굴려진 정보를 누적시킨 후
		ClearSumInfo();
		SumReward();

		// 누적이 끝나면 해당 드랍프로세서를 초기화 해야한다. 이래야 이전 드랍정보랑 섞이지 않게 된다.
		// 보통 다른 경우엔 결과창 끝나고 초기화하거나 할텐데 여긴 반복 뽑기라서 이렇게 예외처리하는거다.
		DropManager.instance.ClearLobbyDropInfo();

		// 반복을 시작
		RequestRepeat();
	}

	List<DropManager.CharacterPpRequest> _listSumPpInfo = null;
	List<string> _listSumGrantInfo = null;
	List<DropManager.CharacterLbpRequest> _listSumLbpInfo = null;
	void SumReward()
	{
		// 오리진은 반복이 불가능하므로 골드와 다이아는 제외한채 캐릭터 정보 3개만 누적하도록 하겠다.
		if (_listSumPpInfo == null)
			_listSumPpInfo = new List<DropManager.CharacterPpRequest>();
		if (_listSumGrantInfo == null)
			_listSumGrantInfo = new List<string>();
		if (_listSumLbpInfo == null)
			_listSumLbpInfo = new List<DropManager.CharacterLbpRequest>();

		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterLbpRequest> listLbpInfo = DropManager.instance.GetLimitBreakPointInfo();
		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			bool find = false;
			for (int j = 0; j < _listSumPpInfo.Count; ++j)
			{
				if (_listSumPpInfo[j].actorId == listPpInfo[i].actorId)
				{
					find = true;
					_listSumPpInfo[j].pp = listPpInfo[i].pp;
					_listSumPpInfo[j].add += listPpInfo[i].add;
					break;
				}
			}

			if (!find)
			{
				DropManager.CharacterPpRequest newInfo = new DropManager.CharacterPpRequest();
				newInfo.actorId = listPpInfo[i].actorId;
				newInfo.ChrId = listPpInfo[i].ChrId;
				newInfo.pp = listPpInfo[i].pp;
				newInfo.add = listPpInfo[i].add;
				_listSumPpInfo.Add(newInfo);
			}
		}
		for (int i = 0; i < listGrantInfo.Count; ++i)
		{
			if (_listSumGrantInfo.Contains(listGrantInfo[i]) == false)
				_listSumGrantInfo.Add(listGrantInfo[i]);
		}
		for (int i = 0; i < listLbpInfo.Count; ++i)
		{
			DropManager.CharacterLbpRequest newInfo = new DropManager.CharacterLbpRequest();
			newInfo.actorId = listLbpInfo[i].actorId;
			newInfo.ChrId = listLbpInfo[i].ChrId;
			newInfo.lbp = listLbpInfo[i].lbp;
			_listSumLbpInfo.Add(newInfo);
		}
	}

	void ClearSumInfo()
	{
		if (_listSumPpInfo != null)
			_listSumPpInfo.Clear();
		if (_listSumGrantInfo != null)
			_listSumGrantInfo.Clear();
		if (_listSumLbpInfo != null)
			_listSumLbpInfo.Clear();
	}

	public void OnClickCancelRepeatButton()
	{
		_repeatRemainCount = 0;
		repeatButtonGroupObject.SetActive(false);
	}

	void RequestRepeat()
	{
		// 패킷을 날려서 응답부터 받아야한다.
		// 반복 뽑기가 여러개 있었다면 타입별로 나눴겠지만 지금은 구조상 캐릭터뽑기만 반복이 되므로
		// 조건문 없이 이대로 한다.
		--_repeatRemainCount;
		_dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflrflr", "", true, true);
		_dropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return;
		PlayFabApiManager.instance.RequestCharacterBox(_repeatPrice, OnRecvCharacterBox);
	}

	void OnRecvCharacterBox(bool serverFailure)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;

		// 반복뽑기 결과도 누적
		SumReward();
		DropManager.instance.ClearLobbyDropInfo();

		// 누적까지 했으면 반복횟수가 0인지 확인 후 0이하면 버튼 하이드 시킨다. 어차피 이제 눌러봤자 의미없다.
		if (_repeatRemainCount <= 0)
			repeatButtonGroupObject.SetActive(false);

		// UI부터 연출까지 이미 다 플레이 중인거니 바로 열기 루틴으로 넘어가면 된다.
		Timing.RunCoroutine(RepeatOpenDropProcess());
	}

	IEnumerator<float> RepeatOpenDropProcess()
	{
		// 상자를 다시 그자리에 소환
		_randomBoxAnimator.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(1.0f);

		// 반복 뽑기때는 터치를 기다리지 않는다.
		_randomBoxAnimator.punchScaleTweenAnimation.DOPause();
		_randomBoxAnimator.openAnimator.enabled = true;
		_randomBoxAnimator.disableObjectComponent.enabled = true;
		BattleInstanceManager.instance.GetCachedObject(boxOpenEffectPrefab, _randomBoxAnimator.cachedTransform.position, Quaternion.identity);

		// 박스 애니가 둘로 나눠져있는 상자들 때문에 예외처리 한다.
		if (_randomBoxAnimator.internalOpenAnimator != null)
		{
			yield return Timing.WaitForSeconds(_randomBoxAnimator.internalOpenDelay);
			_randomBoxAnimator.internalOpenAnimator.enabled = true;
			yield return Timing.WaitForSeconds(0.8f - _randomBoxAnimator.internalOpenDelay);
		}
		else
			yield return Timing.WaitForSeconds(0.8f);

		// 드랍프로세서를 작동
		_dropProcessor.StartDrop();

		// 드랍프로세서는 드랍이 끝나면 알아서 active false로 바뀔테니 그때를 기다린다.
		while (_dropProcessor.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;

		// 회수를 알리려고 했는데 onAfterBattle true로 생성하니 알아서 흡수된다.
		while (DropManager.instance.IsExistAcquirableDropObject())
			yield return Timing.WaitForSeconds(0.1f);

		// 반복횟수가 아직 남아있다면 다음 반복으로 넘어가야한다.
		if (_repeatRemainCount > 0)
		{
			RequestRepeat();
			yield break;
		}

		// 마지막 드랍이 들어오고나서 0.5초 대기는 연속 굴림이 전부 끝났을때만 하기로 한다. 이래야 좀더 빨리 뽑는다.
		yield return Timing.WaitForSeconds(0.5f);

		ResetObject();

		CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(_listSumGrantInfo, _listSumLbpInfo, OnResult);
	}

	void OnResult()
	{
		// 획득 결과 캔버스를 띄우면 된다.
		// 그런데 _completeAction에 들어있는건 최초 굴림에 대한 정보이기 때문에 여기선 예외처리로
		// 누적시켜놓은 정보를 가지고 결과창을 보여준다.
		UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxResultCanvas", () =>
		{
			if (CharacterBoxShowCanvas.instance != null && CharacterBoxShowCanvas.instance.gameObject.activeSelf)
				CharacterBoxShowCanvas.instance.gameObject.SetActive(false);

			CharacterBoxResultCanvas.instance.RefreshInfo(0, 0, _listSumPpInfo, _listSumGrantInfo, _listSumLbpInfo);
		});
	}
	#endregion
}