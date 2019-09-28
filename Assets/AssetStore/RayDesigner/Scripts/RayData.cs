using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class RayData
{
#if UNITY_EDITOR
    public bool unfolded = true;
#endif

    public RayDesigner.FaceMode faceMode;
    public GameObject RayHolder;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public Material Mat;
    public int Steps = 10;
    public int Sides = 3;//Used only if Tube Sim is used

    public AnimationCurve Shape = new AnimationCurve();
    public AnimationCurve AmplitudeMask = new AnimationCurve();
    public float WidthAmplitude = 1f;

    public float TextureSpeed = 1f;
    public float DistortionSpeed = 1f;

    public ParticleSystem StartEffects;
    public ParticleSystem HitEffects;
    public ParticleSystem EndEffects;
    public Light PointLights;

    //Buffer
    private Mesh mesh;
    private Quaternion dir = Quaternion.identity;
    private float lower;
    float Width = 0;
    int index = 0;

    private Vector3[] Vertices;
    private Vector3[] Normals;
    private Vector2[] UVs;
    private int[] Triangles;
    private Color[] VertColor;

	public Mesh CreateMesh(Transform _parent, Vector3[] _BezierPoints, float _Fade)
	{
		if (mesh == null)
			mesh = new Mesh();

		index = 0;

		switch (faceMode)
		{
			case RayDesigner.FaceMode.CameraSymmetric:
				int VertNum = (Steps - 1) * 3 + 2;
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != VertNum)
				{
					mesh = new Mesh();
					Vertices = new Vector3[VertNum];
					//Normals = new Vector3[Steps * 2];
					UVs = new Vector2[VertNum];
					Triangles = new int[(Steps - 1) * 9];
					VertColor = new Color[VertNum];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (Camera.main == null)
						{
							Debug.LogWarning("[URD] Main Camera not assigned. Ray cannot face Camera! Assign a main Camera to use this feature!");
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
						}
						else
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					if (i == 0)
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
					else
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i]);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(.5f, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
				}
				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					if (i == 0)
					{
						Triangles[index] = 0;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 0;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 2;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 4;
						index++;
						Triangles[index] = 3;
						index++;
					}
					else
					{
						int ii = (i - 1) * 3 + 2;

						if (ii > Vertices.Length)
							Debug.Log(i + " " + ii + " " + index);

						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 3;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 5;
						index++;
						Triangles[index] = ii + 4;
						index++;
					}
				}

				mesh.vertices = Vertices;
				//mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.Camera:
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != Steps * 2)
				{
					mesh = new Mesh();
					Vertices = new Vector3[Steps * 2];
					//Normals = new Vector3[Steps * 2];
					UVs = new Vector2[Steps * 2];
					Triangles = new int[Steps * 6 - 6];
					VertColor = new Color[Steps * 2];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (Camera.main == null)
						{
							Debug.LogWarning("[URD] Main Camera not assigned. Ray cannot face Camera! Assign a main Camera to use this feature!");
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
						}
						else
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
					//Normals[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * Vector3.up);
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(0, lower);
					index++;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
					//Normals[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * Vector3.up);
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(1, lower);
					index++;
				}

				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					int ii = i * 2;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 1;
					index++;
					Triangles[index] = ii + 3;
					index++;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 3;
					index++;
					Triangles[index] = ii + 2;
					index++;
				}

				mesh.vertices = Vertices;
				//mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.CrossSymmetric:
				VertNum = ((Steps - 1) * 3 + 4) * 2;
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != VertNum)
				{
					mesh = new Mesh();
					Vertices = new Vector3[VertNum];
					//Normals = new Vector3[Steps * 2];
					UVs = new Vector2[VertNum];
					Triangles = new int[((Steps - 1) * 9) * 2];
					VertColor = new Color[VertNum];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (Camera.main == null)
						{
							Debug.LogWarning("[URD] Main Camera not assigned. Ray cannot face Camera! Assign a main Camera to use this feature!");
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
						}
						else
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					if (i == 0)
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
					else
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i]);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(.5f, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (Camera.main == null)
						{
							Debug.LogWarning("[URD] Main Camera not assigned. Ray cannot face Camera! Assign a main Camera to use this feature!");
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
						}
						else
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					if (i == 0)
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, -Width, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, Width, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
					else
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, -Width, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i]);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(.5f, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, Width, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
				}
				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					if (i == 0)
					{

						Triangles[index] = 0;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 0;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 2;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 4;
						index++;
						Triangles[index] = 3;
						index++;
					}
					else
					{
						int ii = (i - 1) * 3 + 2;

						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 3;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 5;
						index++;
						Triangles[index] = ii + 4;
						index++;
					}
				}

				for (int i = _BezierPoints.Length; i < (_BezierPoints.Length * 2) - 1; i++)
				{
					if (i == _BezierPoints.Length)
					{
						int halfVertNum = (Steps - 1) * 3 + 2;
						Triangles[index] = halfVertNum + 0;
						index++;
						Triangles[index] = halfVertNum + 1;
						index++;
						Triangles[index] = halfVertNum + 3;
						index++;
						Triangles[index] = halfVertNum + 0;
						index++;
						Triangles[index] = halfVertNum + 3;
						index++;
						Triangles[index] = halfVertNum + 2;
						index++;
						Triangles[index] = halfVertNum + 1;
						index++;
						Triangles[index] = halfVertNum + 4;
						index++;
						Triangles[index] = halfVertNum + 3;
						index++;
					}
					else
					{
						int ii = (i - 1) * 3 + 1;

						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 3;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 5;
						index++;
						Triangles[index] = ii + 4;
						index++;
					}
				}

				mesh.vertices = Vertices;
				//mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.Cross:
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != Steps * 4)
				{
					mesh = new Mesh();
					Vertices = new Vector3[Steps * 4];
					UVs = new Vector2[Steps * 4];
					Triangles = new int[Steps * 12 - 12];
					VertColor = new Color[Steps * 4];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (i < _BezierPoints.Length - 1)
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(0, lower);
					index++;


					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(1, lower);
					index++;
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						if (i < _BezierPoints.Length - 1)
						{
							dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, (Camera.main.transform.position - _BezierPoints[i]).normalized);
						}
					}

					lower = (float)i / (float)_BezierPoints.Length;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, Width, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(0, lower);
					index++;


					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(0, -Width, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(1, lower);
					index++;
				}

				index = 0;

				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					int ii = i * 2;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 1;
					index++;
					Triangles[index] = ii + 3;
					index++;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 3;
					index++;
					Triangles[index] = ii + 2;
					index++;
				}

				for (int i = _BezierPoints.Length; i < ((_BezierPoints.Length) * 2) - 1; i++)
				{
					int ii = i * 2;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 1;
					index++;
					Triangles[index] = ii + 3;
					index++;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 3;
					index++;
					Triangles[index] = ii + 2;
					index++;
				}

				mesh.vertices = Vertices;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.VerticalSymmetric:
				VertNum = (Steps - 1) * 3 + 2;
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != VertNum)
				{
					mesh = new Mesh();
					Vertices = new Vector3[VertNum];
					//Normals = new Vector3[Steps * 2];
					UVs = new Vector2[VertNum];
					Triangles = new int[(Steps - 1) * 9];
					VertColor = new Color[VertNum];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.back);
					}

					lower = (float)i / (float)_BezierPoints.Length;

					if (i == 0)
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
					else
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i]);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(.5f, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
				}
				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					if (i == 0)
					{
						Triangles[index] = 0;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 0;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 2;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 4;
						index++;
						Triangles[index] = 3;
						index++;
					}
					else
					{
						int ii = (i - 1) * 3 + 2;

						if (ii > Vertices.Length)
							Debug.Log(i + " " + ii + " " + index);

						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 3;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 5;
						index++;
						Triangles[index] = ii + 4;
						index++;
					}
				}

				mesh.vertices = Vertices;
				//mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.Vertical:
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != Steps * 2)
				{
					mesh = new Mesh();
					Vertices = new Vector3[Steps * 2];
					UVs = new Vector2[Steps * 2];
					Triangles = new int[Steps * 6 - 6];
					VertColor = new Color[Steps * 2];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.back);
					}

					lower = (float)i / (float)_BezierPoints.Length;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(0, lower);
					index++;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(1, lower);
					index++;
				}

				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					int ii = i * 2;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 1;
					index++;
					Triangles[index] = ii + 3;
					index++;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 3;
					index++;
					Triangles[index] = ii + 2;
					index++;
				}

				mesh.vertices = Vertices;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.HorizontalSymmetric:
				VertNum = (Steps - 1) * 3 + 2;
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != VertNum)
				{
					mesh = new Mesh();
					Vertices = new Vector3[VertNum];
					//Normals = new Vector3[Steps * 2];
					UVs = new Vector2[VertNum];
					Triangles = new int[(Steps - 1) * 9];
					VertColor = new Color[VertNum];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
					}

					lower = (float)i / (float)_BezierPoints.Length;

					if (i == 0)
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;

						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
					else
					{
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(0, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i]);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(.5f, lower);
						index++;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						UVs[index] = new Vector2(1, lower);
						index++;
					}
				}
				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					if (i == 0)
					{
						Triangles[index] = 0;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 0;
						index++;
						Triangles[index] = 3;
						index++;
						Triangles[index] = 2;
						index++;
						Triangles[index] = 1;
						index++;
						Triangles[index] = 4;
						index++;
						Triangles[index] = 3;
						index++;
					}
					else
					{
						int ii = (i - 1) * 3 + 2;

						if (ii > Vertices.Length)
							Debug.Log(i + " " + ii + " " + index);

						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 0;
						index++;
						Triangles[index] = ii + 4;
						index++;
						Triangles[index] = ii + 3;
						index++;
						Triangles[index] = ii + 2;
						index++;
						Triangles[index] = ii + 5;
						index++;
						Triangles[index] = ii + 4;
						index++;
					}
				}

				mesh.vertices = Vertices;
				//mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.Horizontal:
				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != Steps * 2)
				{
					mesh = new Mesh();
					Vertices = new Vector3[Steps * 2];
					UVs = new Vector2[Steps * 2];
					Triangles = new int[Steps * 6 - 6];
					VertColor = new Color[Steps * 2];
				}

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
					}

					lower = (float)i / (float)_BezierPoints.Length;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(-Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(0, lower);
					index++;

					Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * new Vector3(Width, 0, 0));
					VertColor[index] = new Color(Amp, Amp, Amp, Amp);
					UVs[index] = new Vector2(1, lower);
					index++;
				}

				index = 0;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					int ii = i * 2;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 1;
					index++;
					Triangles[index] = ii + 3;
					index++;

					Triangles[index] = ii;
					index++;
					Triangles[index] = ii + 3;
					index++;
					Triangles[index] = ii + 2;
					index++;
				}

				mesh.vertices = Vertices;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;

			case RayDesigner.FaceMode.Tube:

				if (_BezierPoints.Length != Steps || Vertices == null || Vertices.Length != Steps * Sides)
				{
					mesh = new Mesh();
					Vertices = new Vector3[Steps * Sides];
					Normals = new Vector3[Steps * Sides];
					UVs = new Vector2[Steps * Sides];
					Triangles = new int[((Steps - 1) * (Sides * 2)) * 3];
					VertColor = new Color[Steps * Sides];
				}

				float Angle = 360f / Sides;

				for (int i = 0; i < _BezierPoints.Length; i++)
				{
					Width = Shape.Evaluate((float)i / (float)_BezierPoints.Length) * WidthAmplitude * _Fade;
					float Amp = AmplitudeMask.Evaluate((float)i / (float)_BezierPoints.Length);

					if (i < _BezierPoints.Length - 1)
					{
						dir = Quaternion.LookRotation((_BezierPoints[i] - _BezierPoints[i + 1]).normalized, Vector3.up);
					}

					lower = (float)i / (float)_BezierPoints.Length;

					for (int j = 0; j < Sides; j++)
					{
						Vector3 PointDir = (Quaternion.Euler(0, 0, (float)j * Angle) * Vector3.up) * Width;
						Vertices[index] = _parent.InverseTransformPoint(_BezierPoints[i] + dir * PointDir);
						Normals[index] = Vector3.Normalize(Vertices[index] - _parent.InverseTransformPoint(_BezierPoints[i]));
						UVs[index] = new Vector2(((float)(j + 1) / (float)Sides), lower);
						VertColor[index] = new Color(Amp, Amp, Amp, Amp);
						index++;
					}
				}

				index = 0;

				int CurrentStartIndex = 0;
				int NextStartIndex = Sides;

				//Triangles
				for (int i = 0; i < _BezierPoints.Length - 1; i++)
				{
					int StartIndex = CurrentStartIndex;
					for (int j = 0; j < Sides; j++)
					{
						if (j == Sides - 1)
						{
							Triangles[index] = CurrentStartIndex;
							index++;
							Triangles[index] = NextStartIndex;
							index++;
							Triangles[index] = NextStartIndex + 1 - Sides;
							index++;

							Triangles[index] = CurrentStartIndex;
							index++;
							Triangles[index] = NextStartIndex + 1 - Sides;
							index++;
							Triangles[index] = CurrentStartIndex + 1 - Sides;
							index++;
						}
						else
						{
							Triangles[index] = CurrentStartIndex;
							index++;
							Triangles[index] = NextStartIndex;
							index++;
							Triangles[index] = NextStartIndex + 1;
							index++;

							Triangles[index] = CurrentStartIndex;
							index++;
							Triangles[index] = NextStartIndex + 1;
							index++;
							Triangles[index] = CurrentStartIndex + 1;
							index++;
						}

						CurrentStartIndex++;
						NextStartIndex++;
					}
				}

				mesh.vertices = Vertices;
				mesh.normals = Normals;
				mesh.uv = UVs;
				mesh.triangles = Triangles;
				mesh.colors = VertColor;
				mesh.RecalculateBounds();
				//mesh.RecalculateNormals();
				return mesh;
		}

		return null;
	}
}