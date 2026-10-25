var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Head.asset");
var values=System.IO.File.ReadAllText("output/DaonP09Compatibility/fitted_vertices.json").Split(new[]{'[',']',',',' ','\r','\n'},System.StringSplitOptions.RemoveEmptyEntries).Select(v=>float.Parse(v,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
var vertices=mesh.vertices;
if(values.Length!=7015*3 || vertices.Length!=7125)throw new System.Exception("얼굴·목 정점 계약 불일치");
for(int i=0;i<values.Length/3;i++)vertices[i]=new UnityEngine.Vector3(values[i*3],values[i*3+1],values[i*3+2]);
mesh.vertices=vertices;mesh.RecalculateBounds();UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssets();return mesh.vertexCount;
