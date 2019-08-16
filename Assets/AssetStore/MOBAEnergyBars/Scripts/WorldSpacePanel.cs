using UnityEngine;
using System.Collections;

public class WorldSpacePanel : MonoBehaviour {
    public Camera Camera;
    public bool FaceCamera = true;

    Vector3 offset;
    GameObject lastCamera;
    bool preRenderCallback;

    Camera cam { get { return (Camera != null) ? Camera : Camera.main; } set { Camera = value; } }

    public void DoUpdate()
    {
        doFollow();
    }

    void Awake()
    {
        preRenderCallback = false;
    }

    void Start()
    {
        offset = transform.localPosition;
    }

    void Update()
    {
        updatePreRenderRegistration();
    }

    void LateUpdate()
    {
        if (!preRenderCallback)
        {
            DoUpdate();
        }
    }

    void OnDestroy()
    {
        removePreRenderRegistration();
    }

    void updatePreRenderRegistration()
    {
        if (lastCamera != cam)
        {
            removePreRenderRegistration();
            UpdateMOBAPanelsOnPreRender pr = cam.GetComponent<UpdateMOBAPanelsOnPreRender>();
            if (pr != null)
            {
                pr.CallList += DoUpdate;
                preRenderCallback = true;
            }
            lastCamera = cam.gameObject;
        }
    }

    void removePreRenderRegistration()
    {
        if (lastCamera != null)
        {
            UpdateMOBAPanelsOnPreRender pr = lastCamera.GetComponent<UpdateMOBAPanelsOnPreRender>();
            if (pr != null)
            {
                pr.CallList -= DoUpdate;
            }
        }
        preRenderCallback = false;
    }

    void doFollow()
    {
        if (FaceCamera)
        {
            transform.rotation = Quaternion.identity;
            if (transform.parent != null)
                transform.position = transform.parent.position + offset;
            else
                transform.position = offset;
            transform.LookAt(cam.transform, cam.transform.rotation * Vector3.up);
        }
    }
}
