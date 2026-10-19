// Unity MCP execute_code에서 실행하는 읽기 전용 회귀 검사.
var path="Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R06/NPC_YeonHoa.prefab";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
var results=new System.Collections.Generic.List<object>();
foreach(var cloth in prefab.GetComponentsInChildren<MagicaCloth2.MagicaCloth>(true))
{
    var data=cloth.GetSerializeData2().preBuildData;
    if(!data.enabled)continue;
    if(data.DataValidate().IsFaild())throw new System.InvalidOperationException("정상 캐시 검사 실패: "+cloth.name);
    results.Add(new{name=cloth.name,valid=true});
    var shared=data.GetSharePreBuildData();
    if(shared.proxyMesh.transformData.flagArray.count<=5)continue;
    // 수정 전 충돌 ID(앞머리 5개 본)에 옷/머리의 고유 데이터를 연결해 동일한 손상을 재현한다.
    var mismatched=new MagicaCloth2.PreBuildSerializeData
    {
        enabled=true,
        buildId="1bad0560",
        preBuildScriptableObject=data.preBuildScriptableObject,
        uniquePreBuildData=data.uniquePreBuildData
    };
    if(!mismatched.DataValidate().IsFaild())throw new System.InvalidOperationException("충돌 캐시를 허용함: "+cloth.name);
    results.Add(new{name=cloth.name+" / 충돌 ID 거부",valid=true});
}
if(results.Count!=6)throw new System.InvalidOperationException("검사 누락: "+results.Count);
return results;
