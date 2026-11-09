import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const output='output/WorldThemeProduction/ShadeCreek';
const hostPath='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const text=fs.readFileSync(hostPath,'utf8'),heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
const docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const ref=s=>s?.match(/guid: (\w+)/)?.[1];
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const vector=s=>Object.fromEntries([...s.matchAll(/([xyzw]): ([\d.eE+-]+)/g)].map(m=>[m[1],Number(m[2])]));
const decode=s=>{try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}};
const gos=new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,d]));
const transforms=new Map([...docs.values()].filter(d=>d.type==='4').map(d=>[fid(scalar(d.text,'m_GameObject')),d]));
const pathFor=id=>{let parts=[],go=id;for(let i=0;i<40&&go;i++){parts.unshift(decode(scalar(gos.get(go)?.text??'','m_Name'))??`fileID:${go}`);const t=transforms.get(go),p=docs.get(fid(scalar(t?.text??'','m_Father')));go=p?fid(scalar(p.text,'m_GameObject')):null;}return parts.join('/');};
const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
const world=id=>{let p=[0,0,0],t=transforms.get(id),complete=!!t;for(let i=0;i<40&&t;i++){if(t.stripped){complete=false;break;}const pos=vector(scalar(t.text,'m_LocalPosition')??''),q=vector(scalar(t.text,'m_LocalRotation')??''),s=vector(scalar(t.text,'m_LocalScale')??'');p=p.map((x,j)=>x*(s[['x','y','z'][j]]??1));const u=[q.x??0,q.y??0,q.z??0],twice=cross(u,p).map(x=>x*2),c=cross(u,twice);p=p.map((x,j)=>x+(q.w??1)*twice[j]+c[j]+(pos[['x','y','z'][j]]??0));const parent=fid(scalar(t.text,'m_Father'));if(parent&&parent!=='0'&&!docs.has(parent))complete=false;t=docs.get(parent);}return {position:complete?p:null,complete};};
const guidPaths=new Map();
const walk=dir=>{for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p);else if(e.name.endsWith('.meta')){const g=fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m)?.[1];if(g)guidPaths.set(g,p.slice(0,-5).replaceAll('\\','/'));}}};
walk('Assets/10.Datas');walk('Assets/02.Scripts');walk('Assets/03.Prefabs');walk('Assets/01.Scenes');walk('Assets/AddressableAssetsData');
const bounds=[1061,1229,471,634];
const inside=p=>p&&p[0]>=bounds[0]&&p[0]<=bounds[1]&&p[2]>=bounds[2]&&p[2]<=bounds[3];
const mono=[...docs.values()].filter(d=>d.type==='114');
const components=mono.filter(d=>/\n  (Regions:|MapID:|_placementData:)/.test(d.text)).map(d=>{const go=fid(scalar(d.text,'m_GameObject'));return {fileId:d.id,path:pathFor(go),scriptGuid:ref(scalar(d.text,'m_Script')),scriptPath:guidPaths.get(ref(scalar(d.text,'m_Script'))),pose:world(go),serialized:d.text};});
const placementLoaders=components.filter(c=>/\n  _placementData:/.test(c.serialized)).map(c=>{
 const guid=ref(scalar(c.serialized,'_placementData')),assetPath=guidPaths.get(guid),asset=assetPath?fs.readFileSync(assetPath,'utf8'):'';
 const rows=[...asset.matchAll(/^  - placementGuid: ([^\r\n]*)\r?$/gm)];
 const records=rows.map((m,i)=>{const s=asset.slice(m.index,rows[i+1]?.index??asset.length),p=vector(s.match(/^    position: (.*)$/m)?.[1]??'');return {placementGuid:m[1],sceneEntityGuid:s.match(/^    sceneEntityGuid: (.*)$/m)?.[1]?.trim(),actorId:s.match(/^    actorId: (.*)$/m)?.[1]?.trim(),position:[p.x,p.y,p.z],sourceKind:s.match(/^    sourceKind: (.*)$/m)?.[1],spawnWhenNearby:s.match(/^    spawnWhenNearby: (.*)$/m)?.[1],initiallyActive:s.match(/^    initiallyActive: (.*)$/m)?.[1]};});
 return {loaderPath:c.path,loaderPose:c.pose,placementGuid:guid,assetPath,assetHash:assetPath?sha(assetPath):null,recordCount:records.length,expandedReferenceRecords:records.filter(r=>inside(r.position)),note:'RuntimePlacementLoader가 record.position을 ActorSpawn/Instantiate/SetPositionAndRotation에 직접 전달하므로 이미 world 좌표. loader TransformPoint 적용 금지'};
});
const sceneContexts=components.filter(c=>/\n  MapID:/.test(c.serialized)).map(c=>{
 const dbGuid=ref(scalar(c.serialized,'_mapConfigDB')),overrideGuid=ref(scalar(c.serialized,'_regionInfoOverride')),dbPath=guidPaths.get(dbGuid),mapId=scalar(c.serialized,'MapID'),db=dbPath?fs.readFileSync(dbPath,'utf8'):'';
 const entries=[...db.matchAll(/^  - mapId: (.*)$/gm)].map((m,i,all)=>({mapId:m[1].trim(),text:db.slice(m.index,all[i+1]?.index??db.length)}));
 const entry=entries.find(e=>e.mapId===mapId),regionGuid=overrideGuid??ref(entry?.text.match(/^    regionInfo: (.*)$/m)?.[1]),regionPath=guidPaths.get(regionGuid),regionText=regionPath?fs.readFileSync(regionPath,'utf8'):'';
 return {componentPath:c.path,componentType:c.scriptPath,mapId,dbGuid,dbPath,regionGuid,regionPath,dbEntry:entry?.text,regionText,limits:'DB 미연결 시 Addressable MapConfigDatabase fallback은 여기서 이름으로 대체하지 않는다'};
});
const referenceComponents=mono.map(d=>{const go=fid(scalar(d.text,'m_GameObject'));return {fileId:d.id,path:pathFor(go),pose:world(go),scriptPath:guidPaths.get(ref(scalar(d.text,'m_Script'))),serialized:d.text};}).filter(c=>inside(c.pose.position));
const story=mono.filter(d=>/R07R08/.test(d.text)||/R07R08/.test(pathFor(fid(scalar(d.text,'m_GameObject'))))).map(d=>{
 const go=fid(scalar(d.text,'m_GameObject')),assetReferences=[...new Set([...d.text.matchAll(/guid: (\w+)/g)].map(m=>m[1]))].map(g=>({guid:g,path:guidPaths.get(g)}));
 return {fileId:d.id,path:pathFor(go),pose:world(go),scriptPath:guidPaths.get(ref(scalar(d.text,'m_Script'))),assetReferences,serialized:d.text};
});
const nearVectorsInStoryAssets=[];
for(const r of story.flatMap(s=>s.assetReferences).filter(a=>a.path?.endsWith('.asset'))){
 const asset=fs.readFileSync(r.path,'utf8');const vectors=[...asset.matchAll(/\{x: ([\d.eE+-]+), y: ([\d.eE+-]+), z: ([\d.eE+-]+)\}/g)].map(m=>({position:m.slice(1,4).map(Number),offset:m.index,context:asset.slice(Math.max(0,m.index-90),m.index+130)})).filter(v=>inside(v.position));
 nearVectorsInStoryAssets.push({...r,hash:sha(r.path),directYamlVectorCountInReference:vectors.length,vectors,limits:'직접 YAML Vector3 검색만. FlowGraph 문자열·runtime lookup·다른 참조 asset의 부재를 증명하지 않는다'});
}
const result={status:'읽기 파일 조사. Unity 호출0·기존461E 제작 guard 유지·새8F43 baseline 승인 아님',sourceScene:hostPath,observedSceneHash:sha(hostPath),expandedReference:bounds,boundsContract:'ContinuousWorldController FindRegion/Contains/DistanceToBounds/CrossesBounds는 플레이어 world x/z를 Regions.Bounds와 직접 비교. SourceOffset/yaw를 Bounds에 재적용하지 않음',components,placementLoaders,sceneContexts,referenceComponents,story,nearVectorsInStoryAssets,limits:['Host native serialized chain/직접 연결 SO만 조사','stripped prefab 대상의 전체 복원·runtime/flowgraph 조건·Collider/Terrain 실제 접지는 미검증','인근 script가 없다는 것은 앞으로 접근할 콘텐츠/연출의 부재 증명이 아님']};
fs.writeFileSync(output+'/a04-streaming-context.json',JSON.stringify(result,null,2));
if(process.argv[1]?.endsWith('audit-streaming-a04-context.mjs'))console.log(JSON.stringify({observedSceneHash:result.observedSceneHash,controllers:components.filter(c=>/\n  Regions:/.test(c.serialized)).map(c=>({path:c.path,scriptPath:c.scriptPath})),contexts:sceneContexts.map(c=>({path:c.componentPath,mapId:c.mapId,db:c.dbPath,region:c.regionPath})),placementLoaders:placementLoaders.map(p=>({path:p.loaderPath,asset:p.assetPath,records:p.recordCount,near:p.expandedReferenceRecords})),referenceComponents:referenceComponents.map(c=>({path:c.path,pose:c.pose,script:c.scriptPath})),storyCount:story.length,storyAssetNearVectors:nearVectorsInStoryAssets.filter(a=>a.path.includes('/ScenarioMaps/')).map(a=>({path:a.path,count:a.directYamlVectorCountInReference}))},null,2));
export {hostPath,docs,guidPaths,sha,scalar,ref,fid,vector,gos,transforms,world,pathFor,sceneContexts};
