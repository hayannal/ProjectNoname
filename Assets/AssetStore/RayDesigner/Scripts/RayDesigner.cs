using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class RayDesigner : MonoBehaviour
{
#if UNITY_EDITOR
    public bool UnfoldMainTab = true;
    public bool ShowGizmos = true;
#endif

	public enum FaceMode
	{
		Camera, Cross, Horizontal, Vertical, Tube, CameraSymmetric, CrossSymmetric, HorizontalSymmetric, VerticalSymmetric
	};

	public List<RayData> Rays = new List<RayData>();

    public GameObject StartPoint;
    public GameObject EndPoint;
    public GameObject ControlPointOne;
    public GameObject ControlPointTwo;

    public bool RayIsActive = true;

    public bool Simulate = true;
    public bool IsDynamic = true;
    private float FadeIn = 1f;
    private float FadeTarget = 1f;
    public float FadeSpeed = 1f;

    public ParticleSystem StartEffect;
    public ParticleSystem HitEffect;

    public Light StartLight;
    public Light EndLight;

    public AnimationCurve StartLightIntensityOverTime;
    public AnimationCurve EndLightIntensityOverTime;

    //Buffer
    private RayDesignerBezier b;
#pragma warning disable 414
    private Mesh MeshBuffer;// = new Mesh(); //Causes "Internal_Create is not allowed to be called from a MonoBehaviour " Error/ Bug
#pragma warning restore 414
    private Vector3[] BezierPoints;
    private float time = 0;
    private float step = 0;
    private Color ColorBuffer= Color.white;
    private Vector2 Offset1 = Vector3.zero;
    private Vector2 Offset2 = Vector3.zero;

    public bool ActiveOnStart = true;

    void OnEnable()
    {
        MeshBuffer = new Mesh();
#if UNITY_EDITOR
        EditorApplication.update += Update;
#endif

        if (!Application.isPlaying)
        {
            FadeIn = 1f;
            Show();
        }

        Keyframe k3 = new Keyframe();
        k3.time = 0f;
        k3.value = 1f;

        Keyframe k4 = new Keyframe();
        k4.time = 10f;
        k4.value = 1f;

        StartLightIntensityOverTime = new AnimationCurve();
        EndLightIntensityOverTime = new AnimationCurve();
        StartLightIntensityOverTime.AddKey(k3);
        StartLightIntensityOverTime.AddKey(k4);
        EndLightIntensityOverTime.AddKey(k3);
        EndLightIntensityOverTime.AddKey(k4);
    }

    void Awake()
    {
        if (ActiveOnStart)
        {
            FadeIn = 1f;
            Show();
        }
        else {
            Hide();
        }
    }

#if UNITY_EDITOR
    void OnDisable()
    {
        EditorApplication.update -= Update;
    }
#endif
    public void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F11))
            Show();

        if (Input.GetKeyDown(KeyCode.F12))
            Hide();
#endif
        CheckPoints();

		FadeIn = Mathf.Lerp(FadeIn, FadeTarget, FadeSpeed * Time.deltaTime);

		if (BezierPoints != null && BezierPoints.Length > 3)
		{
			if (StartLight != null)
			{
				StartLight.transform.position = BezierPoints[Mathf.RoundToInt(BezierPoints.Length * 0.95f)];
				StartLight.intensity = StartLightIntensityOverTime.Evaluate(Time.time) * FadeIn;
			}

			if (EndLight != null)
			{
				EndLight.transform.position = BezierPoints[Mathf.RoundToInt(BezierPoints.Length * 0.05f)];
				EndLight.intensity = EndLightIntensityOverTime.Evaluate(Time.time) * FadeIn;
			}
		}

		for (int r = 0; r < Rays.Count; r++)
        {
            if (Rays[r] == null)
            {
                Debug.LogError("Empty Ray! Removing it: index = "+r);
                Rays.RemoveAt(r);
                return;
            }

            if (Rays[r].meshFilter == null)
            {
                Rays[r].meshFilter = gameObject.GetComponent<MeshFilter>();
                if (Rays[r].meshFilter == null)
                {
                    Rays[r].meshFilter = Rays[r].RayHolder.AddComponent<MeshFilter>();
                }
            }

            if (Rays[r].meshRenderer == null)
            {
                Rays[r].meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (Rays[r].meshRenderer == null)
                {
                    Rays[r].meshRenderer = Rays[r].RayHolder.AddComponent<MeshRenderer>();
                }
            }

            if(Rays[r].meshRenderer.sharedMaterial == null || Rays[r].meshRenderer.sharedMaterial.GetInstanceID() != Rays[r].Mat.GetInstanceID())
                Rays[r].meshRenderer.sharedMaterial = Rays[r].Mat;

            if (Simulate)
            {
                CalculateBezier(StartPoint.transform.position, EndPoint.transform.position, ControlPointOne.transform.position, ControlPointTwo.transform.position, Rays[r].Steps);

                if(IsDynamic || Rays[r].meshFilter.sharedMesh == null)
                    Rays[r].meshFilter.mesh = Rays[r].CreateMesh(Rays[r].RayHolder.transform, BezierPoints, FadeIn);
            }
                
        }
        
        if(Simulate)
            UpdateMaterials();

        if (FadeIn <= 0.004f)
        {
            if(StartLight != null)
                if (StartLight.enabled)
                 StartLight.enabled = false;

            if (EndLight != null)
                if (EndLight.enabled)
                    EndLight.enabled = false;

            gameObject.SetActive(false);
        }
        else {
            if (StartLight != null)
                if (!StartLight.enabled)
                StartLight.enabled = true;

            if (EndLight != null)
                if (!EndLight.enabled)
                    EndLight.enabled = true;
        }
    }

	public void UpdateMesh()
	{
		for (int r = 0; r < Rays.Count; r++)
		{
			if (Rays[r] == null)
			{
				Debug.LogError("Empty Ray! Removing it: index = " + r);
				continue;
			}

			CalculateBezier(StartPoint.transform.position, EndPoint.transform.position, ControlPointOne.transform.position, ControlPointTwo.transform.position, Rays[r].Steps);
			Rays[r].meshFilter.mesh = Rays[r].CreateMesh(Rays[r].RayHolder.transform, BezierPoints, FadeIn);
		}
	}

    void CheckPoints()
    {
        if (!StartPoint)
        {
            StartPoint = new GameObject("Start Point");
            StartPoint.transform.parent = transform;
            StartPoint.transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        if (!EndPoint)
        {
            EndPoint = new GameObject("EndPoint");
            EndPoint.transform.parent = transform;
            EndPoint.transform.localPosition = new Vector3(10f, 0f, 0f);
        }

        if (!ControlPointOne)
        {
            ControlPointOne = new GameObject("ControlPointOne");
            ControlPointOne.transform.parent = transform;
            ControlPointOne.transform.localPosition = new Vector3(2f, 4f, 0f);
            ControlPointOne.transform.parent = StartPoint.transform;
        }

        if (!ControlPointTwo)
        {
            ControlPointTwo = new GameObject("ControlPointTwo");
            ControlPointTwo.transform.parent = transform;
            ControlPointTwo.transform.localPosition = new Vector3(8f, 4f, 0f);
            ControlPointTwo.transform.parent = EndPoint.transform;
        }
    }

    public void CalculateBezier(Vector3 _SP, Vector3 _EP, Vector3 _CP1, Vector3 _CP2, int _Steps)
    {
        if (BezierPoints == null || BezierPoints.Length != _Steps)
            BezierPoints = new Vector3[_Steps];

        b = new RayDesignerBezier(_SP, _CP1, _CP2, _EP);

        time = 0;
        step = 1f / (_Steps-1);

        for (int i = 0; i < _Steps; i++)
        {
            BezierPoints[i] = b.GetPointAtTime(time);
            time += step;
        }
#if UNITY_EDITOR
        if (!ShowGizmos)
            return;

        for (int j = 0; j < _Steps - 1; j++)
        {
            Debug.DrawLine(BezierPoints[j], BezierPoints[j + 1], Color.red);
        }
#endif
    }

    private void UpdateMaterials()
    {
        for (int r = 0; r < Rays.Count; r++)
        {
            if (Rays[r].Mat != null)
            {
                ColorBuffer = Rays[r].Mat.GetColor("_TintColor");
                ColorBuffer.a = FadeIn;
                Rays[r].Mat.SetColor("_TintColor", ColorBuffer);

                Offset1 = Rays[r].Mat.GetTextureOffset("_Mask");
                Offset2 = Rays[r].Mat.GetTextureOffset("_Distortion");

                if (Application.isPlaying)
                {
                    Offset1.y += Rays[r].TextureSpeed * Time.deltaTime;
                    Offset2.y += Rays[r].DistortionSpeed * Time.deltaTime;
                }
                else {
                    Offset1.y += Rays[r].TextureSpeed * (1f/60f);               //60 FPS simulation
                    Offset2.y += Rays[r].DistortionSpeed * (1f / 60f);
                }

                Rays[r].Mat.SetTextureOffset("_Mask", Offset1);
                Rays[r].Mat.SetTextureOffset("_Distortion", Offset2);
            }
        }
    }
    
    public void AddRay()
    {
        GameObject g = new GameObject("Ray " + (Rays.Count + 1));
        g.transform.parent = transform;

        CheckPoints();

        RayData rayData = new RayData();
        Keyframe k1 = new Keyframe();
        k1.time = 0f;
        k1.value = 1f;

        Keyframe k2 = new Keyframe();
        k2.time = 1f;
        k2.value = 1f;

        rayData.Shape.AddKey(k1);
        rayData.Shape.AddKey(k2);
        rayData.AmplitudeMask.AddKey(k1);
        rayData.AmplitudeMask.AddKey(k2);
        rayData.RayHolder = g;

        Material m = new Material(Shader.Find("UltimateRayDesigner/Ray_Add"));
        m.SetTexture("_MainTex", Resources.Load("Textures/RayTexture_5") as Texture2D);
        m.SetTexture("_Mask", Resources.Load("Textures/Mask1") as Texture2D);
        m.SetTexture("_Distortion", Resources.Load("Textures/Noise_1") as Texture2D);
        m.SetColor("_TintColor", new Color(0.0f,.3f,1f,1f));
        rayData.Mat = m;

        Rays.Add(rayData);
    }

    public void DuplicateRay(int _index)
    {
        GameObject g = new GameObject("Ray " + (Rays.Count + 1));
        g.transform.parent = transform;

        if (!StartPoint)
        {
            StartPoint = new GameObject("Start Point");
            StartPoint.transform.parent = transform;
            StartPoint.transform.localPosition = new Vector3(0f, 0f, 0f);

        }

        if (!EndPoint)
        {
            EndPoint = new GameObject("EndPoint");
            EndPoint.transform.parent = transform;
            EndPoint.transform.localPosition = new Vector3(10f, 0f, 0f);

        }

        if (!ControlPointOne)
        {
            ControlPointOne = new GameObject("ControlPointOne");
            ControlPointOne.transform.parent = transform;
            ControlPointOne.transform.localPosition = new Vector3(2f, 4f, 0f);

        }

        if (!ControlPointTwo)
        {
            ControlPointTwo = new GameObject("ControlPointTwo");
            ControlPointTwo.transform.parent = transform;
            ControlPointTwo.transform.localPosition = new Vector3(8f, 4f, 0f);

        }

        RayData rayData = new RayData();
        rayData.faceMode = Rays[_index].faceMode;
        rayData.Sides = Rays[_index].Sides;
        rayData.Steps = Rays[_index].Steps;
        rayData.AmplitudeMask = CopyAnimationCurve(Rays[_index].AmplitudeMask);
        rayData.Shape = CopyAnimationCurve(Rays[_index].Shape);
        rayData.WidthAmplitude = Rays[_index].WidthAmplitude;
        rayData.TextureSpeed = Rays[_index].TextureSpeed;
        rayData.DistortionSpeed = Rays[_index].DistortionSpeed;
        Material mat = new Material(Shader.Find("UltimateRayDesigner/Ray_Add"));
        mat.CopyPropertiesFromMaterial(Rays[_index].Mat);
        rayData.Mat = mat;
        rayData.RayHolder = g;

        Rays.Add(rayData);
    }

    public void RemoveRay()
    {
        if (Rays.Count > 0)
        {
            DestroyImmediate(Rays[Rays.Count-1].RayHolder);
            Rays.RemoveAt(Rays.Count - 1);
        }
    }

#pragma warning disable 618
    public void Show()
    {
        gameObject.SetActive(true);

        if (StartEffect != null)
            StartEffect.enableEmission = true;

        if (HitEffect != null)
            HitEffect.enableEmission = true;

        FadeTarget = 1f;
    }

    public void Hide()
    {
        if (StartEffect != null)
            StartEffect.enableEmission = false;

        if (HitEffect != null)
            HitEffect.enableEmission = false;

        FadeTarget = 0f;
    }
#pragma warning restore 618

    public void UpdateTargetPosition(Vector3 _TargetPoint, Vector3 _ControlPoint)
    {
        EndPoint.transform.position = _TargetPoint;
        ControlPointTwo.transform.position = _ControlPoint;
    }

    public void UpdateStartPosition(Vector3 _TargetPoint, Vector3 _ControlPoint)
    {
        StartPoint.transform.position = _TargetPoint;
        ControlPointOne.transform.position = _ControlPoint;
    }

    private AnimationCurve CopyAnimationCurve(AnimationCurve _AnimationCurve)
    {
        AnimationCurve anim = new AnimationCurve();

        foreach (Keyframe k in _AnimationCurve.keys)
        {
            Keyframe nK = new Keyframe();
            nK.time = k.time;
            nK.value = k.value;
            nK.inTangent = k.inTangent;
            nK.outTangent = k.outTangent;
            nK.tangentMode = k.tangentMode;
            anim.AddKey(k);
        }

        return anim;
    }

    void OnDrawGizmos()
    {
        #if UNITY_EDITOR
            if (!ShowGizmos)
                return;
        #endif

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(StartPoint.transform.position, 1f);
        Gizmos.DrawWireSphere(EndPoint.transform.position, 1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(ControlPointOne.transform.position, 1f);
        Gizmos.DrawWireSphere(ControlPointTwo.transform.position, 1f);

        Gizmos.DrawLine(StartPoint.transform.position, ControlPointOne.transform.position);
        Gizmos.DrawLine(EndPoint.transform.position, ControlPointTwo.transform.position);
    }
}
