Shader "Unlit/grass"
{
	Properties
	{
		_PlayerLoc("Player Location", Vector) = (0,0,0)
		_PlayerVel("Player Velocity", Vector) = (0,0,0)
		_PlayerBendFactor("Player Bend Factor", Float) = 0
		_MainTex ("Texture", 2D) = "white" {}
		_BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_TipColor("Tip Color", Color) = (1, 1, 1, 1)
		_ScatterStrength("Translucency", Range(0, 1)) = 0.3
		_ScatterTintStrength("Translucency Tint", Range(0, 1)) = 0.5

		_Scale("Scale", Range(0, 100)) = 1

		_BladeWidthMin("Blade Width (Min)", Range(0, 0.5)) = 0.02
		_BladeWidthMax("Blade Width (Max)", Range(0, 0.5)) = 0.05
		_BladeHeightMin("Blade Height (Min)", Range(0, 6)) = 0.1
		_BladeHeightMax("Blade Height (Max)", Range(0, 6)) = 0.2

		_BladeSegments("Blade Segments", Range(1, 10)) = 3
		_BladeBendDistance("Blade Forward Amount", Float) = 0.38
		_BladeBendCurve("Blade Curvature Amount", Range(1, 4)) = 2

		_BendDelta("Bend Variation", Range(0, 1)) = 0.2

		_TessellationGrassDistance("Tessellation Grass Distance", Float) = 0.1 // figure out a cleaner way to controll this
		_CullFalloff("Tessellation Grass Distance Falloff", Range(0, 10)) = 1
		_MaxTessellation("Maximum Tessellation", Float) = 0.1 

		_GrassMap("Grass Visibility Map", 2D) = "white" {}
		_GrassThreshold("Grass Visibility Threshold", Range(-0.1, 1)) = 0.5
		_GrassFalloff("Grass Visibility Fade-In Falloff", Range(0, 0.5)) = 0.05

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
	    "Queue" = "AlphaTest" // "Transparent" would be used for transparent objects //opaque?????CHECK THIS
	    "RenderPipeline" = "UniversalPipeline"
	    "LightMode" = "UniversalForward"
	}

//		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		Cull Off 

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		

			#pragma shader_feature _ _MAIN_LIGHT_SHADOWS
			#pragma shader_feature _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma shader_feature _ _ADDITIONAL_LIGHTS
			#pragma shader_feature _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma shader_feature _ _SHADOWS_SOFT
			#pragma shader_feature _ _SCREEN_SPACE_OCCLUSION // needed?
			#pragma multi_compile _ LIGHTMAP_ON              // ★ static light-maps
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED     // ★ directional light-map
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON       // ★ realtime GI needed?
			// #pragma multi_compile _ _FORWARD_PLUS

			#define UNITY_PI 3.14159265359f
			#define UNITY_TWO_PI 6.28318530718f
			#define BLADE_SEGMENTS 4
			#define _MAIN_LIGHT_SHADOWS
			
			CBUFFER_START(UnityPerMaterial)
				float3 _PlayerLoc;
				float3 _PlayerVel;
				float _PlayerBendFactor;
			
				float4 _BaseColor;
				float4 _TipColor;
				float _ScatterStrength;
				float _ScatterTintStrength;

				float _BladeWidthMin;
				float _BladeWidthMax;
				float _BladeHeightMin;
				float _BladeHeightMax;

				float _Scale;

				float _BladeBendDistance;
				float _BladeBendCurve;

				float _BendDelta;

				float _TessellationGrassDistance;
				float _CullFalloff;
				float _MaxTessellation;

				sampler2D _GrassMap;
				float4 _GrassMap_ST;
				float _GrassThreshold;
				float _GrassFalloff;

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

			//VERTEX INPUT & OUTPUT MUST HAVE SAME MEM STRUCTURE/ ARGS!!!
			struct VertexInput
			{
				float4 vertex  : POSITION;
				half3 normal  : NORMAL;
				half4 tangent : TANGENT;
				half2 uv      : TEXCOORD0;
				half2 uvLM     : TEXCOORD1;
				half3 vertexSH   : TEXCOORD5; 
				float perlin : TEXCOORD3;
				float mask : TEXCOORD4;
				float4 color : COLOR;
			};

			struct VertexOutput
			{
				float4 vertex  : POSITION;
				half3 normal  : NORMAL;
				half4 tangent : TANGENT;
				half2 uv      : TEXCOORD0;
				half2 uvLM     : TEXCOORD1;
				half3 vertexSH   : TEXCOORD5; 
				float perlin : TEXCOORD3;
				float mask : TEXCOORD4;
				float4 color : COLOR;
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside  : SV_InsideTessFactor;
			};

			struct GeomData
			{
				float4 pos : SV_POSITION;
				half2 uv  : TEXCOORD0;
				half2 uvLM     : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				half3 normal : NORMAL; // [n]???
				float t : TEXCOORD3;
				half3 vertexSH   : TEXCOORD5; 
			};

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


			// Following functions from Roystan's code:
			// (https://github.com/IronWarrior/UnityGrassGeometryShader)

			// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
			// Extended discussion on this function can be found at the following link:
			// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
			// Returns a number in the 0...1 range.
			float rand(float3 co)
			{
				return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
			}

			// Construct a rotation matrix that rotates around the provided axis, sourced from:
			// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
			float3x3 angleAxis3x3(float angle, float3 axis)
			{
				float c, s;
				sincos(angle, s, c);

				float t = 1 - c;
				float x = axis.x;
				float y = axis.y;
				float z = axis.z;

				return float3x3
				(
					t * x * x + c, t * x * y - s * z, t * x * z + s * y,
					t * x * y + s * z, t * y * y + c, t * y * z - s * x,
					t * x * z - s * y, t * y * z + s * x, t * z * z + c
				);
			}

			// Regular vertex shader used by typical shaders.
			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.normal = v.normal;
				o.tangent = v.tangent;
				o.uv = TRANSFORM_TEX(v.uv, _GrassMap);
				o.uvLM = v.uvLM * unity_LightmapST.xy + unity_LightmapST.zw;
				o.vertexSH = SampleSHVertex(TransformObjectToWorldNormal(v.normal));
				o.perlin = v.perlin;
				o.mask = v.mask;
				o.color = v.color;
				return o;
			}

			// Vertex shader which just passes data to tessellation stage.
			VertexOutput tessVert(VertexInput v)
			{
				VertexOutput o;
				o.vertex = v.vertex;
				o.normal = v.normal;
				o.tangent = v.tangent;
				o.uv = v.uv;
				o.uvLM = v.uvLM;
				o.vertexSH = SampleSHVertex(TransformObjectToWorldNormal(v.normal));
				o.perlin = v.perlin;
				o.mask = v.mask;
				o.color = v.color;
				return o;
			}

			// Vertex shader which translates from object to world space.
			VertexOutput geomVert (VertexInput v)
            {
				VertexOutput o; 
				o.vertex = float4(v.vertex.xyz, 1.0f); //check if this makes any sense???
				o.normal = TransformObjectToWorldNormal(v.normal);
				o.tangent = v.tangent;
				o.uv = TRANSFORM_TEX(v.uv, _GrassMap);// needed both here and in vert?
				  // Sample wind map for static variation
				o.uvLM = v.uvLM * unity_LightmapST.xy + unity_LightmapST.zw; // needed both here and in vert?
				o.vertexSH = SampleSHVertex(TransformObjectToWorldNormal(v.normal));
			    float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
			    float2 windUV = TRANSFORM_TEX(float2(worldPos.x, worldPos.z), _WindMap);
			    o.perlin = tex2Dlod(_WindMap, float4(windUV*8, 0, 0)).r;
				o.mask = tex2Dlod(_GrassMap, float4(o.uv,0,0)).r;
				o.color = v.color;
                return o;
            }

			// This function lets us derive the tessellation factor for an edge
			// from the vertices.
			float tessellationEdgeFactor(VertexInput vert0, VertexInput vert1)
			{
				float3 v0 = vert0.vertex.xyz;
				float3 v1 = vert1.vertex.xyz;
				float edgeLength = distance(v0, v1);
				return edgeLength / _TessellationGrassDistance;
			}

			// This is a test version of the tessellation that takes distance from the viewer
			// into account. It works fine, but I think it could do with refinement.
			float tessellationEdgeFactor_distanceTest(VertexInput vert0, VertexInput vert1,float mask)
			{
				float3 v0 = vert0.vertex.xyz;
				float3 v1 = vert1.vertex.xyz;
				float edgeLength = distance(v0, v1);

				float3 edgeCenter = (v0 + v1) * 0.5f;
				float viewDist = pow(distance(TransformObjectToWorld(edgeCenter), _WorldSpaceCameraPos),_CullFalloff); //rework falloff and grassdistance params

				return min(edgeLength * _ScreenParams.y / (_TessellationGrassDistance * viewDist) ,_MaxTessellation)*smoothstep(_GrassThreshold, _GrassThreshold + _GrassFalloff, mask);
			}

			// Tessellation hull and domain shaders derived from Catlike Coding's tutorial:
			// https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/

			// The patch constant function is where we create new control
			// points on the patch. For the edges, increasing the tessellation
			// factors adds new vertices on the edge. Increasing the inside
			// will add more 'layers' inside the new triangle.
			TessellationFactors patchConstantFunc(InputPatch<VertexInput, 3> patch)
			{
				TessellationFactors f;

				f.edge[0] = tessellationEdgeFactor_distanceTest(patch[1], patch[2],patch[0].color.r); //patch[0].color.r /.mask
				f.edge[1] = tessellationEdgeFactor_distanceTest(patch[2], patch[0],patch[0].color.r);
				f.edge[2] = tessellationEdgeFactor_distanceTest(patch[0], patch[1],patch[0].color.r);
				f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0f;

				return f;
			}

			// The hull function is the first half of the tessellation shader.
			// It operates on each patch (in our case, a patch is a triangle),
			// and outputs new control points for the other tessellation stages.
			//
			// The patch constant function is where we create new control points
			// (which are kind of like new vertices).
			[domain("tri")]
			[outputcontrolpoints(3)]
			[outputtopology("triangle_cw")]
			[partitioning("integer")]
			[patchconstantfunc("patchConstantFunc")]
			VertexInput hull(InputPatch<VertexInput, 3> patch, uint id : SV_OutputControlPointID) // do I need to do perlin separate?
			{
			    VertexInput v = patch[id];
			    
			    // Pass through the perlin value
			    v.perlin = patch[id].perlin;
				v.mask = patch[id].mask;
				v.color = patch[id].color;
			    
			    return v;
			}


			// In between the hull shader stage and the domain shader stage, the
			// tessellation stage takes place. This is where, under the hood,
			// the graphics pipeline actually generates the new vertices.

			// The domain function is the second half of the tessellation shader.
			// It interpolates the properties of the vertices (position, normal, etc.)
			// to create new vertices.
			[domain("tri")]
			VertexOutput domain(TessellationFactors factors, OutputPatch<VertexInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
			{
			    VertexInput v;
			    
			    // Barycentric interpolation of vertex data
			    v.vertex = patch[0].vertex * barycentricCoordinates.x + 
			               patch[1].vertex * barycentricCoordinates.y + 
			               patch[2].vertex * barycentricCoordinates.z;
			    v.normal = patch[0].normal * barycentricCoordinates.x + 
			               patch[1].normal * barycentricCoordinates.y + 
			               patch[2].normal * barycentricCoordinates.z;
			    v.tangent = patch[0].tangent * barycentricCoordinates.x + 
			                patch[1].tangent * barycentricCoordinates.y + 
			                patch[2].tangent * barycentricCoordinates.z;
			    v.uv = patch[0].uv * barycentricCoordinates.x + 
			           patch[1].uv * barycentricCoordinates.y + 
			           patch[2].uv * barycentricCoordinates.z;
				v.uvLM = patch[0].uvLM * barycentricCoordinates.x + 
			           patch[1].uvLM * barycentricCoordinates.y + 
			           patch[2].uvLM * barycentricCoordinates.z;
			    
			    // Interpolate the perlin value
			    v.perlin = patch[0].perlin * barycentricCoordinates.x + 
			               patch[1].perlin * barycentricCoordinates.y + 
			               patch[2].perlin * barycentricCoordinates.z;

				v.mask = patch[0].mask * barycentricCoordinates.x + 
			               patch[1].mask * barycentricCoordinates.y + 
			               patch[2].mask * barycentricCoordinates.z;
				v.color = 
				        patch[0].color * barycentricCoordinates.x +
				        patch[1].color * barycentricCoordinates.y +
				        patch[2].color * barycentricCoordinates.z;

			    
			    
			    // Create the output structure (can trim some fat here?)
			    VertexOutput o;
			    o.vertex = float4(v.vertex.xyz, 1.0f);
			    o.normal = TransformObjectToWorldNormal(v.normal);
			    o.tangent = v.tangent;
			    o.uv = TRANSFORM_TEX(v.uv, _GrassMap);// needed here and in geom/vert?
				o.uvLM = v.uvLM * unity_LightmapST.xy + unity_LightmapST.zw; // needed here and in geom/vert?
				o.vertexSH = SampleSHVertex(TransformObjectToWorldNormal(v.normal));
			    o.perlin = v.perlin; // Pass the interpolated perlin value
				o.mask = v.mask;
				o.color = v.color;
			    
			    return o;
			}


			// Geometry functions derived from Roystan's tutorial:
			// https://roystan.net/articles/grass-shader.html

			            // Hash function that converts 3D input to 1D output
			            float hash31(float3 p)
			            {
			                float h = dot(p, float3(127.1, 311.7, 74.7));
			                return frac(sin(h)*43758.5453123);
			            }
			
			            // Hash function that converts 3D input to 3D output
			            float3 hash33(float3 p)
			            {
			                float3 h = float3(dot(p, float3(127.1, 311.7, 74.7)),
			                                dot(p, float3(269.5, 183.3, 246.1)),
			                                dot(p, float3(419.2, 371.9, 158.2)));
			                return frac(sin(h)*43758.5453123);
			            }
			
			// This function applies a transformation (during the geometry shader),
			// converting to clip space in the process.
			GeomData TransformGeomToClip(float3 pos, float3 offset, float3x3 transformationMatrix, float2 uv,float2 uvLM,float3 vertexSH, float3 normal,float t)
			{
				GeomData o;

				o.pos = TransformObjectToHClip(pos + mul(transformationMatrix, offset));
				o.uv = uv;
				o.uvLM = uvLM;
				o.vertexSH = vertexSH;
				o.worldPos = TransformObjectToWorld(pos + mul(transformationMatrix, offset));
				// o.normal = TransformObjectToWorldNormal(normalize(mul(transformationMatrix,normal)));

				// o.normal = normal;
				o.normal =TransformObjectToWorldDir(normal);
				o.t = t;
				///OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);??


				return o;
			}
			

			// Updated maxvertexcount: original blade vertices + 1 for the pass-through vertex.
			
			[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
			void geom(point VertexOutput input[1], inout TriangleStream<GeomData> triStream)
			{
			    float3 pos = input[0].vertex.xyz + hash33(input[0].vertex.xyz)*float3(1,0,1);
				

			    float grassVisibility = input[0].color.r; //tex2Dlod(_GrassMap, float4(input[0].uv, 0, 0)).r;
			    
			    if (grassVisibility >= _GrassThreshold)
			    {
			        float3 normal = input[0].normal;
			        float4 tangent = input[0].tangent;
			        float3 bitangent = cross(normal, tangent.xyz) * tangent.w;
			    	float3 upBiasedNormal = normalize(normal + float3(0,1,0)*2);// work out a better solution to worldspace up/ normal param here

			        float3x3 tangentToLocal = float3x3
			                    (
			                        tangent.x, bitangent.x, upBiasedNormal.x,// work out a better solution to worldspace up/ normal param here
			                        tangent.y, bitangent.y, upBiasedNormal.y,
			                        tangent.z, bitangent.z, upBiasedNormal.z
			                    );

			        // Rotate around the y-axis a random amount.
			        float3x3 randRotMatrix = angleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1.0f));

			        // Rotate around the bottom of the blade a random amount.
			        float3x3 randBendMatrix = angleAxis3x3(rand(pos.zzx) * _BendDelta * UNITY_PI * 0.5f, float3(-1.0f, 0, 0));

			        float time = _Time.y * _Timescale;
			    	float3 worldPos = TransformObjectToWorld(pos);
			        float pointHash = worldPos.x + worldPos.y + worldPos.z; //rand(pos.xyz?)

			       	float2 windUV = float2(worldPos.x  + (time + sin((time + worldPos.x) * _GustScale ) *_WindTurbulence) * _WindVelocity.x, 
			                                worldPos.z  + (time + sin((time + worldPos.z) * _GustScale) *_WindTurbulence) * _WindVelocity.z)*_WindMap_ST.xy;
					double windSample = pow(tex2Dlod(_WindMap, float4(windUV, 0, 0) * 2 - 1).r*2, _WindGustiness)*_WindStrength; // maybe move texture lookup to vertex shader

			        float2 windUVAux = float2(pos.x * _WindMap_ST.x / 4 + (time + sin((time + pointHash) * 64) * 0.0078125) * _WindVelocity.x, //check needed?
			                                   pos.z * _WindMap_ST.y + (time + sin((time + pointHash) * 64) * 0.0078125) * _WindVelocity.z);
			        float windSampleAux = 0;//pow(tex2Dlod(_WindMap, float4(windUVAux, 0, 0) * 2 - 1).r, _WindGustiness) * UNITY_TWO_PI;

			        float3x3 windMatrix = angleAxis3x3(_WindStrength * windSample, _WindVelocity + float3(sin(windSampleAux) / 2, 0, cos(windSampleAux) / 2));

			        // Transform the grass blades to the correct tangent space. 
			        float3x3 baseTransformationMatrix = mul(tangentToLocal, randRotMatrix);
			        float3x3 tipTransformationMatrix = mul(windMatrix, mul(tangentToLocal, randRotMatrix));
			    	float rad = 16;
			    	float pDist = distance(_PlayerLoc,worldPos);
			    	if (pDist < rad)
			    	{
			    		float3 worldspaceUp = mul(tangentToLocal, float3(0, 0, 1));
			    		tipTransformationMatrix = mul(angleAxis3x3(min(_PlayerBendFactor /pow(pDist,1-_PlayerVel.y*0.01),1.57079632679),cross(normalize(_PlayerLoc-worldPos -(_PlayerVel+input[0].perlin*0)),worldspaceUp)),tipTransformationMatrix);
			    	}

			        float falloff = smoothstep(_GrassThreshold, _GrassThreshold + _GrassFalloff, grassVisibility);

			        // In your geometry shader:
			        float3 cameraPos = _WorldSpaceCameraPos.xyz; // Built-in Unity variable
			        float3 vertexWorldPos = mul(unity_ObjectToWorld, float4(pos, 1.0)).xyz;
			        float distanceToCamera = length(vertexWorldPos - cameraPos);

			    	float sizeScale = input[0].perlin;
			        float width = lerp(_BladeWidthMin, _BladeWidthMax, rand(pos.xzy) * falloff) * max(1.0, distanceToCamera/64)+sizeScale*_BladeWidthMax;
			        float height = lerp(_BladeHeightMin, _BladeHeightMax, rand(pos.zyx) * falloff)+sizeScale*_BladeHeightMax*2;
			        float forward = rand(pos.yyz) * _BladeBendDistance;

			    	
			        // Create blade segments by adding two vertices at once.
			    	float3 vertexSH = input[0].vertexSH;
			        [unroll] for (int i = 0; i < BLADE_SEGMENTS; ++i)
			        {
			            float t = i / (float)BLADE_SEGMENTS;
			            float3 offset = float3(width * (1 - t), pow(t, _BladeBendCurve) * forward, height * t);
			            float3x3 transformationMatrix = (i == 0) ? baseTransformationMatrix : tipTransformationMatrix;
			            
			            // Calculate blade direction vectors using analytical derivative.
			            float forwardDerivative = _BladeBendCurve * pow(t, _BladeBendCurve - 1) * forward;
			            float3 tangentAlongBlade = normalize(mul(transformationMatrix, float3(-width, forwardDerivative, height)));
			            float3 tangentWidth = normalize(mul(transformationMatrix, float3(1, 0, 0)));
						// float3 normalDir = lerp(worldspaceUp, cross(tangentWidth, tangentAlongBlade), t);
			        	float3 normalDir = cross(tangentWidth, tangentAlongBlade);
						
			            // Left side vertex
			            triStream.Append(TransformGeomToClip(pos, float3(offset.x, offset.y, offset.z) * _Scale, 
			                                                  transformationMatrix, float2(0, t),input[0].uvLM,vertexSH, normalize(normalDir + float3((1-t)+0.4, 0, 0)), windSample));
			            // Right side vertex (using negative normal for proper back-face lighting)
			            triStream.Append(TransformGeomToClip(pos, float3(-offset.x, offset.y, offset.z) * _Scale, 
			                                                  transformationMatrix, float2(1, t),input[0].uvLM,vertexSH, normalize(normalDir + float3(-(1-t)-0.4, 0, 0)), windSample));
			        }

			        // Tip vertex
			        float t_tip = 1.0;
			        float forwardDerivative_tip = _BladeBendCurve * pow(t_tip, _BladeBendCurve - 1) * forward;
			        float3 tangentAlongBlade_tip = normalize(mul(tipTransformationMatrix, float3(0, forwardDerivative_tip, height)));
			        float3 tangentWidth_tip = normalize(mul(tipTransformationMatrix, float3(1, 0, 0)));
			        float3 tipNormal = normalize(cross(tangentWidth_tip, tangentAlongBlade_tip));//lerp(normalize(cross(tangentWidth_tip, tangentAlongBlade_tip)),worldspaceUp,distanceToCamera*0.01)

			        triStream.Append(TransformGeomToClip(pos, float3(0, forward, height) * _Scale, 
			                                              tipTransformationMatrix, float2(0.5, 1),input[0].uvLM,vertexSH,tipNormal , windSample));

			        triStream.RestartStrip();
			    }
			}
		ENDHLSL

		// This pass draws the grass blades generated by the geometry shader.
        Pass
        {
			Name "GrassPass"
			Tags { "LightMode" = "UniversalForward" }
			ZWrite On //?

            HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
				
				#pragma require geometry
				#pragma require tessellation tessHW
				#pragma multi_compile_fog 
				// #pragma multi_compile _ _FORWARD_PLUS

				#pragma vertex geomVert
				#pragma hull hull
				#pragma domain domain
				#pragma geometry geom
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
				
				float edgeFactor(float2 uv)
				{
					// Compute screen-space derivatives of the UV coordinates
					float2 dx = ddx(uv);
					float2 dy = ddy(uv);
					float edgeDistance = length(dx) + length(dy);
					return saturate(0.1 / edgeDistance);
				}
				inline float quantize(float x)
				{
					return floor(x*ShadeLevels)/ShadeLevels;
				}
				inline float quantize(float3 x)
				{
					return floor(length(x)*ShadeLevels)/ShadeLevels;
				}				

				float3 lightContribution(Light l, SurfaceVariables s, float dist, float t, float specularModifier,float diffuseModifier) {
					float diffuseFront = max(0, dot(s.normal, l.direction)); 
					float diffuseBack = max(0, dot(-s.normal, l.direction));
					float attenuation = l.distanceAttenuation*l.shadowAttenuation;
					// this stuff here with pow(..,diffuseModifier) is probably not the right way
					float3 diffuse = l.color.rgb * pow(diffuseFront,diffuseModifier) + lerp(l.color.rgb, _BaseColor, _ScatterTintStrength) * pow(diffuseBack,diffuseModifier) * _ScatterStrength;

					// Specular lighting calculation
					float3 viewDir = normalize(s.view);
					float3 incidentLightDir = -l.direction;
					float3 reflectDir = reflect(incidentLightDir, s.normal);
					float RdotV = max(0.0, dot(reflectDir, viewDir));

					float shininess = exp2(4* Smoothness + 1); 
					float specularIntensity = pow(RdotV, shininess)*specularModifier;
					float3 specular = l.color.rgb * lerp(0,specularIntensity * Smoothness*8,pow(t,2));

					float3 lighting = diffuse + specular;
					return lerp(lighting, l.color.rgb + min(specular,0.2), min(max(dist,1-t), 1))* attenuation;
				}

				float4 frag(GeomData i) : SV_Target
				{
					float distanceFromCamera = LinearEyeDepth(i.pos.z / i.pos.w, _ZBufferParams)/(pow(2,13)) +0.3;
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

					if (dot(s.view, i.normal) < 0) //CHECK THIS IS RIGHT!!!!
					{
						s.normal = lerp(-i.normal,normalize(mul(unity_ObjectToWorld, float4(0, 1, 0, 0)).xyz),0);
					}else
					{
						s.normal = lerp(i.normal,normalize(mul(unity_ObjectToWorld, float4(0, 1, 0, 0)).xyz),0);//min(distanceFromCamera,1.0)
					}
					
					s.smoothness = Smoothness;
					s.shininess = exp2(10 * Smoothness + 1);
					s.rimThreshold = RimThreshold;
					s.rimBrightness = RimBrightness;
					s.shadeLevels = floor(ShadeLevels);
					s.tint = Tint;
					s.tintPower = TintPower;
					s.ec = ec;
		
					VertexPositionInputs vertexInput = (VertexPositionInputs) 0;
					vertexInput.positionWS = i.worldPos;

					float4 shadowCoord = GetShadowCoord(vertexInput);
					Light light = GetMainLight(shadowCoord);
					float3 baseColor = lerp((lerp(_BaseColor, _TipColor, i.uv.y) - i.t/64),_BaseColor,min(distanceFromCamera,1.0));
					
					float3 lighting = lightContribution(light,s,distanceFromCamera,i.uv.y,0.25,0.5); //*SampleAmbientOcclusion()
					float3 litColor = baseColor*lighting;

					float3 finalLight = light.color;
					
					 int pixelLightCount = GetAdditionalLightsCount();
					 for (int lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
					 {
					 	Light light = GetAdditionalLight(lightIndex, i.worldPos,shadowCoord);
					 	lighting = lightContribution(light,s,distanceFromCamera,i.uv.y,2,1);
					 	litColor += baseColor*lighting;
					 	finalLight += light.color;
					 }
					float2 screenUV = GetNormalizedScreenSpaceUV(i.pos.xy);
					float ao = 1-SampleAmbientOcclusion(screenUV);
					ao = pow(ao,2);
					ao = 1-min(ao,1);
					float3 finalColor = quantize(litColor)*litColor + quantize(ao)*_Ambient*baseColor*lerp(1 + floor(max(dot(s.normal,float3(0,1,0)),0)*s.shadeLevels*1.4)/s.shadeLevels,1,min(max(distanceFromCamera,1-i.uv.y), 1)); //floor(max(dot(s.normal,float3(0,1,0)),0.5)*s.shadeLevels)/s.shadeLevels
					
					float3 gi = SAMPLE_GI(i.uvLM,float3(0,0,0),s.normal);
					finalColor*=pow(gi,0.25);
					float4 output = float4(finalColor,1); //float4(color,min((1-i.t)*8,1));
					Applyfog(output,i.worldPos);
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
				#pragma vertex vert

				#pragma fragment ShadowPassFragment

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

//		Pass {
//			Name"ShadowCaster"
//			Tags
//			{"LightMode"="ShadowCaster"
//			}
//
//			ZWrite On    
//
//			ZTest LEqual
//
//			HLSLPROGRAM
//				#pragma require geometry
//				#pragma require tessellation tessHW
//
//				#pragma vertex geomVert
//				#pragma hull hull
//				#pragma domain domain
//				#pragma geometry geom
//
//				#pragma fragment ShadowPassFragment
//
//				// Material Keywords
//				#pragma shader_feature _ALPHATEST_ON
//				#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//				// GPU Instancing
//				#pragma multi_compile_instancing
//				// (Note, this doesn't support instancing for properties though. Same as URP/Lit)
//				#pragma multi_compile _ DOTS_INSTANCING_ON
//				// (This was handled by LitInput.hlsl. I don't use DOTS so haven't bothered to support it)
//
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
//			ENDHLSL
//		}
		
		Pass                           // depth-normals pass for SSAO
		{
		    Name "DepthNormals"
		    Tags { "LightMode" = "DepthNormals" }

		    HLSLPROGRAM
		    #pragma vertex geomVert
			#pragma hull hull
			#pragma domain domain
			#pragma geometry geom
		    #pragma fragment fragDN

		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		    struct Attributes   { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
		    struct Varyings     { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0;float t : TEXCOORD2; };

		    float4 fragDN (Varyings i) : SV_Target
		    {
		        return float4(lerp(i.normalWS,float3(0,1,0),i.t), 1);
		    }
		    ENDHLSL
		}
    }
}