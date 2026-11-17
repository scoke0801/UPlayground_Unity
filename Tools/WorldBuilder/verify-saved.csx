// 별도 요청에서 재로드한 미리보기의 원본·이웃·충돌체를 대조한다.
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var reports = new System.Collections.Generic.List<object>();
var paths = System.IO.Directory.GetFiles("Assets/WorldBuilder/Worlds", "WorldPreview.unity", System.IO.SearchOption.AllDirectories);
foreach (var pathOnDisk in paths) {
    string path=pathOnDisk.Replace('\\','/');
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    if(!scene.IsValid() || !scene.isLoaded)throw new System.Exception("먼저 미리보기 씬을 재로드하세요.");
    var terrains = new Terrain[4,4];
    foreach(var root in scene.GetRootGameObjects()) {
        var terrain=root.GetComponent<Terrain>(); if(terrain==null)continue;
        int x=Mathf.RoundToInt(root.transform.position.x/512),z=Mathf.RoundToInt(root.transform.position.z/512); terrains[z,x]=terrain;
    }
    string sourceFolder=System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(path))+"/Source";
    var bytes=System.IO.File.ReadAllBytes(sourceFolder+"/BaseHeight.bytes");
    var corners=new System.Collections.Generic.List<object>(); double maxError=0,maxSeam=0;
    var heights=new float[4,4][,];
    for(int z=0;z<4;z++)for(int x=0;x<4;x++) {
        var terrain=terrains[z,x]; if(terrain==null)throw new System.Exception("타일 누락");
        if(terrain.GetComponent<TerrainCollider>().terrainData!=terrain.terrainData)throw new System.Exception("충돌체 원본 불일치");
        if(terrain.materialTemplate==null||!terrain.materialTemplate.shader.isSupported)throw new System.Exception("미리보기 머티리얼 누락");
        var surface=terrain.terrainData.terrainLayers;
        if(surface.Length!=1||surface[0]==null||surface[0].diffuseTexture==null||!UnityEditor.AssetDatabase.Contains(surface[0].diffuseTexture))throw new System.Exception("미리보기 표면의 저장 참조 누락");
        if(terrain.terrainData.heightmapResolution!=513 || terrain.terrainData.holesResolution!=512)throw new System.Exception("해상도 불일치");
        if(terrain.leftNeighbor!=(x>0?terrains[z,x-1]:null)||terrain.topNeighbor!=(z<3?terrains[z+1,x]:null)||terrain.rightNeighbor!=(x<3?terrains[z,x+1]:null)||terrain.bottomNeighbor!=(z>0?terrains[z-1,x]:null))throw new System.Exception("저장된 이웃 불일치");
        heights[z,x]=terrain.terrainData.GetHeights(0,0,513,513);
    }
    for(int i=0;i<4;i++) {
        int tx=i%2*3,tz=i/2*3,lx=i%2*512,lz=i/2*512;
        int index=2*((tz*512+lz)*2049+tx*512+lx); int raw=bytes[index]|bytes[index+1]<<8;
        var terrain=terrains[tz,tx]; double expected=terrain.transform.position.y+raw/65535.0*terrain.terrainData.size.y;
        double actual=terrain.transform.position.y+heights[tz,tx][lz,lx]*terrain.terrainData.size.y;
        maxError=System.Math.Max(maxError,System.Math.Abs(expected-actual));
        corners.Add(new {corner=new []{"SW","SE","NW","NE"}[i],raw,expected,actual});
    }
    for(int z=0;z<4;z++)for(int x=0;x<4;x++)for(int i=0;i<513;i++) {
        if(x<3)maxSeam=System.Math.Max(maxSeam,System.Math.Abs(heights[z,x][i,512]-heights[z,x+1][i,0])*terrains[z,x].terrainData.size.y);
        if(z<3)maxSeam=System.Math.Max(maxSeam,System.Math.Abs(heights[z,x][512,i]-heights[z+1,x][0,i])*terrains[z,x].terrainData.size.y);
    }
    if(maxError>.03||maxSeam>.01)throw new System.Exception("저장/재로드 고도 오차 초과");
    reports.Add(new {path,terrainCount=16,maxCornerError=maxError,maxSeam,corners});
}
if(previous.IsValid() && previous.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
return new {previews=reports,originalScene=previous.path,originalDirty=previous.isDirty};
