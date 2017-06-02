Shader "Decal/Internal/Blit" 
{
	Properties
	{
	}
	SubShader
	{
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragA

			#include "PrePass.cginc"
			void fragA(PrePassInput i, out half4 outAlbedo : SV_Target, out half4 outAmbient : SV_Target1)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outAlbedo = tex2D(_CameraGBufferTexture0, screenPos);
				outAmbient = tex2D(_CameraGBufferTexture3, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragG

			#include "PrePass.cginc"
			void fragG(PrePassInput i, out half4 outGloss : SV_Target)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outGloss = tex2D(_CameraGBufferTexture1, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragN

			#include "PrePass.cginc"
			void fragN(PrePassInput i, out half4 outNormal : SV_Target)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outNormal = tex2D(_CameraGBufferTexture2, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragAG

			#include "PrePass.cginc"
			void fragAG(PrePassInput i, out half4 outAlbedo : SV_Target, out half4 outGloss : SV_Target1, out half4 outAmbient : SV_Target2)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outAlbedo = tex2D(_CameraGBufferTexture0, screenPos);
				outGloss = tex2D(_CameraGBufferTexture1, screenPos);
				outAmbient = tex2D(_CameraGBufferTexture3, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragAN

			#include "PrePass.cginc"
			void fragAN(PrePassInput i, out half4 outAlbedo : SV_Target, out half4 outNormal : SV_Target1, out half4 outAmbient : SV_Target2)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outAlbedo = tex2D(_CameraGBufferTexture0, screenPos);
				outNormal = tex2D(_CameraGBufferTexture2, screenPos);
				outAmbient = tex2D(_CameraGBufferTexture3, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragGN

			#include "PrePass.cginc"
			void fragGN(PrePassInput i, out half4 outGloss : SV_Target, out half4 outNormal : SV_Target1)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outGloss = tex2D(_CameraGBufferTexture1, screenPos);
				outNormal = tex2D(_CameraGBufferTexture2, screenPos);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertPrePass
			#pragma fragment fragAGN

			#include "PrePass.cginc"
			void fragAGN(PrePassInput i, out half4 outAlbedo : SV_Target, out half4 outGloss : SV_Target1, out half4 outNormal : SV_Target2, out half4 outAmbient : SV_Target3)
			{
				//Calculate screen space position
				float2 screenPos = i.screenPos.xy / i.screenPos.w;

				//Output
				outAlbedo = tex2D(_CameraGBufferTexture0, screenPos);
				outGloss = tex2D(_CameraGBufferTexture1, screenPos);
				outNormal = tex2D(_CameraGBufferTexture2, screenPos);
				outAmbient = tex2D(_CameraGBufferTexture3, screenPos);
			}
			ENDCG
		}
	}
	Fallback Off
}
