using UnityEngine;
using System.Collections;

public class ScreenSpacePanel : MonoBehaviour {
    public Canvas ShowOnCanvas;
    public GameObject UIElementPrefab;

    Vector3 offset;
    GameObject panelHolder;
    GameObject panelElement;
    RectTransform canvasRect;
    RectTransform panelRect;
    bool createdCanvas = false;
    GameObject lastCamera;
    bool preRenderCallback;

    Camera cam { get { return (ShowOnCanvas.worldCamera != null) ? ShowOnCanvas.worldCamera : Camera.main; } }

    public GameObject PanelUIElement { get { return panelElement; } }

    public void DoUpdate()
    {
        doScale();
        doFollow();
    }

    void Awake ()
    {
        if (UIElementPrefab == null)
        {
            Debug.LogError("ScreenSpacePanel needs a UI Element Prefab assigned. Will deactivate.");
            gameObject.SetActive(false);
            return;
        }
        panelHolder = new GameObject("ScreenSpacePanel Holder - " + UIElementPrefab.name);
        panelHolder.AddComponent<RectTransform>();
        panelElement = Instantiate(UIElementPrefab);
        preRenderCallback = false;
    }
	
    void Start ()
    {
        offset = transform.localPosition;
        if (ShowOnCanvas == null)
        {
            Debug.Log("No canvas assigned to ScreenSpacePanel - will create one from scratch.");
            ShowOnCanvas = (new GameObject("Canvas For Screen Space Panel")).AddComponent<Canvas>();
            ShowOnCanvas.transform.parent = transform;
            ShowOnCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            ShowOnCanvas.worldCamera = cam;
            createdCanvas = true;
        }
        panelHolder.transform.SetParent(ShowOnCanvas.transform, false);
        panelElement.transform.SetParent(panelHolder.transform, false);
        panelElement.transform.localPosition = Vector2.zero;
        canvasRect = ShowOnCanvas.GetComponent<RectTransform>();
        panelRect = panelElement.GetComponent<RectTransform>();
    }

    void Update ()
    {
        updatePreRenderRegistration();
    }

	void LateUpdate ()
    {
        if (!preRenderCallback)
        {
            DoUpdate();
        }
	}

    void OnDestroy ()
    {
        removePreRenderRegistration();
        if (panelElement != null)
            Destroy(panelElement.gameObject);
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

    void doScale()
    {
        // When an existing canvas is used, the ui panel doesn't scale when this ScreenSpacePanel is scaled.
        // Manually scale it in this case so behaviour is consistent across all modes.
        if (!createdCanvas)
        {
            panelHolder.transform.localScale = transform.lossyScale;
        }
    }

    void doFollow()
    {
        transform.rotation = Quaternion.identity;
        if (transform.parent != null)
        {
            transform.position = transform.parent.position + offset;
        }
        Vector3 screen = cam.WorldToScreenPoint(transform.position);
        Rect tRect;

        if (ShowOnCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            tRect = new Rect(
                screen.x / ShowOnCanvas.scaleFactor - canvasRect.sizeDelta.x / 2f - (panelRect.rect.width / 2f * panelRect.lossyScale.x),
                screen.y / ShowOnCanvas.scaleFactor - canvasRect.sizeDelta.y / 2f - (panelRect.rect.height / 2f * panelRect.lossyScale.y),
                panelRect.rect.width * panelRect.lossyScale.x, panelRect.rect.height * panelRect.lossyScale.y);
        }
        else
        {
            ShowOnCanvas.planeDistance = screen.z;

            Vector3 panelScale = Vector3.Scale(panelRect.lossyScale, new Vector3(1f/canvasRect.lossyScale.x, 1f/canvasRect.lossyScale.y, 1f/canvasRect.lossyScale.z));
            tRect = new Rect(
                screen.x / ShowOnCanvas.scaleFactor - canvasRect.sizeDelta.x / 2f - (panelRect.rect.width / 2f * panelScale.x),
                screen.y / ShowOnCanvas.scaleFactor - canvasRect.sizeDelta.y / 2f - (panelRect.rect.height / 2f * panelScale.y),
                panelRect.rect.width * panelScale.x, panelRect.rect.height * panelScale.y);
        }

        if (tRect.Overlaps(canvasRect.rect))
        {
            // Health panel is on screen, make sure its active and move to projected position
            if (!panelElement.gameObject.activeSelf)
                panelElement.gameObject.SetActive(true);

            if (ShowOnCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                panelHolder.transform.position = (Vector2)screen;
            }
            else if (ShowOnCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                screen = new Vector2(
                    (screen.x / ShowOnCanvas.scaleFactor - (canvasRect.sizeDelta.x * 0.5f)),
                    (screen.y / ShowOnCanvas.scaleFactor - (canvasRect.sizeDelta.y * 0.5f)));

                panelHolder.GetComponent<RectTransform>().anchoredPosition = screen;
            }   
        }
        else
        {
            // Health panel is off screen, make sure its deactivated
            if (panelElement.gameObject.activeSelf)
                panelElement.gameObject.SetActive(false);
        }
    }
}