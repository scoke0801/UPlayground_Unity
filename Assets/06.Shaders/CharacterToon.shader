Shader "UPlayground/Character/CharacterToon"
{
 Properties
 {
  [MainTexture] _BaseMap("기본 텍스처",2D)="white"{}
  [MainColor] _BaseColor("기본색",Color)=(1,1,1,1)
  _Shade("그늘색",Color)=(0.74,0.68,0.77,1)
  _FaceMap("좌우 얼굴 전환각",2D)="gray"{}
  _FaceWeight("얼굴 마스크 강도",Range(0,1))=0
  _FaceFeather("얼굴 명암 경계 폭",Range(0.005,0.12))=0.023
  _FacePigment("얼굴 채색",Color)=(1,1,1,1)
  [Toggle(_CHARACTER_FRINGE)] _UseFringe("앞머리 근접 그림자",Float)=0
  _FringeOcclusion("앞머리 가림 좌·정면·우",2D)="black"{}
  _FringeStrength("앞머리 그림자 강도",Range(0,1))=0.38
  _FringeTint("앞머리 그림자색",Color)=(0.74,0.60,0.70,1)
  [Toggle(_CHARACTER_INK)] _UseInk("실루엣 안쪽 선",Float)=0
  _InkMask("부위별 선 농도",2D)="white"{}
  _InkTint("선 채색",Color)=(0.55,0.39,0.51,1)
  _InkWidth("안쪽 선 폭",Range(0,2))=1
  _HairWeight("모발 방향광 강도",Range(0,1))=0
  _Gloss("광택 강도",Range(0,1))=0.05
  _Power("광택 지수",Range(2,128))=24
  [Toggle(_CHARACTER_RIM)] _UseRim("주광 림라이트",Float)=0
  _RimTint("윤곽 반사색",Color)=(0.85,0.88,1,1)
  _RimStrength("윤곽 반사 강도",Range(0,1))=0.12
  [Toggle(_CHARACTER_METAL)] _UseMetal("금속 반사",Float)=0
  _Metallic("금속성",Range(0,1))=0.72
  _Smoothness("매끄러움",Range(0,1))=0.65
  [Toggle(_CHARACTER_PAINT)] _UsePaint("원본 채색 보존",Float)=0
  _PaintLight("채색광 비중",Range(0,1))=0.75
  _ShadowKeyRetention("월드 그림자 안의 주광 비중",Range(0,1))=1
  _LocalLightTint("보조광 색 반영",Range(0,1))=1
  _BounceTint("그늘 반사색",Color)=(0,0,0,1)
  [Toggle(_CHARACTER_MATCAP)] _UseMatCap("마스크 재질광",Float)=0
  _MatCapTex("재질광",2D)="white"{}
  _MatCapMask("재질광 영역",2D)="white"{}
  _MatCapColor("재질광 색",Color)=(1,1,1,1)
  _MatCapMultiply("그늘 곱셈",Range(0,1))=0
  [Toggle(_CHARACTER_SURFACE_DETAIL)] _UseSurfaceDetail("원본 표면 지도",Float)=0
  [Normal] _DetailNormal("표면 노멀",2D)="bump"{}
  _DetailScale("노멀 강도",Range(0,1))=0.5
  _DetailGloss("광택 영역",2D)="white"{}
  [Normal] _StrandDirection("모발 방향",2D)="bump"{}
  _LusterTint("표면 반사색",Color)=(0.82,0.86,1,1)
  [Toggle(_CHARACTER_FABRIC)] _UseFabric("의상 영역별 반사",Float)=0
  _FabricRegions("테두리·직조 무늬·등 자수",2D)="black"{}
  _ThreadColor("자수 실 색",Color)=(0.72,0.56,0.36,1)
  [HideInInspector] _ActorCameraVisibility("카메라 가시도",Range(0,1))=1
  [HideInInspector] _DitherTex("카메라 디더",2D)="white"{}
  _Cutoff("알파 임계값",Range(0,1))=0
  [HideInInspector] _HeadForward("머리 전방",Vector)=(0,0,1,0)
  [HideInInspector] _HeadRight("머리 우측",Vector)=(1,0,0,0)
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("컬링",Float)=2
  [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("소스 블렌드",Float)=1
  [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("대상 블렌드",Float)=0
  [Toggle] _ZWrite("깊이 쓰기",Float)=1
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Cull [_Cull]
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  TEXTURE2D(_FaceMap); SAMPLER(sampler_FaceMap);
  TEXTURE2D(_FringeOcclusion); SAMPLER(sampler_FringeOcclusion);
  TEXTURE2D(_InkMask); SAMPLER(sampler_InkMask);
  TEXTURE2D(_MatCapTex); SAMPLER(sampler_MatCapTex);
  TEXTURE2D(_MatCapMask); SAMPLER(sampler_MatCapMask);
  TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);
  TEXTURE2D(_DetailGloss); SAMPLER(sampler_DetailGloss);
  TEXTURE2D(_StrandDirection); SAMPLER(sampler_StrandDirection);
  TEXTURE2D(_FabricRegions); SAMPLER(sampler_FabricRegions);
  TEXTURE2D(_DitherTex);
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST, _BaseColor, _Shade, _HeadForward, _HeadRight;
   float _FaceWeight, _HairWeight, _Gloss, _Power, _Cutoff, _Cull, _SrcBlend, _DstBlend, _ZWrite;
   float4 _RimTint;
   float _RimStrength, _Metallic, _Smoothness, _UseRim, _UseMetal;
   float4 _MatCapColor, _BounceTint;
   float _PaintLight, _UsePaint, _UseMatCap, _MatCapMultiply, _ShadowKeyRetention, _LocalLightTint;
   float4 _LusterTint, _FacePigment;
   float4 _FringeTint;
   float _UseFringe, _FringeStrength;
   float4 _InkTint;
   float _UseInk, _InkWidth;
   float4 _ThreadColor;
   float _UseFabric;
   float _ActorCameraVisibility;
   float4 _DitherTex_TexelSize;
   float _UseSurfaceDetail, _DetailScale, _FaceFeather;
  CBUFFER_END
  struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
  struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half3 strandWS:TEXCOORD2; float2 uv:TEXCOORD3; half fog:TEXCOORD4; float2 faceUV:TEXCOORD5;
   #if defined(_CHARACTER_SURFACE_DETAIL)
    half3 tangentWS:TEXCOORD6;
   #endif
  };
  Varyings Vert(Attributes a)
  {
   Varyings o;
   VertexNormalInputs basis=GetVertexNormalInputs(a.normalOS,a.tangentOS);
   o.positionWS=TransformObjectToWorld(a.positionOS.xyz);
   o.positionCS=TransformWorldToHClip(o.positionWS);
   o.normalWS=basis.normalWS;
   o.strandWS=basis.bitangentWS;
   #if defined(_CHARACTER_SURFACE_DETAIL)
    o.tangentWS=basis.tangentWS;
   #endif
   o.uv=TRANSFORM_TEX(a.uv,_BaseMap);
   o.faceUV=a.uv;
   o.fog=ComputeFogFactor(o.positionCS.z);
   return o;
  }
  half4 Surface(Varyings i)
  {
   half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
   clip(c.a-_Cutoff);
   return c;
  }
  void ClipCameraFade(float2 pixel)
  {
   if (_ActorCameraVisibility < 1)
   {
    int2 cell=int2(fmod(floor(pixel),max(_DitherTex_TexelSize.zw,1)));
    half threshold=(LOAD_TEXTURE2D(_DitherTex,cell).r*255+1)/257;
    clip(_ActorCameraVisibility-threshold);
   }
  }
  ENDHLSL
  Pass
  {
   Name "CharacterForward"
   Tags { "LightMode"="UniversalForwardOnly" }
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_ZWrite]
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma shader_feature_local_fragment _CHARACTER_RIM
   #pragma shader_feature_local_fragment _CHARACTER_METAL
   #pragma shader_feature_local_fragment _CHARACTER_PAINT
   #pragma shader_feature_local_fragment _CHARACTER_MATCAP
   #pragma shader_feature_local _CHARACTER_SURFACE_DETAIL
   #pragma shader_feature_local_fragment _CHARACTER_FABRIC
   #pragma shader_feature_local_fragment _CHARACTER_FRINGE
   #pragma shader_feature_local_fragment _CHARACTER_INK
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   half4 Frag(Varyings i):SV_Target
   {
    ClipCameraFade(i.positionCS.xy);
    half4 c=Surface(i);
    half3 n=NormalizeNormalPerPixel(i.normalWS);
    InputData inputData=(InputData)0;
    inputData.positionWS=i.positionWS;
    inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
     float4 shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.positionWS));
    #else
     float4 shadowCoord=TransformWorldToShadowCoord(i.positionWS);
    #endif
    Light key=GetMainLight(shadowCoord);
    half3 l=key.direction;
    half band=smoothstep(-0.10,0.10,dot(n,l));
    if (_FaceWeight>0)
    {
     float2 local=float2(dot(l,_HeadForward.xyz),dot(l,_HeadRight.xyz));
     float angle=acos(clamp(local.x/max(length(local),0.0001),-1,1))/PI;
     half3 thresholds=SAMPLE_TEXTURE2D(_FaceMap,sampler_FaceMap,i.faceUV).rgb;
     c.rgb*=lerp(1,_FacePigment.rgb,thresholds.b);
     float d=(local.y>=0?thresholds.r:thresholds.g)-angle;
     float w=max(fwidth(d),_FaceFeather);
     band=lerp(band,lerp(0.65,smoothstep(-w,w,d),smoothstep(0.01,0.18,length(local))),_FaceWeight);
    }
    half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
    half3 h=SafeNormalize(l+view);
    half strand=dot(SafeNormalize(i.strandWS),h);
    half3 luster=1;
    #if defined(_CHARACTER_SURFACE_DETAIL)
     // 큰 명암면은 기하 노멀을 유지하고 자수·주름의 작은 면만 재질광에 반영한다.
     half3 t=SafeNormalize(i.tangentWS);
     half3 b=SafeNormalize(i.strandWS);
     half3 localNormal=UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,i.faceUV),_DetailScale);
     n=SafeNormalize(t*localNormal.x+b*localNormal.y+n*localNormal.z);
     if (_HairWeight>0)
     {
      half2 flow=UnpackNormal(SAMPLE_TEXTURE2D(_StrandDirection,sampler_StrandDirection,i.faceUV)).xy;
      half3 direction=dot(flow,flow)>0.001 ? SafeNormalize(t*flow.x+b*flow.y) : b;
      strand=dot(direction,h);
     }
     luster=SAMPLE_TEXTURE2D(_DetailGloss,sampler_DetailGloss,i.faceUV).rgb*_LusterTint.rgb;
    #endif
    half gloss=lerp(pow(saturate(dot(n,h)),_Power),pow(saturate(1-strand*strand),_Power)*smoothstep(0,0.4,dot(n,h)),_HairWeight);
    half direct=key.shadowAttenuation*key.distanceAttenuation;
    half3 indirect=max(SampleSH(n),half3(0.08,0.08,0.08));
    half3 lighting=indirect+key.color*lerp(_Shade.rgb*0.55,1,band)*direct;
    #if defined(_CHARACTER_PAINT)
     // 환경의 밝기 변화는 유지하고, 그늘에서도 원화의 고유색을 읽게 한다.
     half visibleKey=lerp(_ShadowKeyRetention,1,key.shadowAttenuation);
     half3 paintLight=indirect+key.color*key.distanceAttenuation*visibleKey*lerp(_Shade.rgb,1,band*key.shadowAttenuation);
     lighting=lerp(lighting,paintLight,_PaintLight);
    #endif
    half3 fill=0;
    #if defined(_ADDITIONAL_LIGHTS)
     #if USE_CLUSTER_LIGHT_LOOP
      // 클러스터 루프의 점광원 목록에는 보조 방향광(예: 달)이 포함되지 않는다.
      [loop] for (uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);++lightIndex)
      {
       CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
       Light lamp=GetAdditionalLight(lightIndex,i.positionWS);
       fill+=lamp.color*lamp.distanceAttenuation*saturate(dot(n,lamp.direction)*0.65+0.35);
      }
     #endif
     LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
      Light lamp=GetAdditionalLight(lightIndex,i.positionWS);
      fill+=lamp.color*lamp.distanceAttenuation*saturate(dot(n,lamp.direction)*0.65+0.35);
     LIGHT_LOOP_END
    #endif
    fill=min(fill,1.2);
    fill=lerp(Luminance(fill).xxx,fill,_LocalLightTint);
    half3 color=c.rgb*(lighting+fill)+key.color*luster*gloss*_Gloss*direct;
    #if defined(_CHARACTER_PAINT)
     color+=_BounceTint.rgb*(1-band*direct)*saturate(dot(indirect+key.color*key.distanceAttenuation,half3(0.21,0.72,0.07)));
    #endif
    #if defined(_CHARACTER_METAL)
     BRDFData brdf;
     half alpha=c.a;
     InitializeBRDFData(c.rgb,_Metallic,0,_Smoothness,alpha,brdf);
     half fresnel=Pow4(1-saturate(dot(n,view)));
     half3 reflection=GlossyEnvironmentReflection(reflect(-view,n),i.positionWS,brdf.perceptualRoughness,1,inputData.normalizedScreenSpaceUV);
     color=brdf.diffuse*(lighting+fill)
         +brdf.specular*DirectBRDFSpecular(brdf,n,l,view)*saturate(dot(n,l))*key.color*direct
         +reflection*EnvironmentBRDFSpecular(brdf,fresnel);
    #endif
    #if defined(_CHARACTER_RIM)
     half edge=1-saturate(dot(n,view));
     half rim=smoothstep(0.42,0.93,edge)*smoothstep(-0.32,0.65,dot(n,l));
     color+=_RimTint.rgb*key.color*(rim*_RimStrength*direct);
    #endif
    #if defined(_CHARACTER_MATCAP)
     half2 capUV=TransformWorldToViewDir(n).xy*0.5+0.5;
     half3 cap=SAMPLE_TEXTURE2D(_MatCapTex,sampler_MatCapTex,capUV).rgb*_MatCapColor.rgb;
     half mask=SAMPLE_TEXTURE2D(_MatCapMask,sampler_MatCapMask,i.faceUV).r*_MatCapColor.a;
     half3 screen=color+cap*(1-saturate(color))*saturate(lighting);
     color=lerp(color,lerp(screen,color*cap,_MatCapMultiply),mask);
    #endif
    #if defined(_CHARACTER_FABRIC)
     half3 region=SAMPLE_TEXTURE2D(_FabricRegions,sampler_FabricRegions,i.faceUV).rgb;
     half3 ambient=indirect+fill;
     half3 broadLight=ambient+key.color*key.distanceAttenuation*lerp(_ShadowKeyRetention,1,key.shadowAttenuation)*lerp(0.52,1,band*key.shadowAttenuation);
     half grazing=Pow4(1-saturate(dot(n,view)));
     // 비단의 넓은 반사와 금속 가장자리의 얇은 반사를 분리한다.
     half weave=saturate(region.g+region.b);
     half satin=smoothstep(0.32,0.88,dot(n,h))*direct;
     color+=region.g*(0.09+0.23*satin)*_ThreadColor.rgb*broadLight;
     half3 thread=_ThreadColor.rgb*(0.55+0.55*satin)*broadLight;
     color=lerp(color,thread,region.b*0.87);
     half3 env=GlossyEnvironmentReflection(reflect(-view,n),i.positionWS,0.34,1,inputData.normalizedScreenSpaceUV);
     half3 trim=c.rgb*(ambient+key.color*direct*(0.52+0.48*band))
               +lerp(c.rgb,1,grazing)*(env*0.64+key.color*pow(saturate(dot(n,h)),48)*direct);
     color=lerp(color,trim,region.r);
     color+=(1-saturate(region.r+weave))*grazing*half3(0.035,0.019,0.051)*broadLight;
    #endif
    #if defined(_CHARACTER_FRINGE)
     half3 occlusion=SAMPLE_TEXTURE2D(_FringeOcclusion,sampler_FringeOcclusion,i.faceUV).rgb;
     half side=dot(l,_HeadRight.xyz);
     half contact=lerp(occlusion.g,side<0?occlusion.r:occlusion.b,saturate(abs(side)*1.4));
     half height=dot(l,cross(_HeadForward.xyz,_HeadRight.xyz));
     half front=dot(l,_HeadForward.xyz);
     contact*=smoothstep(-0.10,0.38,height)*smoothstep(-0.42,0.25,front);
     color*=lerp(1,_FringeTint.rgb,contact*_FringeStrength*direct);
    #endif
    #if defined(_CHARACTER_INK)
     // 메시를 팽창시키지 않고 형태가 시선에서 돌아서는 안쪽에만 색선을 얹는다.
     half facing=abs(dot(NormalizeNormalPerPixel(i.normalWS),view));
     half pixel=max(fwidth(facing),0.008);
     half width=min(pixel*_InkWidth,0.18);
     half ink=1-smoothstep(0.015+width,0.015+width+pixel,facing);
     half inkMask=SAMPLE_TEXTURE2D(_InkMask,sampler_InkMask,i.faceUV).r;
     color*=lerp(1,_InkTint.rgb,ink*inkMask*_InkTint.a);
    #endif
    return half4(MixFog(color,i.fog),c.a);
   }
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly"
   Tags { "LightMode"="DepthOnly" }
   ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Depth
   half4 Depth(Varyings i):SV_Target { ClipCameraFade(i.positionCS.xy); Surface(i); return 0; }
   ENDHLSL
  }
  Pass
  {
   Name "DepthNormalsOnly"
   Tags { "LightMode"="DepthNormalsOnly" }
   ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Normals
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   half4 Normals(Varyings i):SV_Target
   {
    ClipCameraFade(i.positionCS.xy);
    Surface(i);
    half3 n=NormalizeNormalPerPixel(i.normalWS);
    #if defined(_GBUFFER_NORMALS_OCT)
     return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*0.5+0.5)),0);
    #else
     return half4(n,0);
    #endif
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster"
   Tags { "LightMode"="ShadowCaster" }
   ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex ShadowVert
   #pragma fragment Depth
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection,_LightPosition;
   Varyings ShadowVert(Attributes a)
   {
    Varyings o=Vert(a);
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
     float3 l=normalize(_LightPosition-o.positionWS);
    #else
     float3 l=_LightDirection;
    #endif
    o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,l)));
    return o;
   }
   half4 Depth(Varyings i):SV_Target { Surface(i); return 0; }
   ENDHLSL
  }
 }
}
