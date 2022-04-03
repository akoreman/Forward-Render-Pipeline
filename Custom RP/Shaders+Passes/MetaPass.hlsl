#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../Auxiliary/Common.hlsl"
#include "../Auxiliary/Surface.hlsl"
#include "../Auxiliary/Shadows.hlsl"
#include "../Auxiliary/Light.hlsl"
#include "../Auxiliary/BRDF.hlsl"

// Pass to determine what colour the GI should have at a given clip space position.

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct FragmentInput
{
	float3 positionOS : POSITION;
	float2 coordsUV : TEXCOORD0;
	float2 lightMapUV : TEXCOORD1;
};

struct FragmentOutput 
{
	float4 positionCS : SV_POSITION;
	float2 coordsUV : VAR_BASE_UV;
};

FragmentOutput MetaPassVertex(FragmentInput input)
{
    FragmentOutput output;
	input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS = TransformWorldToHClip(input.positionOS);
	output.coordsUV = input.coordsUV;

	return output;
}

float4 MetaPassFragment(FragmentOutput input) : SV_TARGET
{
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.coordsUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 base = baseMap * baseColor;

	Surface surface;

	ZERO_INITIALIZE(Surface, surface);

	surface.color = base.rgb;
	surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
	surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
	BRDF brdf = GetBRDF(surface);
	float4 meta = 0.0;

	if (unity_MetaFragmentControl.x) {
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5;
		meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
	}


	return meta;
}

#endif