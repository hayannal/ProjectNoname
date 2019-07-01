#ifdef BLEND_TEX_2
#define BLEND_TEX 2
#endif
#ifdef BLEND_TEX_3
#define BLEND_TEX 3
#endif
#ifdef BLEND_TEX_4
#define BLEND_TEX 4
#endif
#ifdef BLEND_HEIGHT
#define BLEND_HARD
#endif

#ifdef BLEND_SEPARATEOFFSETS
	#if BLEND_TEX > 3
		#define UVTRANSFORM(co, o) o.uv_MainTex = TRANSFORM_TEX(co, _MainTex); o.uv_MainTex2 = TRANSFORM_TEX(co, _MainTex2); o.uv_MainTex3 = TRANSFORM_TEX(co, _MainTex3); o.uv_MainTex4 = TRANSFORM_TEX(co, _MainTex4)
		#define UVARGS float2 aUV1, float2 aUV2, float2 aUV3, float2 aUV4
		#define UVVALS(x) x.uv_MainTex, x.uv_MainTex2, x.uv_MainTex3, x.uv_MainTex4
	#elif BLEND_TEX > 2
		#define UVTRANSFORM(co, o) o.uv_MainTex = TRANSFORM_TEX(co, _MainTex); o.uv_MainTex2 = TRANSFORM_TEX(co, _MainTex2); o.uv_MainTex3 = TRANSFORM_TEX(co, _MainTex3)
		#define UVARGS float2 aUV1, float2 aUV2, float2 aUV3
		#define UVVALS(x) x.uv_MainTex, x.uv_MainTex2, x.uv_MainTex3
	#elif BLEND_TEX > 1
		#define UVTRANSFORM(co, o) o.uv_MainTex = TRANSFORM_TEX(co, _MainTex); o.uv_MainTex2 = TRANSFORM_TEX(co, _MainTex2)
		#define UVARGS float2 aUV1, float2 aUV2
		#define UVVALS(x) x.uv_MainTex, x.uv_MainTex2
	#elif BLEND_TEX > 0
		#define UVTRANSFORM(co, o) o.uv_MainTex = TRANSFORM_TEX(co, _MainTex)
		#define UVARGS float2 aUV1
		#define UVVALS(x) x.uv_MainTex
	#endif
#else
	#define UVARGS float2 aUV
	#define UVVALS(x) x.uv_MainTex
	#define aUV1 aUV
	#define aUV2 aUV
	#define aUV3 aUV
	#define aUV4 aUV
#endif

#define blend_height_multiplier 0.5
float _BlendStrength;

#ifdef BLEND_SOLIDCOLOR
	float4 _Color;
	#if BLEND_TEX > 1
		float4 _Color2;
	#endif
	#if BLEND_TEX > 2
		float4 _Color3;
	#endif
	#if BLEND_TEX > 3
		float4 _Color4;
	#endif
#else
	sampler2D _MainTex;
	#if BLEND_TEX > 1
		sampler2D _MainTex2;
	#endif
	#if BLEND_TEX > 2
		sampler2D _MainTex3;
	#endif
	#if BLEND_TEX > 3
		sampler2D _MainTex4;
	#endif

	#if defined(BLEND_SEPARATEOFFSETS) && !defined(BLEND_SURFACE)
		float4 _MainTex_ST;
		#if BLEND_TEX > 1
			float4 _MainTex2_ST;
		#endif
		#if BLEND_TEX > 2
			float4 _MainTex3_ST;
		#endif
		#if BLEND_TEX > 3
			float4 _MainTex4_ST;
		#endif
	#endif
#endif

#if defined(BLEND_NORMAL)
	#ifdef BLEND_NORMAL
		sampler2D _BumpMap;
	#endif
	
	#if BLEND_TEX > 1
		sampler2D _BumpMap2;
	#endif
	#if BLEND_TEX > 2
		sampler2D _BumpMap3;
	#endif
	#if BLEND_TEX > 3
		sampler2D _BumpMap4;
	#endif
#endif

#if defined(BLEND_SPECULAR)
	float     _Shininess;
	sampler2D _SpecTex;
	
	#if BLEND_TEX > 1
		sampler2D _SpecTex2;
	#endif
	#if BLEND_TEX > 2
		sampler2D _SpecTex3;
	#endif
	#if BLEND_TEX > 3
		sampler2D _SpecTex4;
	#endif
#endif

#ifdef BLEND_REFLECTION
	samplerCUBE _Reflection;
#endif

#ifdef BLEND_CRISPNESS
	float _Crispness;
#endif

float2 WorldUVOffset(float3 aObjNormal, float3 aObjPos, float2 aOffset) {
	float2 result;
	float3 worldNormal = abs(normalize(mul(unity_ObjectToWorld, float4(aObjNormal,0))));
	float3 worldPos    =               mul(unity_ObjectToWorld, float4(aObjPos,   1));
	result = float2((worldNormal.x*worldPos.z) + 
					(worldNormal.y*worldPos.x) +
					(worldNormal.z*worldPos.x),
								 
					(worldNormal.x*worldPos.y) + 
					(worldNormal.y*worldPos.z) +
					(worldNormal.z*worldPos.y));
	result += aOffset;
	return result;
}

float2 WorldUV(float3 aObjNormal, float3 aObjPos) {
	float2 result;
	float3 worldNormal = abs(normalize(mul(unity_ObjectToWorld, float4(aObjNormal,0))));
	float3 worldPos    =               mul(unity_ObjectToWorld, float4(aObjPos,   1));
	result = float2((worldNormal.x*worldPos.z) + 
					(worldNormal.y*worldPos.x) +
					(worldNormal.z*worldPos.x),
								 
					(worldNormal.x*worldPos.y) + 
					(worldNormal.y*worldPos.z) +
					(worldNormal.z*worldPos.y));
	return result;
}
float2 WorldUVSimple(float3 aObjPos) {
	return mul(unity_ObjectToWorld, float4(aObjPos, 1)).xz;
}

float2 WorldUVWorld(float3 aWorldNormal, float3 aWorldPos) {
	float2 result;
	result = float2((aWorldNormal.x*aWorldPos.z) + 
					(aWorldNormal.y*aWorldPos.x) + 
					(aWorldNormal.z*aWorldPos.x),
								 
					(aWorldNormal.x*aWorldPos.y) + 
					(aWorldNormal.y*aWorldPos.z) +
					(aWorldNormal.z*aWorldPos.y));
	return result;
}

fixed vecMax(fixed4 aVal) {
	fixed m = max(aVal.x, aVal.y);
	      m = max(m,      aVal.z);
	      m = max(m,      aVal.w);
	return m;
}

fixed vecMax(fixed3 aVal) {
	fixed m = max(aVal.x, aVal.y);
	      m = max(m,      aVal.z);
	return m;
}

fixed vecMax(fixed2 aVal) {
	return max(aVal.x, aVal.y);
}

fixed4 FixChannels(fixed4 aChannels) {
#ifdef BLEND_FIXCHANNELS
	return aChannels / (aChannels.x + aChannels.y + aChannels.z + aChannels.w);
#else
	return aChannels;
#endif
}

/*fixed4 HardBlendChannels(fixed4 aChannels) {
	#if   BLEND_TEX == 4
		return aChannels >= vecMax(aChannels    );
	#elif BLEND_TEX == 3
		return aChannels >= vecMax(aChannels.xyz);
	#elif BLEND_TEX == 2
		return aChannels >= vecMax(aChannels.xy );
	#endif
}*/

//float4 heightblend(float4 input1, float height1, float4 input2, float height2, float4 input3, float height3, float4 input4, float height4)
float4 heightblend(fixed4 aChannels) {
	fixed floor = vecMax(aChannels) - 0.05;
	fixed b1 = max(aChannels.r - floor, 0);
	fixed b2 = max(aChannels.g - floor, 0);
	fixed b3 = max(aChannels.b - floor, 0);
	fixed b4 = max(aChannels.a - floor, 0);
	return fixed4(b1, b2, b3, b4) / (b1 + b2 + b3 + b4);
}

fixed4 HardBlendChannels(fixed4 aChannels) {
	#if   BLEND_TEX == 4
		fixed floor = vecMax(aChannels) - _BlendStrength;
	#elif BLEND_TEX == 3
		fixed floor = vecMax(aChannels) - _BlendStrength;
	#elif BLEND_TEX == 2
		fixed floor = vecMax(aChannels) - _BlendStrength;
	#endif
	fixed b1 = max(aChannels.r - floor, 0);
	fixed b2 = max(aChannels.g - floor, 0);
	fixed b3 = max(aChannels.b - floor, 0);
	fixed b4 = max(aChannels.a - floor, 0);
	return fixed4(b1, b2, b3, b4) / (b1 + b2 + b3 + b4);
}

fixed4 Blend2(fixed4 aChannels, fixed4 aCol1, fixed4 aCol2) {
	return aChannels.r * aCol1 + aChannels.g * aCol2;
}
half4 Blend2Half(fixed4 aChannels, half4 aCol1, half4 aCol2) {
	return aChannels.r * aCol1 + aChannels.g * aCol2;
}
float3 Blend2Norm(half4 aChannels, float3 aNorm1, float3 aNorm2) {
	return aChannels.r * aNorm1 + aChannels.g * aNorm2;
}


fixed4 Blend3(fixed4 aChannels, fixed4 aCol1, fixed4 aCol2, fixed4 aCol3) {
	return aChannels.r * aCol1 + aChannels.g * aCol2 + aChannels.b * aCol3;
}
half4 Blend3Half(fixed4 aChannels, half4 aCol1, half4 aCol2, half4 aCol3) {
	return aChannels.r * aCol1 + aChannels.g * aCol2 + aChannels.b * aCol3;
}
float3 Blend3Norm(fixed4 aChannels, float3 aNorm1, float3 aNorm2, float3 aNorm3) {
	return aChannels.r * aNorm1 + aChannels.g * aNorm2 + aChannels.b * aNorm3;
}


fixed4 Blend4(fixed4 aChannels, fixed4 aCol1, fixed4 aCol2, fixed4 aCol3, fixed4 aCol4) {
	return aChannels.r * aCol1 + aChannels.g * aCol2 + aChannels.b * aCol3 + aChannels.a * aCol4;
}
fixed4 Blend4Half(fixed4 aChannels, half4 aCol1, half4 aCol2, half4 aCol3, half4 aCol4) {
	return aChannels.r * aCol1 + aChannels.g * aCol2 + aChannels.b * aCol3 + aChannels.a * aCol4;
}
float3 Blend4Norm(fixed4 aChannels, float3 aNorm1, float3 aNorm2, float3 aNorm3, float3 aNorm4) {
	return aChannels.r * aNorm1 + aChannels.g * aNorm2 + aChannels.b * aNorm3 + aChannels.a * aNorm4;
}

fixed4 BlendGetColor(UVARGS, inout fixed4 aVertexColor) {
	#ifndef BLEND_SOLIDCOLOR
		fixed4      col1 = tex2D (_MainTex,  aUV1);
		#if BLEND_TEX > 1
			fixed4  col2 = tex2D (_MainTex2, aUV2);
		#endif
		#if BLEND_TEX > 2
			fixed4  col3 = tex2D (_MainTex3, aUV3);
		#endif
		#if BLEND_TEX > 3
			fixed4  col4 = tex2D (_MainTex4, aUV4);
		#endif
	#endif

	#ifdef BLEND_HEIGHT
		fixed4 height;
		#if BLEND_TEX == 4
			height = float4(col1.a, col2.a, col3.a, col4.a);
		#elif BLEND_TEX == 3
			height = float4(col1.a, col2.a, col3.a, 0);
		#elif BLEND_TEX == 2
			height = float4(col1.a, col2.a, 0, 0);
		#elif BLEND_TEX == 1
			height.r = col1.a;
		#endif
		aVertexColor += height * blend_height_multiplier;
	#endif
	
	#ifdef BLEND_HARD
	aVertexColor = HardBlendChannels(aVertexColor);
	#endif
	aVertexColor = FixChannels(aVertexColor);
	
	#ifdef BLEND_SOLIDCOLOR
		#if   BLEND_TEX > 3
			fixed4 col1 = Blend4(aVertexColor, _Color, _Color2, _Color3, _Color4);
		#elif BLEND_TEX > 2
			fixed4 col1 = Blend3(aVertexColor, _Color, _Color2, _Color3);
		#elif BLEND_TEX > 1
			fixed4 col1 = Blend2(aVertexColor, _Color, _Color2);
		#endif
	#else
		#if   BLEND_TEX > 3
			col1 = Blend4(aVertexColor, col1, col2, col3, col4);
		#elif BLEND_TEX > 2
			col1 = Blend3(aVertexColor, col1, col2, col3);
		#elif BLEND_TEX > 1
			col1 = Blend2(aVertexColor, col1, col2);
		#endif
	#endif
	
	return col1;
}

#ifdef BLEND_SOLIDCOLOR
fixed4 BlendGetColorSolid(inout fixed4 aVertexColor) {
	#ifdef BLEND_HARD
		aVertexColor = HardBlendChannels(aVertexColor);
	#endif
	aVertexColor = FixChannels(aVertexColor);
	
	#if   BLEND_TEX > 3
		fixed4 col1 = Blend4(aVertexColor, _Color, _Color2, _Color3, _Color4);
	#elif BLEND_TEX > 2
		fixed4 col1 = Blend3(aVertexColor, _Color, _Color2, _Color3);
	#elif BLEND_TEX > 1
		fixed4 col1 = Blend2(aVertexColor, _Color, _Color2);
	#endif
	
	return col1;
}
#endif

#ifdef BLEND_NORMAL
float3 BlendGetNormal(UVARGS, fixed4 aChannels) {
	float3     norm  = UnpackNormal(tex2D( _BumpMap,  aUV1 ));
	#if BLEND_TEX > 1
		float3 norm2 = UnpackNormal(tex2D( _BumpMap2, aUV2 ));
	#endif
	#if BLEND_TEX > 2
		float3 norm3 = UnpackNormal(tex2D( _BumpMap3, aUV3 ));
	#endif
	#if BLEND_TEX > 3
		float3 norm4 = UnpackNormal(tex2D( _BumpMap4, aUV4 ));
	#endif
	
	#if   BLEND_TEX > 3
		norm = Blend4Norm(aChannels, norm, norm2, norm3, norm4);
	#elif BLEND_TEX > 2
		norm = Blend3Norm(aChannels, norm, norm2, norm3);
	#elif BLEND_TEX > 1
		norm = Blend2Norm(aChannels, norm, norm2);
	#endif
	
	return norm;
}
#endif

#ifdef BLEND_SPECULAR
half4 BlendGetSpecular(UVARGS, fixed4 aChannels) {
	half4     spec  = tex2D( _SpecTex,  aUV1 );
	#if BLEND_TEX > 1
		half4 spec2 = tex2D( _SpecTex2, aUV2 );
	#endif
	#if BLEND_TEX > 2
		half4 spec3 = tex2D( _SpecTex3, aUV3 );
	#endif
	#if BLEND_TEX > 3
		half4 spec4 = tex2D( _SpecTex4, aUV4 );
	#endif
	
	#if   BLEND_TEX > 3
		spec = Blend4Half(aChannels, spec, spec2, spec3, spec4);
	#elif BLEND_TEX > 2
		spec = Blend3Half(aChannels, spec, spec2, spec3);
	#elif BLEND_TEX > 1
		spec = Blend2Half(aChannels, spec, spec2);
	#endif
			
	return spec;
}
#endif