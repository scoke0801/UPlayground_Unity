// 수면 삼각형과 실제 수심을 조사하며 씬과 에셋을 변경하지 않는다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new System.InvalidOperationException("LakeVillageWorld Edit Mode에서 조사해야 합니다.");
var water = UnityEngine.GameObject.Find("LakeWater");
var terrain = UnityEngine.Terrain.activeTerrain;
if (water == null || terrain == null) throw new System.InvalidOperationException("호수 또는 Terrain이 없습니다.");
var renderer = water.GetComponent<UnityEngine.Renderer>();
var mesh = water.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
var vertices = mesh.vertices;
var triangles = mesh.triangles;
var world = new UnityEngine.Vector3[vertices.Length];
for (int i = 0; i < world.Length; i++) world[i] = water.transform.TransformPoint(vertices[i]);
System.Func<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3, float> cross = (a,b,p) => (b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
var rows = new System.Collections.Generic.List<string>();
rows.Add("x,z,waterY,terrainY,depth,insideWaterMesh");
var bounds = renderer.bounds;
int wet=0, dry=0;
for (float z = bounds.min.z; z <= bounds.max.z; z += 5f) for (float x = bounds.min.x; x <= bounds.max.x; x += 5f)
{
    var p = new UnityEngine.Vector3(x,water.transform.position.y,z);
    bool inside = false;
    for (int i=0;i<triangles.Length;i+=3)
    {
        var a=world[triangles[i]]; var b=world[triangles[i+1]]; var c=world[triangles[i+2]];
        if (UnityEngine.Mathf.Abs(cross(a,b,c)) < .00001f) continue;
        float ab=cross(a,b,p), bc=cross(b,c,p), ca=cross(c,a,p);
        if ((ab>=0 && bc>=0 && ca>=0)||(ab<=0 && bc<=0 && ca<=0)) {inside=true;break;}
    }
    if (!inside) continue;
    float ground=terrain.SampleHeight(p)+terrain.transform.position.y;
    float depth=p.y-ground;
    if(depth>0) wet++; else dry++;
    rows.Add(x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+ground.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+depth.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",true");
}
string folder="output/WorldThemeProduction/LakeShrine";
System.IO.Directory.CreateDirectory(folder);
System.IO.File.WriteAllLines(folder+"/live-water-depth.csv",rows);
return "수면 삼각형 내부 5m 격자: 수중="+wet+", 육지/섬="+dry+", dirty="+scene.isDirty+". 육지/섬 표본을 오류 면적으로 해석하지 않음.";
