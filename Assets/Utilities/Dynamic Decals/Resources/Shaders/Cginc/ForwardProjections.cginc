// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#include "Cginc/Projections.cginc"

//Camera depth buffer
uniform sampler2D_float _CameraDepthTexture;
uniform sampler2D_float _CameraNormalTexture;
uniform sampler2D_float _CameraDepthNormalsTexture;

//Eraser buffer
sampler2D _Eraser;
uniform float4 _Eraser_TexelSize;

//UnityLight
UnityLight MainLight(half3 normalWorld)
{
	UnityLight l;
#ifdef LIGHTMAP_OFF

	l.color = _LightColor0.rgb;
	l.dir = _WorldSpaceLightPos0.xyz;
	l.ndotl = LambertTerm(normalWorld, l.dir);
#else
	// no light specified by the engine
	// analytical light might be extracted from Lightmap data later on in the shader depending on the Lightmap type
	l.color = half3(0.f, 0.f, 0.f);
	l.ndotl = 0.f;
	l.dir = half3(0.f, 0.f, 0.f);
#endif

	return l;
}
UnityLight AdditiveLight(half3 normalWorld, half3 lightDir, half atten)
{
	UnityLight l;

	l.color = _LightColor0.rgb;
	l.dir = lightDir;
#ifndef USING_DIRECTIONAL_LIGHT
	l.dir = normalize(l.dir);
#endif
	l.ndotl = LambertTerm(normalWorld, l.dir);

	// shadow the light
	l.color *= atten;
	return l;
}
UnityIndirect ZeroIndirect()
{
	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = 0;
	return ind;
}

//Lighting
inline fixed LightAttenuation(float3 posWorld, float2 ScreenPos)
{
	fixed atten = 1;

	//Correct LightCoords per pixel	
#ifdef POINT
	float3 LightCoord = mul(unity_WorldToLight, float4(posWorld, 1)).xyz;
	atten = (tex2D(_LightTexture0, dot(LightCoord, LightCoord).rr).UNITY_ATTEN_CHANNEL);
#endif
#ifdef SPOT
	float4 LightCoord = mul(unity_WorldToLight, float4(posWorld, 1));
	atten = ((LightCoord.z > 0) * UnitySpotCookie(LightCoord) * UnitySpotAttenuate(LightCoord.xyz));
#endif
#ifdef DIRECTIONAL
	atten = 1;
#endif
#ifdef POINT_COOKIE
	float3 LightCoord = mul(unity_WorldToLight, float4(posWorld, 1)).xyz;
	atten = (tex2D(_LightTextureB0, dot(LightCoord, LightCoord).rr).UNITY_ATTEN_CHANNEL * texCUBE(_LightTexture0, LightCoord).w);
#endif
#ifdef DIRECTIONAL_COOKIE
	float2 LightCoord = mul(unity_WorldToLight, float4(posWorld, 1)).xy;
	atten = (tex2D(_LightTexture0, LightCoord).w);
#endif

	//Correct ShadowCoords per pixel
#if defined (SHADOWS_SCREEN)
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
	float4 ShadowCoord = mul(unity_WorldToShadow[0], float4(posWorld, 1));
	atten *= unitySampleShadow(ShadowCoord);
#else
	atten *= tex2D(_ShadowMapTexture, ScreenPos).r;
#endif
#endif
#if defined (SHADOWS_DEPTH) && defined (SPOT)
	//Spot
	float4 ShadowCoord = mul(unity_WorldToShadow[0], float4(posWorld, 1));
	atten *= UnitySampleShadowmap(ShadowCoord);
#endif
#if defined (SHADOWS_CUBE)
	//Point
	float3 ShadowCoord = posWorld - _LightPositionRange.xyz;
	atten *= UnitySampleShadowmap(ShadowCoord);
#endif

	return atten;
}
inline fixed ShadowAttenuation(float3 posWorld, float2 ScreenPos)
{
	fixed atten = 1;

	//Correct ShadowCoords per pixel
#if defined (SHADOWS_SCREEN)
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
	float4 ShadowCoord = mul(unity_WorldToShadow[0], float4(posWorld, 1));
	atten *= unitySampleShadow(ShadowCoord);
#else
	atten *= tex2D(_ShadowMapTexture, ScreenPos).r;
#endif
#endif
#if defined (SHADOWS_DEPTH) && defined (SPOT)
	//Spot
	float4 ShadowCoord = mul(unity_WorldToShadow[0], float4(posWorld, 1));
	atten *= UnitySampleShadowmap(ShadowCoord);
#endif
#if defined (SHADOWS_CUBE)
	//Point
	float3 ShadowCoord = posWorld - _LightPositionRange.xyz;
	atten *= UnitySampleShadowmap(ShadowCoord);
#endif

	return atten;
}
inline float3 LightDiriction(float3 posWorld)
{
	//Calculate LightDirection
	return _WorldSpaceLightPos0.xyz - posWorld * _WorldSpaceLightPos0.w;
}

//Depth & Normal input
float3 LowPrecisionDepthNormal(float2 ScreenPos, out float Depth)
{
	float3 surfaceNormal;

	//Grab our view space normal
	DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, ScreenPos), Depth, surfaceNormal);

	//Convert to worldspace normal
	return mul(float4(surfaceNormal, 1.0), UNITY_MATRIX_V).xyz;
}
float3 HighPrecisionDepthNormal(float2 ScreenPos, out float Depth)
{
    float3 surfaceNormal;

	//Grab our view space normal
    surfaceNormal = (tex2D(_CameraNormalTexture, ScreenPos).xyz - 0.5) * 2;
	
	//Logarithmic Depth
	float LogarithmicDepth = Linear01Depth(tex2D(_CameraDepthTexture, ScreenPos).r);

	//Linear Depth
	float LinearDepth = 1 - tex2D(_CameraDepthTexture, ScreenPos).r;

	//Blend depth based on projection type
	Depth = (unity_OrthoParams.w * LinearDepth) + ((1 - unity_OrthoParams.w) * LogarithmicDepth);

	//Convert to worldspace normal
    return mul(float4(surfaceNormal, 1.0), UNITY_MATRIX_V).xyz;
}
float3 DepthNormal(float2 ScreenPos, out float Depth)
{
    float3 normalWorld;

#ifdef _HighPrecision
	normalWorld = HighPrecisionDepthNormal(ScreenPos, Depth);
#else
    normalWorld = LowPrecisionDepthNormal(ScreenPos, Depth);
#endif

    return normalWorld;
}

float3x3 Surface2WorldTranspose(float3 WorldUp, float3 surfaceNormal)
{
	//Calculate bi-normal
	float3 binormalWorld = normalize(cross(WorldUp, surfaceNormal));

	//Calculate tangent
	float3 tangentWorld = normalize(cross(surfaceNormal, binormalWorld));

	//Object 2 World Matrix
	return float3x3(tangentWorld, binormalWorld, surfaceNormal);
}

//Clip Normal
void ClipNormal(float3 surfaceNormal, half3 worldForward)
{
	//Use dot product to determine angle & clip
	float d = dot(surfaceNormal, normalize(-worldForward));
	clip(d - _NormalCutoff);
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

	UNITY_FOG_COORDS(5)
};

ProjectionInput vertProjection(VertexInput v)
{
	ProjectionInput o;
	o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
	o.screenPos = ComputeScreenPos(o.pos);
	o.ray = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.eyeVec = posWorld.xyz - _WorldSpaceCameraPos;

	o.worldForward = mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1));
	o.worldUp = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)); //(Now Right)

	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

//Vertex program - OmniDecal
struct OmniDecalInput
{
	float4 pos : SV_POSITION;
	float4 screenPos : TEXCOORD0;
	float3 ray : TEXCOORD1;
	UNITY_FOG_COORDS(6)
};
OmniDecalInput vertOmniDecal(VertexInput v)
{
	OmniDecalInput o;
	o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
	o.screenPos = ComputeScreenPos(o.pos);
	o.ray = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);

	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

//Fragment setups
inline FragmentCommonData FragmentUnlit(float4 i_screenPos, float3 i_ray, half3 i_worldForward, half3 i_worldUp, half3 i_eyeVec)
{
	FragmentCommonData o = (FragmentCommonData)0;

	//Calculate screenspace position
	o.screenPos = i_screenPos.xy / i_screenPos.w;

	//Calculate depth & normals
	float depth;
	o.normalWorld = DepthNormal(o.screenPos, depth);

	//Calculate world position - Perspective
	//o.posWorld = CalculatePerspectiveWorldPos(i_ray, depth);

	//Calculate world position - Orthagraphic
	//o.posWorld = CalculateOrthographicWorldPos(o.screenPos, depth);

	//Calculate world position - Blended
	o.posWorld = (unity_OrthoParams.w * CalculateOrthographicWorldPos(o.screenPos, depth)) + ((1 - unity_OrthoParams.w) * CalculatePerspectiveWorldPos(i_ray, depth));

	//Calculate local uvs
	o.localUV = ProjectionUVs(o.posWorld);

	//Occlusion
	o.occlusion = AlbedoOcclusion(o.localUV);

	//Clip Pixels
	ClipMasking(o.screenPos);
	ClipNormal(o.normalWorld, i_worldForward);

	//Calculate world normals
#if SHADER_TARGET < 30
	//Don't use normal maps when using shader model 2.0
#else
	//float3x3 surface2WorldTranspose = Surface2WorldTranspose(i_worldUp, o.normalWorld);
	//o.normalWorld = WorldNormal(o.localUV, surface2WorldTranspose);
#endif

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
	half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular(Albedo(o.localUV), specColor, oneMinusReflectivity);

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

//Output
half4 Output(half3 color, half occlusion)
{
#ifdef _AlphaTest
	return half4(color, 1);
#else
	return half4(color, occlusion);
#endif
}
half4 EraserOutput(float2 ScreenPos, half Occlusion)
{
	//Check if projection has been flipped
#if UNITY_UV_STARTS_AT_TOP
	if (_Eraser_TexelSize.y < 0)
		ScreenPos.y = 1 - ScreenPos.y;
#endif

	//Check if sampling has been flipped
	if (_ProjectionParams.x < 0)
		ScreenPos.y = 1 - ScreenPos.y;

	half3 eraser = tex2D(_Eraser, ScreenPos).rgb;
	return half4(eraser, Occlusion);
}

//Metallic Programs
half4 fragForwardMetallic(ProjectionInput i) : SV_Target{
	//Generate base data
	FragmentCommonData fragment = FragmentMetallic(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

	//Clip pixels
	ClipPixels(fragment.occlusion);

	//Setup Light
	UnityLight mainLight = MainLight(fragment.normalWorld);
	half atten = ShadowAttenuation(fragment.posWorld, fragment.screenPos);

	//Setup GI
	UnityGI gi = FragmentGI(fragment, 1, half4(0,0,0,0), atten, mainLight, true);

	//Calculate final output
	half4 c = UNITY_BRDF_PBS(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, 1, gi);
	c.rgb += EmissionAlpha(fragment.localUV);

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	return Output(c.rgb, fragment.occlusion);
}
half4 fragForwardAddMetallic(ProjectionInput i) : SV_Target {
	//Generate base data
	FragmentCommonData fragment = FragmentMetallic(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

	//Clip pixels
	ClipPixels(fragment.occlusion);

	//Calculate lighting data
	float atten = LightAttenuation(fragment.posWorld, fragment.screenPos);
	float3 lightDir = LightDiriction(fragment.posWorld);

	//Setup Light
	UnityLight light = AdditiveLight(fragment.normalWorld, lightDir, atten);
	UnityIndirect noIndirect = ZeroIndirect();

	half4 c = UNITY_BRDF_PBS(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, light, noIndirect);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0.0, 0.0, 0.0, 0.0));
	return Output(c.rgb, fragment.occlusion);
}

//Specular Programs
half4 fragForwardSpecular(ProjectionInput i) : SV_Target{
	//Generate base data
	FragmentCommonData fragment = FragmentSpecular(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

	//Clip pixels
	ClipPixels(fragment.occlusion);

	//Setup Light
	UnityLight mainLight = MainLight(fragment.normalWorld);
	half atten = ShadowAttenuation(fragment.posWorld, fragment.screenPos);

	//Setup GI
	UnityGI gi = FragmentGI(fragment, 1, half4(0,0,0,0), atten, mainLight, true);

	//Calculate final output
	half4 c = UNITY_BRDF_PBS(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, 1, gi);
	c.rgb += EmissionAlpha(fragment.localUV);

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	return Output(c.rgb, fragment.occlusion);
}
half4 fragForwardAddSpecular(ProjectionInput i) : SV_Target{
	//Generate base data
	FragmentCommonData fragment = FragmentSpecular(i.screenPos, i.ray, i.worldForward, i.worldUp, i.eyeVec);

	//Clip pixels
	ClipPixels(fragment.occlusion);

	//Calculate lighting data
	float atten = LightAttenuation(fragment.posWorld, fragment.screenPos);
	float3 lightDir = LightDiriction(fragment.posWorld);

	//Setup Light
	UnityLight light = AdditiveLight(fragment.normalWorld, lightDir, atten);
	UnityIndirect noIndirect = ZeroIndirect();

	half4 c = UNITY_BRDF_PBS(fragment.diffColor, fragment.specColor, fragment.oneMinusReflectivity, fragment.oneMinusRoughness, fragment.normalWorld, -fragment.eyeVec, light, noIndirect);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0, 0, 0, 0));
	return Output(c.rgb, fragment.occlusion);
}