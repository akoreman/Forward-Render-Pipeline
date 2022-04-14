#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"


#define UNITY_MATRIX_M unity_ObjectToWorld

#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#define UNITY_PREV_MATRIX_M unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

float Square(float input)
{
	return input * input;
}

float3 DecodeNormal (float4 sample, float scale) {
	#if defined(UNITY_NO_DXT5nm)
	    return UnpackNormalRGB(sample, scale);
	#else
	    return UnpackNormalmapRGorAG(sample, scale);
	#endif
}

float DistanceSquared(float3 vectorA, float3 vectorB)
{
    return dot(vectorA - vectorB, vectorA - vectorB);
}

float DistanceSquared(float4 vectorA, float4 vectorB)
{
    return dot(vectorA - vectorB, vectorA - vectorB);
}

float3 NormalTangentToWorld (float3 normalTangentSpace, float3 normalWorldSpace, float4 tangentWorldSpace) {
    float3x3 tangentToWorld = CreateTangentToWorld(normalWorldSpace, tangentWorldSpace.xyz, tangentWorldSpace.w);
	
    return TransformTangentToWorld(normalTangentSpace, tangentToWorld);
}

#endif