using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapCanvasListItem : MonoBehaviour
{
	public Image characterImage;

	string _actorId;
	public void Initialize(CharacterData characterData)
	{
		_actorId = characterData.actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(characterData.actorId);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});
	}

	public void OnClickButton()
	{
		SwapCanvas.instance.OnClickListItem(_actorId);
	}
}
