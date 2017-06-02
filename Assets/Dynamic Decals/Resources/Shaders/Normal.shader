Shader "Decal/Normal"
{
	Properties
	{
		_MainTex("Shape Map", 2D) = "white" {}
		_Multiplier("Multiplier", Range(0.0, 1.0)) = 1.0

		_BumpScale("Normal Strength", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpFlip("Invert Normals", Range(0.0, 1.0)) = 0.0

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_NormalCutoff("Normal Cutoff", Range(0.0, 1.0)) = 0.5

		_MaskBase("Mask Base", Range(0.0, 1.0)) = 0.0
		_MaskLayers("Layers", Color) = (0.5, 0.5, 0.5, 0.5)
	}
	SubShader
	{
		Tags{ "Queue" = "AlphaTest+1" "DisableBatching" = "True"  "IgnoreProjector" = "True" }
		ZWrite Off ZTest Always Cull Front

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

			void fragDeferred(ProjectionInput i, out half4 outNormal : SV_Target)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Calculate depth
				float depth = Depth(screenPos);

				//Calculate world position
				float3 posWorld = CalculatePerspectiveWorldPos(i.ray, depth);

				//Calculate local uvs
				float2 localUVs = ProjectionUVs(posWorld);

				//Occlusion
				half occlusion = ShapeOcclusion(localUVs);

				//Calculate normals
				float3x3 surface2WorldTranspose = Surface2WorldTranspose(i.worldUp, screenPos);
				half3 normalWorld = WorldNormal(localUVs, surface2WorldTranspose);

				//Clip Pixels
				ClipPixels(occlusion);
				ClipMasking(screenPos);
				ClipNormal(screenPos, i.worldForward);

				//Normal output
				half4 n = half4(normalWorld, 1.0);
				outNormal = NormalOutput(n, occlusion, screenPos);
			}
			ENDCG
		}
	}
	Fallback Off
}