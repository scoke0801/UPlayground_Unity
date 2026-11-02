UPlayGround.PlayerActor player=null;
UPlayGround.MovementController.PlayerMovementController controller=null;
bool checkRelease=SessionState.GetBool("SuperJumpGliding.CheckRelease",false);
SessionState.SetBool("SuperJumpGliding.CheckRelease",false);
string interruption=SessionState.GetString("SuperJumpGliding.CheckInterruption","");
SessionState.SetString("SuperJumpGliding.CheckInterruption","");
var prefix="output/PlayerSuperJumpGliding/"+(!string.IsNullOrEmpty(interruption)?"play-"+interruption:checkRelease?"play-release":"play-smoke");
System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
var output=prefix+".tsv";
var rows=new System.Text.StringBuilder("time\tstate\tphase\tx\ty\tz\tverticalSpeed\n");
var transitions=new System.Collections.Generic.List<string>();
var errors=new System.Collections.Generic.List<string>();
float start=0f;float baseHeight=0f;float peak=0f;
double waitingStarted=EditorApplication.timeSinceStartup;
bool attached=false;bool gliding=false;bool finished=false;bool started=false;
bool released=false;bool reuseRejected=false;
bool interrupted=false;
var phaseField=typeof(UPlayGround.State.PlayerSuperJumpGlidingState).GetField("_phase",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
var contactField=typeof(UPlayGround.State.PlayerSuperJumpGlidingState).GetField("_hasContact",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
System.Action<UPlayGround.State.GameActorState,UPlayGround.State.GameActorState> onState=(oldState,newState)=>{
    transitions.Add((Time.time-start).ToString("F3")+":"+(oldState==null?"None":oldState.StateName)+"->"+newState.StateName);
    var traversal=oldState as UPlayGround.State.PlayerSuperJumpGlidingState;
    if(traversal!=null)transitions.Add("exit="+traversal.LastExitReason+" interruptedMotion="+traversal.InterruptedMotionName);
};
Application.LogCallback onLog=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action finish=()=>{
    if(finished)return;finished=true;EditorApplication.update-=tick;if(controller!=null)controller.OnStateChanged-=onState;Application.logMessageReceived-=onLog;
    System.IO.File.WriteAllText(output,rows.ToString());
    transitions.Add("activeGliderViews="+UnityEngine.Object.FindObjectsByType<UPlayGround.Components.PlayerGliderView>(FindObjectsSortMode.None).Length);
    System.IO.File.WriteAllText(prefix+"-result.txt","started="+started+"\nattached="+attached+"\ngliding="+gliding+"\nlanded="+(controller!=null&&controller.Motor.GroundingStatus.IsStableOnGround)+"\npeakHeight="+(peak-baseHeight).ToString("F4")+"\nstate="+(controller==null?"NONE":controller.CurrentState.StateName)+"\nfootIKDisabled="+(player!=null&&player.FootIK!=null&&player.FootIK.ForceDisabled)+"\nmanualRelease="+released+"\nairborneReuseRejected="+reuseRejected+"\nnewErrors="+errors.Count+"\n"+string.Join("\n",transitions.ToArray())+"\n"+string.Join("\n",errors.ToArray())+"\nState API start, no physical input device.\n");
};
tick=()=>{
    if(!started){
        if(EditorApplication.timeSinceStartup-waitingStarted>60){finish();return;}
        if(!EditorApplication.isPlaying)return;
        if(player==null)player=UnityEngine.Object.FindFirstObjectByType<UPlayGround.PlayerActor>();
        if(player==null||player.PlayerController==null||player.IsInputSuppressed||!player.IsAlive())return;
        controller=player.PlayerController;
        if(controller.GliderBinding==null||!controller.Motor.GroundingStatus.IsStableOnGround||controller.CurrentState==null)return;
        start=Time.time;baseHeight=peak=controller.Motor.TransientPosition.y;
        controller.OnStateChanged+=onState;
        started=controller.TryStartSuperJump();
        if(!started){finish();return;}
    }
    if(!EditorApplication.isPlaying||player==null){finish();return;}
    float elapsed=Time.time-start;var position=controller.Motor.TransientPosition;peak=Mathf.Max(peak,position.y);
    var state=controller.CurrentState;var traversal=state as UPlayGround.State.PlayerSuperJumpGlidingState;
    if(traversal!=null){attached|=(bool)contactField.GetValue(traversal);gliding|=phaseField.GetValue(traversal).ToString()=="Gliding";}
    if(checkRelease&&!released&&gliding&&elapsed>2.2f){controller.ReleaseGlider();released=true;reuseRejected=!controller.TryStartSuperJump();}
    if(interruption=="view-lost"&&!interrupted&&gliding&&elapsed>2.2f){UnityEngine.Object.Destroy(UnityEngine.Object.FindFirstObjectByType<UPlayGround.Components.PlayerGliderView>().gameObject);interrupted=true;}
    rows.AppendLine(elapsed.ToString("F4")+"\t"+state.StateName+"\t"+(traversal==null?"":phaseField.GetValue(traversal).ToString())+"\t"+position.x.ToString("F4")+"\t"+position.y.ToString("F4")+"\t"+position.z.ToString("F4")+"\t"+controller.Motor.Velocity.y.ToString("F4"));
    if(elapsed>12f||(elapsed>0.5f&&!controller.IsTraversing&&controller.Motor.GroundingStatus.IsStableOnGround
        && (state.StateId==UPlayGround.State.ActorStateId.Idle||state.StateId==UPlayGround.State.ActorStateId.GroundMove)))finish();
};
Application.logMessageReceived+=onLog;EditorApplication.update+=tick;
return "짧은 상태 API 주행 검사 준비. 기존 Domain Reload 설정을 유지함.";
