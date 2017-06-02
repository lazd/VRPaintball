Shader "Decal/Eraser/Read"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Multiplier("Multiplier", Range(0.0, 1.0)) = 1.0

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_NormalCutoff("Normal Cutoff", Range(0.0, 1.0)) = 0.5

		_MaskBase("Mask Base", Range(0.0, 1.0)) = 0.0
		_MaskLayers("Layers", Color) = (0.5, 0.5, 0.5, 0.5)
	}

	//Base Pass
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True"}
		ZWrite Off ZTest Always Cull Front
		LOD 100

		//Forward
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma glsl

			#pragma vertex vertProjection
			#pragma fragment fragForward

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(ProjectionInput i) : SV_Target
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth & normals
				float depth;
				float3 surfaceNormal = DepthNormal(screenPos, depth);

				//Calculate world position - Perspective
				//float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate world position - Orthagraphic
				//float3 posWorld = CalculateOrthographicWorldPos(screenPos, depth);

				//Calculate world position - Blended
				float3 posWorld = (UNITY_MATRIX_P[3][3] * CalculateOrthographicWorldPos(screenPos, depth)) + ((1 - UNITY_MATRIX_P[3][3]) * CalculatePerspectiveWorldPos(i.ray, depth));

				//Calculate local uvs
				float2 localUVs = ProjectionUVs(posWorld);

				//Occlusion
				half occlusion = ShapeOcclusion(localUVs);

				//Clip
				ClipPixels(occlusion);
				ClipMasking(screenPos);
				ClipNormal(surfaceNormal, i.worldForward);

				//Output
				return EraserOutput(screenPos, occlusion);
			}
			ENDCG
		}

		//Deferred
		Pass
		{
			Name "DEFERRED"
			Tags{ "LightMode" = "Deferred" }
			LOD 1000

			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt
			#pragma glsl

			#pragma vertex vertProjection
			#pragma fragment fragDeferred

			#pragma multi_compile _AlphaTest _Blend
			
			#include "Cginc/DeferredProjections.cginc"

			void fragDeferred(ProjectionInput i, out half4 outAlbedo : SV_Target, out half4 outGloss : SV_Target1, out half4 outNormal : SV_Target2, out half4 outAmbient : SV_Target3)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth
				float depth = Depth(screenPos);

				//Calculate world position - Perspective
				float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate local uvs
				float2 localUVs = ProjectionUVs(posWorld);

				//Occlusion
				half occlusion = ShapeOcclusion(localUVs);

				//Clip
				ClipPixels(occlusion);
				ClipMasking(screenPos);
				ClipNormal(screenPos, i.worldForward);

				//Output
				outAlbedo = EraserAlbedo(screenPos, occlusion);
				outGloss = EraserGloss(screenPos, occlusion);
				outNormal = EraserNormal(screenPos, occlusion);
				outAmbient = EraserAmbient(screenPos, occlusion);
			}
			ENDCG
		}
	}

	//Forward Forced
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front

		//Forward
		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma glsl

			#pragma vertex vertProjection
			#pragma fragment fragForward

			#pragma multi_compile _AlphaTest _Blend
			#pragma multi_compile _LowPrecision _HighPrecision

			#include "Cginc/ForwardProjections.cginc"

			half4 fragForward(ProjectionInput i) : SV_Target
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth & normals
				float depth;
				float3 surfaceNormal = DepthNormal(screenPos, depth);

				//Calculate world position - Perspective
				//float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate world position - Orthagraphic
				//float3 posWorld = CalculateOrthographicWorldPos(screenPos, depth);

				//Calculate world position - Blended
				float3 posWorld = (UNITY_MATRIX_P[3][3] * CalculateOrthographicWorldPos(screenPos, depth)) + ((1 - UNITY_MATRIX_P[3][3]) * CalculatePerspectiveWorldPos(i.ray, depth));

				//Calculate local uvs
				float2 localUVs = ProjectionUVs(posWorld);

				//Occlusion
				half occlusion = ShapeOcclusion(localUVs);

				//Clip
				ClipPixels(occlusion);
				ClipMasking(screenPos);
				ClipNormal(surfaceNormal, i.worldForward);

				//Output
				return EraserOutput(screenPos, occlusion);
			}
			ENDCG
		}
	}
}