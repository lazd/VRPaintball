Shader "Decal/OmniDecal"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

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

			#pragma shader_feature _ALPHATEST_ON
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertOmniDecal
			#pragma fragment fragForward

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(OmniDecalInput i) : SV_Target
			{
				//Calculate screenspace position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth
				float depth;
				DepthNormal(screenPos, depth);

				//Calculate world position - Perspective
				//float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate world position - Orthagraphic
				//float3 posWorld = CalculateOrthographicWorldPos(screenPos, depth);

				//Calculate world position - Blended
				float3 posWorld = (UNITY_MATRIX_P[3][3] * CalculateOrthographicWorldPos(screenPos, depth)) + ((1 - UNITY_MATRIX_P[3][3]) * CalculatePerspectiveWorldPos(i.ray, depth));

				//Calculate local uvs
				float2 localUVs = OmniDecalUVs(posWorld);

				//Calculate our occlusion
				half occlusion = AlbedoOcclusion(localUVs);

				//Clip Pixels
				ClipPixels(occlusion);
				ClipMasking(screenPos);

				half3 c = Albedo(localUVs);

				UNITY_APPLY_FOG(i.fogCoord, c);
				return Output(c, occlusion);
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

			#pragma vertex vertOmniDecal
			#pragma fragment fragDeferred

			#pragma shader_feature _ALPHATEST_ON
			#pragma multi_compile ___ UNITY_HDR_ON

			#include "Cginc/DeferredProjections.cginc"

			void fragDeferred(OmniDecalInput i, out half4 outAlbedo : SV_Target, out half4 outSmoothSpec : SV_Target1, out half4 outEmission : SV_Target2)
			{
				//Calculate screenspace position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate Depth
				float depth = Depth(screenPos);

				//Calculate world position
				float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate local uvs
				float2 localUVs = OmniDecalUVs(posWorld);

				//Calculate our occlusion
				half occlusion = AlbedoOcclusion(localUVs);

				//Clip Pixels
				ClipPixels(occlusion);
				ClipMasking(screenPos);

				half3 c = Albedo(localUVs);

				//Albedo output
				outAlbedo = AlbedoOutput(c, occlusion, screenPos);
				//Specsmooth output
				half4 s = half4(0,0,0,0);
				outSmoothSpec = SpecSmoothOutput(s, occlusion, screenPos);
				//Emission output
				outEmission = EmissionOutput(half4(c, 1), occlusion, screenPos);
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

			#pragma shader_feature _ALPHATEST_ON
			#pragma multi_compile _LowPrecision _HighPrecision

			#pragma vertex vertOmniDecal
			#pragma fragment fragForward

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(OmniDecalInput i) : SV_Target
			{
				//Calculate screenspace position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth
				float depth;
				DepthNormal(screenPos, depth);

				//Calculate world position - Perspective
				//float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate world position - Orthagraphic
				//float3 posWorld = CalculateOrthographicWorldPos(screenPos, depth);

				//Calculate world position - Blended
				float3 posWorld = (UNITY_MATRIX_P[3][3] * CalculateOrthographicWorldPos(screenPos, depth)) + ((1 - UNITY_MATRIX_P[3][3]) * CalculatePerspectiveWorldPos(i.ray, depth));

				//Calculate local uvs
				float2 localUVs = OmniDecalUVs(posWorld);

				//Calculate our occlusion
				half occlusion = AlbedoOcclusion(localUVs);

				//Clip Pixels
				ClipPixels(occlusion);
				ClipMasking(screenPos);

				half3 c = Albedo(localUVs);

				UNITY_APPLY_FOG(i.fogCoord, c);
				return Output(c, occlusion);
			}
			ENDCG
		}
	}
	Fallback Off
}