// 실제 에셋과 물리 접합을 검사한다. 입력 플레이와 미술 인수는 별도다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentForm";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("월드 Edit Mode 필요");
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var root=GameObject.Find("AuthoredEnvironment/사면과 거점 후속 저작");var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();Physics.SyncTransforms();
var rows=new List<string>();var errors=new List<string>();
foreach(Transform t in root.transform){
var renderers=t.GetComponentsInChildren<MeshRenderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader")))errors.Add("재질 오류 "+t.name);
var colliders=t.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();if(colliders.Length==0){if(t.name.StartsWith("역참_서측숙영자리"))rows.Add(t.name+" 통과형 바닥 장식: 원본 프리팹 충돌 없음");else errors.Add("충돌 없음 "+t.name);}
if(t.name.StartsWith("채석장_")||t.name.StartsWith("고개_")){
float low=float.MaxValue,high=float.MinValue;foreach(var mesh in t.GetComponentsInChildren<MeshFilter>(true)){var vertices=mesh.sharedMesh.vertices;for(int i=0;i<vertices.Length;i++){var p=mesh.transform.TransformPoint(vertices[i]);float delta=p.y-terrain.SampleHeight(p)-terrain.transform.position.y;low=Mathf.Min(low,delta);high=Mathf.Max(high,delta);}}
rows.Add(t.name+" terrainIntersection="+low.ToString("F2")+".."+high.ToString("F2"));if(low>0||high<0)errors.Add("암반 지형 교차 실패 "+t.name);
}else{
int support=0;foreach(var point in new[]{b.center,new Vector3(b.min.x,b.center.y,b.min.z),new Vector3(b.max.x,b.center.y,b.min.z),new Vector3(b.min.x,b.center.y,b.max.z),new Vector3(b.max.x,b.center.y,b.max.z)}){
var p=new Vector3(point.x,b.min.y+.15f,point.z);var hits=Physics.RaycastAll(p,Vector3.down,.45f,-1,QueryTriggerInteraction.Ignore);if(hits.Any(hit=>!hit.collider.transform.IsChildOf(t)&&hit.normal.y>.5f&&hit.collider.GetComponentInParent<UPlayGround.GameActor>()==null))support++;
}rows.Add(t.name+" supportSamples="+support);if(support==0)errors.Add("접지 확인 실패 "+t.name);
}
}
int contacts=0;foreach(var id in ids){var p=id.transform.position;foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.6f,p+Vector3.up*1.25f,.45f,-1,QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(root.transform)){contacts++;errors.Add("진행 개체 신규 환경 겹침 "+id.Guid+" "+hit.name);}}}
rows.AddRange(errors.Select(e=>"FAIL "+e));rows.Add("RESULT errors="+errors.Count+" newEnvironmentContacts="+contacts+" identities="+ids.Length);System.IO.File.WriteAllLines(folder+"/contact.txt",rows);return string.Join("\n",rows.ToArray());
