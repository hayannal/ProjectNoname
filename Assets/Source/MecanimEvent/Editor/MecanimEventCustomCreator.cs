using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;

// user class
public static class MecanimEventCustomCreator
{
	public static MecanimEventBase CreateMecanimEvent(AnimatorState targetState, eMecanimEventType eventType)
	{
		MecanimEventBase eventBase = null;
		switch(eventType)
		{
#region USER_CODE
		case eMecanimEventType.State: eventBase = targetState.AddStateMachineBehaviour<MeState>(); break;
		case eMecanimEventType.Effect: eventBase = targetState.AddStateMachineBehaviour<MeEffect>(); break;
		//case eMecanimEventType.Sound: eventBase = targetState.AddStateMachineBehaviour<MeSound>(); break;
		//case eMecanimEventType.TimeScale: eventBase = targetState.AddStateMachineBehaviour<MeTimeScale>(); break;
		case eMecanimEventType.Destroy: eventBase = targetState.AddStateMachineBehaviour<MeDestroy>(); break;
		//case eMecanimEventType.ApplyAffector: eventBase = targetState.AddStateMachineBehaviour<MeApplyAffector>(); break;
		case eMecanimEventType.AnimatorSpeed: eventBase = targetState.AddStateMachineBehaviour<MeAnimatorSpeed>(); break;
		case eMecanimEventType.HitObject: eventBase = targetState.AddStateMachineBehaviour<MeHitObject>(); break;
		case eMecanimEventType.RangeHitObject: eventBase = targetState.AddStateMachineBehaviour<MeRangeHitObject>(); break;
		case eMecanimEventType.GlobalLight: eventBase = targetState.AddStateMachineBehaviour<MeGlobalLight>(); break;
		case eMecanimEventType.MovePositionCurve: eventBase = targetState.AddStateMachineBehaviour<MeMovePositionCurve>(); break;
		case eMecanimEventType.DontMove: eventBase = targetState.AddStateMachineBehaviour<MeDontMove>(); break;
#endregion
		}
		return eventBase;
	}

	public static eMecanimEventType GetMecanimEventType(MecanimEventBase eventBase)
	{
#region USER_CODE
		if (eventBase is MeState) return eMecanimEventType.State;
		if (eventBase is MeEffect) return eMecanimEventType.Effect;
		//if (eventBase is MeSound) return eMecanimEventType.Sound;
		//if (eventBase is MeTimeScale) return eMecanimEventType.TimeScale;
		if (eventBase is MeDestroy) return eMecanimEventType.Destroy;
		//if (eventBase is MeApplyAffector) return eMecanimEventType.ApplyAffector;
		if (eventBase is MeAnimatorSpeed) return eMecanimEventType.AnimatorSpeed;
		if (eventBase is MeRangeHitObject) return eMecanimEventType.RangeHitObject;
		if (eventBase is MeHitObject) return eMecanimEventType.HitObject;
		if (eventBase is MeGlobalLight) return eMecanimEventType.GlobalLight;
		if (eventBase is MeMovePositionCurve) return eMecanimEventType.MovePositionCurve;
		if (eventBase is MeDontMove) return eMecanimEventType.DontMove;
		return eMecanimEventType.State;
#endregion
	}
}