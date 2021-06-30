using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Scroll-Snap")]
[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
	#region Fields
	public bool menualSetup = true;
	public MovementType movementType = MovementType.Fixed;
	public MovementAxis movementAxis = MovementAxis.Horizontal;
	public int startingPanel = 0;
	public bool swipeGestures = true;
	public float minimumSwipeSpeed = 0f;
	public SnapTarget snapTarget = SnapTarget.Next;
	public float snappingSpeed = 10f;
	public float thresholdSnappingSpeed = -1f;
	public bool hardSnap = true;
	public bool useUnscaledTime = false;
	public UnityEvent onPanelChanged, onPanelSelecting, onPanelSelected, onPanelChanging;
	public List<TransitionEffect> transitionEffects = new List<TransitionEffect>();

	private bool dragging, selected = true, pressing;
	private float releaseSpeed, contentLength;
	private Direction releaseDirection;
	private Canvas canvas;
	private RectTransform canvasRectTransform;
	private CanvasScaler canvasScaler;
	private ScrollRect scrollRect;
	private Vector2 previousContentAnchoredPosition, velocity;
	private Dictionary<int, Graphic[]> panelGraphics = new Dictionary<int, Graphic[]>();
	#endregion

	#region Properties
	public RectTransform Content
	{
		get { return scrollRect.content; }
	}
	public RectTransform Viewport
	{
		get { return scrollRect.viewport; }
	}

	public int CurrentPanel { get; set; }
	public int TargetPanel { get; set; }
	public int NearestPanel { get; set; }

	private RectTransform[] PanelsRT { get; set; }
	public GameObject[] Panels { get; set; }
	public Toggle[] Toggles { get; set; }

	public int NumberOfPanels { get; set; }

	public bool MenualSetupFinished { get; set; }
	#endregion

	#region Enumerators
	public enum MovementType
	{
		Fixed,
		Free
	}
	public enum MovementAxis
	{
		Horizontal,
		Vertical
	}
	public enum Direction
	{
		Up,
		Down,
		Left,
		Right
	}
	public enum SnapTarget
	{
		Nearest,
		Previous,
		Next
	}
	#endregion

	#region Methods
	private void Awake()
	{
		Initialize();
	}
	private void OnEnable()
	{
		if (menualSetup)
			return;

		if (Validate())
		{
			Setup();
		}
		else
		{
			throw new Exception("Invalid configuration.");
		}
	}
	private void Update()
	{
		if (menualSetup && MenualSetupFinished == false)
			return;

		if (NumberOfPanels == 0) return;

		OnSelectingAndSnapping();
		OnTransitionEffects();
		OnSwipeGestures();

		DetermineVelocity();
	}
#if UNITY_EDITOR
	private void OnValidate()
	{
		Initialize();
	}
#endif

	public void OnPointerDown(PointerEventData eventData)
	{
		pressing = true;
	}
	public void OnPointerUp(PointerEventData eventData)
	{
		pressing = false;
	}
	public void OnBeginDrag(PointerEventData eventData)
	{
		if (hardSnap)
		{
			scrollRect.inertia = true;
		}

		selected = false;
		dragging = true;
	}
	public void OnDrag(PointerEventData eventData)
	{
		if (dragging)
		{
			onPanelSelecting.Invoke();
		}
	}
	public void OnEndDrag(PointerEventData eventData)
	{
		dragging = false;

		if (movementAxis == MovementAxis.Horizontal)
		{
			releaseDirection = scrollRect.velocity.x > 0 ? Direction.Right : Direction.Left;
		}
		else if (movementAxis == MovementAxis.Vertical)
		{
			releaseDirection = scrollRect.velocity.y > 0 ? Direction.Up : Direction.Down;
		}

		releaseSpeed = scrollRect.velocity.magnitude;
	}

	private void Initialize()
	{
		scrollRect = GetComponent<ScrollRect>();
		canvas = GetComponentInParent<Canvas>();

		if (canvas != null)
		{
			canvasScaler = canvas.GetComponentInParent<CanvasScaler>();
			canvasRectTransform = canvas.GetComponent<RectTransform>();
		}
	}
	private bool Validate()
	{
		bool valid = true;

		if (snappingSpeed < 0)
		{
			Debug.LogError("<b>[SimpleScrollSnap]</b> Snapping speed cannot be negative.", gameObject);
			valid = false;
		}

		return valid;
	}
	public void Setup()
	{
		if (scrollRect == null)
			Initialize();

		NumberOfPanels = 0;
		for (int i = 0; i < Content.childCount; ++i)
		{
			if (Content.GetChild(i) == null || Content.GetChild(i).gameObject.activeSelf == false)
				continue;
			++NumberOfPanels;
		}

		// ScrollRect
		if (movementType == MovementType.Fixed)
		{
			scrollRect.horizontal = (movementAxis == MovementAxis.Horizontal);
			scrollRect.vertical = (movementAxis == MovementAxis.Vertical);
		}
		else
		{
			scrollRect.horizontal = scrollRect.vertical = true;
		}

		int index = 0;
		Panels = new GameObject[NumberOfPanels];
		PanelsRT = new RectTransform[NumberOfPanels];
		for (int i = 0; i < Content.childCount; i++)
		{
			if (Content.GetChild(i) == null || Content.GetChild(i).gameObject.activeSelf == false)
				continue;
			Panels[index] = Content.GetChild(i).gameObject;
			PanelsRT[index] = Panels[index].GetComponent<RectTransform>();
			++index;
		}

		// Starting Panel
		float xOffset = (movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free) ? Viewport.rect.width / 2f : 0f;
		float yOffset = (movementAxis == MovementAxis.Vertical || movementType == MovementType.Free) ? Viewport.rect.height / 2f : 0f;
		Vector2 offset = new Vector2(xOffset, yOffset);
		//previousContentAnchoredPosition = Content.anchoredPosition = -PanelsRT[startingPanel].anchoredPosition + offset;
		previousContentAnchoredPosition = -PanelsRT[startingPanel].anchoredPosition + offset;
		if (movementAxis == MovementAxis.Horizontal) previousContentAnchoredPosition.y = Content.anchoredPosition.y;
		if (movementAxis == MovementAxis.Vertical) previousContentAnchoredPosition.x = Content.anchoredPosition.x;
		Content.anchoredPosition = previousContentAnchoredPosition;
		CurrentPanel = TargetPanel = NearestPanel = startingPanel;

		if (panelGraphics == null)
			panelGraphics = new Dictionary<int, Graphic[]>();
		panelGraphics.Clear();
		for (int i = 0; i < Content.childCount; i++)
		{
			if (Content.GetChild(i) == null || Content.GetChild(i).gameObject.activeSelf == false)
				continue;
			panelGraphics.Add(i, Content.GetChild(i).GetComponentsInChildren<Graphic>());
		}

		if (menualSetup)
			MenualSetupFinished = true;
	}

	private Vector2 DisplacementFromCenter(int index)
	{
		Vector2 center = PanelsRT[index].anchoredPosition + Content.anchoredPosition - new Vector2(Viewport.rect.width * (0.5f - Content.anchorMin.x), Viewport.rect.height * (0.5f - Content.anchorMin.y));
		if (movementAxis == MovementAxis.Horizontal) center.y = Content.anchoredPosition.y;
		if (movementAxis == MovementAxis.Vertical) center.x = Content.anchoredPosition.x;
		return center;
	}
	private int DetermineNearestPanel()
	{
		int panelNumber = NearestPanel;
		float[] distances = new float[NumberOfPanels];
		for (int i = 0; i < Panels.Length; i++)
		{
			distances[i] = DisplacementFromCenter(i).magnitude;
		}
		float minDistance = Mathf.Min(distances);
		for (int i = 0; i < Panels.Length; i++)
		{
			if (minDistance == distances[i])
			{
				panelNumber = i;
				break;
			}
		}
		return panelNumber;
	}
	private void DetermineVelocity()
	{
		Vector2 displacement = Content.anchoredPosition - previousContentAnchoredPosition;
		float time = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

		velocity = displacement / time;

		previousContentAnchoredPosition = Content.anchoredPosition;
	}
	private void SelectTargetPanel()
	{
		Vector2 displacementFromCenter = DisplacementFromCenter(NearestPanel = DetermineNearestPanel());

		if (snapTarget == SnapTarget.Nearest || releaseSpeed <= minimumSwipeSpeed)
		{
			GoToPanel(NearestPanel);
		}
		else if (snapTarget == SnapTarget.Previous)
		{
			if ((releaseDirection == Direction.Right && displacementFromCenter.x < 0f) || (releaseDirection == Direction.Up && displacementFromCenter.y < 0f))
			{
				GoToNextPanel();
			}
			else if ((releaseDirection == Direction.Left && displacementFromCenter.x > 0f) || (releaseDirection == Direction.Down && displacementFromCenter.y > 0f))
			{
				GoToPreviousPanel();
			}
			else
			{
				GoToPanel(NearestPanel);
			}
		}
		else if (snapTarget == SnapTarget.Next)
		{
			if ((releaseDirection == Direction.Right && displacementFromCenter.x > 0f) || (releaseDirection == Direction.Up && displacementFromCenter.y > 0f))
			{
				GoToPreviousPanel();
			}
			else if ((releaseDirection == Direction.Left && displacementFromCenter.x < 0f) || (releaseDirection == Direction.Down && displacementFromCenter.y < 0f))
			{
				GoToNextPanel();
			}
			else
			{
				GoToPanel(NearestPanel);
			}
		}
	}
	private void SnapToTargetPanel()
	{
		float xOffset = (movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free) ? Viewport.rect.width / 2f : 0f;
		float yOffset = (movementAxis == MovementAxis.Vertical || movementType == MovementType.Free) ? Viewport.rect.height / 2f : 0f;
		Vector2 offset = new Vector2(xOffset, yOffset);

		Vector2 targetPosition = -PanelsRT[TargetPanel].anchoredPosition + offset;
		if (movementAxis == MovementAxis.Horizontal) targetPosition.y = Content.anchoredPosition.y;
		if (movementAxis == MovementAxis.Vertical) targetPosition.x = Content.anchoredPosition.x;
		Content.anchoredPosition = Vector2.Lerp(Content.anchoredPosition, targetPosition, (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * snappingSpeed);

		if (CurrentPanel != TargetPanel)
		{
			if (DisplacementFromCenter(TargetPanel).magnitude < (Viewport.rect.width / 10f))
			{
				CurrentPanel = TargetPanel;

				onPanelChanged.Invoke();
			}
			else
			{
				onPanelChanging.Invoke();
			}
		}
	}

	private void OnSelectingAndSnapping()
	{
		if (selected)
		{
			if (!((dragging || pressing) && swipeGestures))
			{
				SnapToTargetPanel();
			}
		}
		else if (!dragging && (scrollRect.velocity.magnitude <= thresholdSnappingSpeed || thresholdSnappingSpeed == -1f))
		{
			SelectTargetPanel();
		}
	}
	private void OnTransitionEffects()
	{
		if (transitionEffects.Count == 0) return;

		for (int i = 0; i < NumberOfPanels; i++)
		{
			foreach (TransitionEffect transitionEffect in transitionEffects)
			{
				// Displacement
				float displacement = 0f;
				if (movementType == MovementType.Fixed)
				{
					if (movementAxis == MovementAxis.Horizontal)
					{
						displacement = DisplacementFromCenter(i).x;
					}
					else if (movementAxis == MovementAxis.Vertical)
					{
						displacement = DisplacementFromCenter(i).y;
					}
				}
				else
				{
					displacement = DisplacementFromCenter(i).magnitude;
				}

				// Value
				RectTransform panel = PanelsRT[i];
				switch (transitionEffect.Label)
				{
					case "localPosition.z":
						panel.transform.localPosition = new Vector3(panel.transform.localPosition.x, panel.transform.localPosition.y, transitionEffect.GetValue(displacement));
						break;
					case "localScale.x":
						panel.transform.localScale = new Vector2(transitionEffect.GetValue(displacement), panel.transform.localScale.y);
						break;
					case "localScale.y":
						panel.transform.localScale = new Vector2(panel.transform.localScale.x, transitionEffect.GetValue(displacement));
						break;
					case "localRotation.x":
						panel.transform.localRotation = Quaternion.Euler(new Vector3(transitionEffect.GetValue(displacement), panel.transform.localEulerAngles.y, panel.transform.localEulerAngles.z));
						break;
					case "localRotation.y":
						panel.transform.localRotation = Quaternion.Euler(new Vector3(panel.transform.localEulerAngles.x, transitionEffect.GetValue(displacement), panel.transform.localEulerAngles.z));
						break;
					case "localRotation.z":
						panel.transform.localRotation = Quaternion.Euler(new Vector3(panel.transform.localEulerAngles.x, panel.transform.localEulerAngles.y, transitionEffect.GetValue(displacement)));
						break;
					case "color.r":
						foreach (Graphic graphic in panelGraphics[i])
						{
							graphic.color = new Color(transitionEffect.GetValue(displacement), graphic.color.g, graphic.color.b, graphic.color.a);
						}
						break;
					case "color.g":
						foreach (Graphic graphic in panelGraphics[i])
						{
							graphic.color = new Color(graphic.color.r, transitionEffect.GetValue(displacement), graphic.color.b, graphic.color.a);
						}
						break;
					case "color.b":
						foreach (Graphic graphic in panelGraphics[i])
						{
							graphic.color = new Color(graphic.color.r, graphic.color.g, transitionEffect.GetValue(displacement), graphic.color.a);
						}
						break;
					case "color.a":
						foreach (Graphic graphic in panelGraphics[i])
						{
							graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, transitionEffect.GetValue(displacement));
						}
						break;
				}
			}
		}
	}
	private void OnSwipeGestures()
	{
		if (swipeGestures)
		{
			scrollRect.horizontal = movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free;
			scrollRect.vertical = movementAxis == MovementAxis.Vertical || movementType == MovementType.Free;
		}
		else
		{
			scrollRect.horizontal = scrollRect.vertical = !dragging;
		}
	}

	public void GoToPanel(int panelNumber)
	{
		TargetPanel = panelNumber;
		selected = true;
		onPanelSelected.Invoke();

		if (hardSnap)
		{
			scrollRect.inertia = false;
		}
	}
	public void GoToPreviousPanel()
	{
		NearestPanel = DetermineNearestPanel();
		if (NearestPanel != 0)
		{
			GoToPanel(NearestPanel - 1);
		}
		else
		{
			GoToPanel(NearestPanel);
		}
	}
	public void GoToNextPanel()
	{
		NearestPanel = DetermineNearestPanel();
		if (NearestPanel != (NumberOfPanels - 1))
		{
			GoToPanel(NearestPanel + 1);
		}
		else
		{
			GoToPanel(NearestPanel);
		}
	}
	public void GoToLastPanel()
	{
		GoToPanel(NumberOfPanels - 1);
	}
	public void AddVelocity(Vector2 velocity)
	{
		scrollRect.velocity += velocity;
		selected = false;
	}
	#endregion

	#region Inner Classes
	[Serializable]
	public class TransitionEffect
	{
		#region Fields
		[SerializeField] protected float minDisplacement, maxDisplacement, minValue, maxValue, defaultMinValue, defaultMaxValue, defaultMinDisplacement, defaultMaxDisplacement;
		[SerializeField] protected bool showPanel, showDisplacement, showValue;
		[SerializeField] private string label;
		[SerializeField] private AnimationCurve function;
		[SerializeField] private AnimationCurve defaultFunction;
		[SerializeField] private ScrollSnap scrollSnap;
		#endregion

		#region Properties
		public string Label
		{
			get { return label; }
			set { label = value; }
		}
		public float MinValue
		{
			get { return MinValue; }
			set { minValue = value; }
		}
		public float MaxValue
		{
			get { return maxValue; }
			set { maxValue = value; }
		}
		public float MinDisplacement
		{
			get { return minDisplacement; }
			set { minDisplacement = value; }
		}
		public float MaxDisplacement
		{
			get { return maxDisplacement; }
			set { maxDisplacement = value; }
		}
		public AnimationCurve Function
		{
			get { return function; }
			set { function = value; }
		}
		#endregion

		#region Methods
		public TransitionEffect(string label, float minValue, float maxValue, float minDisplacement, float maxDisplacement, AnimationCurve function, ScrollSnap scrollSnap)
		{
			this.label = label;
			this.scrollSnap = scrollSnap;
			this.minValue = minValue;
			this.maxValue = maxValue;
			this.minDisplacement = minDisplacement;
			this.maxDisplacement = maxDisplacement;
			this.function = function;

			SetDefaultValues(minValue, maxValue, minDisplacement, maxDisplacement, function);
#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
		}

		private void SetDefaultValues(float minValue, float maxValue, float minDisplacement, float maxDisplacement, AnimationCurve function)
		{
			defaultMinValue = minValue;
			defaultMaxValue = maxValue;
			defaultMinDisplacement = minDisplacement;
			defaultMaxDisplacement = maxDisplacement;
			defaultFunction = function;
		}
#if UNITY_EDITOR
		public void Init()
		{
			GUILayout.BeginVertical("HelpBox");
			showPanel = EditorGUILayout.Foldout(showPanel, label, true);
			if (showPanel)
			{
				EditorGUI.indentLevel++;
				float x = minDisplacement;
				float y = minValue;
				float width = maxDisplacement - minDisplacement;
				float height = maxValue - minValue;

				// Min/Max Values
				showValue = EditorGUILayout.Foldout(showValue, "Value", true);
				if (showValue)
				{
					EditorGUI.indentLevel++;
					minValue = EditorGUILayout.FloatField(new GUIContent("Min"), minValue);
					maxValue = EditorGUILayout.FloatField(new GUIContent("Max"), maxValue);
					EditorGUI.indentLevel--;
				}

				// Min/Max Displacements
				showDisplacement = EditorGUILayout.Foldout(showDisplacement, "Displacement", true);
				if (showDisplacement)
				{
					EditorGUI.indentLevel++;
					minDisplacement = EditorGUILayout.FloatField(new GUIContent("Min"), minDisplacement);
					maxDisplacement = EditorGUILayout.FloatField(new GUIContent("Max"), maxDisplacement);
					EditorGUI.indentLevel--;
				}

				// Function
				function = EditorGUILayout.CurveField("Function", function, Color.white, new Rect(x, y, width, height));

				// Reset
				GUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUI.indentLevel * 16);
				if (GUILayout.Button("Reset"))
				{
					Reset();
				}

				// Remove
				if (GUILayout.Button("Remove"))
				{
					scrollSnap.transitionEffects.Remove(this);
				}
				GUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
			GUILayout.EndVertical();
		}
#endif
		public void Reset()
		{
			minValue = defaultMinValue;
			maxValue = defaultMaxValue;
			minDisplacement = defaultMinDisplacement;
			maxDisplacement = defaultMaxDisplacement;
			function = defaultFunction;
		}
		public float GetValue(float displacement)
		{
			return (function != null) ? function.Evaluate(displacement) : 0f;
		}
		#endregion
	}
	#endregion
}