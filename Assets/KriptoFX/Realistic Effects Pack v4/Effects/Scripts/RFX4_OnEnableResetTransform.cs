#define USE_LOCAL

using UnityEngine;
using System.Collections;

public class RFX4_OnEnableResetTransform : MonoBehaviour {

    Transform t;
    Vector3 startPosition;
    Quaternion startRotation;
    Vector3 startScale;
    bool isInitialized;

	void OnEnable () {
	    if(!isInitialized)
        {
            isInitialized = true;
            t = transform;
#if USE_LOCAL
			startPosition = t.localPosition;
            startRotation = t.localRotation;
#else
            startPosition = t.position;
            startRotation = t.rotation;
#endif
            startScale = t.localScale;
        }
        else
        {
#if USE_LOCAL
			t.localPosition = startPosition;
            t.localRotation = startRotation;
#else
            t.position = startPosition;
            t.rotation = startRotation;
#endif
            t.localScale = startScale;
        }
	}

    void OnDisable()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            t = transform;
#if USE_LOCAL
			startPosition = t.localPosition;
            startRotation = t.localRotation;
#else
            startPosition = t.position;
            startRotation = t.rotation;
#endif
            startScale = t.localScale;
        }
        else
        {
#if USE_LOCAL
			t.localPosition = startPosition;
            t.localRotation = startRotation;
#else
            t.position = startPosition;
            t.rotation = startRotation;
#endif
            t.localScale = startScale;
        }
    }
}
