Shader "Decal/Roughness"
{
	Properties
	{
		_MainTex("Shape Map", 2D) = "white" {}
		_Multiplier("Multiplier", Range(0.0, 1.0)) = 1.0

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossTex("Roughness Map", 2D) = "white" {}

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

			void fragDeferred(ProjectionInput i, out half4 outSmoothSpec : SV_Target)
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

				//Clip
				ClipPixels(occlusion);
				ClipMasking(screenPos);
				ClipNormal(screenPos, i.worldForward);

				//Specsmooth output
				outSmoothSpec = RoughnessOutput(localUVs, screenPos, occlusion);
			}
			ENDCG
		}
	}
	Fallback Off
}