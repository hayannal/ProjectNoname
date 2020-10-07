using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// 유니티의 SpriteAtlas는 Include in Build 옵션을 켜고 빌드했을때 빌드에 아틀라스가 들어가버리기 때문에 메모리 낭비가 발생한다.
// 그래서 번들을 쓰는 경우에는 이 옵션을 해제하고 늦은 바인딩 처리를 '직접' 해야한다.
// 씬에다가 던져놓으면 알아서 바인딩하고 해제하는 형태니 어드레서블에 잘 등록시켜두면 될거다.
public class AtlasLateBinder : MonoBehaviour
{
	void OnEnable()
	{
		SpriteAtlasManager.atlasRequested += RequestLateBindingAtlas;
	}

	void OnDisable()
	{
		SpriteAtlasManager.atlasRequested -= RequestLateBindingAtlas;
	}

	void RequestLateBindingAtlas(string atlasName, System.Action<SpriteAtlas> callback)
	{
		//Debug.LogWarningFormat("RequestLateBindingAtlas : {0}", atlasName);

		AddressableAssetLoadManager.GetAddressableSpriteAtlas(atlasName, "Atlas", (atlas) =>
		{
			callback(atlas);
		});
	}
}