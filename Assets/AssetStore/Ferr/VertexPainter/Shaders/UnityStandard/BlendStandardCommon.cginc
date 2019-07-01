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
};

#ifdef BLEND_METALLIC
half   _Metallic;
#endif
half   _Smoothness;

void vert (inout appdata_full v) {
	#ifdef BLEND_WORLDUV
		v.texcoord.xy = WorldUVSimple(v.vertex); 
		v.tangent = mul(unity_WorldToObject, float4(1, 0, 0, -1));
	#endif
}

#ifdef BLEND_METALLIC
void surf (Input IN, inout SurfaceOutputStandard o) {
#else
void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
#endif
	fixed4 c = BlendGetColor(UVVALS(IN), IN.color);
	o.Albedo     = c.rgb;
	o.Alpha      = c.a;
	
	#ifdef BLEND_NORMAL
		o.Normal = BlendGetNormal(UVVALS(IN), IN.color);
	#endif
	
	fixed4 specData = BlendGetSpecular(UVVALS(IN), IN.color);
	#ifdef BLEND_METALLIC
		#ifdef BLEND_METAL_MULTICOMPONENT
			o.Metallic   = specData.r * _Metallic;
			o.Smoothness = specData.a * _Smoothness;
		#else
			o.Metallic   = specData.r * _Metallic;
			o.Smoothness = specData.r * _Smoothness;
		#endif
	#else
		#ifdef BLEND_METAL_MULTICOMPONENT
			o.Specular   = specData.rgb * _SpecColor;
			o.Smoothness = specData.a   * _Smoothness;
		#else
			o.Specular   = specData.rgb * _SpecColor;
			o.Smoothness = specData.r   * _Smoothness;
		#endif
	#endif
}