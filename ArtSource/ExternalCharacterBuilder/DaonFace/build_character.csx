var folder="Assets/08.Models/Characters/DaonNewIdentity";
System.IO.File.Copy("ArtSource/ExternalCharacterBuilder/DaonFace/Daon_Face_Albedo.png",folder+"/Daon_Face_Albedo.png",false);
UnityEditor.AssetDatabase.ImportAsset(folder+"/Daon_Face_Albedo.png",UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var textureImporter=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/Daon_Face_Albedo.png");
textureImporter.sRGBTexture=true;textureImporter.maxTextureSize=2048;textureImporter.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;textureImporter.SaveAndReimport();
var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Daon_Face_Albedo.png");
var basePath=folder+"/Daon_NewIdentity_Base.prefab";
var instance=UnityEditor.PrefabUtility.LoadPrefabContents(basePath);
try
{
    var replacement=new System.Collections.Generic.Dictionary<UnityEngine.Material,UnityEngine.Material>();
    foreach(var renderer in instance.GetComponentsInChildren<UnityEngine.Renderer>(true))
    {
        var materials=renderer.sharedMaterials;
        for(int i=0;i<materials.Length;i++)
        {
            var original=materials[i];
            if(original==null)continue;
            bool face=original.name.StartsWith("ho_face")||original.name.StartsWith("ho_brow");
            bool hair=original.name.StartsWith("ho_hair");
            if(!face && !hair)continue;
            UnityEngine.Material material;
            if(!replacement.TryGetValue(original,out material))
            {
                material=new UnityEngine.Material(original);material.name="Daon_"+original.name;
                if(face)material.SetTexture("_MainTex",texture);
                if(hair && material.HasProperty("_Color"))material.SetColor("_Color",new UnityEngine.Color(.78f,.68f,.59f,1));
                if(material.HasProperty("_UseMatCap"))material.SetFloat("_UseMatCap",0);
                if(material.HasProperty("_UseMatCap2nd"))material.SetFloat("_UseMatCap2nd",0);
                if(material.HasProperty("_UseReflection"))material.SetFloat("_UseReflection",0);
                if(material.HasProperty("_UseRim"))material.SetFloat("_UseRim",0);
                string path=folder+"/"+material.name+".mat";
                if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new System.Exception("기존 얼굴 재질 보존: "+path);
                UnityEditor.AssetDatabase.CreateAsset(material,path);replacement.Add(original,material);
            }
            materials[i]=material;
        }
        renderer.sharedMaterials=materials;
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,basePath);
}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(instance);}
var prototype=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.ExternalCharacterProfile>("Assets/10.Datas/Actor/ExternalModels/Honoka_Traveler.asset");
var profile=UnityEngine.Object.Instantiate(prototype);
profile.name="Daon_NewIdentity";
profile.sourcePrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(basePath);
profile.authoringNotes="새 얼굴 조형: 눈 높이·간격·눈꼬리, 광대·턱선, 콧대, 입술 폭 변경. Blender에서 모든 표정에 동일 변형 적용. 얼굴 UV는 ImageGen으로 별도 제작. 원본 모델은 리깅·의상 기반으로 사용. 정규 다온 미교체.";
var profilePath="Assets/10.Datas/Actor/ExternalModels/Daon_NewIdentity.asset";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(profilePath)!=null)throw new System.Exception("기존 프로필 보존");
UnityEditor.AssetDatabase.CreateAsset(profile,profilePath);
var result=UPlayGround.Editor.P09Builder.ExternalCharacterAssembler.Build(profile,"Assets/03.Prefabs/Characters/ExternalModels/Daon_NewIdentity.prefab");
UnityEditor.AssetDatabase.SaveAssets();
return new {prefab=UnityEditor.AssetDatabase.GetAssetPath(result),profile=profilePath};
