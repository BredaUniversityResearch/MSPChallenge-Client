// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.30 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.30;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:True,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:2,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False;n:type:ShaderForge.SFN_Final,id:1,x:33690,y:32156,varname:node_1,prsc:2|emission-8315-OUT,alpha-8789-OUT;n:type:ShaderForge.SFN_Tex2d,id:151,x:33055,y:32383,ptovrint:False,ptlb:Pattern,ptin:_Pattern,varname:_Diffuse,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:6097c65d68a6b5a4ea069d8d32d6cee0,ntxv:0,isnm:False|UVIN-3082-OUT;n:type:ShaderForge.SFN_Color,id:5880,x:33025,y:32190,ptovrint:False,ptlb:Color,ptin:_Color,varname:node_5880,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_Multiply,id:8315,x:33265,y:32215,varname:node_8315,prsc:2|A-5880-RGB,B-151-RGB;n:type:ShaderForge.SFN_Multiply,id:3082,x:32875,y:32432,varname:node_3082,prsc:2|A-578-OUT,B-7357-XYZ;n:type:ShaderForge.SFN_Multiply,id:3955,x:33265,y:32383,varname:node_3955,prsc:2|A-5880-A,B-151-A;n:type:ShaderForge.SFN_ScreenPos,id:302,x:32250,y:32013,varname:node_302,prsc:2,sctp:0;n:type:ShaderForge.SFN_Vector4Property,id:7625,x:32485,y:32397,ptovrint:False,ptlb:Offset,ptin:_Offset,varname:node_7625,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1,v2:1,v3:0,v4:0;n:type:ShaderForge.SFN_Add,id:578,x:32709,y:32317,varname:node_578,prsc:2|A-4139-OUT,B-7625-XYZ;n:type:ShaderForge.SFN_Vector4Property,id:7357,x:32485,y:32597,ptovrint:False,ptlb:Scale,ptin:_Scale,varname:node_7357,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0.5,v2:0.5,v3:0,v4:0;n:type:ShaderForge.SFN_Add,id:4139,x:32457,y:32156,varname:node_4139,prsc:2|A-302-UVOUT,B-9602-UVOUT;n:type:ShaderForge.SFN_TexCoord,id:9602,x:32250,y:32204,varname:node_9602,prsc:2,uv:1;n:type:ShaderForge.SFN_TexCoord,id:1433,x:32852,y:32641,varname:node_1433,prsc:2,uv:0;n:type:ShaderForge.SFN_Tex2d,id:5176,x:33055,y:32639,ptovrint:False,ptlb:Inner Glow,ptin:_InnerGlow,varname:node_5176,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-1433-UVOUT;n:type:ShaderForge.SFN_Multiply,id:8789,x:33472,y:32445,varname:node_8789,prsc:2|A-3955-OUT,B-5176-A;proporder:151-5880-7625-7357-5176;pass:END;sub:END;*/

Shader "PolygonShaderWithInnerGlow" {
    Properties {
        _Pattern ("Pattern", 2D) = "white" {}
        _Color ("Color", Color) = (1,0,0,1)
        _Offset ("Offset", Vector) = (1,1,0,0)
        _Scale ("Scale", Vector) = (0.5,0.5,0,0)
        _InnerGlow ("Inner Glow", 2D) = "white" {}
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#ifndef UNITY_PASS_FORWARDBASE
            #define UNITY_PASS_FORWARDBASE
#endif
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma exclude_renderers xbox360 ps3 
            #pragma target 3.0
            uniform sampler2D _Pattern; uniform float4 _Pattern_ST;
            uniform float4 _Color;
            uniform float4 _Offset;
            uniform float4 _Scale;
            uniform sampler2D _InnerGlow; uniform float4 _InnerGlow_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.pos = UnityObjectToClipPos(v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
////// Lighting:
////// Emissive:
                float3 node_3082 = ((float3((i.screenPos.rg+i.uv1),0.0)+_Offset.rgb)*_Scale.rgb);
                float4 _Pattern_var = tex2D(_Pattern,TRANSFORM_TEX(node_3082, _Pattern));
                float3 emissive = (_Color.rgb*_Pattern_var.rgb);
                float3 finalColor = emissive;
                float4 _InnerGlow_var = tex2D(_InnerGlow,TRANSFORM_TEX(i.uv0, _InnerGlow));
                fixed4 finalRGBA = fixed4(finalColor,((_Color.a*_Pattern_var.a)*_InnerGlow_var.a));
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "Meta"
            Tags {
                "LightMode"="Meta"
            }
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#ifndef UNITY_PASS_META
            #define UNITY_PASS_META 1
#endif
            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma exclude_renderers xbox360 ps3 
            #pragma target 3.0
            uniform sampler2D _Pattern; uniform float4 _Pattern_ST;
            uniform float4 _Color;
            uniform float4 _Offset;
            uniform float4 _Scale;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv1 : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv1 = v.texcoord1;
                o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST );
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : SV_Target {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT( UnityMetaInput, o );
                
                float3 node_3082 = ((float3((i.screenPos.rg+i.uv1),0.0)+_Offset.rgb)*_Scale.rgb);
                float4 _Pattern_var = tex2D(_Pattern,TRANSFORM_TEX(node_3082, _Pattern));
                o.Emission = (_Color.rgb*_Pattern_var.rgb);
                
                float3 diffColor = float3(0,0,0);
                o.Albedo = diffColor;
                
                return UnityMetaFragment( o );
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
