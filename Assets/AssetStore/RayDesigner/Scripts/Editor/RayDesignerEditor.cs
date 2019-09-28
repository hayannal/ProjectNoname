using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(RayDesigner))]
public class RayDesignerEditor : Editor
{
    private RayDesigner rayDesigner;

    SerializedProperty UnfoldMainTab;
    SerializedProperty ShowGizmos;
    SerializedProperty Rays;
    SerializedProperty StartPoint;
    SerializedProperty EndPoint;
    SerializedProperty ControlPointOne;
    SerializedProperty ControlPointTwo;
    SerializedProperty Simulate;
    SerializedProperty ActiveOnStart;
    SerializedProperty ActivationSpeed;
    SerializedProperty StartEffect;
    SerializedProperty HitEffect;
    SerializedProperty StartLight;
    SerializedProperty EndLight;
    SerializedProperty StartLightIntensityOverTime;
    SerializedProperty EndLightIntensityOverTime;
    SerializedProperty IsDynamic;    

    [MenuItem("Ray Designer/New Ray")]
    public static void ShowWindow()
    {
        GameObject g = new GameObject("New Ray");
        RayDesigner r = g.AddComponent<RayDesigner>();
        r.AddRay();

        g.transform.position = new Vector3(-5f,0f,0f);
    }

    void OnEnable()
    {
        UnfoldMainTab = serializedObject.FindProperty("UnfoldMainTab");
        ShowGizmos = serializedObject.FindProperty("ShowGizmos");
        Rays = serializedObject.FindProperty("Rays");
        StartPoint = serializedObject.FindProperty("StartPoint");
        EndPoint = serializedObject.FindProperty("EndPoint");
        ControlPointOne = serializedObject.FindProperty("ControlPointOne");
        ControlPointTwo = serializedObject.FindProperty("ControlPointTwo");
        Simulate = serializedObject.FindProperty("Simulate");
        ActiveOnStart = serializedObject.FindProperty("ActiveOnStart");
        ActivationSpeed = serializedObject.FindProperty("FadeSpeed");
        StartEffect = serializedObject.FindProperty("StartEffect");
        HitEffect = serializedObject.FindProperty("HitEffect");
        StartLight = serializedObject.FindProperty("StartLight");
        EndLight = serializedObject.FindProperty("EndLight");
        StartLightIntensityOverTime = serializedObject.FindProperty("StartLightIntensityOverTime");
        EndLightIntensityOverTime = serializedObject.FindProperty("EndLightIntensityOverTime");
        IsDynamic = serializedObject.FindProperty("IsDynamic");
    }

    public override void OnInspectorGUI()
    {

        rayDesigner = (RayDesigner)target;
        serializedObject.Update();

        if (GUILayout.Button("Ray Designer", EditorStyles.toolbarButton))
            UnfoldMainTab.boolValue = !UnfoldMainTab.boolValue;

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Add Ray", EditorStyles.toolbarButton))
            {
                rayDesigner.AddRay();
                return;
            }
                

            if (GUILayout.Button("Remove Ray", EditorStyles.toolbarButton))
            {
                rayDesigner.RemoveRay();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (UnfoldMainTab.boolValue)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Simulate Material");
                    Simulate.boolValue = EditorGUILayout.Toggle(Simulate.boolValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Active on Start");
                    ActiveOnStart.boolValue = EditorGUILayout.Toggle(ActiveOnStart.boolValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Dynamic Update");
                    IsDynamic.boolValue = EditorGUILayout.Toggle(IsDynamic.boolValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Show Gizmos");
                    ShowGizmos.boolValue = EditorGUILayout.Toggle(ShowGizmos.boolValue);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Marker Points");
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Start Point");
                        StartPoint.objectReferenceValue = EditorGUILayout.ObjectField(StartPoint.objectReferenceValue, typeof(GameObject), true) as GameObject;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("End Point");
                        EndPoint.objectReferenceValue = EditorGUILayout.ObjectField(EndPoint.objectReferenceValue, typeof(GameObject), true) as GameObject;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Control Point 1");
                        ControlPointOne.objectReferenceValue = EditorGUILayout.ObjectField(ControlPointOne.objectReferenceValue, typeof(GameObject), true) as GameObject;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Control Point 2");
                        ControlPointTwo.objectReferenceValue = EditorGUILayout.ObjectField(ControlPointTwo.objectReferenceValue, typeof(GameObject), true) as GameObject;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Activation Speed");
                    ActivationSpeed.floatValue = EditorGUILayout.FloatField(ActivationSpeed.floatValue);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5f);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Start Effect");
                    StartEffect.objectReferenceValue = EditorGUILayout.ObjectField(StartEffect.objectReferenceValue, typeof(ParticleSystem), true) as ParticleSystem;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Hit Effect");
                    HitEffect.objectReferenceValue = EditorGUILayout.ObjectField(HitEffect.objectReferenceValue, typeof(ParticleSystem), true) as ParticleSystem;
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5f);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Start Light");
                    StartLight.objectReferenceValue = EditorGUILayout.ObjectField(StartLight.objectReferenceValue, typeof(Light), true) as Light;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("End Light");
                    EndLight.objectReferenceValue = EditorGUILayout.ObjectField(EndLight.objectReferenceValue, typeof(Light), true) as Light;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Start Light Intensity");
                    StartLightIntensityOverTime.animationCurveValue = EditorGUILayout.CurveField(StartLightIntensityOverTime.animationCurveValue, Color.white, new Rect(0f, 0f, 10f, 8f));
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("End Light Intensity");
                    EndLightIntensityOverTime.animationCurveValue = EditorGUILayout.CurveField(EndLightIntensityOverTime.animationCurveValue, Color.white, new Rect(0f, 0f, 10f, 8f));
                }
                EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Show", EditorStyles.toolbarButton))
						rayDesigner.Show();

					if (GUILayout.Button("Hide", EditorStyles.toolbarButton))
						rayDesigner.Hide();
				}
				EditorGUILayout.EndHorizontal();
			}
            EditorGUILayout.EndHorizontal();
        }

        if (rayDesigner.Rays.Count == 0)
            return;

        EditorGUILayout.BeginVertical("Box");
        {
            EditorGUILayout.LabelField("Rays (Click to unfold)", EditorStyles.toolbarButton);
            GUILayout.Space(5f);

            for (int i = 0; i < rayDesigner.Rays.Count; i++)
            {
                if (i % 2f == 0)
                {
                    GUI.color = new Color(.85f, .85f, .85f, 1f);
                }
                else {
                    GUI.color = new Color(.95f, .95f, .95f, 1f);
                }
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Ray " + i.ToString(), EditorStyles.miniButtonLeft))
                    {
                        rayDesigner.Rays[i].unfolded = !rayDesigner.Rays[i].unfolded;
                    }

                    GUI.color = new Color(1f, .8f, 0f, 1f);
                    if (GUILayout.Button("Duplicate", EditorStyles.miniButtonMid, GUILayout.MaxWidth(Screen.width / 6f)))
                    {
                        rayDesigner.DuplicateRay(i);
                        return;
                    }
                    GUI.color = Color.white;

                    if (rayDesigner.Rays[i].RayHolder.activeSelf)
                    {
                        GUI.color = new Color(0f,.8f,0f,1f);
                        if (GUILayout.Button("Hide", EditorStyles.miniButtonMid, GUILayout.MaxWidth(Screen.width / 8f)))
                        {
                            rayDesigner.Rays[i].RayHolder.SetActive(false);
                        }
                        GUI.color = Color.white;
                    }
                    else {
                        GUI.color = new Color(0.3f, .6f, 0.3f, 1f);
                        if (GUILayout.Button("Show", EditorStyles.miniButtonMid, GUILayout.MaxWidth(Screen.width / 8f)))
                        {
                            rayDesigner.Rays[i].RayHolder.SetActive(true);
                        }
                        GUI.color = Color.white;
                    }
                    

                    GUI.color = new Color(.909f, 0.066f, 0.137f, 1f);
                    if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.MaxWidth(Screen.width / 22f)))
                    {
                        if (rayDesigner.Rays[i].RayHolder != null)
                            DestroyImmediate(rayDesigner.Rays[i].RayHolder);

                        rayDesigner.Rays.RemoveAt(i);
                        return;
                    }
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;

                if (rayDesigner.Rays[i].unfolded)
                {
                    EditorGUILayout.BeginVertical("Box");
                    {
                        SerializedProperty FaceMode = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("faceMode");
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Face Mode (Need Main Camera)");
                            FaceMode.enumValueIndex = (int)(RayDesigner.FaceMode)EditorGUILayout.EnumPopup((RayDesigner.FaceMode)FaceMode.enumValueIndex);
                        }
                        EditorGUILayout.EndHorizontal();

                        if ((RayDesigner.FaceMode)FaceMode.enumValueIndex == RayDesigner.FaceMode.Tube)
                        {
                            SerializedProperty SideNum = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("Sides");
                            SideNum.intValue = EditorGUILayout.IntField("Sides", SideNum.intValue);
                            SideNum.intValue = Mathf.Clamp(SideNum.intValue, 3, 200);
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Material");
                            SerializedProperty Mat = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("Mat");

                            if (Mat.objectReferenceValue != null)
                            {
                                if (AssetDatabase.FindAssets(Mat.objectReferenceValue.name).Length == 0)
                                {
                                    GUI.color = Color.green;
                                    if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                                    {

                                        string Path = EditorUtility.SaveFilePanelInProject("Pick file location", "New_Mat", "mat", "Please enter a file name to save the texture to");

                                        if (Path != "")
                                        {
                                            AssetDatabase.CreateAsset(Mat.objectReferenceValue, Path);
                                            AssetDatabase.SaveAssets();
                                        }
                                    }
                                    GUI.color = Color.yellow;
                                    Mat.objectReferenceValue = EditorGUILayout.ObjectField(Mat.objectReferenceValue, typeof(Material), true) as Material;
                                    GUI.color = Color.white;
                                } else {
                                    Mat.objectReferenceValue = EditorGUILayout.ObjectField(Mat.objectReferenceValue, typeof(Material), true) as Material;
                                }
                            } else {
                                Mat.objectReferenceValue = EditorGUILayout.ObjectField(Mat.objectReferenceValue, typeof(Material), true) as Material;
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        SerializedProperty Steps = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("Steps");
                        Steps.intValue = EditorGUILayout.IntField("Smoothness", Steps.intValue);
                        Steps.intValue = Mathf.Max(Steps.intValue, 3);

                        SerializedProperty Shape = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("Shape");
                        Shape.animationCurveValue = EditorGUILayout.CurveField("Shape", Shape.animationCurveValue, Color.white, new Rect(0f, 0f, 1f, 1f));

                        SerializedProperty AmplitudeMask = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("AmplitudeMask");
                        AmplitudeMask.animationCurveValue = EditorGUILayout.CurveField("Amplitude Mask", AmplitudeMask.animationCurveValue, Color.white, new Rect(0f, 0f, 1f, 1f));

                        SerializedProperty WidthAmplitude = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("WidthAmplitude");
                        WidthAmplitude.floatValue = EditorGUILayout.FloatField("Size", WidthAmplitude.floatValue);

                        SerializedProperty TextureSpeed = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("TextureSpeed");
                        TextureSpeed.floatValue = EditorGUILayout.FloatField("Texture Speed", TextureSpeed.floatValue);

                        SerializedProperty DistortionSpeed = Rays.GetArrayElementAtIndex(i).FindPropertyRelative("DistortionSpeed");
                        DistortionSpeed.floatValue = EditorGUILayout.FloatField("Distortion Speed", DistortionSpeed.floatValue);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
