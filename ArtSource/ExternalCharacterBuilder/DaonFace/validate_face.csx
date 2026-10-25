var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Honoka/Prefab/Honoka_alt4.prefab");
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Daon_NewIdentity.prefab");
var original=source.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Body").sharedMesh;
var instance=UnityEngine.Object.Instantiate(prefab);
var baked=new UnityEngine.Mesh();
try
{
    var body=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Body");
    var mesh=body.sharedMesh;
    if(!mesh.uv.SequenceEqual(original.uv)||!mesh.boneWeights.SequenceEqual(original.boneWeights)||!mesh.bindposes.SequenceEqual(original.bindposes)||!mesh.triangles.SequenceEqual(original.triangles))throw new System.Exception("UV/스킨/토폴로지 보존 실패");
    if(mesh.blendShapeCount!=original.blendShapeCount)throw new System.Exception("블렌드셰이프 수 불일치");
    for(int i=0;i<mesh.blendShapeCount;i++)
    {
        if(mesh.GetBlendShapeName(i)!=original.GetBlendShapeName(i)||mesh.GetBlendShapeFrameCount(i)!=original.GetBlendShapeFrameCount(i))throw new System.Exception("블렌드셰이프 계약 불일치");
        float weight=body.GetBlendShapeWeight(i);body.SetBlendShapeWeight(i,100);body.BakeMesh(baked);
        foreach(var v in baked.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))throw new System.Exception("유효하지 않은 변형: "+mesh.GetBlendShapeName(i));
        body.SetBlendShapeWeight(i,weight);
    }
    int equipmentVariants=0;
    var equipment=instance.GetComponent<UPlayGround.Components.CharacterEquipmentVisuals>();
    foreach(var slot in equipment.Slots)
    {
        int initial=slot.selectedIndex;
        foreach(var variant in slot.variants)
        {
            if(!equipment.TryEquip(slot.id,variant.id))throw new System.Exception("장비 전환 실패");
            if(body.sharedMesh!=mesh||!body.sharedMaterials.Any(m=>m!=null&&m.name=="Daon_Eye"))throw new System.Exception("장비 전환 중 얼굴 변경");
            equipmentVariants++;
        }
        equipment.TryEquip(slot.id,slot.variants[initial].id);
    }
    var avatar=instance.GetComponent<UnityEngine.Animator>().avatar;
    if(avatar==null||!avatar.isValid||!avatar.isHuman)throw new System.Exception("Humanoid Avatar 오류");
    int missing=instance.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    if(missing!=0)throw new System.Exception("Missing Script");
    var src=original.vertices;var dst=mesh.vertices;int moved=0;float max=0;
    for(int i=0;i<src.Length;i++){float distance=UnityEngine.Vector3.Distance(src[i],dst[i]);if(distance>0.00001f)moved++;max=UnityEngine.Mathf.Max(max,distance);if(src[i].z<1.29f && distance>0.00001f)throw new System.Exception("목 아래 메시 변경");}
    var report="Body vertices: "+mesh.vertexCount+"\nChanged vertices: "+moved+"\nMax displacement (m): "+max+"\nBlendshape contract + finite bake: "+mesh.blendShapeCount+"\nUV, topology, bindposes, bone weights: preserved\nEquipment variants preserving face: "+equipmentVariants+"\nHumanoid Avatar: valid\nMissing Script: "+missing+"\nVisual expression captures are separate. Dialogue/night Play Mode: not performed.\n";
    System.IO.File.WriteAllText("output/DaonFaceRedesign/validation.txt",report);return report;
}
finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(instance);}
