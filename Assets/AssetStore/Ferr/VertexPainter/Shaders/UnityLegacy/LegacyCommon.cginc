struct Input {
	float2 uv_MainTex;
	#if BLEND_TEX > 1
		float2 uv_MainTex2;
	#endif
	#if BLEND_TEX > 2
		float2 uv_MainTex3;
	#endif
	#if BLEND_TEX > 3
		float2 uv_MainTex4;
	#endif
	fixed4 color : COLOR;
	float3 worldRefl;
	float3 worldNormal;
	float3 viewDir;
	INTERNAL_DATA
};

void litVertex(inout appdata_full v) {
	#ifdef BLEND_WORLDUV
	v.texcoord.xy = WorldUVSimple(v.vertex); 
	v.tangent = mul(unity_WorldToObject, float4(1, 0, 0, -1));
	#endif
}
void litSurface (Input IN, inout SurfaceOutput o) {
	fixed4 c = BlendGetColor(UVVALS(IN), IN.color);
	o.Albedo = c.rgb;
	o.Alpha  = c.a;

	#ifdef BLEND_NORMAL
		o.Normal = BlendGetNormal(UVVALS(IN), IN.color);
	#endif

	#ifdef BLEND_SPECULAR
		fixed4 specData = BlendGetSpecular(UVVALS(IN), IN.color);
		o.Specular = _Shininess;
		o.Gloss    = specData.r;
	#endif

	#ifdef BLEND_REFLECTION
		o.Emission += texCUBE(_Reflection, WorldReflectionVector(IN, o.Normal)).rgb * specData.a;
	#endif
}