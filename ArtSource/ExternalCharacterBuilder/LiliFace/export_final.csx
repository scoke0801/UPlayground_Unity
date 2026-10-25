var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Lili_P09_Face.asset");var m=profile.mesh;
using(var w=new System.IO.BinaryWriter(System.IO.File.Create("ArtSource/ExternalCharacterBuilder/LiliFace/final.bin"))){
w.Write(m.vertexCount);w.Write(m.subMeshCount);w.Write(m.blendShapeCount);Action<Vector3[]> points=a=>{foreach(var v in a){w.Write(v.x);w.Write(v.y);w.Write(v.z);}};points(m.vertices);foreach(var uv in m.uv){w.Write(uv.x);w.Write(uv.y);}
for(int s=0;s<m.subMeshCount;s++){var t=m.GetTriangles(s);w.Write(t.Length);foreach(int i in t)w.Write(i);}
for(int s=0;s<m.blendShapeCount;s++){w.Write(m.GetBlendShapeName(s));var d=new Vector3[m.vertexCount];m.GetBlendShapeFrameVertices(s,0,d,null,null);points(d);}
foreach(var b in m.boneWeights){w.Write(b.boneIndex0);w.Write(b.boneIndex1);w.Write(b.boneIndex2);w.Write(b.boneIndex3);w.Write(b.weight0);w.Write(b.weight1);w.Write(b.weight2);w.Write(b.weight3);}
w.Write(profile.boneNames.Length);foreach(var n in profile.boneNames)w.Write(n);
}
return m.vertexCount;
