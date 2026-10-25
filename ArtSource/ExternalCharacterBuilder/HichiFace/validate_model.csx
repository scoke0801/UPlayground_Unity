var output="output/HichiP09";System.IO.Directory.CreateDirectory(output);
var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset");
var profile=preset.config.FaceMeshProfile;profile.ValidateProfile();var mesh=profile.mesh;
var failures=new System.Collections.Generic.List<string>();var report=new System.Collections.Generic.List<string>();
System.Func<float,bool> finite=v=>!float.IsNaN(v)&&!float.IsInfinity(v);
if(preset.config.Validate().Any())failures.AddRange(preset.config.Validate());
if(mesh.vertices.Any(v=>!finite(v.x)||!finite(v.y)||!finite(v.z)))failures.Add("비정상 정점");
if(mesh.normals.Length!=mesh.vertexCount||mesh.tangents.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount)failures.Add("정점 채널 누락");
foreach(var w in mesh.boneWeights)
{
    if(UnityEngine.Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.0001f)failures.Add("웨이트 합 불일치");
    if(new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i<0||i>=mesh.bindposes.Length))failures.Add("본 인덱스 범위 오류");
}
for(int s=0;s<mesh.blendShapeCount;s++)
{
    var delta=new UnityEngine.Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,delta,null,null);
    if(delta.Any(v=>!finite(v.x)||!finite(v.y)||!finite(v.z)))failures.Add("비정상 표정: "+mesh.GetBlendShapeName(s));
    if(delta.All(v=>v.sqrMagnitude<1e-12f))failures.Add("빈 표정: "+mesh.GetBlendShapeName(s));
}
foreach(var name in new[]{"Hichi_P09Compatible","Hichi_P09_Alternate"})
{
    string path="Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab";
    var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        int missing=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if(missing>0)failures.Add(name+": Missing Script "+missing);
        var animator=root.GetComponent<UnityEngine.Animator>();if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)failures.Add(name+": Avatar 오류");
        foreach(var skin in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
        {
            if(skin.sharedMesh==null||skin.sharedMaterials.Any(m=>m==null)||skin.bones.Any(b=>b==null||!b.IsChildOf(root.transform)))failures.Add(name+": 스킨 참조 오류 "+skin.name);
        }
        foreach(var p in profile.rendererPaths)if(root.transform.Find(p).GetComponent<UnityEngine.SkinnedMeshRenderer>().sharedMesh!=mesh)failures.Add(name+": 얼굴 연결 누락");
        report.Add(name+" : Missing Script="+missing+", active renderers="+root.GetComponentsInChildren<UnityEngine.Renderer>().Count(r=>r.enabled));
    }
    finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
// 프리셋 재로드 뒤 헤어와 장비를 바꿔도 모든 얼굴 슬롯이 유지되는지 별도 검사한다.
var baseRoot=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try
{
    var config=UnityEngine.JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(UnityEngine.JsonUtility.ToJson(preset.config));
    var catalog=new UPlayGround.Editor.P09Builder.P09AssetCatalog();catalog.Refresh();
    foreach(int hair in new[]{1,3,13})
    {
        config.HairStyleSo=catalog.HairStyles.Single(s=>s.name=="HairStyleEditPartData_"+hair.ToString("00"));
        config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,catalog.Chests.Single(s=>s.name=="ChestEditPartData_"+(hair==13?11:3).ToString("00")));
        var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=baseRoot;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
        foreach(var p in profile.rendererPaths)if(baseRoot.transform.Find(p).GetComponent<UnityEngine.SkinnedMeshRenderer>().sharedMesh!=mesh)failures.Add("헤어 교체 후 얼굴 유실");
    }
}
finally{UnityEngine.Object.DestroyImmediate(baseRoot);}
report.Add("Vertices="+mesh.vertexCount+", triangles="+mesh.triangles.Length/3+", bones="+mesh.bindposes.Length+", blendShapes="+mesh.blendShapeCount);
report.Add("Failures="+failures.Count);report.AddRange(failures.Distinct());
System.IO.File.WriteAllLines(output+"/validation.txt",report.ToArray());
if(failures.Count>0)throw new System.Exception(string.Join(";",failures.Distinct().ToArray()));
return report;
