import fs from 'node:fs';
let text=fs.readFileSync('Tools/WorldThemes/A04/build-redforest-study.csx','utf8');
text=text.slice(0,text.indexOf('var redRoot=verifiedRedRoot;'));
text=text.replace('if (!Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("A00 core mask 승인 전 편집할 수 없습니다.");','if (Convert.ToBoolean(plan["maskAuthorized"])) throw new Exception("baseline 전용 단계는 core 잠금 상태에서만 실행합니다.");');
text=text.replace('if (!Convert.ToBoolean(plan["corridorConfirmed"])) throw new Exception("실제 길·후퇴로·뿌리·동굴·서사 VFX 조사 전에는 편집하지 않습니다.");','');
text=text.replace('if(corridor.Length<2)throw new Exception("보존할 실제 길 표본이 없습니다.");','');
text=text.replace('var preflightPlacements=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));','var preflightPlacements=new Dictionary<string,object>[0];');
text=text.replaceAll('Theme_RedTreeForest.unity','Theme_RedTreeForest_Baseline.unity').replaceAll('Assets/10.Datas/World/ThemeStudies/RedTreeForest"','Assets/10.Datas/World/ThemeStudies/RedTreeForest_Baseline"');
text+=String.raw`
var additions=new GameObject("Theme_RedTreeForest_Environment");additions.transform.SetParent(environment.transform,false);
if(RenderSettings.skybox!=null&&RenderSettings.skybox.HasProperty("_AtmosphereThickness"))RenderSettings.skybox.SetFloat("_AtmosphereThickness",1f);
var comparisonRoot=new GameObject("ExistingAssetComparison_DisabledUntilCapture");comparisonRoot.transform.SetParent(preview.transform,false);comparisonRoot.SetActive(false);
var candidatePaths=(string[])read(plan["comparisonPrefabs"].ToString(),typeof(string[]));var candidateRows=new List<object>();
foreach(string candidatePath in candidatePaths){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);if(prefab==null)throw new Exception("비교 후보 정확 경로 누락: "+candidatePath);candidateRows.Add(new{path=candidatePath,guid=AssetDatabase.AssetPathToGUID(candidatePath),rendererCount=prefab.GetComponentsInChildren<Renderer>(true).Length,lodCount=prefab.GetComponentsInChildren<LODGroup>(true).Length,colliderCount=prefab.GetComponentsInChildren<Collider>(true).Length,scriptCount=prefab.GetComponentsInChildren<MonoBehaviour>(true).Length,status="metadata only; actual-render comparison pending"});}
for(int i=0;i<copy.detailPrototypes.Length;i++)if(contentHash(copy.GetDetailLayer(0,0,copy.detailWidth,copy.detailHeight,i))!=sourceDetailHashes[i])throw new Exception("clone detail equality failed");
if(contentHash(copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution))!=sourceHeightHash||contentHash(copy.GetHoles(0,0,copy.holesResolution,copy.holesResolution))!=sourceHolesHash||treeState(copy)!=sourceTreesState)throw new Exception("clone full geometry equality failed");
if(hash(sourcePath)!=sourceHash||hash(sourceScene.path)!=sourceSceneHash||sourceScene.isDirty!=sourceDirty||EditorUtility.IsDirty(sourceData)||hash(baseline["navMesh"].ToString())!=plan["baselineNavMeshHash"].ToString()||contentHash(sourceData.GetAlphamaps(0,0,sourceData.alphamapWidth,sourceData.alphamapHeight))!=sourceAlphaHash||hash(sourceSkyPath)!=originalSkyHash||hash(commonSkyPath)!=commonSkyHash||entityGroundState()!=initialEntityGroundState)throw new Exception("source baseline/317ID/ground/sky/memory alpha changed");
EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review,scenePath))throw new Exception("baseline scene save failed");
var report=new{status="baseline clone only; no terrain painting, height editing or additions",scene=scenePath,terrain=dataFolder+"/Terrain.asset",sourceSceneHash=sourceSceneHash,sourceTerrainHash=sourceHash,hostDirtyBefore=sourceDirty,hostDirtyAfter=sourceScene.isDirty,sourceLayerCount=layers.Length,privateLayerCount=copy.terrainLayers.Length,layerMap=layerMap,fullAlphaEquality=true,fullHeightEquality=true,fullDetailEquality=true,fullHolesEquality=true,treeInstancesEquality=true,scatterMode=copy.detailScatterMode.ToString(),sourceFilesEqual=true,sourceNavHash=hash(baseline["navMesh"].ToString()),nativeReferenceRendererCount=rendererMap.Count,comparisonCandidates=candidateRows,comparisonStatus="actual screenshots pending",coreAuthorized=false,generatedAssets=new object[0]};
System.IO.File.WriteAllText(output+"/baseline-scaffold.json",stateJson(report));return "A04 baseline private clone saved; no core edits; context="+rendererMap.Count;
`;
fs.writeFileSync('Tools/WorldThemes/A04/build-baseline.csx',text);
console.log('Prepared locked A04 baseline scaffold only');
