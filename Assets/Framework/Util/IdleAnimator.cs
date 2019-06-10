using UnityEngine;
using System.Collections;

public class IdleAnimator : MonoBehaviour {

	Animator m_Animator;
	int idleHash;
	
	void Awake()
	{
		m_Animator = GetComponent<Animator>();
	}
	
	void Start()
	{
		idleHash = Animator.StringToHash("Idle");
	}
	
	const float _defaultFadeDuration = 0.15f;
	void Update()
	{
		// Check Animation End
		AnimatorStateInfo asi = m_Animator.GetCurrentAnimatorStateInfo(0);
		float duration = _defaultFadeDuration / (asi.length * 0.6f);	// (60.0f / 100.0f) = 100 frames to float time
		if (!asi.loop && asi.normalizedTime > (1.0f - duration) && !m_Animator.IsInTransition(0))
		{
			m_Animator.CrossFade(idleHash, 1.0f - asi.normalizedTime);
		}
	}
}
