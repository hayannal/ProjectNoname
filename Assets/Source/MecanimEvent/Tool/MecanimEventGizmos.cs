using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
public class MecanimEventGizmos : MonoBehaviour {

	Transform m_Transform;
	MecanimEventBase m_MecanimEventBase;

	public void SetMecanimEventTransform(Transform t)
	{
		m_Transform = t;
	}

	public void SetMecanimEvent(MecanimEventBase eventBase)
	{
		m_MecanimEventBase = eventBase;
	}

	void OnDrawGizmos()
	{
		if (m_MecanimEventBase == null) return;
		m_MecanimEventBase.OnDrawGizmo(m_Transform);
	}
}
#endif