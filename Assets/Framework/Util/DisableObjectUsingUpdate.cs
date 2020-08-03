using UnityEngine;

// delayTime 남은 상황에서도 임의로 끌 수 있는 DisableObject가 필요해져서 추가.
// 코루틴 대신 Update함수를 사용한다.
public class DisableObjectUsingUpdate : MonoBehaviour
{
	public float delayTime;

	bool started = false;
	void Start()
	{
		started = true;
		_remainTime = delayTime;
	}

	void OnEnable()
	{
		if (!started)
			return;
		_remainTime = delayTime;
	}

	float _remainTime;
	void Update()
	{
		if (_remainTime > 0.0f)
		{
			_remainTime -= Time.deltaTime;
			if (_remainTime <= 0.0f)
			{
				_remainTime = 0.0f;
				gameObject.SetActive(false);
			}
		}
	}
}