using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NaughtyAttributes;

[DisallowMultipleComponent]
public class QuickOutline : MonoBehaviour
{
	private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

	public Color OutlineColor
	{
		get { return outlineColor; }
		set
		{
			outlineColor = value;
			needsUpdate = true;
		}
	}

	public float OutlineWidth
	{
		get { return outlineWidth; }
		set
		{
			outlineWidth = value;
			needsUpdate = true;
		}
	}

	public bool UseBlink
	{
		get { return useBlink; }
		set
		{
			useBlink = value;
			needsUpdate = true;
		}
	}

	[Serializable]
	public class ListVector3
	{
		public List<Vector3> data;
	}

	[SerializeField]
	private Color outlineColor = Color.white;

	[SerializeField, Range(0f, 3f)]
	private float outlineWidth = 1f;

	[SerializeField]
	private bool useBlink = false;

	[Header("Optional")]

	[SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
	+ "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
	private bool precomputeOutline;

	[SerializeField, HideInInspector]
	private List<Mesh> bakeKeys = new List<Mesh>();

	[SerializeField, HideInInspector]
	private List<ListVector3> bakeValues = new List<ListVector3>();

	private MeshRenderer[] meshRenderers;
	private Material quickOutlineStencilMaterial;
	private Material quickOutlineMaterial;

	private bool needsUpdate;

	void Awake()
	{
		// Cache renderers
		meshRenderers = GetComponentsInChildren<MeshRenderer>();

		// Instantiate outline materials
		quickOutlineStencilMaterial = Instantiate(Resources.Load<Material>(@"QuickOutlineStencil"));
		quickOutlineMaterial = Instantiate(Resources.Load<Material>(@"QuickOutline"));
		quickOutlineStencilMaterial.name = "QuickOutlineStencil (Instance)";
		quickOutlineMaterial.name = "QuickOutline (Instance)";

		// Retrieve or generate smooth normals
		LoadSmoothNormals();

		// Apply material properties immediately
		needsUpdate = true;
	}

	bool _started = false;
	void Start()
	{
		_startTime = Time.time;

		AddMaterial();
		_started = true;
	}

	void OnEnable()
	{
		if (_started == false)
			return;

		AddMaterial();
	}

	void OnDisable()
	{
		RemoveMaterial();
	}

	void AddMaterial()
	{
		foreach (var renderer in meshRenderers)
		{
			// Append outline shaders
			var materials = renderer.materials.ToList();

			materials.Add(quickOutlineStencilMaterial);
			materials.Add(quickOutlineMaterial);

			renderer.materials = materials.ToArray();
		}
	}

	void RemoveMaterial()
	{
		foreach (var renderer in meshRenderers)
		{
			// Remove outline shaders
			var materials = renderer.materials.ToList();

			for (int i = materials.Count - 1; i >= 0; --i)
			{
				if (materials[i].name.Contains(quickOutlineStencilMaterial.name))
				{
					materials.RemoveAt(i);
					continue;
				}
				if (materials[i].name.Contains(quickOutlineMaterial.name))
				{
					materials.RemoveAt(i);
					continue;
				}
			}
			// shared일땐 가능해도 materials면 사본이 생기는거라 안된다.
			//materials.Remove(quickOutlineStencilMaterial);
			//materials.Remove(quickOutlineMaterial);

			renderer.materials = materials.ToArray();
		}
	}

	void OnValidate()
	{
		// Update material properties
		needsUpdate = true;

		// Clear cache when baking is disabled or corrupted
		if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
		{
			bakeKeys.Clear();
			bakeValues.Clear();
		}

		// Generate smooth normals when baking is enabled
		if (precomputeOutline && bakeKeys.Count == 0)
		{
			Bake();
		}
	}

	void Update()
	{
		if (needsUpdate)
		{
			needsUpdate = false;

			UpdateMaterialProperties();
		}
		UpdateBlink();
	}

	void OnDestroy()
	{
		// Destroy material instances
		Destroy(quickOutlineStencilMaterial);
		Destroy(quickOutlineMaterial);
	}

	void Bake()
	{
		// Generate smooth normals for each mesh
		var bakedMeshes = new HashSet<Mesh>();

		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			// Skip duplicates
			if (!bakedMeshes.Add(meshFilter.sharedMesh))
			{
				continue;
			}

			// Serialize smooth normals
			var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

			bakeKeys.Add(meshFilter.sharedMesh);
			bakeValues.Add(new ListVector3() { data = smoothNormals });
		}
	}

	void LoadSmoothNormals()
	{
		if (LoadCachedSmoothNormals())
			return;

		// Retrieve or generate smooth normals
		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			// Skip if smooth normals have already been adopted
			if (!registeredMeshes.Add(meshFilter.sharedMesh))
			{
				continue;
			}

			// Retrieve or generate smooth normals
			var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
			var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

			// Store smooth normals in UV3
			meshFilter.sharedMesh.SetUVs(3, smoothNormals);
		}

		// Clear UV3 on skinned mesh renderers
		foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
		{
			if (registeredMeshes.Add(skinnedMeshRenderer.sharedMesh))
			{
				skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
			}
		}
	}

	List<Vector3> SmoothNormals(Mesh mesh)
	{
		// Group vertices by location
		var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

		// Copy normals to a new list
		var smoothNormals = new List<Vector3>(mesh.normals);

		// Average normals for grouped vertices
		foreach (var group in groups)
		{
			// Skip single vertices
			if (group.Count() == 1)
			{
				continue;
			}

			// Calculate the average normal
			var smoothNormal = Vector3.zero;

			foreach (var pair in group)
			{
				smoothNormal += mesh.normals[pair.Value];
			}

			smoothNormal.Normalize();

			// Assign smooth normal to each vertex
			foreach (var pair in group)
			{
				smoothNormals[pair.Value] = smoothNormal;
			}
		}

		return smoothNormals;
	}

	void UpdateMaterialProperties()
	{
		// Apply properties according to mode
		quickOutlineMaterial.SetColor("_OutlineColor", outlineColor);
		quickOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
	}

	public void SetBlink(float blinkSpeed, float blinkWidthRange = 0.5f, float blinkAlphaRange = 0.3f)
	{
		useBlink = true;
		_blinkSpeed = blinkSpeed;
		_blinkWidthRange = blinkWidthRange;
		_blinkAlphaRange = blinkAlphaRange;
	}

	float _startTime;
	float _blinkSpeed = 1.0f;
	float _blinkWidthRange = 0.5f;
	float _blinkAlphaRange = 0.3f;
	void UpdateBlink()
	{
		if (useBlink == false)
			return;

		float fTime = Time.time - _startTime;
		float value = Mathf.PingPong(fTime / _blinkSpeed, 1.0f);

		Color blinkColor = outlineColor;
		blinkColor.a -= value * _blinkAlphaRange;
		quickOutlineMaterial.SetColor("_OutlineColor", blinkColor);

		float blinkWidth = outlineWidth;
		blinkWidth -= value * _blinkWidthRange;
		quickOutlineMaterial.SetFloat("_OutlineWidth", blinkWidth);
	}

	#region Bake Normal To File
	[Button("Bake Normal")]
	public void BakeNormal()
	{
		// Generate smooth normals for each mesh
		var bakedMeshes = new HashSet<Mesh>();

		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			// Skip duplicates
			if (!bakedMeshes.Add(meshFilter.sharedMesh))
			{
				continue;
			}

			// Serialize smooth normals
			var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

			bakeValues.Add(new ListVector3() { data = smoothNormals });
		}

		QuickOutlineNormalData quickOutlineNormalData = ScriptableObject.CreateInstance<QuickOutlineNormalData>();
		quickOutlineNormalData.bakeValues = bakeValues;
#if UNITY_EDITOR
		AssetDatabase.CreateAsset(quickOutlineNormalData, string.Format("{0}/{1}.asset", GetSelectedPathOrFallback(), gameObject.name));
		AssetDatabase.SaveAssets();
#endif
	}

#if UNITY_EDITOR
	public static string GetSelectedPathOrFallback()
	{
		string path = "Assets";

		foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
		{
			path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				path = Path.GetDirectoryName(path);
				break;
			}
		}
		return path;
	}
#endif
	
	bool LoadCachedSmoothNormals()
	{
		EquipPrefabInfo equipPrefabInfo = GetComponent<EquipPrefabInfo>();
		if (equipPrefabInfo == null)
			return false;

		if (equipPrefabInfo.quickOutlineNormalData == null)
			return false;

		// 미리 한번 foreach 돌려서 MeshFilter의 인덱스를 구성해둔다.
		// 메시 내용물이 변하지 않는 이상 같은 인덱스 리스트를 리턴하게 될거다.
		var bakedMeshes = new HashSet<Mesh>();
		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			// Skip duplicates
			if (!bakedMeshes.Add(meshFilter.sharedMesh))
			{
				continue;
			}
			bakeKeys.Add(meshFilter.sharedMesh);
		}

		// Retrieve or generate smooth normals
		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			// Skip if smooth normals have already been adopted
			if (!registeredMeshes.Add(meshFilter.sharedMesh))
			{
				continue;
			}

			// Retrieve or generate smooth normals
			var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
			if (index >= 0 && index < equipPrefabInfo.quickOutlineNormalData.bakeValues.Count)
			{
				// Store smooth normals in UV3
				meshFilter.sharedMesh.SetUVs(3, equipPrefabInfo.quickOutlineNormalData.bakeValues[index].data);
			}
			else
			{
				Debug.LogErrorFormat("Invalid Cached Normal Data! name = {0} / index = {1}", gameObject.name, index);
			}
		}

		return true;
	}
	#endregion
}