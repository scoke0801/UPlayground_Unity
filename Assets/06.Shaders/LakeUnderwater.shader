Shader "Hidden/UPlayGround/LakeUnderwater"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        // 기존 RendererData의 셰이더 GUID를 유지하는 호환 진입점이다.
        UsePass "Hidden/UPlayGround/WaterSceneResolve/WaterSceneResolve"
    }
}