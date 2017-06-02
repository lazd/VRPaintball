Shader "Decal/Unlit"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_NormalCutoff("Normal Cutoff", Range(0.0, 1.0)) = 0.5

		_MaskBase("Mask Base", Range(0.0, 1.0)) = 0.0
		_MaskLayers("Layers", Color) = (0.5, 0.5, 0.5, 0.5)
	}

	//Base Pass
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front
		LOD 100

		//Forward
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase"}
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForward

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(ProjectionInput i) : SV_Target
			{
				//Generate base data
				FragmentCommonData fragment = FragmentUnlit(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

				//Clip pixels
				ClipPixels(fragment.occlusion);

				//Grab out color
				half3 c = fragment.diffColor;

				//Apply Fog
				UNITY_APPLY_FOG(i.fogCoord, c);
				return Output(c, fragment.occlusion);
			}
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
			#pragma fragment fragDeferred

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile ___ DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

			#include "Cginc/DeferredProjections.cginc"

			void fragDeferred(ProjectionInput i, out half4 outAlbedo : SV_Target, out half4 outSmoothSpec : SV_Target1, out half4 outEmission : SV_Target2)
			{
				//Generate base data
				FragmentCommonData fragment = FragmentUnlit(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

				//Albedo output
				outAlbedo = AlbedoOutput(fragment.diffColor, fragment.occlusion, fragment.screenPos);
				
				//Specsmooth output
				half4 s = half4(fragment.specColor, fragment.oneMinusRoughness);
				outSmoothSpec = SpecSmoothOutput(s, fragment.occlusion, fragment.screenPos);
				
				//Emission output
				outEmission = EmissionOutput(half4(fragment.diffColor,1), fragment.occlusion, fragment.screenPos);
			}
			ENDCG
		}
	}

	//Forward Forced
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front

		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase"}
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma glsl

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertProjection
			#pragma fragment fragForward

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(ProjectionInput i) : SV_Target
			{
				//Generate base data
				FragmentCommonData fragment = FragmentUnlit(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

				//Clip pixels
				ClipPixels(fragment.occlusion);

				//Grab out color
				half3 c = fragment.diffColor;

				//Apply Fog
				UNITY_APPLY_FOG(i.fogCoord, c);
				return Output(c, fragment.occlusion);
			}
			ENDCG
		}
	}

	Fallback Off
}