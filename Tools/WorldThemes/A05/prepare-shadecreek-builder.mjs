// 검증된 기존 독립 씬 저작 순서를 유지하고 A05 잎 재질 슬롯 인계만 더한다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
let code=fs.readFileSync('Tools/WorldThemes/A04/build-redforest-study.csx','utf8');
for(const [from,to] of [
 ['RedTreeForest','ShadeCreek'],['A04','A05'],['redforest-study.json','shadecreek-study.json'],
 ['실제 붉은숲','실제 그늘숲'],['붉은나무숲','그늘숲'],
 ['forestGroundCandidate','forestGroundSource'],['protectedRadius','protectedContentRadius'],
 ['coreEdgeBlendMeters','terrainCoreEdgeFade'],['canopyGroundRadius','forestFloorRadius'],
 ['maximumSurfaceSlope','maximumPlantSlope'],['forestGroundWeight','forestFloorMaximumWeight']
 ,['sourceRedForestRelativePath','sourceShadeForestRelativePath']
])code=code.replaceAll(from,to);
const guard='if (!Convert.ToBoolean(plan["corridorConfirmed"]))';
assert(code.includes(guard));
code=code.replace(guard,'if (!Convert.ToBoolean(plan["replacementTargetsConfirmed"])) throw new Exception("A00 exact leaf Renderer/slot 승인 전 잠금");\n'+guard);
code=code.replace(guard,'if (!Convert.ToBoolean(plan["leafOwnershipAuditConfirmed"]) || !Convert.ToBoolean(plan["leafMaterialComparisonConfirmed"]) || !Convert.ToBoolean(plan["impostorCoverageConfirmed"])) throw new Exception("잎/원거리 impostor 소유권·화면·LOD 범위 검수 전 잠금");\n'+guard);
const beforeCreation='System.IO.Directory.CreateDirectory(output);';
assert(code.includes(beforeCreation));
const preflight=`
Func<string,string> evidenceHash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();};
string pairEvidencePath=plan["variantPairEvidence"].ToString();
if(evidenceHash(pairEvidencePath)!=plan["variantPairEvidenceHash"].ToString())throw new Exception("정적 tree/impostor 근거 hash 불일치");
var pairEvidence=(Dictionary<string,object>)read(System.IO.File.ReadAllText(pairEvidencePath),typeof(Dictionary<string,object>));
if(!Convert.ToBoolean(pairEvidence["billboardMeshSerializedEqual"]))throw new Exception("기존 impostor mesh/UV 동일성 미확인");
var pairSources=new List<Dictionary<string,object>>{(Dictionary<string,object>)read(pairEvidence["red"].ToString(),typeof(Dictionary<string,object>)),(Dictionary<string,object>)read(pairEvidence["green"].ToString(),typeof(Dictionary<string,object>))};
pairSources.AddRange((Dictionary<string,object>[])read(pairEvidence["billboards"].ToString(),typeof(Dictionary<string,object>[])));
foreach(var pair in pairSources)if(evidenceHash(pair["path"].ToString())!=pair["fileHash"].ToString())throw new Exception("기존 tree/impostor 파일 변동. 정적 비교 재검토 필요");
if(string.IsNullOrEmpty(plan["leafOwnershipEvidence"].ToString())||evidenceHash(plan["leafOwnershipEvidence"].ToString())!=plan["leafOwnershipEvidenceHash"].ToString())throw new Exception("정확 Renderer 소유권 역참조 인계 hash 없음/불일치");
var leafTargets=(Dictionary<string,object>[])read(plan["replaceObjects"].ToString(),typeof(Dictionary<string,object>[]));
if(leafTargets.Length==0)throw new Exception("실제 비교 후 확정할 잎 재질 대상이 없습니다.");
var leafSources=new Dictionary<string,Renderer>();
var shadeOwnershipRoot=sourceScene.GetRootGameObjects().Single(g=>g.name==plan["sourceEnvironmentPath"].ToString()).transform.Find(plan["sourceShadeForestRelativePath"].ToString());
if(shadeOwnershipRoot==null)throw new Exception("동쪽 그늘숲 현재 소유 루트 없음");
Func<string,long,Material> loadExactMaterial=(guid,fileId)=>UnityEditor.AssetDatabase.LoadAllAssetsAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)).OfType<Material>().Single(m=>{string g;long id;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out g,out id);return g==guid&&id==fileId;});
foreach(var leafTarget in leafTargets){
    string id=leafTarget["rendererGlobalId"].ToString();UnityEditor.GlobalObjectId globalId;
    if(!UnityEditor.GlobalObjectId.TryParse(id,out globalId))throw new Exception("Renderer GlobalObjectId 오류");
    var leafRenderer=UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as MeshRenderer;
    if(leafRenderer==null||leafRenderer.gameObject.scene!=sourceScene||leafRenderer.HasPropertyBlock())throw new Exception("정확한 정적 잎 Renderer가 아니거나 MPB 소유권 확인 필요");
    int slot=Convert.ToInt32(leafTarget["materialSlot"]);
    string role=leafTarget["rendererRole"].ToString();
    if(slot<0||slot>=leafRenderer.sharedMaterials.Length||!((role=="GeometryLeaves"&&slot>0&&Convert.ToBoolean(leafTarget["isLeafSlot"]))||(role=="Impostor"&&slot==0&&Convert.ToBoolean(leafTarget["impostorComparisonConfirmed"]))))throw new Exception("줄기/미검수 impostor/잘못된 슬롯 교체 금지");
    var treeRoot=UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(leafRenderer.gameObject);
    if(treeRoot==null||UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(treeRoot)!=plan["sourceRedTree"].ToString()||UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(treeRoot).ToString()!=leafTarget["treeGlobalId"].ToString())throw new Exception("정확한 원본 tree 소유권 불일치");
    if(!treeRoot.transform.IsChildOf(shadeOwnershipRoot))throw new Exception("동쪽 그늘숲 외부 tree 교체 금지");
    var filter=leafRenderer.GetComponent<MeshFilter>();string meshGuid;long meshId;
    if(filter==null||filter.sharedMesh==null)throw new Exception("정적 tree mesh 없음");
    UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out meshGuid,out meshId);
    if(meshGuid!=leafTarget["meshGuid"].ToString()||meshId!=Convert.ToInt64(leafTarget["meshFileId"]))throw new Exception("원본 mesh GUID/fileID 불일치");
    var beforeMaterial=leafRenderer.sharedMaterials[slot];
    if(beforeMaterial==null||UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(beforeMaterial))!=leafTarget["beforeMaterialGuid"].ToString())throw new Exception("잎 재질 기준선 불일치");
    string beforeGuid;long beforeId;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(beforeMaterial,out beforeGuid,out beforeId);
    if(beforeId!=Convert.ToInt64(leafTarget["beforeMaterialFileId"]))throw new Exception("원본 재질 subasset fileID 불일치");
    if(loadExactMaterial(leafTarget["greenMaterialGuid"].ToString(),Convert.ToInt64(leafTarget["greenMaterialFileId"]))==null)throw new Exception("정확 Green 재질 GUID/fileID 누락");
    var b=leafRenderer.bounds;
    if(!isInsideCore(new Vector3(b.min.x,0,b.min.z))||!isInsideCore(new Vector3(b.min.x,0,b.max.z))||!isInsideCore(new Vector3(b.max.x,0,b.min.z))||!isInsideCore(new Vector3(b.max.x,0,b.max.z)))throw new Exception("잎 Renderer footprint core 밖");
    leafSources.Add(id,leafRenderer);
}
foreach(var group in leafTargets.GroupBy(t=>t["treeGlobalId"].ToString())){
    var renderer=leafSources[group.First()["rendererGlobalId"].ToString()];var treeRoot=UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);var lod=treeRoot.GetComponent<LODGroup>();
    if(lod==null||lod.GetLODs().SelectMany(l=>l.renderers).Where(r=>r!=null).Distinct().Any(r=>!leafSources.Values.Contains(r)))throw new Exception("일부 LOD Renderer가 교체 검수 목록에서 빠짐. 원거리 붉은색 팝 위험");
}
`;
code=code.replace(beforeCreation,preflight+'\n'+beforeCreation);
const sourceTreeMarker='var redRoot=verifiedRedRoot;';
assert(code.includes(sourceTreeMarker));
const leafApply=`
var privateLeafMaterials=new Dictionary<string,Material>();var leafOverrides=new List<object>();
foreach(var leafTarget in leafTargets){
    string id=leafTarget["rendererGlobalId"].ToString();var original=leafSources[id];
    if(!rendererMap.ContainsKey(original.GetInstanceID()))throw new Exception("잎 대상이 독립 참고 영역 밖");
    string greenGuid=leafTarget["greenMaterialGuid"].ToString();long greenId=Convert.ToInt64(leafTarget["greenMaterialFileId"]);string materialKey=greenGuid+":"+greenId;Material privateLeaf;
    if(!privateLeafMaterials.TryGetValue(materialKey,out privateLeaf)){
        privateLeaf=UnityEngine.Object.Instantiate(loadExactMaterial(greenGuid,greenId));
        string path=dataFolder+"/LeafGreen_"+privateLeafMaterials.Count+".mat";
        UnityEditor.AssetDatabase.CreateAsset(privateLeaf,path);privateLeafMaterials.Add(materialKey,privateLeaf);
    }
    int slot=Convert.ToInt32(leafTarget["materialSlot"]);var renderer=rendererMap[original.GetInstanceID()];
    var materials=renderer.sharedMaterials;materials[slot]=privateLeaf;renderer.sharedMaterials=materials;
    leafOverrides.Add(new{rendererGlobalId=id,treeGlobalId=leafTarget["treeGlobalId"].ToString(),rendererRole=leafTarget["rendererRole"].ToString(),materialSlot=slot,beforeMaterialGuid=leafTarget["beforeMaterialGuid"].ToString(),beforeMaterialFileId=leafTarget["beforeMaterialFileId"].ToString(),greenSourceGuid=greenGuid,greenSourceFileId=greenId.ToString(System.Globalization.CultureInfo.InvariantCulture),meshGuid=leafTarget["meshGuid"].ToString(),meshFileId=leafTarget["meshFileId"].ToString(),path=UnityEditor.AssetDatabase.GetAssetPath(privateLeaf),guid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(privateLeaf)),action="MaterialSlotOnly",preserve="Host position/rotation/scale/geometry trunk/mesh/Collider/LOD/ID; ReferenceOnly는 시각 사본"});
}
`;
code=code.replace(sourceTreeMarker,leafApply+'\n'+sourceTreeMarker);
code=code.replace('replaceObjects=new object[0]','replaceObjects=leafOverrides');
code=code.replace('서사 뿌리/동굴/VFX는 보호 참조이며 임의 생성/교체 없음','DroughtVillage Region의 서사 CreekArt/수위/뿌리는 다른 소유권이며 복제하지 않음');
code=code.replace('첫 대표 수관 가장자리만 저작, 전체 테마 아님','현재 동쪽 forest-water-loop의 바닥/잎 palette 대표만 저작. 신규 수면 없음');
const commonSkyLoad='UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ThemeStudies/LookWater/PreviewSky.mat")';
assert(code.includes(commonSkyLoad));
code=code.replace(commonSkyLoad,'UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(plan["previewSkySource"].ToString())');
const skyGuard='var sourceReflection = RenderSettings.defaultReflectionMode;';
assert(code.includes(skyGuard));
code=code.replace(skyGuard,'string hostSkyFile=baseline["sky"].ToString(),hostSkyHash=evidenceHash(hostSkyFile),commonSkyHash=evidenceHash(plan["previewSkySource"].ToString());\nvar hostSkyAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(hostSkyFile);\nstring hostSkyMemory=UnityEditor.EditorJsonUtility.ToJson(hostSkyAsset),commonSkyMemory=UnityEditor.EditorJsonUtility.ToJson(commonSky);\n'+skyGuard);
const captureMarker='var poses=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));';
assert(code.includes(captureMarker));
code=code.replace(captureMarker,'if(evidenceHash(hostSkyFile)!=hostSkyHash||evidenceHash(plan["previewSkySource"].ToString())!=commonSkyHash||UnityEditor.EditorJsonUtility.ToJson(hostSkyAsset)!=hostSkyMemory||UnityEditor.EditorJsonUtility.ToJson(commonSky)!=commonSkyMemory)throw new Exception("Host DaySky/A01 PreviewSky 공용 원본 변경 감지");\n'+captureMarker);
code=code.replace('replaceObjects=leafOverrides,','replaceObjects=leafOverrides,baselineContract=plan["baselineContract"].ToString(),baselineNavMeshHash=plan["baselineNavMeshHash"].ToString(),previewSkySource=plan["previewSkySource"].ToString(),variantPairEvidenceHash=plan["variantPairEvidenceHash"].ToString(),leafOwnershipEvidence=plan["leafOwnershipEvidence"].ToString(),leafOwnershipEvidenceHash=plan["leafOwnershipEvidenceHash"].ToString(),');
assert(code.length<50000);
assert(!code.includes('Theme_RedTreeForest'));
assert(code.includes('maskAuthorized')&&code.includes('replacementTargetsConfirmed'));
assert(code.includes('alpha 전수 복원 실패')&&code.includes('sourceAlphaHash')&&code.includes('commonSkyMemory'));
fs.writeFileSync('Tools/WorldThemes/A05/build-shadecreek-study.csx',code);
console.log(JSON.stringify({characters:code.length,locked:true,source:'A04 안전 clone/alpha 복원 절차',newBehavior:'정확한 잎 Renderer/slot만 전용 기존 Green 재질로 교체. source 수정 없음'}));
