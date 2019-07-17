using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace BoneTool.Script.Runtime
{
	[ExecuteInEditMode]
    public class BoneVisualiser : MonoBehaviour
    {
        public Transform RootNode;
        public float BoneGizmosSize = 0.01f;
        public Color BoneColor = Color.white;
        public bool HideRoot;
        public bool EnableConstraint = true;

        private Transform _preRootNode;
        private Transform[] _childNodes;
        private BoneTransform[] _previousTransforms;

        public Transform[] GetChildNodes() {
            return _childNodes;
        }

        private void Update() {
            if (EnableConstraint && _previousTransforms != null) {
                foreach (var boneTransform in _previousTransforms) {
                    if (boneTransform.Target && boneTransform.Target.hasChanged) {
                        if (boneTransform.Target.parent.childCount == 1) {
                            boneTransform.Target.localPosition = boneTransform.LocalPosition;
                        }
                        else {
                            boneTransform.SetLocalPosition(boneTransform.Target.localPosition);
                        }
                    }
                }
            }
        }

		private void OnScene(SceneView sceneview) {
            if (RootNode != null) {
                if (_childNodes == null || _childNodes.Length == 0 || _previousTransforms == null || _previousTransforms.Length == 0)
                    PopulateChildren();

                Handles.color = BoneColor;

                foreach (var node in _childNodes) {
                    if (!node.transform.parent) continue;
                    if (HideRoot && node == _preRootNode) continue;

                    var start = node.transform.parent.position;
                    var end = node.transform.position;

                    if (Handles.Button(node.transform.position, Quaternion.identity, BoneGizmosSize, BoneGizmosSize, Handles.SphereHandleCap)) {
                        Selection.activeGameObject = node.gameObject;
                    }

                    if (HideRoot && node.parent == _preRootNode) continue;

                    if (node.transform.parent.childCount == 1)
                        Handles.DrawAAPolyLine(5f, start, end);
                    else
                        Handles.DrawDottedLine(start, end, 0.5f);
                }
            }
        }

		public void PopulateChildren() {
            if (!RootNode) return;
            _preRootNode = RootNode;
            _childNodes = RootNode.GetComponentsInChildren<Transform>();
            _previousTransforms = new BoneTransform[_childNodes.Length];
            for (var i = 0; i < _childNodes.Length; i++) {
                var childNode = _childNodes[i];
                _previousTransforms[i] = new BoneTransform(childNode, childNode.localPosition);
            }
        }

        private void OnEnable() {
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable() {
            // ReSharper disable once DelegateSubtraction
            SceneView.duringSceneGui -= OnScene;
        }

        [Serializable]
        private struct BoneTransform
        {
            public Transform Target;
            public Vector3 LocalPosition;

            public BoneTransform(Transform target, Vector3 localPosition) {
                Target = target;
                LocalPosition = localPosition;
            }

            public void SetLocalPosition(Vector3 position) {
                LocalPosition = position;
            }
        }
    }
}
#endif