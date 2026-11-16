// 최초 배치 기록이다. 이후 수동 보정된 저장 씬·Fall.asset·Fall.mat가 정본이므로 재실행하지 않는다.
// 최종 차이·제약은 docs/work-status/lake-waterfall.md를 따른다.
if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드에서 실행하세요.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity") throw new InvalidOperationException("대상 씬이 다릅니다.");
if (GameObject.Find("LakeWaterfall_Northeast") != null) throw new InvalidOperationException("이미 배치됐습니다. 중복 적용하지 마세요.");
const string Dir = "Assets/10.Datas/World/OpenWorld/LakeWaterfall";
System.IO.Directory.CreateDirectory(Dir);
AssetDatabase.Refresh();
var terrain = Terrain.activeTerrain;
var lake = GameObject.Find("LakeWater");
var lakeMaterial = lake.GetComponent<MeshRenderer>().sharedMaterial;
var baseline = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToDictionary(t => t.GetInstanceID(), t => t.position);
var heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
var root = new GameObject("LakeWaterfall_Northeast");
root.transform.SetParent(GameObject.Find("AuthoredEnvironment").transform, false);
Undo.RegisterCreatedObjectUndo(root, "호수 북동 폭포 배치");
var origin = new Vector3(1147, 0, 1033);
root.transform.position = origin;
var path = new[] {
    new Vector3(1156,101.1f,1048), new Vector3(1153,99.2f,1042),
    new Vector3(1149,97.5f,1037), new Vector3(1147,96.8f,1033),
    new Vector3(1145.5f,96.4f,1030), new Vector3(1144.2f,94.2f,1027),
    new Vector3(1142,89.2f,1023.5f), new Vector3(1138,81.6f,1017),
    new Vector3(1134,77.06f,1011), new Vector3(1129,77.018f,1004)
};
var halfWidths = new[] {1.1f,1.5f,2.2f,3.5f,3.7f,3.55f,3.6f,3.1f,4.4f,5.2f};
var distances = new float[path.Length];
for (int i=1;i<path.Length;i++) distances[i]=distances[i-1]+Vector3.Distance(path[i],path[i-1]);
Func<float,Vector3> point = s => {
    int i=0; while(i<path.Length-2 && distances[i+1]<s) i++;
    return Vector3.Lerp(path[i],path[i+1],Mathf.InverseLerp(distances[i],distances[i+1],s));
};
Func<float,float> width = s => {
    int i=0; while(i<path.Length-2 && distances[i+1]<s) i++;
    return Mathf.Lerp(halfWidths[i],halfWidths[i+1],Mathf.InverseLerp(distances[i],distances[i+1],s));
};
var fallMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR3/B1RiverFall_R3.mat"));
fallMaterial.name="LakeNortheastFall";
foreach (string p in new[]{"ShallowColor","DeepColor","FoamColor"}) fallMaterial.SetColor("_River"+p,lakeMaterial.GetColor("_"+p));
fallMaterial.SetVector("_RiverWaveVelocity",lakeMaterial.GetVector("_WaveVelocity"));
var sourceProperties=new[]{"DepthDistance","Absorption","SurfaceOpacity","EdgeDistance","WaveScale","WaveStrength","ReflectionStrength","RefractionStrength","FoamWidth","FoamStrength","FoamScale","FoamSpeed","WeatherWindResponse"};
var targetProperties=new[]{"DepthDistance","Absorption","Opacity","EdgeDistance","WaveScale","WaveStrength","Reflection","Refraction","FoamWidth","FoamStrength","FoamScale","FoamSpeed","WindResponse"};
for(int i=0;i<sourceProperties.Length;i++) fallMaterial.SetFloat("_River"+targetProperties[i],lakeMaterial.GetFloat("_"+sourceProperties[i]));
fallMaterial.SetTexture("_NormalMap",lakeMaterial.GetTexture("_NormalMap"));
fallMaterial.SetColor("_RiverUnderwaterColor",lakeMaterial.GetColor("_UnderwaterColor"));
fallMaterial.SetFloat("_RiverUnderwaterOpacity",lakeMaterial.GetFloat("_UnderwaterOpacity"));
fallMaterial.SetFloat("_RiverFrontFace",1);
fallMaterial.SetColor("_ShallowColor",new Color(.14f,.45f,.44f,1));
fallMaterial.SetColor("_DeepColor",new Color(.08f,.31f,.34f,1));
fallMaterial.SetColor("_FoamColor",new Color(.78f,.87f,.84f,1));
fallMaterial.SetFloat("_FoamAmount",.67f);
fallMaterial.SetFloat("_FoamStart",.22f);
fallMaterial.SetVector("_NoiseTiling",new Vector4(2.6f,1.3f,0,0));
AssetDatabase.CreateAsset(fallMaterial,Dir+"/Fall.mat");
var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
const int Rows=110, Columns=24;
for(int row=0;row<=Rows;row++) {
    float s=distances.Last()*row/Rows;
    var p=point(s);var tangent=point(Mathf.Min(distances.Last(),s+.2f))-point(Mathf.Max(0,s-.2f));
    var side=Vector3.Cross(Vector3.up,tangent).normalized;
    float flow=Mathf.InverseLerp(distances[3],distances[8],s);
    float transition=Mathf.SmoothStep(0,1,Mathf.InverseLerp(distances[1],distances[3],s));
    if(s>distances[8]) transition=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(distances[8],distances[9],s));
    for(int col=0;col<=Columns;col++) {
        float u=col/(float)Columns, across=u*2-1;
        float wobble=(Mathf.PerlinNoise(s*.19f+9,u*3.2f)-.5f)*.16f;
        var v=p+side*(across*width(s)*(1+.05f*Mathf.Sin(s*.42f+u*2)))+Vector3.up*(wobble*Mathf.Sin(u*Mathf.PI));
        vertices.Add(v-origin);uv.Add(new Vector2(u,Mathf.Lerp(.01f,1,Mathf.InverseLerp(distances[0],distances[9],s))));colors.Add(new Color(transition,0,0,1));
    }
}
for(int row=0;row<Rows;row++)for(int col=0;col<Columns;col++){int a=row*(Columns+1)+col,b=a+Columns+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
var mesh=new Mesh{name="LakeNortheastFall"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
if(mesh.normals[0].y<0) {triangles.Reverse();mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();}
if(vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))) throw new InvalidOperationException("정점 오류");
AssetDatabase.CreateAsset(mesh,Dir+"/Fall.asset");
var water=new GameObject("낙수와 호수 연결");water.transform.SetParent(root.transform,false);water.AddComponent<MeshFilter>().sharedMesh=mesh;
var renderer=water.AddComponent<MeshRenderer>();renderer.sharedMaterial=fallMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
// 수평 상류만 등록한다. 낙수면 전체를 등록하면 허공에서 수영하게 된다.
var query=new Mesh{name="LakeNortheastSpringQuery"};int qr=33;
query.SetVertices(vertices.Take((qr+1)*(Columns+1)).ToList());query.SetTriangles(triangles.Take(qr*Columns*6).ToArray(),0);query.RecalculateBounds();
AssetDatabase.CreateAsset(query,Dir+"/SpringQuery.asset");
var surface=water.AddComponent<UPlayGround.Components.FootstepWaterSurface>();surface.surfaceMesh=water.GetComponent<MeshFilter>();surface.queryMesh=query;surface.profile=lake.GetComponent<UPlayGround.Components.FootstepWaterSurface>().profile;surface.maximumDepth=.6f;surface.useVolumeBounds=true;surface.volumeBounds=new Bounds(new Vector3(4,98,7),new Vector3(25,7,27));surface.RefreshMesh();
var rocksRoot=new GameObject("침식 암반");rocksRoot.transform.SetParent(root.transform,false);
var rockPaths=new[]{"PF_EastLands_RiverRock_01_Gold_17","PF_EastLands_RiverRock_02_Gold_16","PF_EastLands_RiverRock_03_Gold_14","PF_EastLands_RiverRock_04_Gold_15"};
var random=new System.Random(1511);
for(int i=0;i<16;i++) {
    float s=distances[1]+(distances[8]-distances[1])*(i/2f)/8;
    var p=point(s);var tangent=point(s+.2f)-point(s-.2f);var side=Vector3.Cross(Vector3.up,tangent).normalized;
    p+=side*(i%2==0?-1:1)*(width(s)+.7f+(float)random.NextDouble());
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1/"+rockPaths[i%4]+".prefab");
    var rock=(GameObject)PrefabUtility.InstantiatePrefab(prefab,rocksRoot.transform);rock.name="폭포 호안 암반 "+(i+1);
    rock.transform.rotation=Quaternion.Euler(-8+(float)random.NextDouble()*16,(float)random.NextDouble()*360,5);
    float scale=i<10?3.7f:2.5f;rock.transform.localScale=new Vector3(scale*1.3f,scale*(i%3==0?1.6f:1),scale*1.1f);
    rock.transform.position=new Vector3(p.x,terrain.SampleHeight(p)+terrain.transform.position.y,p.z);
    var bounds=rock.GetComponentsInChildren<MeshRenderer>().First().bounds;
    rock.transform.position+=Vector3.up*(rock.transform.position.y-bounds.min.y-bounds.size.y*.32f);
}
var original=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try {
    var objects=original.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    var splash=objects.First(t=>t.name=="낙수 물보라");var mist=objects.First(t=>t.name=="낙수 안개");
    for(int i=0;i<3;i++) {
        var fx=UnityEngine.Object.Instantiate(splash.gameObject,root.transform);fx.name="입수 물보라 "+(i+1);
        fx.transform.position=new Vector3(1134+(i-1)*2.1f,77.2f,1010-(i-1)*1.2f);fx.transform.localScale=Vector3.one*.8f;
    }
    var fog=UnityEngine.Object.Instantiate(mist.gameObject,root.transform);fog.name="입수 미스트";fog.transform.position=new Vector3(1134,78,1010);fog.transform.localScale=Vector3.one*.85f;
} finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(original,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene); }
var impact=new GameObject("호수 입수 지점");impact.transform.SetParent(root.transform,false);impact.transform.position=new Vector3(1133,77,1010);
var link=root.AddComponent<UPlayGround.Gameplay.World.WaterfallLink>();link.source=surface;link.target=lake.GetComponent<UPlayGround.Components.FootstepWaterSurface>();link.impact=impact.transform;link.mist=root.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="입수 미스트");
var changed=baseline.Where(pair=>EditorUtility.InstanceIDToObject(pair.Key)==null||((Transform)EditorUtility.InstanceIDToObject(pair.Key)).position!=pair.Value).Count();
var after=terrain.terrainData.GetHeights(0,0,heights.GetLength(0),heights.GetLength(1));int changedHeight=0;for(int z=0;z<after.GetLength(0);z++)for(int x=0;x<after.GetLength(1);x++)if(after[z,x]!=heights[z,x])changedHeight++;
if(changed!=0||changedHeight!=0)throw new InvalidOperationException("기존 배치 또는 지형이 변경됐습니다.");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
AssetDatabase.SaveAssets();
return new {root=root.name,vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,existingTransformsChanged=changed,terrainHeightsChanged=changedHeight};
