Shader "Decal/Metallic"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_NormalCutoff("Normal Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Normal Strength", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpFlip("Invert Normals", Range(0.0, 1.0)) = 0.0

		_EmissionColor("Emission Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_MaskBase("Mask Base", Range(0.0, 1.0)) = 0.0
		_MaskLayers("Layers", Color) = (0.5, 0.5, 0.5, 0.5)
	}

	//3.0
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front
		LOD 100

		//Forward Base
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 3.0
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}

		//Forward Add
		Pass
		{
			Name "FORWARD_ADD"
			Tags{ "LightMode" = "ForwardAdd" }

			Blend One One
			Fog{ Color(0,0,0,0) } // in additive pass fog should be black

			CGPROGRAM
			#pragma target 3.0
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardAddMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}

		//Deferred
		Pass
		{
			Name "DEFERRED"
			Tags{ "LightMode" = "Deferred" }
			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt
			#pragma glsl

			#pragma vertex vertProjection
			#pragma fragment fragMetallic

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile ___ DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

			#include "Cginc/DeferredProjections.cginc"

			void fragMetallic(ProjectionInput i, out half4 outAlbedo : SV_Target, out half4 outSmoothSpec : SV_Target1, out half4 outNormal : SV_Target2, out half4 outEmission : SV_Target3)
				{
					//Generate base data
					FragmentCommonData fragment = FragmentMetallic(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

					//Calculate ambient & reflections
					half3 a = Ambient(fragment);

					//Emission
					a += EmissionAlpha(fragment.localUV);

					//Albedo output
					half3 c = fragment.diffColor;
					outAlbedo = AlbedoOutput(c, fragment.occlusion, fragment.screenPos);
					//Specsmooth output
					half4 s = half4(fragment.specColor, fragment.oneMinusRoughness);
					outSmoothSpec = SpecSmoothOutput(s, fragment.occlusion, fragment.screenPos);
					//Normal output
					half4 n = half4(fragment.normalWorld, 1.0);
					outNormal = NormalOutput(n, fragment.occlusion, fragment.screenPos);
					//Emission output
					outEmission = EmissionOutput(half4(a, 1.0), fragment.occlusion, fragment.screenPos);
				}
			ENDCG
		}
	}

	//3.0 Forward Forced
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front

		//Forward Base
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 3.0
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}

		//Forward Add
		Pass
		{
			Name "FORWARD_ADD"
			Tags{ "LightMode" = "ForwardAdd" }

			Blend One One
			Fog{ Color(0,0,0,0) } // in additive pass fog should be black

			CGPROGRAM
			#pragma target 3.0
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardAddMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}
	}

	//2.0
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front

		//Forward Base
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}

		//Forward Add
		Pass
		{
			Name "FORWARD_ADD"
			Tags{ "LightMode" = "ForwardAdd" }

			Blend One One
			Fog{ Color(0,0,0,0) } // in additive pass fog should be black

			CGPROGRAM
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForwardAddMetallic

			#include "Cginc/ForwardProjections.cginc"
			ENDCG
		}
	}
	Fallback Off
}