// 기존 좌표·GUID를 유지한 채 현재 호수 월드의 지도 배경만 다시 촬영한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 대기 필요");
const string ConfigPath="Assets/10.Datas/World/LakeVillage/MinimapConfig.asset";
var config=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.MinimapIconConfigSO>(ConfigPath);
var path=AssetDatabase.GetAssetPath(config.backgroundSprite);var size=new Vector2Int(config.backgroundSprite.texture.width,config.backgroundSprite.texture.height);
var utility=typeof(UPlayGround.Tool.Editor.MinimapCaptureEditorWindow).Assembly.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility");
var render=utility.GetMethod("Render",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);
var import=utility.GetMethod("ImportSprite",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);
var go=new GameObject("LakeWaterfallMinimapCapture"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=config.captureWorldSizeXY.y*.5f;camera.aspect=config.captureWorldSizeXY.x/config.captureWorldSizeXY.y;
camera.transform.position=new Vector3(config.captureCenter.x,700,config.captureCenter.y);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.nearClipPlane=.2f;camera.farClipPlane=1400;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.ambientGroundColor;
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.requiresDepthTexture=true;extra.requiresColorTexture=true;
var waters=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.sharedMaterials.Any(m=>m!=null&&(m.shader.name.ToLower().Contains("water")||m.shader.name.ToLower().Contains("river")||m.shader.name.Contains("Wide Fall")))).ToArray();
var previous=waters.Select(r=>r.sharedMaterials).ToArray();
var mapWater=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mapWater.SetColor("_BaseColor",new Color(.10f,.49f,.70f,1));mapWater.SetFloat("_Cull",0);
Texture2D texture=null;
try{
 foreach(var water in waters)water.sharedMaterials=Enumerable.Repeat(mapWater,water.sharedMaterials.Length).ToArray();
 texture=(Texture2D)render.Invoke(null,new object[]{camera,size,true,1000f});
 System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
 // 원본에서 폭포 주변 픽셀을 잘라 검수 근거로 보존한다.
 int x=Mathf.RoundToInt((1144-(config.captureCenter.x-config.captureWorldSizeXY.x*.5f))/config.captureWorldSizeXY.x*size.x);
 int y=Mathf.RoundToInt((1029-(config.captureCenter.y-config.captureWorldSizeXY.y*.5f))/config.captureWorldSizeXY.y*size.y);
 var crop=new Texture2D(240,240);crop.SetPixels(texture.GetPixels(x-120,y-120,240,240));crop.Apply();System.IO.File.WriteAllBytes("output/lake-waterfall/minimap/waterfall-detail.png",crop.EncodeToPNG());UnityEngine.Object.DestroyImmediate(crop);
}finally{for(int i=0;i<waters.Length;i++)waters[i].sharedMaterials=previous[i];if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mapWater);}
config.backgroundSprite=(Sprite)import.Invoke(null,new object[]{path,size,false});EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
bool restored=waters.Select((r,i)=>r.sharedMaterials.SequenceEqual(previous[i])).All(x=>x);System.IO.File.WriteAllText("output/lake-waterfall/minimap/forced-color-audit.txt","waterRenderers="+waters.Length+" restored="+restored+"\n"+string.Join("\n",waters.Select(r=>r.name).ToArray()));
return path+" "+size+" center="+config.captureCenter+" world="+config.captureWorldSizeXY+" guid="+AssetDatabase.AssetPathToGUID(path)+" waterRenderers="+waters.Length+" restored="+restored;

