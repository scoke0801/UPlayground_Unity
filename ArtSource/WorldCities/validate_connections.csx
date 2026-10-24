var report=new System.Collections.Generic.List<string>();
var arrivals=new System.Collections.Generic.Dictionary<string,string>();
var targets=new System.Collections.Generic.List<string[]>();
int failures=0;
foreach(string name in new[]{"LakeOfLifeScenario","SaltwindHarbor","StonegateCitadel"}){
    var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/"+name+".unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
    var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
    foreach(var root in scene.GetRootGameObjects())if(root.name.StartsWith("CityTravel_")){
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
            if(c==null)continue;
            var so=new UnityEditor.SerializedObject(c);
            if(c.GetType().Name=="PortalActor"){
                c.gameObject.layer=LayerMask.NameToLayer("InteractableObject");
                var p=c.transform.position;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
                UnityEngine.AI.NavMeshHit hit;
                if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,4,UnityEngine.AI.NavMesh.AllAreas))p=hit.position;
                else {report.Add("FAIL portal_ground "+name+"/"+root.name);failures++;}
                c.transform.position=p;
                string target=so.FindProperty("_targetSceneName").stringValue,id=so.FindProperty("_targetArrivalId").stringValue;
                targets.Add(new[]{name,target,id});
                report.Add("portal="+name+" -> "+target+" @ "+p+" layer="+c.gameObject.layer);
                if(!so.FindProperty("_travelOnInteract").boolValue||!so.FindProperty("_isActive").boolValue){failures++;report.Add("FAIL portal_disabled");}
            }
            if(c.GetType().Name=="SceneArrivalPoint"){
                var p=c.transform.position;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
                UnityEngine.AI.NavMeshHit hit;
                if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,4,UnityEngine.AI.NavMesh.AllAreas))p=hit.position;
                else {report.Add("FAIL arrival_ground "+name+"/"+root.name);failures++;}
                c.transform.position=p+Vector3.up*.15f;
                string id=so.FindProperty("_id").stringValue;if(arrivals.ContainsKey(id)){failures++;report.Add("FAIL duplicate "+id);}else arrivals.Add(id,name);
                report.Add("arrival="+id+" @ "+c.transform.position);
            }
        }
    }
    UnityEngine.Physics.SyncTransforms();
    var start=name=="LakeOfLifeScenario"?new Vector3(1091,57,418):new Vector3(180,10,55);
    UnityEngine.AI.NavMeshHit from;
    bool hasStart=UnityEngine.AI.NavMesh.SamplePosition(start,out from,4,UnityEngine.AI.NavMesh.AllAreas);
    foreach(var root in scene.GetRootGameObjects())if(root.name.StartsWith("CityTravel_")){
        var exit=root.transform.Find("TravelSign");UnityEngine.AI.NavMeshHit to;var path=new UnityEngine.AI.NavMeshPath();
        bool pass=hasStart&&UnityEngine.AI.NavMesh.SamplePosition(exit.position,out to,2,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
        report.Add("exit_path="+name+"/"+root.name+" "+pass);if(!pass)failures++;
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
foreach(var target in targets){string owner;bool pass=arrivals.TryGetValue(target[2],out owner)&&owner==target[1];report.Add("arrival_target="+target[0]+" -> "+target[1]+" "+pass);if(!pass)failures++;}
report.Add("portals="+targets.Count+" arrivals="+arrivals.Count+" failures="+failures);
System.IO.File.WriteAllLines("output/WorldCities/connections-validation.txt",report.ToArray());
return string.Join("\n",report.ToArray());
