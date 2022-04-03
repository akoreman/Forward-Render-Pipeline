#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../Auxiliary/Common.hlsl"
#include "../Auxiliary/Surface.hlsl"
#include "../Auxiliary/Shadows.hlsl"
#include "../Auxiliary/Light.hlsl"
#include "../Auxiliary/BRDF.hlsl"
#include "../Auxiliary/GlobalIllumination.hlsl"
#include "../Auxiliary/Lighting.hlsl"

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
	#define GI_ATTRIBUTE_DATA
	#define GI_VARYINGS_DATA
	#define TRANSFER_GI_DATA(input, output)
	#define GI_FRAGMENT_DATA(input) 0.0
#endif

TEXTURE2D(_BaseMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);
SAMPLER(sampler_EmissionMap);

TEXTURE2D(_NormalMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct FragmentInput
{
	float3 positionObjectSpace : POSITION;
	float2 coordsUV : TEXCOORD0;
	float3 normalObjectSpace : NORMAL;
	float4 tangentOS : TANGENT;
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FragmentOutput 
{
	float4 positionClipSpace : SV_POSITION;
	float3 positionWorldSpace : VAR_POSITION;
	float2 coordsUV : VAR_BASE_UV;
	float3 normalWorldSpace : VAR_NORMAL;
	float4 tangentWS : VAR_TANGENT;
	GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float3 GetNormalTS (float2 coordsUV) {
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, coordsUV);
	float scale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);
	float3 normal = DecodeNormal(map, scale);
	return normal;
}

FragmentOutput LitPassVertex(FragmentInput input)
{
	//Setup output struct and transfer the instance IDs.
    FragmentOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	TRANSFER_GI_DATA(input, output);

	output.positionWorldSpace = TransformObjectToWorld(input.positionObjectSpace);
	output.positionClipSpace = TransformWorldToHClip(output.positionWorldSpace);
	output.normalWorldSpace = TransformObjectToWorldNormal(input.normalObjectSpace);

	output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

	output.coordsUV = input.coordsUV;
	

	return output;
}

float4 LitPassFragment(FragmentOutput input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.coordsUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 base = baseMap * baseColor;

	#if defined(_CLIPPING)
		clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#endif

	Surface surface;
	surface.position = input.positionWorldSpace;

	surface.normal = NormalTangentToWorld(GetNormalTS(input.coordsUV), input.normalWorldSpace, input.tangentWS);
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
	surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWorldSpace);
	surface.depth = -TransformWorldToView(input.positionWorldSpace).z;
	surface.interpolatedNormal = input.normalWorldSpace;

	#if defined(_PREMULTIPLY_ALPHA)
		BRDF brdf = GetBRDF(surface, true);
	#else
		BRDF brdf = GetBRDF(surface);
	#endif

	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	float3 color = GetLighting(surface, brdf, gi);
    color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.coordsUV) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
	
	return float4(color, surface.alpha);
}

#endif