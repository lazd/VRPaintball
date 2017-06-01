// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#include "UnityStandardInput.cginc"

//Render Buffers
sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
sampler2D _CameraGBufferTexture3;

//Vertex Input
struct VertInput
{
	float4 vertex : POSITION;
};

//Fragment Input
struct PrePassInput
{
	float4 vertex : SV_POSITION;
	float4 screenPos : TEXCOORD0;
};

//Vertex to Fragment Program
PrePassInput vertPrePass(VertInput v)
{
	PrePassInput o;
	o.vertex = UnityObjectToClipPos(float4(v.vertex));
	o.screenPos = ComputeScreenPos(o.vertex);
	return o;
}
