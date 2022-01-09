Shader "Custom RP/Unlit"
{
    Properties {
		_baseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_alphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_Texture("Texture", 2D) = "white" {}

		[Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
	}
	
	SubShader {
		Blend [_SrcBlend] [_DstBlend]
		ZWrite [_ZWrite]

		Pass { 
			Tags {
				"LightMode" = "Unlit"
			}

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma target 3.5
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment

			#pragma shader_feature _CLIPPING

			#include "UnlitPass.hlsl"
			ENDHLSL
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL

		}


	}
}
