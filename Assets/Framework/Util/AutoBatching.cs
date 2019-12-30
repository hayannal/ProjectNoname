using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBatching : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		StaticBatchingUtility.Combine(gameObject);
	}
}
