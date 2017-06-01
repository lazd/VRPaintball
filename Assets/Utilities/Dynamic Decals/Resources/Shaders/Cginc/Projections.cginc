// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable

#include "UnityStandardInput.cginc"
#include "AutoLight.cginc"
#include "UnityCG.cginc"
#include "UnityShadowLibrary.cginc"

//Shader Parameters
float _NormalCutoff;
float _Multiplier;

//InvertNormals
float _BumpFlip;

//Mask Input
float _MaskBase;
half4 _MaskLayers;

//Mask buffer
sampler2D _MaskBuffer;

//World pos from depth
float3 CalculatePerspectiveWorldPos(float3 Ray, float Depth)
{
	//Set magnitude of ray to far clipping plane
	Ray = Ray * (_ProjectionParams.z / Ray.z);

	//Calculate view position
	float4 viewpos = float4(Ray * Depth, 1);

	//Calculate & return world position
	return mul(unity_CameraToWorld, viewpos).xyz;
}
float3 CalculateOrthographicWorldPos(float2 ScreenPos, float Depth)
{
	//Calculate position in view space
	float4 viewpos = float4(unity_OrthoParams.x * ((ScreenPos.x * 2) - 1), unity_OrthoParams.y * ((ScreenPos.y * 2) - 1), (Depth * (_ProjectionParams.z - _ProjectionParams.y)) + _ProjectionParams.y, 1);
	
	//Calculate & return world position
	return mul(unity_CameraToWorld, viewpos).xyz;
}

//Clip pixels & return local uvs
half2 ProjectionUVs(float3 WorldPos)
{
	//Calculate position in object space
	float3 opos = mul(unity_WorldToObject, float4(WorldPos, 1)).xyz;

	//Remove pixels outside the bounds of our object
	clip(float3(0.5, 0.5, 0.5) - abs(opos.xyz));

	//Calculate local uvs, projecting along Z, so using xy position as co-ordinates
	return float2(opos.xy + 0.5);
}
half2 OmniDecalUVs(float3 WorldPos)
{
	//Calculate position in object space
	float3 opos = mul(unity_WorldToObject, float4(WorldPos, 1)).xyz;

	//Remove pixels outside the bounds of our object
	clip(float3(0.5, 0.5, 0.5) - abs(opos.xyz));

	//Calculate local uvs, projecting along Z, so using xy position as co-ordinates
	float magnitude = length(opos) / 0.5;

	//Remove pixels outside the range of our OmniDecal
	clip(1 - magnitude);

	return float2(magnitude, 0.5);
}

//Clip pixels
void ClipPixels(half alpha)
{
	clip(alpha - _Cutoff);
}
void ClipMasking(float2 uv)
{
	half mask = _MaskBase;
	half4 maskBuffer = tex2D(_MaskBuffer, uv);
	mask += maskBuffer.x * (_MaskLayers.x * 2 - 1);
	mask += maskBuffer.y * (_MaskLayers.y * 2 - 1);
	mask += maskBuffer.z * (_MaskLayers.z * 2 - 1);
	mask += maskBuffer.w * (_MaskLayers.w * 2 - 1);

	clip(mask - 0.5);
}

//Fragment common data (Modified Unity Struct)
struct FragmentCommonData
{
	float2 screenPos, localUV;
	float3 posWorld;

	half occlusion, oneMinusReflectivity, oneMinusRoughness;
	half3 diffColor, specColor;
	half3 normalWorld, eyeVec;
};

//GI
inline UnityGI FragmentGI(FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
{
	UnityGIInput d;

	d.light = light;
	d.worldPos = s.posWorld;
	d.worldViewDir = -s.eyeVec;
	d.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	d.ambient = 0;
	d.lightmapUV = i_ambientOrLightmapUV;
#else
	d.ambient = i_ambientOrLightmapUV.rgb;
	d.lightmapUV = 0;
#endif

	d.probeHDR[0] = unity_SpecCube0_HDR;
	d.probeHDR[1] = unity_SpecCube1_HDR;
#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
	d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION
	d.boxMax[0] = unity_SpecCube0_BoxMax;
	d.probePosition[0] = unity_SpecCube0_ProbePosition;
	d.boxMax[1] = unity_SpecCube1_BoxMax;
	d.boxMin[1] = unity_SpecCube1_BoxMin;
	d.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif

	if (reflections)
	{
		Unity_GlossyEnvironmentData g;
		g.roughness = 1 - s.oneMinusRoughness;
		g.reflUVW = reflect(s.eyeVec, s.normalWorld);
		return UnityGlobalIllumination(d, occlusion, s.normalWorld, g);
	}
	else
	{
		return UnityGlobalIllumination(d, occlusion, s.normalWorld);
	}
}

//Input
half AlbedoOcclusion(float2 localUvs)
{
#if !defined(SHADER_API_D3D11_9X)
	return tex2Dlod(_MainTex, float4(localUvs, 0, 0)).a * _Color.a;
#else
	return tex2D(_MainTex, localUvs).a * _Color.a;
#endif
}
half ShapeOcclusion(float2 localUvs)
{
#if !defined(SHADER_API_D3D11_9X)
	return tex2Dlod(_MainTex, float4(localUvs, 0, 0)).r * _Multiplier;
#else
	return tex2D(_MainTex, localUvs).r * _Multiplier;
#endif
}

half3 Albedo(float2 localUvs)
{
	return _Color.rgb * tex2D(_MainTex, localUvs).rgb;
}
half4 SpecGloss(float2 localUvs)
{
	half4 sg = tex2D(_SpecGlossMap, localUvs);
	sg.rgb *= _SpecColor.rgb;
	sg.a *= _Glossiness;

	return sg;
}
half2 MetalGloss(float2 localUvs)
{
	half2 mg = tex2D(_MetallicGlossMap, localUvs).ra;
	mg.r *= _Metallic;
	mg.g *= _Glossiness;
	return mg;
}
half3 WorldNormal(float2 localUvs, float3x3 Surface2WorldTranspose)
{
	//Grab & Scale Normal Map
	float3 normalMap = UnpackNormal(tex2D(_BumpMap, localUvs));
	normalMap.z /= clamp(_BumpScale, 0.1, 4);
	normalMap = normalize(normalMap);

	normalMap.y = lerp(normalMap.y, -normalMap.y, _BumpFlip);

	//Transform normal from tangent space into view space
	half3 normal = mul(normalMap, Surface2WorldTranspose);
	return normalize(normal);
}
half3 EmissionAlpha(float2 localUvs)
{
	half4 Emission = tex2D(_EmissionMap, localUvs) * _EmissionColor;
	return Emission.rgb;
}