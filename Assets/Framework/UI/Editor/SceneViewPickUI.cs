using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;

[InitializeOnLoad]
[ExecuteInEditMode]
public static class SceneViewPickUI
{
    const string KEY_USE_PICKER = "UsePicker";

    internal sealed class Destructor { ~Destructor() { SceneView.duringSceneGui -= OnSceneGUI; } }

    static bool usePicker = false;

    static List<GameObject> cachedResults = null;
    static int pickingIndex = 0;

    static SceneViewPickUI()
	{
        SceneView.duringSceneGui += SceneViewPickUI.OnSceneGUI;
        usePicker = EditorPrefs.GetBool(KEY_USE_PICKER, false);
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();
        {
            GUI.color = usePicker ? Color.green : Color.gray;
            if (GUI.Button(new Rect(5, 8, 90, 16), "UI Picker {0}".Format(usePicker ? "ON" : "OFF")))
            {
                usePicker = !usePicker;
                EditorPrefs.SetBool(KEY_USE_PICKER, usePicker);
            }
            GUILayout.Space(15);

            if (usePicker)
            {
                {
                    GUI.color = Color.yellow;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(5);
                        GUI.color = Color.yellow;
                        GUILayout.BeginVertical(GUI.skin.FindStyle("ColorPickerCurrentExposureSwatchBorder"), GUILayout.Width(154));
                        {
                            bool isEmpty = cachedResults.IsNullOrEmpty();
                            if (isEmpty == false)
                            {
                                GUILayout.TextField("Picked List.");

                                Color unselected = Color.gray;
                                Color selected = Color.magenta;

                                for (int i = 0; i < cachedResults.Count; ++i)
                                {
                                    GameObject go = cachedResults[i].gameObject;
                                    if (go)
                                    {
                                        GUI.color = go == Selection.activeGameObject ? selected : unselected;
                                        if (GUILayout.Button(go.name, GUILayout.Height(30), GUILayout.Width(150)))
                                        {
                                            PickingObject(i);
                                        }
                                    }
                                    else
                                    {
                                        cachedResults.Clear();
                                        break;
                                    }
                                }
                            }
                            GUILayout.Space(5);

                            GUI.color = Color.yellow;
                            GUILayout.TextField("Picker Help.");
                            GUI.color = Color.green;

                            if (isEmpty)
                            {
                                GUILayout.TextField("Right Button Click To UI");
                            }
                            else
                            {
                                GUILayout.TextField("KeyCode.A Up.");
                                GUILayout.TextField("KeyCode.D Down.");
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
        Handles.EndGUI();
        if (usePicker) OnPickerGUI(sceneView);
    }

    // 클릭 위치의 오브젝트들 수집, 트랜스폼의 실제 rect와 마우스 포인트를 비교.
    static Vector3[] fourcorners = new Vector3[4];
    static Rect guiRect = new Rect();

    struct DepthEntry
    {
        public float depth;
        public GameObject go;
    }


    static void OnPickerGUI(SceneView sceneView)
    {
        if (IsKey(KeyCode.A))
        {
            PickingObject(pickingIndex - 1);
        }

        if (IsKey(KeyCode.D))
        {
            PickingObject(pickingIndex + 1);
        }

        if (Event.current != null)
        {
            if (Event.current.isMouse)
            {
                if (Event.current.type == UnityEngine.EventType.MouseDown && Event.current.button == 1 && Event.current.clickCount >= 1)
                {
                    var results = HitCheck_New(sceneView);
                    bool isSame = false;
                    if (results.IsNullOrEmpty() == false && cachedResults.IsNullOrEmpty() == false)
                    {
                        isSame = results.SequenceEqual(cachedResults);
                    }

                    if (isSame)
                    {
                        PickingObject(pickingIndex + 1);
                    }
                    else
                    {
                        cachedResults = results;
                        pickingIndex = 0;
                        PickingObject(pickingIndex);
                    }
                }
            }
        }

        {
            var nowPicked = NowPickingObject();
            if (nowPicked && nowPicked == Selection.activeGameObject)
            {
                var rt = nowPicked.GetComponent<RectTransform>();
                if (rt)
                {
                    rt.GetWorldCorners(fourcorners);
                    DrawShadowedRect(fourcorners, Color.green);
                }
            }
        }
        sceneView.Repaint();


    }


    static List<GameObject> HitCheck_New(SceneView sceneView)
    {
        List<GameObject> ret = null;
        List<DepthEntry> results = null;
        Vector2 mousePos = Event.current.mousePosition;

        Graphic[] graphics = null;

        var targetPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        if (targetPrefabStage != null && targetPrefabStage.prefabContentsRoot)
        {
            graphics = targetPrefabStage.prefabContentsRoot.GetComponentsInChildren<Graphic>();
        }
        else
        {
            graphics = UnityEngine.Object.FindObjectsOfType<Graphic>();
        }

        foreach (var graphic in graphics)
        {
            if (graphic == null || graphic.IsActive() == false)
                continue;

            graphic.rectTransform.GetWorldCorners(fourcorners);
            WorldCornersToGUIRect(fourcorners);

            if (!guiRect.Contains(mousePos))
            {
                continue;
            }

            if (results == null)
                results = new List<DepthEntry>();

            float depth = graphic.GetComponent<CanvasRenderer>().relativeDepth;

            if (graphic.GetComponentInParent<Canvas>().worldCamera != null)
                depth += graphic.GetComponentInParent<Canvas>().worldCamera.depth * 1000;

            results.Add(new DepthEntry()
            {
                go = graphic.gameObject,
                depth = depth,
            });
        }

        if (results.IsNullOrEmpty() == false)
        {
            results.Sort((a, b) =>
            {
                return b.depth.CompareTo(a.depth);
            });

            ret = new List<GameObject>();
            for (int i = 0; i < results.Count; ++i)
            {
                ret.Add(results[i].go);
            }
        }

        return ret;

        void WorldCornersToGUIRect(Vector3[] corners)
        {
            Vector2 guiP0 = HandleUtility.WorldToGUIPoint(corners[0]);
            Vector2 guiP1 = HandleUtility.WorldToGUIPoint(corners[1]);
            Vector2 guiP3 = HandleUtility.WorldToGUIPoint(corners[3]);

            guiRect.x = guiP0.x < guiP3.x ? guiP0.x : guiP3.x;
            guiRect.y = guiP0.y < guiP1.y ? guiP0.y : guiP1.y;

            guiRect.width = Mathf.Abs(guiP3.x - guiP0.x);
            guiRect.height = Mathf.Abs(guiP1.y - guiP0.y);
        }

    }

    static GameObject NowPickingObject()
    {
        if (cachedResults.IsNullOrEmpty())
            return null;

        if (pickingIndex >= cachedResults.Count)
            return null;

        return cachedResults[pickingIndex];
    }

    static void PickingObject(int index)
    {
        if (cachedResults.IsNullOrEmpty() == false)
        {
            index = RollingIndex(index, 0, cachedResults.Count - 1);
            pickingIndex = index;

            if (cachedResults[index].gameObject)
            {
                Selection.activeGameObject = cachedResults[index].gameObject;
                EditorGUIUtility.PingObject(Selection.activeGameObject);
            }
        }


        int RollingIndex(int now, int min, int max)
        {
            if (now > max) now = min;
            if (now < min) now = max;
            return now;
        }
    }



    static KeyCode currentKey = KeyCode.None;

    public static bool IsKey(KeyCode keyCode)
    {
        if (Event.current == null || !Event.current.isKey) return false;

        if (Event.current.type == UnityEngine.EventType.KeyDown && currentKey != keyCode && keyCode == Event.current.keyCode)
        {
            currentKey = keyCode;
            return true;
        }
        else if (Event.current.type == UnityEngine.EventType.KeyUp)
        {
            currentKey = KeyCode.None;
        }
        return false;
    }

    static readonly Vector2[] OUTLINE_POS = new Vector2[]
    {
        new Vector2(1.0f, 0.0f),
        new Vector2(-1.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(0.0f, -1.0f),
        new Vector2(1.0f, 1.0f),
        new Vector2(-1.0f, -1.0f),
        new Vector2(1.0f, -1.0f),
        new Vector2(-1.0f, -1.0f),
    };

    public static void DrawShadowedRect(Vector3[] corners, Color color, float thickness = 2.0f)
    {
        Color handlesColor = color;
        Vector3[] handles = GetHandles(corners);

        for (float i = 0; i < thickness; i += 0.5f)
        {
            Vector3 posA = AddByScreenCoord(corners, handles[0], new Vector2(i, 0.0f));
            Vector3 posB = AddByScreenCoord(corners, handles[1], new Vector2(i, 0.0f));
            DrawShadowedLine(handles, posA, posB, handlesColor);

            posA = AddByScreenCoord(corners, handles[1], new Vector2(0.0f, i));
            posB = AddByScreenCoord(corners, handles[2], new Vector2(0.0f, i));
            DrawShadowedLine(handles, posA, posB, handlesColor);

            posA = AddByScreenCoord(corners, handles[2], new Vector2(-i, 0.0f));
            posB = AddByScreenCoord(corners, handles[3], new Vector2(-i, 0.0f));
            DrawShadowedLine(handles, posA, posB, handlesColor);

            posA = AddByScreenCoord(corners, handles[0], new Vector2(0.0f, -i));
            posB = AddByScreenCoord(corners, handles[3], new Vector2(0.0f, -i));
            DrawShadowedLine(handles, posA, posB, handlesColor);
        }
    }

    static Vector3[] GetHandles(Vector3[] corners)
    {
        Vector3[] v = new Vector3[8];

        v[0] = corners[0];
        v[1] = corners[1];
        v[2] = corners[2];
        v[3] = corners[3];

        v[4] = (corners[0] + corners[1]) * 0.5f;
        v[5] = (corners[1] + corners[2]) * 0.5f;
        v[6] = (corners[2] + corners[3]) * 0.5f;
        v[7] = (corners[0] + corners[3]) * 0.5f;
        return v;
    }

    static Vector3 AddByScreenCoord(Camera cam, Vector3 world, Vector2 add)
    {
        Vector3 guiPos = cam.WorldToScreenPoint(world);
        guiPos.x += add.x;
        guiPos.y += add.y;
        return cam.ScreenToWorldPoint(guiPos);
    }

    static Vector3 AddByScreenCoord(Vector3[] corners, Vector3 world, Vector2 add)
    {
        Vector2 guiPos = HandleUtility.WorldToGUIPoint(world);
        guiPos += add;
        Vector3 worldPos;
        ScreenToWorldPoint(corners, guiPos, out worldPos);
        return worldPos;
    }


    static bool ScreenToWorldPoint(Vector3[] corners, Vector2 screenPos, out Vector3 worldPos)
    {
        Plane p = new Plane(corners[0], corners[1], corners[3]);
        return ScreenToWorldPoint(p, screenPos, out worldPos);
    }

    static bool ScreenToWorldPoint(Plane p, Vector2 screenPos, out Vector3 worldPos)
    {
        float dist;
        Ray ray = HandleUtility.GUIPointToWorldRay(screenPos);

        if (p.Raycast(ray, out dist))
        {
            worldPos = ray.GetPoint(dist);
            return true;
        }
        worldPos = Vector3.zero;
        return false;
    }

    static void DrawShadowedLine(Vector3[] corners, Vector3 worldPos0, Vector3 worldPos1, Color c)
    {
        Plane p = new Plane(corners[0], corners[1], corners[2]);
        Vector2 s0 = HandleUtility.WorldToGUIPoint(worldPos0);
        Vector2 s1 = HandleUtility.WorldToGUIPoint(worldPos1);
    }

    static void DrawShadowedLine(Plane p, Vector2 screenPos0, Vector2 screenPos1, Color c)
    {
        Handles.color = new Color(0f, 0f, 0f, 0.5f);
        DrawLine(p, screenPos0 + Vector2.one, screenPos1 + Vector2.one);
        Handles.color = c;
        DrawLine(p, screenPos0, screenPos1);
    }

    static void DrawLine(Plane p, Vector2 v0, Vector2 v1)
    {
        Vector3 w0, w1;
        if (ScreenToWorldPoint(p, v0, out w0) && ScreenToWorldPoint(p, v1, out w1))
            Handles.DrawLine(w0, w1);
    }

    public static bool IsNullOrEmpty<T>(this T collection) where T : System.Collections.ICollection
    {
        return collection == null || collection.Count == 0;
    }

    public static string Format(this string str, object obj) { return string.Format(str, obj); }
}


