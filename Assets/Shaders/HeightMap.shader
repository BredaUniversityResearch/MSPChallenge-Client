// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:3,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:31798,y:32990,varname:node_3138,prsc:2|emission-1623-OUT;n:type:ShaderForge.SFN_Tex2d,id:5348,x:30283,y:33017,ptovrint:False,ptlb:Height Map,ptin:_MainTex,varname:_node_5348,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Color,id:5054,x:31339,y:32818,ptovrint:False,ptlb:Deep Colour,ptin:_MinColour,varname:node_5054,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_Color,id:6249,x:31339,y:33011,ptovrint:False,ptlb:Shallow Colour,ptin:_MaxColour,varname:node_6249,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.2551723,c3:1,c4:1;n:type:ShaderForge.SFN_Lerp,id:1623,x:31583,y:33087,varname:node_1623,prsc:2|A-5054-RGB,B-6249-RGB,T-6271-OUT;n:type:ShaderForge.SFN_Vector1,id:4152,x:30305,y:33210,varname:node_4152,prsc:2,v1:1;n:type:ShaderForge.SFN_Vector1,id:1948,x:30305,y:33340,varname:node_1948,prsc:2,v1:0.9607843;n:type:ShaderForge.SFN_Vector1,id:5102,x:30305,y:33467,varname:node_5102,prsc:2,v1:0.9215686;n:type:ShaderForge.SFN_Vector1,id:4018,x:30305,y:33582,varname:node_4018,prsc:2,v1:0.8431373;n:type:ShaderForge.SFN_Vector1,id:4718,x:30305,y:33705,varname:node_4718,prsc:2,v1:0.6078432;n:type:ShaderForge.SFN_Vector1,id:2791,x:30305,y:33827,varname:node_2791,prsc:2,v1:0.4117647;n:type:ShaderForge.SFN_Vector1,id:9161,x:30305,y:33949,varname:node_9161,prsc:2,v1:0.2156863;n:type:ShaderForge.SFN_Vector1,id:9442,x:30305,y:34070,varname:node_9442,prsc:2,v1:0.01960784;n:type:ShaderForge.SFN_Step,id:1586,x:30527,y:33176,varname:node_1586,prsc:2|A-5348-R,B-4152-OUT;n:type:ShaderForge.SFN_Step,id:7259,x:30527,y:33306,varname:node_7259,prsc:2|A-5348-R,B-1948-OUT;n:type:ShaderForge.SFN_Step,id:3808,x:30527,y:33433,varname:node_3808,prsc:2|A-5348-R,B-5102-OUT;n:type:ShaderForge.SFN_Step,id:1883,x:30527,y:33548,varname:node_1883,prsc:2|A-5348-R,B-4018-OUT;n:type:ShaderForge.SFN_Step,id:2831,x:30527,y:33671,varname:node_2831,prsc:2|A-5348-R,B-4718-OUT;n:type:ShaderForge.SFN_Step,id:7619,x:30527,y:33793,varname:node_7619,prsc:2|A-5348-R,B-2791-OUT;n:type:ShaderForge.SFN_Step,id:1970,x:30527,y:33915,varname:node_1970,prsc:2|A-5348-R,B-9161-OUT;n:type:ShaderForge.SFN_Step,id:1856,x:30527,y:34036,varname:node_1856,prsc:2|A-5348-R,B-9442-OUT;n:type:ShaderForge.SFN_Vector1,id:4924,x:30633,y:33210,varname:node_4924,prsc:2,v1:1;n:type:ShaderForge.SFN_Vector1,id:8968,x:30633,y:33340,varname:node_8968,prsc:2,v1:0.875;n:type:ShaderForge.SFN_Vector1,id:4117,x:30633,y:33467,varname:node_4117,prsc:2,v1:0.75;n:type:ShaderForge.SFN_Vector1,id:8665,x:30633,y:33582,varname:node_8665,prsc:2,v1:0.625;n:type:ShaderForge.SFN_Vector1,id:272,x:30633,y:33705,varname:node_272,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Vector1,id:5028,x:30633,y:33827,varname:node_5028,prsc:2,v1:0.375;n:type:ShaderForge.SFN_Vector1,id:1478,x:30633,y:33949,varname:node_1478,prsc:2,v1:0.25;n:type:ShaderForge.SFN_Vector1,id:6466,x:30633,y:34070,varname:node_6466,prsc:2,v1:0.125;n:type:ShaderForge.SFN_Multiply,id:9924,x:30802,y:33176,varname:node_9924,prsc:2|A-1586-OUT,B-4924-OUT;n:type:ShaderForge.SFN_Multiply,id:2935,x:30802,y:33306,varname:node_2935,prsc:2|A-7259-OUT,B-8968-OUT;n:type:ShaderForge.SFN_Multiply,id:4290,x:30802,y:33433,varname:node_4290,prsc:2|A-3808-OUT,B-4117-OUT;n:type:ShaderForge.SFN_Multiply,id:1486,x:30802,y:33548,varname:node_1486,prsc:2|A-1883-OUT,B-8665-OUT;n:type:ShaderForge.SFN_Multiply,id:6428,x:30802,y:33671,varname:node_6428,prsc:2|A-2831-OUT,B-272-OUT;n:type:ShaderForge.SFN_Multiply,id:2086,x:30802,y:33793,varname:node_2086,prsc:2|A-7619-OUT,B-5028-OUT;n:type:ShaderForge.SFN_Multiply,id:2878,x:30802,y:33915,varname:node_2878,prsc:2|A-1970-OUT,B-1478-OUT;n:type:ShaderForge.SFN_Multiply,id:5363,x:30802,y:34036,varname:node_5363,prsc:2|A-1856-OUT,B-6466-OUT;n:type:ShaderForge.SFN_Subtract,id:2655,x:30981,y:33176,varname:node_2655,prsc:2|A-1586-OUT,B-7259-OUT;n:type:ShaderForge.SFN_Subtract,id:7232,x:30981,y:33306,varname:node_7232,prsc:2|A-7259-OUT,B-3808-OUT;n:type:ShaderForge.SFN_Subtract,id:4847,x:30981,y:33433,varname:node_4847,prsc:2|A-3808-OUT,B-1883-OUT;n:type:ShaderForge.SFN_Subtract,id:8780,x:30981,y:33548,varname:node_8780,prsc:2|A-1883-OUT,B-2831-OUT;n:type:ShaderForge.SFN_Subtract,id:3336,x:30981,y:33671,varname:node_3336,prsc:2|A-2831-OUT,B-7619-OUT;n:type:ShaderForge.SFN_Subtract,id:7640,x:30981,y:33793,varname:node_7640,prsc:2|A-7619-OUT,B-1970-OUT;n:type:ShaderForge.SFN_Subtract,id:7659,x:30981,y:33915,varname:node_7659,prsc:2|A-1970-OUT,B-1856-OUT;n:type:ShaderForge.SFN_Multiply,id:169,x:31153,y:33793,varname:node_169,prsc:2|A-7640-OUT,B-2086-OUT;n:type:ShaderForge.SFN_Multiply,id:4536,x:31153,y:33671,varname:node_4536,prsc:2|A-3336-OUT,B-6428-OUT;n:type:ShaderForge.SFN_Multiply,id:3080,x:31153,y:33548,varname:node_3080,prsc:2|A-8780-OUT,B-1486-OUT;n:type:ShaderForge.SFN_Multiply,id:2474,x:31153,y:33433,varname:node_2474,prsc:2|A-4847-OUT,B-4290-OUT;n:type:ShaderForge.SFN_Multiply,id:8683,x:31153,y:33306,varname:node_8683,prsc:2|A-7232-OUT,B-2935-OUT;n:type:ShaderForge.SFN_Multiply,id:5840,x:31153,y:33176,varname:node_5840,prsc:2|A-2655-OUT,B-9924-OUT;n:type:ShaderForge.SFN_Add,id:3813,x:31339,y:33176,varname:node_3813,prsc:2|A-5840-OUT,B-8683-OUT,C-2474-OUT,D-3080-OUT,E-4536-OUT;n:type:ShaderForge.SFN_Add,id:6271,x:31339,y:33306,varname:node_6271,prsc:2|A-3813-OUT,B-169-OUT,C-5710-OUT,D-5363-OUT;n:type:ShaderForge.SFN_Vector1,id:3295,x:30731,y:34820,varname:node_3295,prsc:2,v1:0.01960784;n:type:ShaderForge.SFN_Multiply,id:5710,x:31153,y:33915,varname:node_5710,prsc:2|A-7659-OUT,B-2878-OUT;n:type:ShaderForge.SFN_Color,id:8813,x:31351,y:31875,ptovrint:False,ptlb:Deep Colour_copy,ptin:_MinColour_copy,varname:_MinColour_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_Color,id:9530,x:31351,y:32067,ptovrint:False,ptlb:Shallow Colour_copy,ptin:_MaxColour_copy,varname:_MaxColour_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.2551723,c3:1,c4:1;n:type:ShaderForge.SFN_Lerp,id:8261,x:31595,y:32077,varname:node_8261,prsc:2|A-8813-RGB,B-9530-RGB,T-2423-OUT;n:type:ShaderForge.SFN_Posterize,id:2423,x:31351,y:32238,varname:node_2423,prsc:2|IN-5731-RGB,STPS-800-OUT;n:type:ShaderForge.SFN_Vector1,id:800,x:31170,y:32286,varname:node_800,prsc:2,v1:8;n:type:ShaderForge.SFN_Vector1,id:2127,x:30731,y:34750,varname:node_2127,prsc:2,v1:0.01960784;n:type:ShaderForge.SFN_Tex2d,id:5731,x:31170,y:32089,ptovrint:False,ptlb:Height Map_copy,ptin:_MainTex_copy,varname:_MainTex_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;proporder:5348-5054-6249;pass:END;sub:END;*/

Shader "Shader Forge/HeightMapBathymetry" {
    Properties {
        _MainTex ("Height Map", 2D) = "white" {}
        _MinColour ("Deep Colour", Color) = (1,0,0,1)
        _MaxColour ("Shallow Colour", Color) = (0,0.2551723,1,1)
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#ifndef UNITY_PASS_FORWARDBASE
            #define UNITY_PASS_FORWARDBASE
#endif
            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma only_renderers d3d9 d3d11 glcore gles metal
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _MinColour;
            uniform float4 _MaxColour;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float node_1586 = step(_MainTex_var.r,1.0);
                float node_7259 = step(_MainTex_var.r,0.9607843);
                float node_3808 = step(_MainTex_var.r,0.9215686);
                float node_1883 = step(_MainTex_var.r,0.8431373);
                float node_2831 = step(_MainTex_var.r,0.6078432);
                float node_7619 = step(_MainTex_var.r,0.4117647);
                float node_1970 = step(_MainTex_var.r,0.2156863);
                float node_1856 = step(_MainTex_var.r,0.01960784);
                float3 emissive = lerp(_MinColour.rgb,_MaxColour.rgb,((((node_1586-node_7259)*(node_1586*1.0))+((node_7259-node_3808)*(node_7259*0.875))+((node_3808-node_1883)*(node_3808*0.75))+((node_1883-node_2831)*(node_1883*0.625))+((node_2831-node_7619)*(node_2831*0.5)))+((node_7619-node_1970)*(node_7619*0.375))+((node_1970-node_1856)*(node_1970*0.25))+(node_1856*0.125)));
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
