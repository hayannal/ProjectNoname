﻿// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Version: 1.2.1
// Author: Daniel Lochner

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[CustomEditor(typeof(ScrollSnap))]
public class SimpleScrollSnapEditor : Editor
{
	#region Fields
	private int selectedProperty;
	private float selectedMinValue, selectedMaxValue, selectedMinDisplacement, selectedMaxDisplacement;
	private bool showTransitionEffects = true, showMenual = true, showMovement = true, showMargin = true, showNavigation = true, showSelection = true, showEvents = true, showDisplacement, showValue;
	private SerializedProperty menualConfig, movementType, movementAxis, startingPanel, swipeGestures, minimumSwipeSpeed, toggleNavigation, snapTarget, snappingSpeed, useUnscaledTime, thresholdSnappingSpeed, hardSnap, onPanelSelecting, onPanelSelected, onPanelChanging, onPanelChanged;
	private ScrollSnap scrollSnap;
	private AnimationCurve selectedFunction = AnimationCurve.Constant(0, 1, 1);
	#endregion

	#region Methods
	private void OnEnable()
	{
		scrollSnap = target as ScrollSnap;

		// Serialized Properties
		menualConfig = serializedObject.FindProperty("menualSetup");
		movementType = serializedObject.FindProperty("movementType");
		movementAxis = serializedObject.FindProperty("movementAxis");
		startingPanel = serializedObject.FindProperty("startingPanel");
		swipeGestures = serializedObject.FindProperty("swipeGestures");
		minimumSwipeSpeed = serializedObject.FindProperty("minimumSwipeSpeed");
		toggleNavigation = serializedObject.FindProperty("toggleNavigation");
		snapTarget = serializedObject.FindProperty("snapTarget");
		snappingSpeed = serializedObject.FindProperty("snappingSpeed");
		thresholdSnappingSpeed = serializedObject.FindProperty("thresholdSnappingSpeed");
		hardSnap = serializedObject.FindProperty("hardSnap");
		useUnscaledTime = serializedObject.FindProperty("useUnscaledTime");
		onPanelSelecting = serializedObject.FindProperty("onPanelSelecting");
		onPanelSelected = serializedObject.FindProperty("onPanelSelected");
		onPanelChanging = serializedObject.FindProperty("onPanelChanging");
		onPanelChanged = serializedObject.FindProperty("onPanelChanged");
	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		HeaderInformation();
		MenualSetting();
		MovementAndLayoutSettings();
		NavigationSettings();
		SnapSettings();
		TransitionEffects();
		EventHandlers();

		serializedObject.ApplyModifiedProperties();
		PrefabUtility.RecordPrefabInstancePropertyModifications(scrollSnap);
	}

	private void HeaderInformation()
	{
		GUILayout.BeginVertical("HelpBox");
		GUILayout.Label("Scroll-Snap", new GUIStyle() { fontSize = 30, alignment = TextAnchor.MiddleCenter });
		GUILayout.EndVertical();
	}

	private void MenualSetting()
	{
		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showMenual = EditorGUILayout.Foldout(showMenual, "Menual Settings", true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showMenual)
		{
			MenualConfig();
		}

		EditorGUILayout.Space();
	}

	private void MenualConfig()
	{
		EditorGUILayout.PropertyField(swipeGestures, new GUIContent("Menual Mode", "Initialize Menual Mode?"));
	}

	private void MovementAndLayoutSettings()
	{
		EditorGUILayout.Space();

		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showMovement = EditorGUILayout.Foldout(showMovement, "Movement and Layout Settings", true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showMovement)
		{
			MovementType();
			StartingPanel();
		}

		EditorGUILayout.Space();
	}
	private void MovementType()
	{
		EditorGUILayout.PropertyField(movementType, new GUIContent("Movement Type", "Determines how users will be able to move between panels within the ScrollRect."));
		if (scrollSnap.movementType == ScrollSnap.MovementType.Fixed)
		{
			EditorGUI.indentLevel++;

			MovementAxis();

			EditorGUI.indentLevel--;
		}
	}
	private void MovementAxis()
	{
		EditorGUILayout.PropertyField(movementAxis, new GUIContent("Movement Axis", "Determines the axis the user's movement will be restricted to."));
	}
	private void StartingPanel()
	{
		EditorGUILayout.IntSlider(startingPanel, 0, scrollSnap.NumberOfPanels - 1, new GUIContent("Starting Panel", "The number of the panel that will be displayed first, based on a 0-indexed array."));
	}

	private void NavigationSettings()
	{
		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showNavigation = EditorGUILayout.Foldout(showNavigation, "Navigation Settings", true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showNavigation)
		{
			SwipeGestures();
		}

		EditorGUILayout.Space();
	}
	private void SwipeGestures()
	{
		EditorGUILayout.PropertyField(swipeGestures, new GUIContent("Swipe Gestures", "Should users are able to use swipe gestures to navigate between panels?"));
		if (scrollSnap.swipeGestures)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(minimumSwipeSpeed, new GUIContent("Minimum Swipe Speed", "The speed at which the user must be swiping in order for a transition to occur to another panel."));
			EditorGUI.indentLevel--;
		}
	}

	private void SnapSettings()
	{
		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showSelection = EditorGUILayout.Foldout(showSelection, "Snap Settings", true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showSelection)
		{
			SnapTarget();
			SnapSpeed();
			ThresholdSnapSpeed();
			HardSnap();
			UseUnscaledTime();
		}

		EditorGUILayout.Space();
	}
	private void SnapTarget()
	{
		using (new EditorGUI.DisabledScope(scrollSnap.movementType == ScrollSnap.MovementType.Free))
		{
			EditorGUILayout.PropertyField(snapTarget, new GUIContent("Snap Target", "Determines what panel should be targeted and snapped to once the threshold snapping speed has been reached."));
		}
		if (scrollSnap.movementType == ScrollSnap.MovementType.Free)
		{
			scrollSnap.snapTarget = ScrollSnap.SnapTarget.Nearest;
		}
	}
	private void SnapSpeed()
	{
		EditorGUILayout.PropertyField(snappingSpeed, new GUIContent("Snap Speed", "The speed at which the targeted panel snaps into position."));
	}
	private void ThresholdSnapSpeed()
	{
		EditorGUILayout.PropertyField(thresholdSnappingSpeed, new GUIContent("Threshold Snap Speed", "The speed at which the ScrollRect will stop scrolling and begin snapping to the targeted panel (where -1 is used as infinity)."));
	}
	private void HardSnap()
	{
		EditorGUILayout.PropertyField(hardSnap, new GUIContent("Hard Snap", "Should the inertia of the ScrollRect be disabled once a panel has been selected? If enabled, the ScrollRect will not overshoot the targeted panel when snapping into position and instead Lerp precisely towards the targeted panel."));
	}
	private void UseUnscaledTime()
	{
		EditorGUILayout.PropertyField(useUnscaledTime, new GUIContent("Use Unscaled Time", "Should the scroll-snap update irrespective of the time scale?"));
	}

	private void TransitionEffects()
	{
		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showTransitionEffects = EditorGUILayout.Foldout(showTransitionEffects, new GUIContent("Transition Effects", "Effects applied to panels based on their distance from the center."), true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showTransitionEffects)
		{
			EditorGUI.indentLevel++;
			AddTransitionEffect();
			InitTransitionEffects();
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Space();
	}
	private void AddTransitionEffect()
	{
		// Properties
		List<string> properties = new List<string>()
			{
				"localPosition.z",
				"localScale.x",
				"localScale.y",
				"localRotation.x",
				"localRotation.y",
				"localRotation.z",
				"color.r",
				"color.g",
				"color.b",
				"color.a"
			};
		for (int i = 0; i < scrollSnap.transitionEffects.Count; i++)
		{
			properties.Remove(scrollSnap.transitionEffects[i].Label);
		}
		selectedProperty = EditorGUILayout.Popup(new GUIContent("Property", "The selected property of a panel that will be affected by the distance from the centre."), selectedProperty, properties.ToArray());

		// Selected Min/Max Values
		showValue = EditorGUILayout.Foldout(showValue, "Value", true);
		if (showValue)
		{
			EditorGUI.indentLevel++;
			selectedMinValue = EditorGUILayout.FloatField(new GUIContent("Min", "The minimum value that can be assigned."), selectedMinValue);
			selectedMaxValue = EditorGUILayout.FloatField(new GUIContent("Max", "The maximum value that can be assigned."), selectedMaxValue);
			EditorGUI.indentLevel--;
		}

		// Selected Min/Max Displacements
		showDisplacement = EditorGUILayout.Foldout(showDisplacement, "Displacement", true);
		if (showDisplacement)
		{
			EditorGUI.indentLevel++;
			selectedMinDisplacement = EditorGUILayout.FloatField(new GUIContent("Min", "The minimum displacement at which the value will be affected."), selectedMinDisplacement);
			selectedMaxDisplacement = EditorGUILayout.FloatField(new GUIContent("Max", "The maximum displacement at which the value will be affected."), selectedMaxDisplacement);
			EditorGUI.indentLevel--;
		}

		// Selected Function
		float x = selectedMinDisplacement;
		float y = selectedMinValue;
		float width = selectedMaxDisplacement - selectedMinDisplacement;
		float height = selectedMaxValue - selectedMinValue;
		selectedFunction = EditorGUILayout.CurveField(new GUIContent("Function", "The function (with respect to displacement from centre) that will be used to determine the value."), selectedFunction, Color.white, new Rect(x, y, width, height));

		// Add Transition Effect
		GUILayout.BeginHorizontal();
		GUILayout.Space(EditorGUI.indentLevel * 16);
		if (GUILayout.Button("Add Transition Effect"))
		{
			AnimationCurve function = new AnimationCurve(selectedFunction.keys);
			scrollSnap.transitionEffects.Add(new ScrollSnap.TransitionEffect(properties[selectedProperty], selectedMinValue, selectedMaxValue, selectedMinDisplacement, selectedMaxDisplacement, function, scrollSnap));
		}
		GUILayout.EndHorizontal();
	}
	private void InitTransitionEffects()
	{
		// Initialize
		for (int i = 0; i < scrollSnap.transitionEffects.Count; i++)
		{
			scrollSnap.transitionEffects[i].Init();
		}
	}

	private void EventHandlers()
	{
		EditorStyles.foldout.fontStyle = FontStyle.Bold;
		showEvents = EditorGUILayout.Foldout(showEvents, "Event Handlers", true);
		EditorStyles.foldout.fontStyle = FontStyle.Normal;

		if (showEvents)
		{
			EditorGUILayout.PropertyField(onPanelSelecting, new GUIContent("On Panel Selecting"));
			EditorGUILayout.PropertyField(onPanelSelected, new GUIContent("On Panel Selected"));
			EditorGUILayout.PropertyField(onPanelChanging, new GUIContent("On Panel Changing"));
			EditorGUILayout.PropertyField(onPanelChanged, new GUIContent("On Panel Changed"));
		}
	}

	[MenuItem("GameObject/UI/Simple Scroll-Snap/Scroll-Snap", false)]
	private static void CreateSimpleScrollSnap()
	{
		// Canvas
		Canvas canvas = FindObjectOfType<Canvas>();
		if (canvas == null)
		{
			GameObject canvasObject = new GameObject("Canvas");
			canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.gameObject.AddComponent<GraphicRaycaster>();
			Undo.RegisterCreatedObjectUndo(canvasObject, "Create " + canvasObject.name);
		}

		// Scroll-Snap
		GameObject scrollSnap = new GameObject("Scroll-Snap");
		RectTransform scrollSnapRectTransform = scrollSnap.AddComponent<RectTransform>();
		scrollSnapRectTransform.sizeDelta = new Vector2(400, 250);
		ScrollRect scrollSnapScrollRect = scrollSnap.AddComponent<ScrollRect>();
		scrollSnapScrollRect.horizontal = true;
		scrollSnapScrollRect.vertical = false;
		scrollSnapScrollRect.scrollSensitivity = 0f;
		scrollSnapScrollRect.decelerationRate = 0.01f;
		GameObjectUtility.SetParentAndAlign(scrollSnap, canvas.gameObject);
		scrollSnap.AddComponent<ScrollSnap>();

		// Viewport
		GameObject viewport = new GameObject("Viewport");
		RectTransform viewportRectTransform = viewport.AddComponent<RectTransform>();
		viewportRectTransform.anchorMin = new Vector2(0, 0);
		viewportRectTransform.anchorMax = new Vector2(1, 1);
		viewportRectTransform.offsetMin = Vector2.zero;
		viewportRectTransform.offsetMax = Vector2.zero;
		viewport.AddComponent<Mask>();
		Image viewportImage = viewport.AddComponent<Image>();
		viewportImage.color = new Color(1, 1, 1, 0.5f);
		scrollSnapScrollRect.viewport = viewportRectTransform;
		GameObjectUtility.SetParentAndAlign(viewport, scrollSnap.gameObject);

		// Content
		GameObject content = new GameObject("Content");
		RectTransform contentRectTransform = content.AddComponent<RectTransform>();
		contentRectTransform.sizeDelta = new Vector2(400, 250);
		contentRectTransform.anchorMin = new Vector2(0, 0.5f);
		contentRectTransform.anchorMax = new Vector2(0, 0.5f);
		contentRectTransform.pivot = new Vector2(0, 0.5f);
		scrollSnapScrollRect.content = contentRectTransform;
		GameObjectUtility.SetParentAndAlign(content, viewport.gameObject);

		GameObject[] panels = new GameObject[5];
		for (int i = 0; i < 5; i++)
		{
			// Panel
			string name = (i + 1) + "";
			panels[i] = new GameObject(name);
			RectTransform panelRectTransform = panels[i].AddComponent<RectTransform>();
			panelRectTransform.anchorMin = Vector2.zero;
			panelRectTransform.anchorMax = Vector2.one;
			panelRectTransform.offsetMin = Vector2.zero;
			panelRectTransform.offsetMax = Vector2.zero;
			panels[i].AddComponent<Image>();
			GameObjectUtility.SetParentAndAlign(panels[i], content.gameObject);

			// Text
			GameObject text = new GameObject("Text");
			RectTransform textRectTransform = text.AddComponent<RectTransform>();
			textRectTransform.anchorMin = Vector2.zero;
			textRectTransform.anchorMax = Vector2.one;
			textRectTransform.offsetMin = Vector2.zero;
			textRectTransform.offsetMax = Vector2.zero;
			Text textText = text.AddComponent<Text>();
			textText.text = name;
			textText.fontSize = 50;
			textText.alignment = TextAnchor.MiddleCenter;
			textText.color = Color.black;
			GameObjectUtility.SetParentAndAlign(text, panels[i]);
		}

		// Event System
		if (!FindObjectOfType<EventSystem>())
		{
			GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));
			eventObject.AddComponent<StandaloneInputModule>();
			Undo.RegisterCreatedObjectUndo(eventObject, "Create " + eventObject.name);
		}

		// Editor
		Selection.activeGameObject = scrollSnap;
		Undo.RegisterCreatedObjectUndo(scrollSnap, "Create " + scrollSnap.name);
	}
	[MenuItem("GameObject/UI/Simple Scroll-Snap/Pagination", false)]
	private static void CreatePagination()
	{
		// Canvas
		Canvas canvas = FindObjectOfType<Canvas>();
		if (canvas == null)
		{
			GameObject canvasObject = new GameObject("Canvas");
			canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.gameObject.AddComponent<GraphicRaycaster>();
			Undo.RegisterCreatedObjectUndo(canvasObject, "Create " + canvasObject.name);
		}

		// Pagination
		int numberOfToggles = 5;
		int side = 25;
		int spacing = 15;

		GameObject pagination = new GameObject("Pagination");
		RectTransform paginationRectTransform = pagination.AddComponent<RectTransform>();
		paginationRectTransform.sizeDelta = new Vector2((numberOfToggles * (side + spacing)) - spacing, side);
		GameObjectUtility.SetParentAndAlign(pagination, canvas.gameObject);

		for (int i = 0; i < numberOfToggles; i++)
		{
			// Toggle
			GameObject toggle = new GameObject((i + 1) + "");
			GameObjectUtility.SetParentAndAlign(toggle, pagination);
			RectTransform toggleRectTransform = toggle.AddComponent<RectTransform>();
			toggleRectTransform.anchorMin = toggleRectTransform.anchorMax = new Vector2(0, 0.5f);
			toggleRectTransform.pivot = new Vector2(0, 0.5f);
			toggleRectTransform.sizeDelta = new Vector2(side, side);
			toggleRectTransform.anchoredPosition = new Vector2(i * (side + spacing), 0);
			Toggle toggleToggle = toggle.AddComponent<Toggle>();
			toggleToggle.isOn = false;
			toggleToggle.transition = Selectable.Transition.None;

			// Background
			GameObject background = new GameObject("Background");
			GameObjectUtility.SetParentAndAlign(background, toggle);
			RectTransform backgroundRectTransform = background.AddComponent<RectTransform>();
			backgroundRectTransform.anchorMin = Vector2.zero;
			backgroundRectTransform.anchorMax = Vector2.one;
			backgroundRectTransform.sizeDelta = Vector2.zero;
			Image backgroundImage = background.AddComponent<Image>();
			backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
			backgroundImage.color = new Color(0.75f, 0.75f, 0.75f);

			// Selected
			GameObject selected = new GameObject("Selected");
			GameObjectUtility.SetParentAndAlign(selected, background);
			RectTransform selectedRectTransform = selected.AddComponent<RectTransform>();
			selectedRectTransform.anchorMin = Vector2.zero;
			selectedRectTransform.anchorMax = Vector2.one;
			selectedRectTransform.sizeDelta = Vector2.zero;
			Image selectedImage = selected.AddComponent<Image>();
			selectedImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
			selectedImage.color = Color.white;
			toggleToggle.graphic = selected.GetComponent<Image>();
		}


		// Event System
		if (!FindObjectOfType<EventSystem>())
		{
			GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));
			eventObject.AddComponent<StandaloneInputModule>();
			Undo.RegisterCreatedObjectUndo(eventObject, "Create " + eventObject.name);
		}

		// Editor
		Selection.activeGameObject = pagination;
		Undo.RegisterCreatedObjectUndo(pagination, "Create " + pagination.name);
	}
	#endregion
}