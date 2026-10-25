var folder="Assets/08.Models/Characters/DaonNewIdentity";
var texturePath=folder+"/Daon_Eye_Albedo.png";
System.IO.File.Copy("ArtSource/ExternalCharacterBuilder/DaonFace/Daon_Eye_Albedo.png",texturePath,false);
UnityEditor.AssetDatabase.ImportAsset(texturePath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
importer.sRGBTexture=true;importer.maxTextureSize=2048;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
UnityEngine.Material eye=null;
foreach(var path in new[]{folder+"/Daon_NewIdentity_Base.prefab","Assets/03.Prefabs/Characters/ExternalModels/Daon_NewIdentity.prefab"})
{
    var instance=UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        foreach(var renderer in instance.GetComponentsInChildren<UnityEngine.Renderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                if(materials[i]==null || materials[i].name!="ho_eye_alt3")continue;
                if(eye==null)
                {
                    eye=new UnityEngine.Material(materials[i]);eye.name="Daon_Eye";
                    eye.mainTexture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
                    foreach(var property in new[]{"_UseEmission","_UseEmission2nd","_UseMatCap","_UseMatCap2nd","_UseReflection","_UseRim"})if(eye.HasProperty(property))eye.SetFloat(property,0);
                    UnityEditor.AssetDatabase.CreateAsset(eye,folder+"/Daon_Eye.mat");
                }
                materials[i]=eye;
            }
            renderer.sharedMaterials=materials;
        }
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,path);
    }
    finally{UnityEditor.PrefabUtility.UnloadPrefabContents(instance);}
}
UnityEditor.AssetDatabase.SaveAssets();
return UnityEditor.AssetDatabase.GetAssetPath(eye);
