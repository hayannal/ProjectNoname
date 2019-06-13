﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace BoneTool.Script.Runtime
{
    [RequireComponent(typeof(BoneVisualiser))]
    public class BonePoseLib : MonoBehaviour
    {
        public List<ArmaturePose> Poses;

        public void AddPose(ArmaturePose pose) {
            Poses.Add(pose);
        }

        public void RemovePose(string poseName) {
            for (var i = 0; i < Poses.Count; i++) {
                var pose = Poses[i];
                if (pose.Name == poseName) {
                    Poses.RemoveAt(i);
                    break;
                }
            }
        }

        public void SetPose(int index) {
            Undo.RecordObject(this, "Set Pose");

            var pose = Poses[index];
            pose.BonePoses = new List<BonePose>();

            var nodes = GetComponent<BoneVisualiser>().GetChildNodes();
            foreach (var node in nodes) {
                pose.BonePoses.Add(new BonePose(node));
            }
        }

        public void ApplyPose(int index) {
            var pose = Poses[index];

            if (pose.BonePoses == null) {
                print("Empty pose");
                return;
            }

            Undo.RecordObjects(GetComponent<BoneVisualiser>().GetChildNodes(), "Apply Pose");
            foreach (var bonePose in pose.BonePoses) {
                bonePose.Apply();
            }
        }

        [Serializable]
        public class ArmaturePose
        {
            public string Name = "New Pose";
            public List<BonePose> BonePoses;
        }

        [Serializable]
        public struct BonePose
        {
            public Vector3 SavedPos;
            public Vector3 SavedRot;
            public Vector3 SavedSca;
            public Transform TargetTransform;

            public BonePose(Transform targetTransform) : this() {
                TargetTransform = targetTransform;

                SavedPos = TargetTransform.localPosition;
                SavedRot = TargetTransform.localEulerAngles;
                SavedSca = TargetTransform.localScale;
            }

            public void Apply() {
                TargetTransform.localPosition = SavedPos;
                TargetTransform.localEulerAngles = SavedRot;
                TargetTransform.localScale = SavedSca;
            }
        }
    }
}
#endif