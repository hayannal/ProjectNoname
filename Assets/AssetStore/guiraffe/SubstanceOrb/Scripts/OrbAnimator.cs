﻿using UnityEngine;
using UnityEngine.UI;

namespace guiraffe.SubstanceOrb
{
    [RequireComponent(typeof(Image))]
    public class OrbAnimator : OrbBehaviour
    {
        [Range(-1.0f, 1.0f)] public float Multiplier = 1.0f;

        public Vector2 Smoke1Speed = new Vector2(0.05f, 0.1f);
        public Vector2 Smoke2Speed = new Vector2(0.1f, 0.1f);
        public Vector2 Particles1Speed = new Vector2(0.2f, 0.1f);
        public Vector2 Particles2Speed = new Vector2(0.15f, 0.05f);
        public float SurfaceSpeed = 0.1f;

        Vector2 smoke1Offset;
        Vector2 smoke2Offset;
        Vector2 particles1Offset;
        Vector2 particles2Offset;
        float surfaceOffset;

        void Awake()
        {
            smoke1Offset = Random.Range(0.0f, 1.0f) * Vector2.one;
            smoke2Offset = Random.Range(0.0f, 1.0f) * Vector2.one;
            particles1Offset = Random.Range(0.0f, 1.0f) * Vector2.one;
            particles2Offset = Random.Range(0.0f, 1.0f) * Vector2.one;
            surfaceOffset = Random.Range(0.0f, 1.0f);
        }

        void Update()
        {
            smoke1Offset += Multiplier * Time.deltaTime * Smoke1Speed;
            smoke2Offset += Multiplier * Time.deltaTime * Smoke2Speed;
            particles1Offset += Multiplier * Time.deltaTime * Particles1Speed;
            particles2Offset += Multiplier * Time.deltaTime * Particles2Speed;
            surfaceOffset += Multiplier * Time.deltaTime * SurfaceSpeed;

            Material.SetFloat("_Smoke1OffsetX", smoke1Offset.x);
            Material.SetFloat("_Smoke1OffsetY", smoke1Offset.y);
            Material.SetFloat("_Smoke2OffsetX", smoke2Offset.x);
            Material.SetFloat("_Smoke2OffsetY", smoke2Offset.y);
            Material.SetFloat("_Particles1OffsetX", particles1Offset.x);
            Material.SetFloat("_Particles1OffsetY", particles1Offset.y);
            Material.SetFloat("_Particles2OffsetX", particles1Offset.x);
            Material.SetFloat("_Particles2OffsetY", particles1Offset.y);
            Material.SetFloat("_SurfaceOffsetX", surfaceOffset);
        }
    }
}