#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#ifndef CEL_SHADER_LITE_INCLUDED
#define CEL_SHADER_LITE_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
struct EdgeConstants{
	float edgeDiffuse;
	float diffuseOffset;
	float edgeSpecular;
	float edgeSpecularOffset;
	float edgeDistanceAttenuation;
	float edgeShadowAttenuation;
};


struct SurfaceVariables{
	float3 normal;
	float3 view;
	float smoothness;
	float shininess;
	float shadeLevels;
	float4 tint;
	float tintPower;
	EdgeConstants ec;
};

// Revised cel shader functions: no specular, no smoothness, flat diffuse
float3 CalculateDiffuse(Light l, SurfaceVariables s) {
	// Use only shadow + distance attenuation as "light intensity"
    float shadowAtt = smoothstep(0.0f, s.ec.edgeShadowAttenuation, l.shadowAttenuation);
    float distAtt   = smoothstep(0.0f, s.ec.edgeDistanceAttenuation, l.distanceAttenuation);
    float lightIntensity = shadowAtt * distAtt;

    // Quantize that intensity into cel bands
    float diffuse = smoothstep(s.ec.diffuseOffset, s.ec.diffuseOffset + s.ec.edgeDiffuse, lightIntensity);
    diffuse = floor(diffuse * s.shadeLevels) / s.shadeLevels;

    return diffuse;
}


float3 CalculateCelShading(float diffuse, Light l, SurfaceVariables s, float IsTinted) {
    diffuse = saturate(diffuse);

    // Apply tint (same logic you had before)
    float4 tintCol = s.tint * IsTinted;
    float4 color4 = diffuse + IsTinted * ((tintCol * saturate(1.0 - diffuse)) - s.tintPower);

    // Multiply by light color
    return l.color * color4.rgb;
}
#endif


void CelShaderLite_float(
    in float Smoothness,            // can ignore this input
    in float3 Normal,
	in float3 View,
	in float3 Position,
    in float EdgeDiffuse,
	in float DiffuseOffset,
    in float EdgeSpecular,
	in float EdgeSpecularOffset,
    in float EdgeDistanceAttenuation, 
	in float EdgeShadowAttenuation,
    in float ShadeLevels,
    in float4 Tint, 
	in float TintPower,
    out float3 Color, out float Diffuse)
{
	#if defined(SHADERGRAPH_PREVIEW)
		Color = float3(0.5f, 0.5f, 0.5f);
		Diffuse = 0.5f;
    #else
        // Fill EdgeConstants as before
        EdgeConstants ec;
        ec.edgeDiffuse = EdgeDiffuse;          
		ec.diffuseOffset = DiffuseOffset;
        ec.edgeSpecular = EdgeSpecular;
		ec.edgeSpecularOffset = EdgeSpecularOffset;
        ec.edgeDistanceAttenuation = EdgeDistanceAttenuation;
        ec.edgeShadowAttenuation = EdgeShadowAttenuation;

        SurfaceVariables s;
        s.normal = normalize(Normal); // not actually used in our flat calc
        s.view = SafeNormalize(View);
        // s.smoothness and s.shininess are unused now (omitted)
        s.shadeLevels = floor(ShadeLevels);
        s.tint = Tint;
        s.tintPower = TintPower;
        s.ec = ec;

        // Compute shadow coords and get main light as before
        #if SHADOWS_SCREEN
            float4 clipPos = TransformWorldToHClip(Position);
            float4 shadowCoord = ComputeScreenPos(clipPos);
        #else
            float4 shadowCoord = TransformWorldToShadowCoord(Position);
        #endif
        Light light = GetMainLight(shadowCoord);

        // Diffuse: flat (geometry-independent) cel shading bands
        Diffuse = CalculateDiffuse(light, s);

        // Final Color: use flat diffuse + tint (no specular)
        Color = CalculateCelShading(Diffuse, light, s, 1.0);
    #endif
}

#endif