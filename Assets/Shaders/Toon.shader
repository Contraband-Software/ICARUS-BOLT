Shader "Unlit/Toon"
{
	Properties
	{
		_GroundDiffuse ("ground texture", 2D) = "white" {}
		_GroundNormal ("ground normal", 2D) = "normal" {}
		_BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_TipColor("Tip Color", Color) = (1, 1, 1, 1)
		_ScatterStrength("Translucency", Range(0, 1)) = 0.3
		_ScatterTintStrength("Translucency Tint", Range(0, 1)) = 0.5

		_WindMap("Wind Offset Map", 2D) = "bump" {}
		_WindVelocity("Wind Velocity", Vector) = (1, 0, 0, 0)
		_WindStrength("Wind Strength",Range(0, 20)) = 6.0
		_WindGustiness("Wind Gustiness",Range(0, 10)) = 1
		_WindTurbulence("Wind Turbulence",Range(0, 100)) = 1
		_GustScale("Wind Gust Scale", Float) = 1
		_Timescale("Time Multiplier", Float) = 1

		_Ambient("Ambient Light",Color) = (0.1, 0.1, 0.1, 1)

		Smoothness("Smoothness", Range(0, 1)) = 0.5
		Normal("Normal Vector", Vector) = (0, 0, 1)
		View("View Vector", Vector) = (0, 0, -1)
		RimThreshold("Rim Threshold", Float) = 0.5
		Position("Position", Vector) = (0, 0, 0)
		EdgeDiffuse("Edge Diffuse", Float) = 1
		DiffuseOffset("Diffuse Offset", Float) = 0.5
		EdgeSpecular("Edge Specular", Float) = 1
		EdgeSpecularOffset("Edge Specular Offset", Float) = 0.5
		EdgeDistanceAttenuation("Edge Distance Attenuation", Float) = 1
		EdgeShadowAttenuation("Edge Shadow Attenuation", Float) = 1
		EdgeRim("Edge Rim", Float) = 1
		EdgeRimOffset("Edge Rim Offset", Float) = 0.5
		ShadeLevels("Shade Levels", Float) = 3
		RimBrightness("Rim Brightness", Float) = 1
		Tint("Tint Color", Color) = (1, 1, 1, 1)
		TintPower("Tint Power", Float) = 1
	}
SubShader
	{
	Tags
	{
		"RenderType" = "Opaque"
		"Queue" = "Geometry"
		"RenderPipeline" = "UniversalPipeline"
		"LightMode" = "UniversalForward"
	}

		LOD 100

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _SHADOWS_SOFT

			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f

			
			
			CBUFFER_START(UnityPerMaterial)
				sampler2D _GroundDiffuse;
				sampler2D _GroundNormal;
				float4 _GroundDiffuse_ST;
				float4 _BaseColor;
				float4 _TipColor;
				float _ScatterStrength;
				float _ScatterTintStrength;

				sampler2D _WindMap;
				float4 _WindMap_ST;
				float4 _WindVelocity;
				float _WindStrength;
				float _WindGustiness;
				float _WindTurbulence;
				float _GustScale;
				float _Timescale;


				float4 _Ambient;

				float Smoothness;
				float3 Normal;
				float3 View;
				float RimThreshold;
				float3 Position;
				float EdgeDiffuse;
				float DiffuseOffset;
				float EdgeSpecular;
				float EdgeSpecularOffset;
				float EdgeDistanceAttenuation;
				float EdgeShadowAttenuation;
				float EdgeRim;
				float EdgeRimOffset;
				float ShadeLevels;
				float RimBrightness;
				float4 Tint;
				float TintPower;

			CBUFFER_END

			struct VertexInput
			{
				float4 vertex  : POSITION;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
				float2 uv	  : TEXCOORD0;
				float4 color : COLOR;
			};

            struct GeomData
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 worldPos  : TEXCOORD1;
                float3 normal    : TEXCOORD2;   // world-space normal
                float4 tangent   : TEXCOORD3;   // world-space tangent (w = handedness)
                float  t         : TEXCOORD4;   // your custom scalar
                float4 color     : COLOR;
            };


			GeomData vert (VertexInput v)
            {
                GeomData o;

                // clip-space position
                o.pos = TransformObjectToHClip(v.vertex.xyz);

                // world-space data
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normal   = TransformObjectToWorldNormal(v.normal);
                o.tangent  = float4( TransformObjectToWorldDir(v.tangent.xyz),
                                     v.tangent.w );      // preserve handedness

                // texture coordinates
                o.uv = TRANSFORM_TEX(v.uv, _GroundDiffuse);

                // custom values
                o.t     = 0;
                o.color = v.color;

                return o;
            }

			
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
				float2 uv;
				float3 normal;
				float3 normalAux;
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
			

		ENDHLSL

		// This pass draws the grass blades without tessellation.
		Pass
		{
			Name "GrassPass"
			Tags { "LightMode" = "UniversalForward" }
			ZWrite On //?
			
			HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

				#pragma vertex vert
				#pragma fragment frag

				float smoothFloor(float x, float edgeWidth)          // edgeWidth in [0,1]
				{
				    float b   = floor(x);                            // integer base
				    float f   = x - b;                               // fractional part [0,1)
				    float t   = smoothstep(1.0 - edgeWidth, 1.0, f); // 0 → 1 only near the top
				    return b + t;                                    // blends to b+1 over 'edgeWidth'
				}

				
				float3 lightContribution(Light l,SurfaceVariables s){
					float diffuse = pow(max(0.0, dot(s.normal, l.direction)),1);
					// diffuse = smoothstep(0.5,1,diffuse);
					float diffuseNormalMapped = max(0.0, dot(s.normalAux, l.direction)); 
					float attenuation = l.distanceAttenuation * l.shadowAttenuation;
					float3 shading = (l.color.rgb * (diffuse));
					return shading*attenuation;
				}
				

				float4 frag(GeomData i) : SV_Target
				{
					float distanceFromCamera = LinearEyeDepth(i.pos.z / i.pos.w, _ZBufferParams)/16384;
					
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
					s.view = GetWorldSpaceNormalizeViewDir(i.worldPos);
					float3 normal = UnpackNormal(tex2D(_GroundNormal, TRANSFORM_TEX(i.uv, _GroundDiffuse)));
					float3 tangentNormal = UnpackNormal(tex2D(_GroundNormal, i.uv));
					float3x3 tbn = CreateTangentToWorld(normalize(i.normal), i.tangent,0);

					float3 normalWS      = normalize(TransformTangentToWorld(
                                     tangentNormal,      // TS normal
                                     tbn,           // interpolated WS normal
                                     false));        // interpolated WS tangent

					s.normalAux = normalWS;//normalize(i.normal + normal);
					s.normal = i.normal;
					s.smoothness = Smoothness;
					s.shininess = exp2(10 * Smoothness + 1);
					s.rimThreshold = RimThreshold;
					s.rimBrightness = RimBrightness;
					s.shadeLevels = floor(ShadeLevels);
					s.tint = Tint;
					s.tintPower = TintPower;
					s.ec = ec;

					
					float3 diffuseColor = tex2D(_GroundDiffuse, TRANSFORM_TEX(i.uv, _GroundDiffuse)).rgb;


				// Main directional light contribution
					VertexPositionInputs vertexInput = (VertexPositionInputs) 0;
					vertexInput.positionWS = i.worldPos;

					float4 shadowCoord = GetShadowCoord(vertexInput);
					Light light = GetMainLight(shadowCoord);
					float3 baseColor = diffuseColor;
					
					float3 lighting = lightContribution(light,s); //*SampleAmbientOcclusion()
					float3 litColor = baseColor*lighting;
					
					 int pixelLightCount = GetAdditionalLightsCount();
					 for (int lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
					 {
					 	light = GetAdditionalLight(lightIndex, i.worldPos, 1);
					 	lighting = lightContribution(light,s);
					 	litColor += baseColor*lighting;
					 }

					float3 finalColor = (floor(length(litColor)*s.shadeLevels)/s.shadeLevels)*litColor + _Ambient*baseColor;
					float4 output = float4(finalColor,1); //float4(color,min((1-i.t)*8,1));
					return output;
				}
			ENDHLSL
		}
		Pass {
			Name"ShadowCaster"
			Tags
			{"LightMode"="ShadowCaster"
			}

			ZWrite On

			ZTest LEqual

			HLSLPROGRAM
				// #pragma vertex vert
				//
				// #pragma fragment ShadowPassFragment

				// Material Keywords
				#pragma shader_feature _ALPHATEST_ON
				#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

				// GPU Instancing
				#pragma multi_compile_instancing
				// (Note, this doesn't support instancing for properties though. Same as URP/Lit)
				#pragma multi_compile _ DOTS_INSTANCING_ON
				// (This was handled by LitInput.hlsl. I don't use DOTS so haven't bothered to support it)
				
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}
}