var camera=SceneView.lastActiveSceneView.camera;var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
var target=new RenderTexture(1000,800,24);Texture2D image=null;
try{camera.targetTexture=target;camera.aspect=1.25f;camera.Render();RenderTexture.active=target;image=new Texture2D(1000,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();System.IO.File.WriteAllBytes("output/HichiNeckFix/CurrentSceneView_After.png",image.EncodeToPNG());return "현재 사용자 SceneView 카메라에서 저장된 메시 렌더";}
finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);}
