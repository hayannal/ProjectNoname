using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoTrainingCanvas : MonoBehaviour
{
	public static CharacterInfoTrainingCanvas instance;

	public GameObject needGroupObject;
	public GameObject contentGroupObject;

	public Transform trainingTextTransform;
	public Text trainingPercentValueText;

	public Text attackText;
	public Text attackValueText;
	public Text hpText;
	public Text hpValueText;

	public GameObject priceButtonObject;
	public GameObject[] priceTypeObjectList;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect[] priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public Text remainTimeText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	float _percent;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		//_maxStatPoint = characterData.maxStatPoint;
		//_remainStatPoint = characterData.remainStatPoint;

		if (characterData.limitBreakLevel <= 1)
		{
			needGroupObject.SetActive(true);
			contentGroupObject.SetActive(false);
			return;
		}
		needGroupObject.SetActive(false);
		contentGroupObject.SetActive(true);
		

		_actorId = actorId;
	}
	#endregion

	public void OnClickTrainingTextButton()
	{
		string text = UIString.instance.GetString("GameUI_TrainingMore");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, text, 250, trainingTextTransform, new Vector2(10.0f, -35.0f));
	}

	public void OnClickTrainingButton()
	{
		if (_percent >= 1.0f)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxTrainingToast"), 2.0f);
			return;
		}
	}

	void OnRecvTraining()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TrainingDone"), 2.0f);
	}
}