// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Decal/Internal/Mask" {
	Properties
	{
		_Layer1("Layer1", Range(0,1)) = 0
		_Layer2("Layer2", Range(0,1)) = 0
		_Layer3("Layer3", Range(0,1)) = 0
		_Layer4("Layer4", Range(0,1)) = 0
	}
	SubShader
	{
		ZWrite Off
		ZTest LEqual
		Cull Off
		Offset -1,-1

		Pass
		{
			CGPROGRAM
			#pragma exclude_renderers nomrt

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityStandardInput.cginc"

			half _Layer1, _Layer2, _Layer3, _Layer4;

			struct MaskInput
			{
				float4 pos : SV_POSITION;
			};

			MaskInput vert(VertexInput v)
			{
				MaskInput o;
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
				return o;
			}

			fixed4 frag(MaskInput i) : SV_Target
			{
				return half4(_Layer1, _Layer2, _Layer3, _Layer4);
			}
			ENDCG
		}
	}
}