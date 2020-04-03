using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace ArtIsDark
{
    public abstract class BBEditor<T> : Editor where T : MonoBehaviour
    {
        T targetScript;
        public T BBTarget
        {
            get
            {
                if (targetScript == null)
                {
                    targetScript = (T)target;
                }
                return targetScript;
            }
        }

        public BBStyles BB = new BBStyles();
        public bool debug = false;

        public abstract string Title { get; }
        public abstract int MajorVersion { get; }
        public abstract int MinorVersion { get; }

        public override void OnInspectorGUI()
        {
            BBHeader();
            if (debug)
            {
                base.OnInspectorGUI();
            }
            BBPadding();
            BBEditorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(BBTarget);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkAllScenesDirty();
                }
            }
        }

        virtual public void BBHeader()
        {
            debug = BB.generateTitle(
                BBTarget.gameObject,
                Title,
                MajorVersion,
                MinorVersion
            );
        }

        virtual public void BBPadding()
        {
            GUILayout.Space(10);
        }

        virtual public void BBEditorGUI()
        {

        }

        virtual public bool DropAreaGUI<T>(string label, out T[] obj)
        {
            obj = null;
            GUILayout.BeginHorizontal();
            {
                GUI.backgroundColor = BB.buttonColor;

                var evt = Event.current;
                var dropArea = GUILayoutUtility.GetRect(0f, 80f, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, label, BB.boxStyle);

                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!dropArea.Contains(evt.mousePosition))
                            break;
                        //
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        //
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            //
                            var itms = DragAndDrop.objectReferences;
                            List<T> components = new List<T>();

                            foreach (var item in itms)
                            {
                                var c = (item as GameObject).GetComponent<T>();

                                if (c != null)
                                {
                                    components.Add(c);
                                }
                            }

                            obj = components.ToArray();
                        }
                        Event.current.Use();
                        break;
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();
            return obj != null && obj.Length > 0;
        }
    }
}