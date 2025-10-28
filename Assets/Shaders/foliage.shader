Shader "Unlit/Foliage"
{
	Properties
	{
		/* ────────── SurfaceVariables ────────── */
		_GroundDiffuse ("ground texture", 2D) = "white" {}
		_Albedo ("albedo", Color) = (1, 1, 1)
		_GroundNormal ("ground normal", 2D) = "bump" {}
        _SmoothnessToon          ("Smoothness"                , Range(0, 1))   = 0.5
        _Shininess           ("Shininess (Phong power)"   , Float)         = 32
        _RimThreshold        ("Rim Threshold"             , Range(0, 1))   = 0.5
        _RimBrightness       ("Rim Brightness"            , Range(0, 5))   = 1
        _ShadeLevels         ("Shade Levels"              , Range(1, 10))  = 4
        _DarkRimThreshold    ("Dark Rim Threshold"        , Float)   = 0.2
        _DarkRimDarkness     ("Dark Rim Darkness"         , Range(0, 1))   = 0.5
		_DarkRimPower		("Dark Rim Power"         , Float)   = 0.5
		_DarkRimCutoff		("Dark Rim Cutoff"         , Float)   = 0.5
		_Ambient                ("Ambient Colour"               , Color)         = (1,1,1,1)

        /* ────────── EdgeConstants ────────── */
        _EdgeDiffuse             ("Edge Diffuse"              , Range(0, 1)) = 0.1
        _DiffuseOffset           ("Diffuse Offset"            , Range(0, 1)) = 0.1
        _EdgeSpecular            ("Edge Specular"             , Range(0, 1)) = 0.5
        _EdgeSpecularOffset      ("Edge Specular Offset"      , Range(0, 1)) = 0.0
        _EdgeDistanceAttenuation ("Edge Distance Attenuation" , Float)       = 1
        _EdgeShadowAttenuation   ("Edge Shadow Attenuation"   , Float)       = 1
        _EdgeRim                 ("Edge Rim"                  , Range(0, 1)) = 0.5
        _EdgeRimOffset           ("Edge Rim Offset"           , Range(0, 1)) = 0.1
		
		_MipOffset ("Mip Offset", int) = 0
		_DistanceScale ("Distance Scale" , Float) = 0.005
		
		_AlphaClip ("Alpha Clip Threshold" , Range(0, 1)) = 0.25
		_ShadowClip ("Shadow Alpha Clip Threshold" , Range(0, 1)) = 0.25
		
		_PlayerLoc("Player Location", Vector) = (0,0,0)
		_PlayerVel("Player Velocity", Vector) = (0,0,0)
		_PlayerBendFactor("Player Bend Factor", Float) = 0
		_PlayerLocMul("Player Location Factor Multiplier", Float) = 1
		_PlayerVelMul("Player Velocity Factor Multiplier", Float) = 1

	}
SubShader
	{
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

		LOD 100

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"

			
			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f

			
			
			
			CBUFFER_START(UnityPerMaterial)
				sampler2D _GroundDiffuse;
				sampler2D _GroundNormal;
				float3 _Albedo;
				float4 _GroundDiffuse_ST;
                float  _SmoothnessToon;
                float  _Shininess;
                float  _RimThreshold;
                float  _RimBrightness;
                float  _ShadeLevels;
                float  _DarkRimThreshold;
                float  _DarkRimDarkness;
				float  _DarkRimPower;
				float _DarkRimCutoff;
				float3 _Ambient;

                /* EdgeConstants */
                float  _EdgeDiffuse;
                float  _DiffuseOffset;
                float  _EdgeSpecular;
                float  _EdgeSpecularOffset;
                float  _EdgeDistanceAttenuation;
                float  _EdgeShadowAttenuation;
                float  _EdgeRim;
                float  _EdgeRimOffset;

				int _MipOffset;
				float _DistanceScale;

				float _AlphaClip;
				float _ShadowClip;
				
				float3 _PlayerLoc;
				float3 _PlayerVel;
				float _PlayerBendFactor;

				float _PlayerLocMul;
				float _PlayerVelMul;
            CBUFFER_END


			struct VertexInput
			{
				float4 vertex  : POSITION;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
				float2 uv	  : TEXCOORD0;
            	float2 uvLM     : TEXCOORD1;
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
			    float2 lightmapUV : TEXCOORD5;               // ★ passed to pixel stage
			    float3 vertexSH   : TEXCOORD6;               // ★ SH for probe lighting
			    float4 color      : COLOR;
            };


			struct SurfaceVariables{
				float3 normal;
				float3 nomalStrong;
				float3 view;
			};

			GeomData vert (VertexInput v)
			{
				GeomData o;

				// Apply wind displacement using perlin noise
				float time = _Time.y*0.5;
				float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
				float noise = sin(worldPos.x * 0.5 + time) * 
							cos(worldPos.z * 0.5 + time) * 
							sin(time * 2.0);
				float windStrength = 0.5;
				float3 windDir = float3(sin(time * 0.5), 0, cos(time * 0.5));
				float vertexMask = v.color.r; // Use red vertex color as wind mask
				v.vertex.xyz += windDir * noise * windStrength * vertexMask;
				float rad = 16;
				_PlayerLoc += float3(0,4,0);
			    float pDist = max(distance(_PlayerLoc,worldPos),4);
			    if (pDist < rad)
			    {
					float3 vecToPlayer = normalize(_PlayerLoc+_PlayerVel - TransformObjectToWorld(v.vertex.xyz))*_PlayerLocMul;
			    	float3 velVec = _PlayerVel*_PlayerVelMul;
			    	v.vertex.xyz -= ((velVec*_PlayerBendFactor/pDist)+(vecToPlayer/pow(pDist,1)))*vertexMask;
			    }

				// clip-space position
				o.pos = TransformObjectToHClip(v.vertex.xyz);

				// world-space data
				o.worldPos = TransformObjectToWorld(v.vertex.xyz);
				o.normal   = TransformObjectToWorldNormal(v.normal);
				o.tangent  = float4( TransformObjectToWorldDir(v.tangent.xyz),
									 v.tangent.w );	  // preserve handedness

				// texture coordinates
				o.uv = TRANSFORM_TEX(v.uv, _GroundDiffuse);

				// custom values
				o.t	 = 0;
				o.color = v.color;

				o.lightmapUV = v.uvLM * unity_LightmapST.xy + unity_LightmapST.zw;
				o.vertexSH   = SampleSHVertex(TransformObjectToWorldNormal(v.normal));
				return o;
			}

			float4 mipOffsetSample(sampler2D sampler_in,float2 uv,float rawDistance){
				float factor = _MipOffset;//lerp(_MipOffset,ComputeTextureLOD(uv),rawDistance*_DistanceScale);
				float4 sample =  tex2Dbias(sampler_in, float4(uv, 0, factor));
				return float4(sample.rgb,min(sample.a+ min(rawDistance*_DistanceScale,0.14),1)); //*min(max(rawDistance*_DistanceScale,1),2) min(sample.a + rawDistance*_DistanceScale/8,1)
			}
			
		ENDHLSL
	
		Pass
		{
			Fog {}
			Name "GrassPass"
			Tags { "LightMode" = "UniversalForward" }
//			Blend   SrcAlpha OneMinusSrcAlpha   // classic straight-alpha blending
//            ZWrite  Off     
			ZWrite On
			Cull Off
			
			
			HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ LIGHTMAP_ON              // ★ static light-maps
				#pragma multi_compile _ DIRLIGHTMAP_COMBINED     // ★ directional light-map
				#pragma multi_compile _ DYNAMICLIGHTMAP_ON       // ★ realtime GI needed?
				#pragma multi_compile _ _FORWARD_PLUS
				#pragma multi_compile_fog 
				
				#pragma vertex vert
				#pragma fragment frag
				void Applyfog(inout float4 color, float3 positionWS)
				{
				    float4 inColor = color;
				  
				    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
				    float viewZ = -TransformWorldToView(positionWS).z;
				    float nearZ0ToFarZ = max(viewZ - _ProjectionParams.y, 0);
				    float density = 1.0f - ComputeFogIntensity(ComputeFogFactorZ0ToFar(nearZ0ToFarZ));

				    color = lerp(color, unity_FogColor,  density);

				    #else
				    color = color;
				    #endif
				}

				float lightDiffuse(Light l,SurfaceVariables s)
				{
					float diffuse = pow(max(0.5, dot(s.normal, l.direction)),1);
					diffuse = smoothstep(_DiffuseOffset, _DiffuseOffset + _EdgeDiffuse, diffuse);
					float diffuseNormalMapped = max(0.0, dot(s.normal, l.direction));
					return diffuse;
				}
				float lightDiffuseAttenuated(Light l,SurfaceVariables s)
				{
					float diffuse = lightDiffuse(l,s);
					return diffuse*l.shadowAttenuation*l.distanceAttenuation;
				}
				
				float3 lightContribution(Light l,SurfaceVariables s,float3 baseColor){

					// s.normal = normalize(s.normal+float3(0,2,0));
					float3 diffuse = lightDiffuse(l,s)*l.color;
					float diffuseBack = max(0, dot(-s.normal, l.direction))*l.distanceAttenuation*l.shadowAttenuation;
					diffuse += diffuseBack*lerp(l.color.rgb, baseColor, 1)  *1;

					
				
					return diffuse*l.distanceAttenuation*max(l.shadowAttenuation,0.25);
				}

				float fresnel(float3 viewDir, float3 normal, float power)
				{
				    float cosTheta = saturate(dot(viewDir, normal));
				    return 0.04 + (1.0 - 0.04) *pow(1.0 - cosTheta, power);
				}


				float darkRim(SurfaceVariables s, float3 mainLightDir, float diffuse)
				{
					float fresnelass = fresnel(s.normal,s.view,_DarkRimPower);
					float mainDot = dot(s.normal,mainLightDir);

					float rim = saturate(1-saturate(step(fresnelass*mainDot,_DarkRimCutoff))*_DarkRimDarkness);
					float mask = saturate(step(step(1,(1-diffuse)*fresnel(s.view,s.normal,_DarkRimThreshold)),0.001));

					return saturate(rim+mask);
				}
				inline float quantize(float x)
				{
					return floor(x*_ShadeLevels)/_ShadeLevels;
				}
				inline float quantize(float3 x)
				{
					return floor(length(x)*_ShadeLevels)/_ShadeLevels;
				}
				float blueNoise2D(float2 uv)
				{
				    //  hash based on pixel coords (cheap for full-screen or terrain)
				    uv = fmod(uv, 256);                 // keep numbers small for precision
				    return frac(52.9829189 * frac(dot(uv, float2(0.06711056, 0.00583715))));
				}
				float noise2D(float2 uv)
				{
				    //  hash based on pixel coords (cheap for full-screen or terrain)
				    uv = fmod(uv, 256);                 // keep numbers small for precision
				    return frac(52.9829189 * frac(sin(dot(uv, float2(0.06711056, 0.00583715)))));
				}

				float3 quantizeDither(float3 v, float2 screenUV)
				{
				    float dither  = (blueNoise2D(screenUV/2) - 0.5) / _ShadeLevels;
				    return floor(saturate(length(v) + dither/2) * _ShadeLevels) * rcp(_ShadeLevels);
				}

				
				float4 frag(GeomData i) : SV_Target
				{
					float3 tangentNormal = UnpackNormal(tex2Dbias(_GroundNormal, float4(i.uv, 0, 1)));
					float3 tangentNormalStrong = normalize(tangentNormal*float3(5,5,1));
					float3x3 tbn = CreateTangentToWorld(normalize(i.normal), i.tangent,0);

					float3 normalWS      = normalize(TransformTangentToWorld(
                                     tangentNormal,      // TS normal
                                     tbn,           // interpolated WS normal
                                     false));        // interpolated WS tangent
					float3 normalWSStrong      = normalize(TransformTangentToWorld(
                                     tangentNormalStrong,      // TS normal
                                     tbn,           // interpolated WS normal
                                     false));      

					SurfaceVariables s;
					s.normal = normalWS;//i.normal;
					s.nomalStrong = normalWSStrong;
					s.view = GetWorldSpaceNormalizeViewDir(i.worldPos);
					float distanceFromCamera = distance(_WorldSpaceCameraPos.xyz, i.worldPos);
		    		float4 sample = mipOffsetSample(_GroundDiffuse,i.uv,distanceFromCamera);
					// float3 diffuseColor = sample.rgb* _Albedo;
					float3 diffuseColor = _Albedo;
					float alpha = sample.a;
					clip(alpha-(_AlphaClip));

				// Main directional light contribution
					VertexPositionInputs vertexInput = (VertexPositionInputs) 0;
					vertexInput.positionWS = i.worldPos;

					float4 shadowCoord = GetShadowCoord(vertexInput);
					Light light = GetMainLight(shadowCoord);
					float3 baseColor = diffuseColor;
					
					float3 gi = SAMPLE_GI(i.lightmapUV,float3(0,0,0),s.normal);       
					float3 lighting = lightContribution(light,s,baseColor); //*SampleAmbientOcclusion()
					float diffuseAccumulated = lightDiffuseAttenuated(light,s);
					// float3 litColor = baseColor*gi*32;
					float3 litColor = baseColor*lighting;
					litColor += baseColor*max(dot(s.normal,float3(0,1,0)),0.5)*max(light.shadowAttenuation,0.25)*float3(0.8,0.9,1); //hardcoded fill light
					
					 int pixelLightCount = GetAdditionalLightsCount();
					 for (int lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
					 {
					 	light = GetAdditionalLight(lightIndex, i.worldPos, 1);
					 	lighting = lightContribution(light,s,baseColor);
					 	diffuseAccumulated += lightDiffuseAttenuated(light,s);
					 	litColor += baseColor*lighting;
					 }

					float2 screenUV = GetNormalizedScreenSpaceUV(i.pos.xy);
				    float ao = pow(SampleAmbientOcclusion(screenUV),0.5);
					litColor*=(ao);
					// litColor/=max(deltadist/128,1);

					float3 finalColor = quantize(litColor)*litColor/max(distanceFromCamera/64,1) + _Ambient*baseColor*quantize((0.7+0.3*dot(normalize(s.view*0.25+float3(0,1,0)),s.nomalStrong))*ao);
					finalColor = finalColor * darkRim(s,GetMainLight(shadowCoord).direction,diffuseAccumulated)*pow(gi,0.25);
					float4 output = float4(finalColor,alpha); //float4(color,min((1-i.t)*8,1));
					Applyfog(output,i.worldPos);
					return output;
				}
			ENDHLSL
		}
		Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite on
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitGBufferPassVertex
            #pragma fragment LitGBufferPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"
            ENDHLSL
        }

		Pass
		{
		    Name "ShadowCaster"
		    Tags { "LightMode"="ShadowCaster" }

		    // Always write depth into the shadow map
		    ZWrite On
		    ZTest LEqual
		    Cull Back

		    // No colour buffer → no blend
		    Blend One Zero

		    HLSLPROGRAM
		        #pragma vertex   ShadowPassVertexCustom
		        #pragma fragment fragShadow

		        // Include the same alpha-test keyword you already use
		        #pragma shader_feature_local _ALPHATEST_ON

		        // GPU Instancing etc.
		        #pragma multi_compile_instancing
		        #pragma multi_compile _ DOTS_INSTANCING_ON
		        
	            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

		        struct shadowAttributes
			    {
			        float4 positionOS : POSITION;
			        float4 color      : COLOR;     // needed for vertexMask
		            float2 texcoord      : TEXCOORD0;
			    };

			    struct shadowVaryings
			    {
			        float4 positionCS : SV_POSITION;
			    	float3 positionWS : TEXCOORD0;
			        float2 uv         : TEXCOORD1;
			    };

			 //    shadowVaryings vertShadow(shadowAttributes IN)
				// {
			 //        UNITY_SETUP_INSTANCE_ID(IN);
			 //        shadowVaryings OUT;
			 //        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
			 //
			 //        /* ── copy the *same* bending / wind code ── */
			 //        float time = _Time.y * 0.5;
			 //        float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
			 //        float noise  = sin(posWS.x * 0.5 + time) *
			 //                       cos(posWS.z * 0.5 + time) *
			 //                       sin(time * 2.0);
			 //        float3 windDir = float3(sin(time * 0.5), 0, cos(time * 0.5));
			 //        float vertexMask = IN.color.r;
			 //        posWS += windDir * noise * 0.5 * vertexMask;
			 //
			 //        /* optional player bend – keep identical to forward pass */
			 //        float pDist = max(distance(_PlayerLoc + float3(0,4,0), posWS), 4);
			 //        if (pDist < 16)
			 //        {
			 //            float3 vecToPlayer = normalize(_PlayerLoc + _PlayerVel - posWS) * _PlayerLocMul;
			 //            float3 velVec      = _PlayerVel * _PlayerVelMul;
			 //            posWS -= ((velVec * _PlayerBendFactor / pDist) +
			 //                      (vecToPlayer / pow(pDist,1))) * vertexMask;
			 //        }
			 //
			 //        OUT.positionCS = TransformWorldToHClip(posWS);
			 //        OUT.uv         = IN.uv; // for α-clip
			 //        return OUT;
			 //    }
		        shadowVaryings ShadowPassVertexCustom(shadowAttributes input)
				{
				    shadowVaryings output;
				    UNITY_SETUP_INSTANCE_ID(input);
				    UNITY_TRANSFER_INSTANCE_ID(input, output);
				    
					float time = _Time.y * 0.5;
					float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
					float noise  = sin(posWS.x * 0.5 + time) *
					            cos(posWS.z * 0.5 + time) *
					            sin(time * 2.0);
					float3 windDir = float3(sin(time * 0.5), 0, cos(time * 0.5));
					float vertexMask = input.color.r;
					float3 delta = windDir * noise * 0.5 * vertexMask;

					/* optional player bend – keep identical to forward pass */
					float pDist = max(distance(_PlayerLoc + float3(0,4,0), posWS), 4);
					if (pDist < 16)
					{
						float3 vecToPlayer = normalize(_PlayerLoc + _PlayerVel - posWS) * _PlayerLocMul;
						float3 velVec      = _PlayerVel * _PlayerVelMul;
						delta -= ((velVec * _PlayerBendFactor / pDist) +
						       (vecToPlayer / pow(pDist,1))) * vertexMask;
					}

		        	
			 
			 
		        	output.uv = TRANSFORM_TEX(input.texcoord, _GroundDiffuse);
					Attributes csInput;
					csInput.positionOS = input.positionOS+float4(delta,0);
					csInput.normalOS = float3(0,1,0); 
					csInput.texcoord = input.texcoord;
				    output.positionCS = GetShadowPositionHClip(csInput);
				    return output;
				} 




	            // fragment entry-point expected by ShadowCasterPass.hlsl
	            float4 fragShadow (shadowVaryings i) : SV_Target
	            {
	                // Sample alpha exactly the same way as in the forward pass
	                // (these helpers come from the include above)
	            	float distanceFromCamera = distance(_WorldSpaceCameraPos.xyz, i.positionWS);
					float4 sample = tex2Dbias(_GroundDiffuse, float4(i.uv, 0, 0));
					float alpha = sample.a;
	                clip(alpha-(_ShadowClip-(distanceFromCamera/1024))); //0.36
	            	return 0;
	            }
		    ENDHLSL
		}

		Pass                           // depth-normals pass for SSAO
		{
		    Name "DepthNormals"
		    Tags { "LightMode" = "DepthNormals" }
		    Cull Off

		    HLSLPROGRAM
		    #pragma vertex vert
		    #pragma fragment fragDN

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		    // struct Attributes   { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv	  : TEXCOORD0;};
		    // struct Varyings     { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
		    //
		    // Varyings vertDN (Attributes v)
		    // {
		    //     Varyings o;
		    //     o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		    //     o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
		    // 	o.uv = TRANSFORM_TEX(v.uv, _GroundDiffuse);
		    //     return o;
		    // }

		    float4 fragDN (GeomData i) : SV_Target
		    {
				float distanceFromCamera = distance(_WorldSpaceCameraPos.xyz, i.worldPos);//LinearEyeDepth(i.pos.z / i.pos.w, _ZBufferParams);
		    	float4 sample = mipOffsetSample(_GroundDiffuse,i.uv,distanceFromCamera);
				float alpha = sample.a;
                clip(alpha-(_AlphaClip));
		        return float4(i.normal, 0);
		    }
		    ENDHLSL
		}

	}
}