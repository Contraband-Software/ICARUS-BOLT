#ifndef LIGHTING_CEL_SHADED_INCLUDED
#define LIGHTING_CEL_SHADED_INCLUDED

#ifndef SHADERGRAPH_PREVIEW

struct EdgeConstants{
	float edgeDiffuse;
	float diffuseOffset;
	float edgeSpecular;
	float edgeSpecularOffset;
	float edgeDistanceAttenuation;
	float edgeShadowAttenuation;
	float edgeRim;
	float edgeRimOffset;
};


struct SurfaceVariables{
	float3 normal;
	float3 view;
	float smoothness;
	float shininess;
	float rimThreshold;
	float rimBrightness;
	float shadeLevels;
	float4 tint;
	float tintPower;
	float darkRimThreshold;
	float darkRimDarkness;
	EdgeConstants ec;
};

// Had to split into two functions because HLSL is retarded and thinks im truncating
// vectors when im fucking not 
float3 CalculateDiffuse(Light l, SurfaceVariables s){
	float shadowAttenuationSmoothStepped = smoothstep(0.0f, s.ec.edgeShadowAttenuation, l.shadowAttenuation);
	float distanceAttenuationSmoothStepped = smoothstep(0.0f, s.ec.edgeDistanceAttenuation, l.distanceAttenuation);
	float attenuation = shadowAttenuationSmoothStepped * distanceAttenuationSmoothStepped;

	float diffuse = saturate(dot(s.normal, l.direction));
	diffuse = smoothstep(s.ec.diffuseOffset, s.ec.diffuseOffset + s.ec.edgeDiffuse, diffuse);
	diffuse = floor(diffuse * s.shadeLevels) / s.shadeLevels;
	diffuse *= attenuation;

	return diffuse;
}

float3 CalculateCelShading(float diffuse, Light l, SurfaceVariables s, float IsTinted){
	float3 h = SafeNormalize(l.direction + s.view);
	float specular = saturate(dot(s.normal, h));
	specular = pow(specular, s.shininess);
	specular *= diffuse * s.smoothness;

	float rim = 1 - dot(s.view, s.normal);
	rim *= pow(diffuse, s.rimThreshold);

	specular = s.smoothness * smoothstep(
		(1 - s.smoothness) * s.ec.edgeSpecular + s.ec.edgeSpecularOffset,
		s.ec.edgeSpecular + s.ec.edgeSpecularOffset,
		specular
	);
	rim = s.smoothness * s.rimBrightness * smoothstep(
		s.ec.edgeRim - 0.5f * s.ec.edgeRimOffset,
		s.ec.edgeRim + 0.5f * s.ec.edgeRimOffset,
		rim
	);

	diffuse = saturate(diffuse);
	float4 t = (s.tint * IsTinted);

	//float4 col = diffuse + IsTinted * (t * (1 - diffuse) - 0.5f);
	float4 col = diffuse + IsTinted * ((t * (saturate(1 - diffuse))) - s.tintPower);

	return l.color * (col + max(specular, rim));
}
#endif

void LightingCelShaded_float(
in float Smoothness,
in float3 Normal,
in float3 View,
in float RimThreshold,
in float3 Position,
in float EdgeDiffuse,
in float DiffuseOffset,
in float EdgeSpecular,
in float EdgeSpecularOffset,
in float EdgeDistanceAttenuation,
in float EdgeShadowAttenuation,
in float EdgeRim,
in float EdgeRimOffset,
in float ShadeLevels,
in float RimBrightness,
in float4 Tint,
in float TintPower,
out float3 Color, out float Diffuse){
#if defined(SHADERGRAPH_PREVIEW)
	Color = float3(0.5f, 0.5f, 0.5f);
	Diffuse = 0.5f;
#else
	EdgeConstants ec;
	ec.edgeDiffuse = EdgeDiffuse;
	ec.diffuseOffset = DiffuseOffset;
	ec.edgeSpecular = EdgeSpecular;
	ec.edgeSpecularOffset = EdgeSpecularOffset;
	ec.edgeDistanceAttenuation = EdgeDistanceAttenuation;
	ec.edgeShadowAttenuation = EdgeShadowAttenuation;
	ec.edgeRim = EdgeRim;
	ec.edgeRimOffset = EdgeRimOffset;

	SurfaceVariables s;
	s.normal = normalize(Normal);
	s.view = SafeNormalize(View);
	s.smoothness = Smoothness;
	s.shininess = exp2(10 * Smoothness + 1);
	s.rimThreshold = RimThreshold;
	s.rimBrightness = RimBrightness;
	s.shadeLevels = floor(ShadeLevels);
	s.tint = Tint;
	s.tintPower = TintPower;
	s.ec = ec;

#if SHADOWS_SCREEN
	float4 clipPos = TransformWorldToHClip(Position);
	float4 shadowCoord = ComputeScreenPos(clipPos);
#else
	float4 shadowCoord = TransformWorldToShadowCoord(Position);
#endif
	Light light = GetMainLight(shadowCoord);
	int pixelLightCount = GetAdditionalLightsCount();

	Diffuse = CalculateDiffuse(light, s);
	Color = CalculateCelShading(Diffuse, light, s, 1);

	for(int i = 0; i < pixelLightCount; i++){
		light = GetAdditionalLight(i, Position, 1);
		float d = CalculateDiffuse(light, s);
		Color += CalculateCelShading(d, light, s, 0);

		Diffuse += d;
	}

#endif
}

#endif