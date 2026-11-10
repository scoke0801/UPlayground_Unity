// 실제 배치된 충돌체에 대해 교각 양면, 아치 천장, 열린 통로와 상판을 검사한다.
var bridge=GameObject.Find("Castle_Moat_Bridge (RiverBasinWorld B1 ArtR1)");
var lower=bridge.transform.Find("Castle_Moat_Bridge");
if(lower==null)throw new Exception("하부 충돌체 누락");
var collider=lower.GetComponent<MeshCollider>();Physics.SyncTransforms();
var report=new System.Text.StringBuilder();int checks=0,failures=0;
Action<string,bool> record=(name,passed)=>{checks++;if(!passed)failures++;report.AppendLine(name+"="+passed);};
foreach(float span in new[]{-12f,-4f,4f,12f})foreach(float side in new[]{-1f,1f}){
 Vector3 origin=bridge.transform.TransformPoint(new Vector3(side*5f,-4f,span));
 var ray=new Ray(origin,bridge.transform.TransformDirection(new Vector3(-side,0,0)));
 RaycastHit hit;record("pier "+span+" side "+side,collider.Raycast(ray,out hit,10f));
}
foreach(float span in new[]{-8f,0.25f,8f}){
 Vector3 origin=bridge.transform.TransformPoint(new Vector3(0,-4.5f,span));
 var ceilingHits=Physics.SphereCastAll(origin,.2f,bridge.transform.up,4f,~0,QueryTriggerInteraction.Ignore);
 record("arch ceiling "+span,ceilingHits.Any(hit=>hit.collider==collider));
 Vector3 feet=bridge.transform.TransformPoint(new Vector3(-5f,-5.45f,span));
 var hits=Physics.CapsuleCastAll(feet+Vector3.up*.35f,feet+Vector3.up*1.45f,.35f,bridge.transform.right,10f,~0,QueryTriggerInteraction.Ignore);
 record("arch opening "+span,!hits.Any(h=>h.collider==collider));
}
foreach(float along in new[]{-12f,0f,12f}){
 var ray=new Ray(bridge.transform.TransformPoint(new Vector3(0,3f,along)),Vector3.down);
 var hits=Physics.RaycastAll(ray,5f,~0,QueryTriggerInteraction.Ignore);
 record("deck "+along,hits.Any(hit=>hit.collider is BoxCollider && hit.collider.gameObject==bridge && Mathf.Abs(hit.point.y-77f)<.02f));
}
var rules=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.CameraOccluderNameRulesSO>("Assets/10.Datas/Camera/CameraOccluderNameRules.asset");
UPlayGround.Data.CameraOccluderNameRulesSO.Rule rule;
record("camera solid",rules.TryMatch(lower.name,out rule)&&rule.mode.ToString()=="Solid");
record("static nonconvex",lower.gameObject.isStatic&&!collider.convex&&!collider.isTrigger&&collider.enabled);
record("independent of LOD",!bridge.GetComponent<LODGroup>().GetLODs().Any(lod=>lod.renderers.Any(renderer=>renderer!=null&&renderer.transform==lower)));
report.AppendLine("checks="+checks+" failed="+failures+" triangles="+collider.sharedMesh.triangles.Length/3);
System.IO.Directory.CreateDirectory("output/RiverBasinWorld/BridgeLowerCollision");
System.IO.File.WriteAllText("output/RiverBasinWorld/BridgeLowerCollision/checks.txt",report.ToString());return report.ToString();
