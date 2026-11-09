// 이번 작업 시작 당시 PC RP로 되돌리고 메모리의 촬영용 복제본만 제거한다.
QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
foreach(var rp in Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>().Where(p=>p.name=="ShadeCreekCaptureRP").ToArray())UnityEngine.Object.DestroyImmediate(rp);
return "공유 에셋 변경 없이 캡처 종료";

