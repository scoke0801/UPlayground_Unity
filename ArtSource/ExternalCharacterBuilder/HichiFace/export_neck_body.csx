var root=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest");var mesh=skin.sharedMesh;
using(var writer=new System.IO.BinaryWriter(System.IO.File.Create("output/HichiBareNeckFix/body.bin"))){
    writer.Write(mesh.vertexCount);Action<Vector3[]> points=a=>{foreach(var p in a){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}};points(mesh.vertices);points(mesh.normals);foreach(var uv in mesh.uv){writer.Write(uv.x);writer.Write(uv.y);}var triangles=mesh.triangles;writer.Write(triangles.Length);foreach(int i in triangles)writer.Write(i);
    foreach(var w in mesh.boneWeights){writer.Write(w.boneIndex0);writer.Write(w.boneIndex1);writer.Write(w.boneIndex2);writer.Write(w.boneIndex3);writer.Write(w.weight0);writer.Write(w.weight1);writer.Write(w.weight2);writer.Write(w.weight3);}
    writer.Write(skin.bones.Length);foreach(var bone in skin.bones)writer.Write(bone.name);
}
return mesh.vertexCount;
