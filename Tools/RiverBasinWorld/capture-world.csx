var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("전용 씬 필요");
string folder="output/RiverBasinWorld/captures";System.IO.Directory.CreateDirectory(folder);
var ts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).ToArray();
Func<float,float,float> ground=(x,z)=>{var t=ts.First(g=>x>=g.transform.position.x&&x<=g.transform.position.x+512&&z>=g.transform.position.z&&z<=g.transform.position.z+512);return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;};
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var go=new GameObject("RiverBasinWorldCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=62;camera.farClipPlane=2600;camera.nearClipPlane=.1f;
Func<Vector2Int,Texture2D> render=size=>{var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(size.x,size.y,24,RenderTextureFormat.ARGB32);var oldTarget=camera.targetTexture;try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var texture=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0);texture.Apply();return texture;}finally{camera.targetTexture=oldTarget;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}};
var positions=new[]{new Vector3(135,0,21),new Vector3(219,0,9),new Vector3(267,0,-2),new Vector3(434,0,-78),new Vector3(438,0,-81),new Vector3(307.2f,77,-15.36f)};
var targets=new[]{new Vector3(186,0,15),new Vector3(280,0,-8),new Vector3(325,0,-14),new Vector3(362,0,-1),new Vector3(427,0,-225),new Vector3(307.2f,72,-60)};
var names=new[]{"01_entry","02_fork","03_encounter","04_return","05_meadow","06_crossing"};
try{
 for(int i=0;i<names.Length;i++){positions[i].y=(i==5?77:ground(positions[i].x,positions[i].z))+2;targets[i].y=(i==5?72:ground(targets[i].x,targets[i].z))+1.4f;camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);var texture=render(new Vector2Int(1280,800));try{System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}}
 camera.orthographic=true;camera.orthographicSize=800;camera.transform.position=new Vector3(0,1300,0);camera.transform.rotation=Quaternion.Euler(90,0,0);var overview=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1200,1200),true,2400f});try{System.IO.File.WriteAllBytes(folder+"/00_overview.png",overview.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(overview);}
}finally{UnityEngine.Object.DestroyImmediate(go);}
return folder;
