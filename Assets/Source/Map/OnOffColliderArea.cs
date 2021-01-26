using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnOffColliderArea : MonoBehaviour
{
	public float activeDuration = 10.0f;
	public float cooltimeDuration = 3.0f;
	public GameObject activeGroundEffectObject;
	public GameObject activeOrbEffectObject;
	public Canvas worldCanvas;
	public GameObject gaugeRootObject;
	public GameObject cooltimeRootObject;
	public Slider gaugeSlider;
	public Text cooltimeText;

	float _activeRemainTime;
	float _cooltimeRemainTime;

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnDisable()
	{
		activeGroundEffectObject.SetActive(false);
		activeOrbEffectObject.SetActive(false);
		worldCanvas.gameObject.SetActive(false);

		_cooltimeRemainTime = _cooltimeRemainTime = 0.0f;
	}

	void Update()
	{
		if (_activeRemainTime > 0.0f)
		{
			_activeRemainTime -= Time.deltaTime;
			gaugeSlider.value = (_activeRemainTime / activeDuration);

			if (_activeRemainTime <= 0.0f)
			{
				_activeRemainTime = 0.0f;
				activeGroundEffectObject.SetActive(false);
				activeOrbEffectObject.SetActive(false);

				gaugeRootObject.SetActive(false);
				cooltimeRootObject.SetActive(true);
				_cooltimeRemainTime = cooltimeDuration;
			}
		}

		if (_cooltimeRemainTime > 0.0f)
		{
			_cooltimeRemainTime -= Time.deltaTime;
			cooltimeText.text = cooltimeFloatText;

			if (_cooltimeRemainTime <= 0.0f)
			{
				_cooltimeRemainTime = 0.0f;
				cooltimeRootObject.SetActive(false);
				worldCanvas.gameObject.SetActive(false);
			}
		}
	}

	StringBuilder _sb = new StringBuilder();
	public string cooltimeFloatText
	{
		get
		{
			_sb.Remove(0, _sb.Length);
			if (_cooltimeRemainTime > 1.0f) _sb.AppendFormat("{0}", ((int)(_cooltimeRemainTime + 1.0f)));
			else _sb = _sb.AppendFormat("{0:0.0}", _cooltimeRemainTime);
			return _sb.ToString();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		// 활성화 중에는 처리하지 않는다.
		if (_activeRemainTime > 0.0f)
			return;
		if (_cooltimeRemainTime > 0.0f)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		_activeRemainTime = activeDuration;

		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			OnOffColliderAffector.OnLight(listMonsterActor[i].affectorProcessor, activeDuration);
		}

		activeGroundEffectObject.SetActive(true);
		activeOrbEffectObject.SetActive(true);

		worldCanvas.gameObject.SetActive(true);
		gaugeRootObject.SetActive(true);
		cooltimeRootObject.SetActive(false);
		gaugeSlider.value = 1.0f;
	}

	void OnTriggerStay(Collider other)
	{
		OnTriggerEnter(other);
	}







	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}