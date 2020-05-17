using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스크린 스페이스 캔버스라서 캐릭터에 썼던 이름과 비슷하게 지어둔다.
public class ResearchInfoGrowthCanvas : MonoBehaviour
{
	public static ResearchInfoGrowthCanvas instance;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
    {
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
