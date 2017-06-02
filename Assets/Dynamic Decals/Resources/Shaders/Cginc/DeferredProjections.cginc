// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#include "Cginc/Projections.cginc"

//Static buffers
sampler2D _StcAlbedo;
sampler2D _StcGloss;
sampler2D _StcNormal;
sampler2D _StcAmbient;

//Dynamic buffers 
sampler2D _DynAlbedo;
sampler2D _DynGloss;
sampler2D _DynNormal;
sampler2D _DynAmbient;

//Camera depth buffer
uniform sampler2D_float _CameraDepthTexture;

//Depth input
float Depth(float2 ScreenPos)
{
	//Sample depth texture
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ScreenPos);

	//Return Depth
	return Linear01Depth(depth);
}

//UnityLight
UnityLight DummyLight(half3 normalWorld)
{
	UnityLight l;
	l.color = 0;
	l.dir = half3 (0, 1, 0);
	l.ndotl = LambertTerm(normalWorld, l.dir);
	return l;
}

//Vertex program - Projection
struct ProjectionInput
{
	float4 pos : SV_POSITION;
	float4 screenPos : TEXCOORD0;
	float3 ray : TEXCOORD1;

	half3 worldForward : TEXCOORD2;
	half3 worldUp : TEXCOORD3;

	half3 eyeVec : TEXCOORD4;
};
ProjectionInput vertProjection(VertexInput v)
{
	ProjectionInput o;
	o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
	o.screenPos = ComputeScreenPos(o.pos);
	o.ray = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.eyeVec = posWorld.xyz - _WorldSpaceCameraPos;

	o.worldForward = mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1));	//WorldSpace Forward
	o.worldUp = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)); //WorldSpace Up (Now Right)
	return o;
}

//Vertex program - OmniDecal
struct OmniDecalInput
{
	float4 pos : SV_POSITION;
	float4 screenPos : TEXCOORD0;
	float3 ray : TEXCOORD1;
};
OmniDecalInput vertOmniDecal(VertexInput v)
{
	OmniDecalInput o;
	o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
	o.screenPos = ComputeScreenPos(o.pos);
	o.ray = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);
	return o;
}

//Surface2World
float3x3 Surface2WorldTranspose(float3 WorldUp, float2 ScreenPos)
{
	//Grab normal
	float3 normalWorld = normalize(tex2D(_StcNormal, ScreenPos) * 2 - 1);

	//Calculate bi-normal
	float3 binormalWorld = normalize(cross(WorldUp, normalWorld));

	//Calculate tangent
	float3 tangentWorld = normalize(cross(normalWorld, binormalWorld));

	//Object 2 World Matrix
	return float3x3(tangentWorld, binormalWorld, normalWorld);
}

//Clip
void ClipNormal(float2 screenPos, half3 worldForward)
{
	//Grab worldspace normal
	half3 surfaceNormal = normalize(tex2D(_StcNormal, screenPos) * 2 - 1);

	//Use dot product to determine angle & clip
	float d = dot(surfaceNormal, normalize(-worldForward));
	clip(d - _NormalCutoff);
}

//Fragment
inline FragmentCommonData FragmentUnlit(float4 i_screenPos, float3 i_ray, half3 i_worldForward, half3 i_worldUp, half3 i_eyeVec)
{
	FragmentCommonData o = (FragmentCommonData)0;

	//Calculate screenspace position
	o.screenPos = i_screenPos.xy / i_screenPos.w;

	//Calculate depth
	float depth = Depth(o.screenPos);

	//Calculate world position
	o.posWorld = CalculatePerspectiveWorldPos(i_ray, depth);

	//Calculate local uvs
	o.localUV = ProjectionUVs(o.posWorld);

	//Calculate normals
	float3x3 surface2WorldTranspose = Surface2WorldTranspose(i_worldUp, o.screenPos);
	o.normalWorld = WorldNormal(o.localUV, surface2WorldTranspose);

	//Occlusion
	o.occlusion = AlbedoOcclusion(o.localUV);

	//Clip Pixels
	ClipPixels(o.occlusion);
	ClipMasking(o.screenPos);
	ClipNormal(o.screenPos, i_worldForward);

	//Pass in eye vector
	o.eyeVec = normalize(i_eyeVec);

	//Set default fragment values
	o.diffColor = Albedo(o.localUV);
	o.specColor = half3(0, 0, 0);
	o.oneMinusReflectivity = 1;
	o.oneMinusRoughness = 0;
	return o;
}
inline FragmentCommonData FragmentSpecular(float4 i_screenPos, float3 i_ray, half3 i_worldForward, half3 i_worldUp, half3 i_eyeVec)
{
	//Grab base data
	FragmentCommonData o = FragmentUnlit(i_screenPos, i_ray, i_worldForward, i_worldUp, i_eyeVec);

	half4 specGloss = SpecGloss(o.localUV);
	half3 specColor = specGloss.rgb;
	half oneMinusRoughness = specGloss.a;

	half oneMinusReflectivity;
	half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular(Albedo(o.localUV), specColor, /*out*/ oneMinusReflectivity);

	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
}
inline FragmentCommonData FragmentMetallic(float4 i_screenPos, float3 i_ray, half3 i_worldForward, half3 i_worldUp, half3 i_eyeVec)
{
	//Grab base data
	FragmentCommonData o = FragmentUnlit(i_screenPos, i_ray, i_worldForward, i_worldUp, i_eyeVec);

	half2 metallicGloss = MetalGloss(o.localUV);
	half metallic = metallicGloss.x;
	half oneMinusRoughness = metallicGloss.y;

	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic(Albedo(o.localUV), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
}

//Ambient & reflections
inline half3 Ambient(FragmentCommonData fragment)
{
	//Check reflection
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif

	//Create dummy light for global illumination
	UnityLight dummyLight = DummyLight(fragment.normalWorld);

	//Ambient, global illumination & reflections
	UnityGI gi = FragmentGI(fragment, 1, 0, 1, dummyLight, sampleReflectionsInDeferred);

	half3 Ambient = UNITY_BRDF_PBS(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, gi.light, gi.indirect).rgb;
	Ambient += UNITY_BRDF_GI(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, 1, gi);

	return Ambient;
}

//Output
half4 AlbedoOutput(half3 color /*Albedo (RGB)*/, half occulusion, float2 uv /*Buffer Uvs*/)
{
	#ifdef _AlphaTest
		half3 albedo = color;
	#else
		half3 buffer = tex2D(_DynAlbedo, uv).rgb;
		half3 albedo = lerp(buffer, color, occulusion);
	#endif
		return half4(albedo, 1);
}
half4 SpecSmoothOutput(half4 specSmooth /*Specular Color (RGB), Smoothness (A)*/, half occulusion, float2 uv /*Buffer Uvs*/)
{
	#ifdef _AlphaTest
		half4 SpecSmooth = specSmooth;
	#else
		half4 buffer = tex2D(_DynGloss, uv);
		half4 SpecSmooth = lerp(buffer, specSmooth, occulusion);
	#endif
		return SpecSmooth;
}
half4 NormalOutput(half3 normal /*Normal (RGB)*/, half occulusion, float2 uv /*Buffer Uvs*/)
{
	#ifdef _AlphaTest
		half4 Normal = half4(normal * 0.5 + 0.5, 1);
	#else
		half4 buffer = tex2D(_DynNormal, uv);
		half4 Normal = half4(normal * 0.5 + 0.5, 1);
		Normal = lerp(buffer, Normal, occulusion);
	#endif
		return Normal;
}
half4 EmissionOutput(float4 emission /*Emission (RGBA)*/, half occulusion, float2 uv /*Buffer Uvs*/)
{
	#ifndef UNITY_HDR_ON
		emission.rgb = exp2(-emission.rgb);
	#endif

	#ifdef _AlphaTest
		float4 Emission = emission;
	#else
		float4 buffer = tex2D(_DynAmbient, uv);
		float4 Emission = lerp(buffer, emission, occulusion);
	#endif
		return Emission;
}

half4 EraserAlbedo(float2 ScreenPos, half Occlusion)
{
#ifdef _AlphaTest
	half3 albedo = tex2D(_StcAlbedo, ScreenPos).rgb;
#else
	half3 erased = tex2D(_StcAlbedo, ScreenPos).rgb;
	half3 current = tex2D(_DynAlbedo, ScreenPos).rgb;
	half3 albedo = lerp(current, erased, Occlusion);
#endif
	return half4(albedo, 1);
}
half4 EraserGloss(float2 ScreenPos, half Occlusion)
{
#ifdef _AlphaTest
	half4 gloss = tex2D(_StcGloss, ScreenPos);
#else
	half4 erased = tex2D(_StcGloss, ScreenPos);
	half4 current = tex2D(_DynGloss, ScreenPos);
	half4 gloss = lerp(current, erased, Occlusion);
#endif
	return gloss;
}
half4 EraserNormal(float2 ScreenPos, half Occlusion)
{
#ifdef _AlphaTest
	half4 normal = tex2D(_StcNormal, ScreenPos);
#else
	half4 erased = tex2D(_StcNormal, ScreenPos);
	half4 current = tex2D(_DynNormal, ScreenPos);
	half4 normal = lerp(current, erased, Occlusion);
#endif
	return normal;
}
half4 EraserAmbient(float2 ScreenPos, half Occlusion)
{
#ifdef _AlphaTest
	half4 ambient = tex2D(_StcAmbient, ScreenPos);
#else
	half4 erased = tex2D(_StcAmbient, ScreenPos);
	half4 current = tex2D(_DynAmbient, ScreenPos);
	half4 ambient = lerp(current, erased, Occlusion);
#endif
	return ambient;
}

//Roughness
sampler2D _GlossTex;

half4 RoughnessOutput(float2 LocalUvs, float2 ScreenPos, half Occlusion)
{
	half roughness = _Glossiness * tex2D(_GlossTex, LocalUvs).r;
	half4 buffer = tex2D(_DynGloss, ScreenPos);

#ifdef _AlphaTest
	half4 output = half4(buffer.rgb, roughness);
#else
	half4 output = half4(buffer.rgb, lerp(buffer.a, roughness, Occlusion));
#endif
	return output;
}