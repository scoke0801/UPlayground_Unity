// Editor의 실제 충돌체에 캡슐을 쓸어 접근·낙하 통과 공간을 확인한다. KCC Play Mode 검증과 구분한다.
var currents=UnityEngine.Object.FindObjectsByType<UPlayGround.Components.WaterfallCurrent>(FindObjectsSortMode.None);
var water=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 강 수면").GetComponent<UPlayGround.Components.FootstepWaterSurface>();water.RefreshMesh();
var sb=new System.Text.StringBuilder();int checks=0,blocked=0;
Physics.SyncTransforms();
foreach(var current in currents){
 foreach(float lane in new[]{-3f,0f,3f}){
  Vector3 feet=current.transform.position+current.transform.right*lane-current.transform.forward*4.8f;
  Vector3 waterPoint;if(!water.TrySampleHeight(feet,out waterPoint))throw new Exception("상류 수면 누락");
  feet.y=waterPoint.y-1.35f;
  Vector3 velocity=current.transform.forward*6f;bool reachedDrop=false,landed=false;string obstruction="";
  for(int step=0;step<180;step++){
   bool isFalling;Vector3 flow;
   bool inside=current.TrySample(feet,out flow,out isFalling);
   if(!reachedDrop && !isFalling){
    if(water.TrySampleHeight(feet,out waterPoint))feet.y=waterPoint.y-1.35f;
   }else{
    reachedDrop=true;velocity+=Vector3.down*75f*0.02f;
    if(inside&&isFalling)current.ConstrainFallVelocity(ref velocity);
    if(!inside&&water.TrySampleHeight(feet,out waterPoint)&&waterPoint.y-feet.y>=1.2f){landed=true;break;}
   }
   Vector3 delta=velocity*0.02f;
   var hits=Physics.CapsuleCastAll(feet+Vector3.up*0.35f,feet+Vector3.up*1.45f,0.35f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
   if(hits.Length>0){obstruction=string.Join(",",hits.Select(hit=>hit.collider.name).Distinct().ToArray());blocked++;break;}
   feet+=delta;
  }
  checks++;sb.AppendLine(current.name+" lane="+lane+" drop="+reachedDrop+" lowerSwim="+landed+" blocked="+obstruction+" end="+feet);
  if(!landed&&obstruction=="")blocked++;
 }
}
sb.AppendLine("capsule checks="+checks+" failed="+blocked);
System.IO.Directory.CreateDirectory("output/RiverBasinWorld/WaterfallTraversal");System.IO.File.WriteAllText("output/RiverBasinWorld/WaterfallTraversal/passage-check.txt",sb.ToString());
return sb.ToString();
