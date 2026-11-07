// Play Mode 전용 관찰 도구. 플레이어를 무적으로 두고 대표 조우의 첫 멤버 정면 5m로 차례로 옮겨 몬스터 AI를 기록한다.
// 먼 지점은 ContinuousWorldController.TravelAsync로 이동해 지역 로딩을 기다린 뒤 관찰한다(직접 PlaceAtPose는 미로드 지형에서 낙하 복구로 튕긴다).
// 관찰 중 게임 안내 팝업이 시간을 멈추면 기록하고 닫는다. 씬·에셋은 저장하지 않으며 관찰이 끝난 그룹은 Play Mode 안에서만 비활성화한다.
// 결과: output/OpenWorld/MonsterPopulation/Improvement/play-sample-final.tsv → node Tools/OpenWorld/summarize-encounter-sample.mjs <파일>
if(!EditorApplication.isPlaying)throw new Exception("Play Mode 필요");
var gm=UPlayGround.Manager.GameManager.Instance;
if(gm==null || gm.BootState!=UPlayGround.Manager.GameBootState.Ready)throw new Exception("부팅 미완료");
var player=UPlayGround.Manager.PartyManager.Instance.ActiveCharacter;
var world=UPlayGround.Manager.World.ContinuousWorldController.Active;
if(player==null || world==null)throw new Exception("플레이어/연속 월드 없음");
UPlayGround.PlayerActor.IsTrainingInvincible=true;
var root=GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치").transform;
var names=new[]{
  new KeyValuePair<string,string>("골렘 둘","road_021_Lv25_stone-pass"),
  new KeyValuePair<string,string>("엔트","road_015_Lv20_stone-pass"),
  new KeyValuePair<string,string>("입문 1인","입문 구간 조우 보강/starter_00_Lv3_입문 구간 보강"),
  new KeyValuePair<string,string>("Lv20 거점 4인","숲과 유적 내부 보강/forest_monastery_109_Lv20_남서 수도원 숲"),
  new KeyValuePair<string,string>("Lv20 2인","road_017_Lv20_stone-pass")};
var scenarios=names.Select(n=>new KeyValuePair<string,GameObject>(n.Key,root.Find(n.Value).gameObject)).ToList();
foreach(var s in scenarios)s.Value.SetActive(false);
Func<UPlayGround.MonsterActor,string> idOf=a=>a.Definition!=null?a.Definition.actorId:a.name;
string path="output/OpenWorld/MonsterPopulation/Improvement/play-sample-final.tsv";
var lines=new List<string>{"scenario\tt\tmember\tid\tstate\ttree\tintent\tslot\tability\tring\tdist\tdrift"};
int index=-1,phase=0;float phaseStart=0,nextSample=0;const float duration=25f;
UPlayGround.MonsterActor[] members=null;Vector3[] spawn=null;Vector3 standPoint=Vector3.zero;Quaternion standRotation=Quaternion.identity;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
    if(!EditorApplication.isPlaying){EditorApplication.update-=tick;lines.Add("# 중단: Play Mode 종료");System.IO.File.WriteAllLines(path,lines);return;}
    foreach(var gp in UnityEngine.Object.FindObjectsByType<UPlayGround.UI.UI_Popup_Guide>(FindObjectsSortMode.None))if(gp.gameObject.activeInHierarchy){lines.Add("# 안내 팝업 닫음 t="+Time.realtimeSinceStartup.ToString("F1"));gp.PerformBackFunction();}
    float now=Time.realtimeSinceStartup;
    if(index<0 || (phase==2 && now-phaseStart>=duration)){
        if(members!=null){scenarios[index].Value.SetActive(false);System.IO.File.WriteAllLines(path,lines);}
        index++;
        if(index>=scenarios.Count){EditorApplication.update-=tick;UPlayGround.PlayerActor.IsTrainingInvincible=false;System.IO.File.WriteAllLines(path,lines);System.IO.File.WriteAllText(path+".done","ok "+DateTime.Now.ToString("HH:mm:ss"));return;}
        var g=scenarios[index].Value;
        members=g.GetComponentsInChildren<UPlayGround.MonsterActor>(true);
        spawn=members.Select(m=>m.transform.position).ToArray();
        var lead=members[0].transform;
        standPoint=lead.position+lead.forward*5f;
        standRotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(lead.position-standPoint,Vector3.up).normalized);
        Cysharp.Threading.Tasks.UniTaskExtensions.Forget(world.TravelAsync(world.ResidentMapId,null,standPoint,System.Threading.CancellationToken.None));
        phase=1;phaseStart=now;
        lines.Add("#\t"+scenarios[index].Key);
        return;
    }
    if(phase==1){
        if(world.IsTravelling || now-phaseStart<3f)return;
        UnityEngine.AI.NavMeshHit hit;
        var p=standPoint;if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,4,-1))p=hit.position;
        player.PlaceAtPose(p,standRotation);
        scenarios[index].Value.SetActive(true);
        lines.Add("# 이동 완료 거리="+Vector3.Distance(player.transform.position,standPoint).ToString("F1")+" 소요="+(now-phaseStart).ToString("F1"));
        phase=2;phaseStart=now;nextSample=now+1f;
        return;
    }
    if(now<nextSample)return;
    nextSample=now+0.25f;
    for(int i=0;i<members.Length;i++){
        var m=members[i];if(m==null)continue;
        var runner=m.GetComponent<UPlayGround.AI.BehaviorTree.BehaviorTreeRunner>();
        var combat=m.GetComponent<UPlayGround.Components.EnemyCombat>();
        var bb=runner!=null && runner.Context!=null?runner.Context.Blackboard:null;
        string intent="";bool slot=false;
        if(bb!=null){bb.TryGetString(UPlayGround.AI.BehaviorTree.EnemyBlackboardKeys.DecisionSelectedIntent,out intent);bb.TryGetBool(UPlayGround.AI.BehaviorTree.EnemyBlackboardKeys.HasAttackSlot,out slot);}
        var state=m.ActorController!=null && m.ActorController.CurrentState!=null?m.ActorController.CurrentState.StateId.ToString():"?";
        string ability=combat!=null && combat.CurrentAbility!=null && state=="Attack"?combat.CurrentAbility.abilityId:"";
        bool ring=combat!=null && combat.CurrentSkill!=null && state=="Attack" && combat.CurrentSkill.useDangerRing;
        lines.Add(scenarios[index].Key+"\t"+(now-phaseStart).ToString("F2")+"\t"+i+"\t"+idOf(m)+"\t"+(!m.IsAlive()?"Dead":state)+"\t"+(runner!=null && runner.SourceTree!=null?runner.SourceTree.name:"")+"\t"+intent+"\t"+slot+"\t"+ability+"\t"+ring+"\t"+Vector3.Distance(m.transform.position,player.transform.position).ToString("F1")+"\t"+Vector3.Distance(m.transform.position,spawn[i]).ToString("F1"));
    }
};
EditorApplication.update+=tick;
return "시나리오="+string.Join(", ",scenarios.Select(s=>s.Key).ToArray());
