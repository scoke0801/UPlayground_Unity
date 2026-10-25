// Daon 전용 재질 사본을 만든다. 다른 NPC의 공유 재질은 건드리지 않는다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 모드가 필요합니다.");
string folder="Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonFace";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab");
string[] sources={"Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Face_Toon.mat","Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Common/Eye/P09_Eye_Yellow.mat"};
string[] names={"Daon_Face_Skin","Daon_Eye_Hazel"};
var report=new List<string>();
for(int i=0;i<2;i++)
{
    string path=folder+"/"+names[i]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(sources[i]));AssetDatabase.CreateAsset(material,path);}
    material.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>(sources[i]));material.name=names[i];
    if(i==0)
    {
        material.SetColor("_Color",new Color(1f,.97f,.94f,1f));
        material.SetColor("_ShadowColor",new Color(.90f,.75f,.73f,1f));
        material.SetFloat("_ShadowBorder",.46f);material.SetFloat("_ShadowBlur",.5f);
        material.SetFloat("_BumpScale",.5f);material.SetFloat("_UseReflection",0f);
        material.SetFloat("_UseRim",0f);material.SetFloat("_UseRimShade",0f);
    }
    else
    {
        material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/Characters/DaonFace/Daon_Eye_Hazel.png"));
        material.SetVector("_MainTexHSVG",new Vector4(0f,.78f,1f,1f));
    }
    EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
    string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material,out guid,out id);
    report.Add(names[i]+"|{fileID: "+id+", guid: "+guid+", type: 2}");
}
System.IO.File.WriteAllLines("output/P09DaonFace/material_refs.txt",report);return report;
