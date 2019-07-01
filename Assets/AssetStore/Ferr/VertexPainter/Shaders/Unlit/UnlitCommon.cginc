

struct appdata_ferr {
    float4 vertex   : POSITION;
#ifndef BLEND_WORLDUV
	float3 normal   : NORMAL;
    half2  texcoord : TEXCOORD0;
#endif
    fixed4 color    : COLOR;
};
			
struct unlit_vert {
	float4 position : SV_POSITION;
#ifndef BLEND_SOLIDCOLOR
	half2  uv_MainTex       : TEXCOORD0;
	#if BLEND_TEX > 1
		float2 uv_MainTex2 : TEXCOORD1;
	#endif
	#if BLEND_TEX > 2
		float2 uv_MainTex3 : TEXCOORD2;
	#endif
	#if BLEND_TEX > 3
		float2 uv_MainTex4 : TEXCOORD3;
	#endif
#endif
	fixed4 color    : COLOR;
	UNITY_FOG_COORDS(4)
};

unlit_vert UnlitVert (appdata_full aInput)
{
	unlit_vert result;
	result.position = UnityObjectToClipPos (aInput.vertex);

	result.color = FixChannels(aInput.color);
	#ifndef BLEND_SOLIDCOLOR
		float2 uv;
		#ifdef BLEND_WORLDUV
			uv = WorldUVSimple(aInput.vertex.xyz);
		#else
			uv = aInput.texcoord;
		#endif
		UVTRANSFORM(uv, result);
	#endif

	UNITY_TRANSFER_FOG(result, result.position);
	return result;
}

fixed4 UnlitFrag (unlit_vert aInput) : COLOR
{
	#ifdef BLEND_SOLIDCOLOR
		fixed4 color = BlendGetColorSolid(aInput.color);
	#else
		fixed4 color = BlendGetColor(UVVALS(aInput), aInput.color);
	#endif
	UNITY_APPLY_FOG(aInput.fogCoord, color);

	return color;
}