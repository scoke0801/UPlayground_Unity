import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const output='output/WorldThemeProduction/SaltwindHarbor';
fs.mkdirSync(output,{recursive:true});
const load=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const vector=s=>Object.fromEntries([...s.matchAll(/([xyzw]): ([\d.eE+-]+)/g)].map(m=>[m[1],Number(m[2])]));
const decode=s=>{try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}};
const parse=p=>{
 const text=fs.readFileSync(p,'utf8'),heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
 const docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));
 const gos=new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,d]));
 const transforms=new Map([...docs.values()].filter(d=>d.type==='4').map(d=>[fid(scalar(d.text,'m_GameObject')),d]));
 const pathFor=id=>{let parts=[],go=id;for(let i=0;i<45&&go;i++){parts.unshift(decode(scalar(gos.get(go)?.text??'','m_Name'))??`fileID:${go}`);const parent=docs.get(fid(scalar(transforms.get(go)?.text??'','m_Father')));go=parent?fid(scalar(parent.text,'m_GameObject')):null;}return parts.join('/');};
 const rotate=(v,q)=>{const u=[q.x??0,q.y??0,q.z??0],w=q.w??1,a=[v.x??0,v.y??0,v.z??0];const cross=(x,y)=>[x[1]*y[2]-x[2]*y[1],x[2]*y[0]-x[0]*y[2],x[0]*y[1]-x[1]*y[0]];const t=cross(u,a).map(x=>2*x),uxt=cross(u,t);return Object.fromEntries(['x','y','z'].map((k,i)=>[k,a[i]+w*t[i]+uxt[i]]));};
 const positionFor=id=>{let point={x:0,y:0,z:0},t=transforms.get(id),complete=!!t,chain=[];for(let i=0;i<45&&t;i++){if(t.stripped){complete=false;break;}const p=vector(scalar(t.text,'m_LocalPosition')??''),q=vector(scalar(t.text,'m_LocalRotation')??''),s=vector(scalar(t.text,'m_LocalScale')??'');chain.push({fileId:t.id,position:p,rotation:q,scale:s});point=rotate({x:point.x*(s.x??1),y:point.y*(s.y??1),z:point.z*(s.z??1)},q);point={x:point.x+(p.x??0),y:point.y+(p.y??0),z:point.z+(p.z??0)};const parent=fid(scalar(t.text,'m_Father'));if(parent&&parent!=='0'&&!docs.has(parent))complete=false;t=docs.get(parent);}return {position:complete?point:null,complete,chain};};
 return {text,docs,gos,transforms,pathFor,positionFor};
};
const live=load('output/WorldThemeProduction/A00/live-baseline.json');
const disk=load('output/WorldThemeProduction/A12/disk-baseline-environment.json');
const city=load('Tools/OpenWorld/city-layout.json').cities.find(c=>c.mapId==='SaltwindHarbor');
const localPosition=r=>{const p={};for(const m of r.transformOverrides??[]){const key=m.property.match(/^m_LocalPosition\.([xyz])$/)?.[1];if(key)p[key]=Number(m.value);}return p;};
const environmentInstances=disk.environmentInstances.filter(r=>/\/항구(?:\/|$)|\/항구_/.test(r.parentPath??'')||/^항구_/.test(r.overrideName??'')).map(r=>({...r,serializedLocalPosition:localPosition(r)}));
const nativeObjects=disk.environment.filter(r=>/항구|CoastalWater|Harbor/i.test(r.path));
const types={};for(const row of environmentInstances)types[row.sourcePrefabPath]=(types[row.sourcePrefabPath]??0)+1;
const candidatePaths=[
 'Assets/03.Prefabs/Environment/HarborRevision/Harbor_TimberDeck.prefab',
 'Assets/03.Prefabs/Environment/HarborRevision/Harbor_StoneQuay.prefab',
 'Assets/03.Prefabs/Environment/HarborRevision/Harbor_Mooring.prefab',
 'Assets/03.Prefabs/Environment/HarborRevision/Harbor_FishingSkiff.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Reeds_03.prefab',
 'Assets/03.Prefabs/Environment/LakeVillage/LakeHouse_Shop.prefab',
 'Assets/10.Datas/World/OpenWorld/HarborNaturalWater.mat',
 'Assets/10.Datas/World/OpenWorld/ResourceOverhaul/Ground_6.terrainlayer',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_RiverBed.terrainlayer'
];
const candidates=candidatePaths.map(path=>{
 const exists=fs.existsSync(path),s=exists?fs.readFileSync(path,'utf8'):'';
 return {path,exists,guid:exists?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:exists?sha(path):null,nativeCounts:{meshRenderer:(s.match(/^--- !u!23 /gm)??[]).length,skinnedMeshRenderer:(s.match(/^--- !u!137 /gm)??[]).length,meshCollider:(s.match(/^--- !u!64 /gm)??[]).length,boxCollider:(s.match(/^--- !u!65 /gm)??[]).length,lodGroup:(s.match(/^--- !u!205 /gm)??[]).length,monoBehaviour:(s.match(/^--- !u!114 /gm)??[]).length},referencedMeshes:[...new Set([...s.matchAll(/m_Mesh: \{fileID: (-?\d+), guid: (\w+)/g)].map(m=>m[2]+':'+m[1]))],status:'디스크 조사만. 실제 렌더·URP·충돌·LOD 전이·성능·채택 미검증. native component 수는 nested prefab/model 렌더러를 재귀 로드한 결과가 아님'};
});
const regionPath=city.regionPath,region=parse(regionPath);
const regionAnchors=[...region.docs.values()].filter(d=>d.type==='114'&&(/_guid:|_mapId:|_destinationMap|_arrivalId:/.test(d.text))).map(d=>{
 const go=fid(scalar(d.text,'m_GameObject'));const pose=region.positionFor(go);
 return {fileId:d.id,gameObject:go,path:region.pathFor(go),guid:scalar(d.text,'_guid'),regionNativePose:pose,worldPosition:pose.position,scriptGuid:d.text.match(/m_Script:.*guid: (\w+)/)?.[1],serialized:d.text,limits:'Region native hierarchy에 이미 offset/yaw가 포함된다. Host offset을 다시 적용하지 않는다. stripped prefab transform 미해결은 null. 실제 런타임 지면·콜라이더·등록 미검증'};
});
const regionWaypoints=[...region.gos.keys()].filter(id=>/Arrival|Travel|HarborWorkPoint|DockApproach|Fishing|Rest/.test(region.pathFor(id))).map(id=>({path:region.pathFor(id),sourcePose:region.positionFor(id)}));
const host=parse(live.scene);
const coastal=[...host.gos.keys()].filter(id=>/CoastalWater/.test(decode(scalar(host.gos.get(id).text,'m_Name'))??'')).map(id=>{const go=host.gos.get(id);return {fileId:id,path:host.pathFor(id),pose:host.positionFor(id),transform:host.transforms.get(id)?.text,components:[...go.text.matchAll(/component: \{fileID: (-?\d+)\}/g)].map(m=>host.docs.get(m[1])).filter(d=>d&&d.type!=='4').map(d=>({type:d.type,text:d.text}))};});
const regionBinding=[...host.docs.values()].filter(d=>d.type==='114'&&d.text.includes('OpenWorld.SaltwindHarbor.Content')).map(d=>{const line=d.text.indexOf('  - MapId: SaltwindHarbor');return {fileId:d.id,serialized:d.text.slice(line,d.text.indexOf('  - MapId:',line+10)>0?d.text.indexOf('  - MapId:',line+10):undefined)};});
const data={status:'A08 사전 조사. Editor 호출/Assets 생성/제작/시각 검증 없음',sourceScene:live.scene,sourceSceneHash:sha(live.scene),sourceTerrainHash:sha('Assets/10.Datas/World/OpenWorld/Terrain.asset'),regionScene:regionPath,regionSceneHash:sha(regionPath),oldRecipeCityContract:city,liveBranches:live.branches.filter(r=>/항구|CoastalWater|Harbor/i.test(r.path)),environmentInstances,nativeObjects,topTypes:Object.entries(types).sort((a,b)=>b[1]-a[1]),candidates,regionAnchors,regionWaypoints,coastalWater:coastal,actualHostRegionBinding:regionBinding,hostNearbyIds:live.ids.filter(r=>r.position[0]>=1700&&r.position[2]<=150),scatterMode:live.terrain.scatter,detailPrototypeCount:live.terrain.details.length,limits:['recipe는 참고 입력이며 core 편집 권한이 아니다','prefab 로컬/native 직렬화 좌표와 실제 Renderer/Collider bounds 검증을 구분한다','Region 고유 NPC/낚시/휴식/도착/작업/이동을 ArtReview에 복제하지 않는다','CoastalWater 대평면을 축소하지 않고 수면/해안/접안 높이부터 실제 화면에서 확인한다']};
assert.equal(data.scatterMode,'InstanceCountMode');assert.equal(data.detailPrototypeCount,6);assert.ok(city&&coastal.length===1);
fs.writeFileSync(output+'/survey.json',JSON.stringify(data,null,2));
console.log(JSON.stringify({instances:environmentInstances.length,nativeObjects:nativeObjects.length,topTypes:data.topTypes.slice(0,6),coastal:coastal.map(r=>({path:r.path,pose:r.pose,transform:r.transform})),regionAnchors:regionAnchors.map(r=>({path:r.path,guid:r.guid,world:r.worldPosition,complete:r.regionNativePose.complete})),candidateCounts:candidates.map(r=>({path:r.path,exists:r.exists,counts:r.nativeCounts}))},null,2));
