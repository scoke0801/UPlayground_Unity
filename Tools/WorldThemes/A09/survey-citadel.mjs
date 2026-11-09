import fs from 'node:fs';
import crypto from 'node:crypto';
const out='output/WorldThemeProduction/StonegateCitadel'; fs.mkdirSync(out,{recursive:true});
const live=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A00/live-baseline.json','utf8'));
const disk=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A12/disk-baseline-environment.json','utf8'));
const position=r=>{const p={};for(const m of r.transformOverrides??[]){const k=m.property.match(/^m_LocalPosition\.([xyz])$/);if(k)p[k[1]]=+m.value;}return p;};
const instances=disk.environmentInstances.filter(r=>r.parentPath.includes('/돌등성')).map(r=>({...r,positionOverride:position(r)}));
const metadata=path=>({path,exists:fs.existsSync(path),guid:fs.existsSync(path+'.meta')?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:fs.existsSync(path)?crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'):null});
const regionPath='Assets/01.Scenes/Scenario/OpenWorld/Regions/StonegateCitadelRegion.unity';
const yaml=fs.readFileSync(regionPath,'utf8');
const docs=[...yaml.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>({type:+m[1],id:m[2],text:m[3]}));
const field=(s,n)=>s.match(new RegExp('^  '+n+': (.*)$','m'))?.[1];
const fileId=(s,n)=>field(s,n)?.match(/fileID: (-?\d+)/)?.[1];
const vec=s=>[...s.matchAll(/[xyzw]: ([-\deE+.]+)/g)].map(m=>+m[1]);
const transforms=new Map(docs.filter(d=>d.type===4).map(d=>[d.id,{id:d.id,parent:fileId(d.text,'m_Father'),position:vec(field(d.text,'m_LocalPosition')??''),rotation:vec(field(d.text,'m_LocalRotation')??''),scale:vec(field(d.text,'m_LocalScale')??'')}]));
function rotate(v,q){const [x,y,z,w]=q;const t=[2*(y*v[2]-z*v[1]),2*(z*v[0]-x*v[2]),2*(x*v[1]-y*v[0])];return[v[0]+w*t[0]+y*t[2]-z*t[1],v[1]+w*t[1]+z*t[0]-x*t[2],v[2]+w*t[2]+x*t[1]-y*t[0]];}
function world(p,parent){let t=transforms.get(parent),count=0;while(t&&count++<30){p=rotate(p.map((n,i)=>n*(t.scale[i]??1)),t.rotation.length===4?t.rotation:[0,0,0,1]).map((n,i)=>n+(t.position[i]??0));t=transforms.get(t.parent);}return p;}
const npc=[];
for(const d of docs.filter(d=>d.type===1001)){
 const name=d.text.match(/propertyPath: m_Name\s+value: (NPC_[^\r\n]+)/)?.[1];if(!name)continue;
 const p=['x','y','z'].map(axis=>Number(d.text.match(new RegExp('propertyPath: m_LocalPosition\\.'+axis+'\\s+value: ([^\\r\\n]+)'))?.[1]));
 const parent=d.text.match(/m_TransformParent: \{fileID: (\d+)/)?.[1];
 npc.push({name,instanceFileId:d.id,parentTransform:parent,localPosition:p,worldPositionFromSerializedHierarchy:world(p,parent),sourcePrefabGuid:d.text.match(/m_SourcePrefab: \{fileID: \d+, guid: (\w+)/)?.[1],liveStatus:'실제 로드·접지 미확인'});
}
const byParent={};for(const r of instances)byParent[r.parentPath]=(byParent[r.parentPath]??0)+1;
const kit=['Castle_Gate','Castle_Keep','Castle_Wall_Straight','Castle_Terrace_Stairs','Road_Castle_Gate_Approach','Road_Plaza','Prop_Stone_Lantern','Prop_Market_Stall_East'].map(n=>({...metadata('Assets/03.Prefabs/Environment/WorldCities/StonegateEast/'+n+'.prefab'),status:'파일/GUID/hash만 조사. 실제 화면·URP·LOD·충돌·성능 미검증'}));
const result={themeId:'StonegateCitadel',status:'파일 사전조사. Editor 미호출·씬 미제작',sourceRegion:metadata(regionPath),existingInstanceCount:instances.length,positionOverrideBounds:{minX:Math.min(...instances.map(r=>r.positionOverride.x).filter(Number.isFinite)),maxX:Math.max(...instances.map(r=>r.positionOverride.x).filter(Number.isFinite)),minZ:Math.min(...instances.map(r=>r.positionOverride.z).filter(Number.isFinite)),maxZ:Math.max(...instances.map(r=>r.positionOverride.z).filter(Number.isFinite))},byParent,landmarks:instances.filter(r=>/Castle_Keep|Castle_Gate|Road_Plaza/.test(r.sourcePrefabPath??'')),regionNpcs:npc,regionContracts:{mapId:'StonegateCitadel',contentRootInitiallyInactive:true,cityParentPosition:[760,52,280],cityParentYaw:180,arrivalIds:[...yaml.matchAll(/^  _id: (worldcities\.StonegateCitadel[^\r\n]+)/gm)].map(m=>m[1]),activationIds:[...new Set([...yaml.matchAll(/^  _activationId: ([^\r\n]+)/gm)].map(m=>m[1]))],explicitSceneEntityIds:[...yaml.matchAll(/^  _guid: ([^\r\n]+)/gm)].map(m=>m[1]),warning:'NPC IDs는 inherited/added component 포함 live 전체 대조 필요. Host ID 목록에 없다고 비보호 판정 금지'},hostProtectedIds:live.ids.filter(r=>r.position[0]>=380&&r.position[0]<=810&&r.position[2]>=-120&&r.position[2]<=310),currentNav:live.nav,candidates:kit,derivedMeshes:['KeepCityEntry','CityEntryStair'].map(n=>metadata('Assets/10.Datas/World/OpenWorld/CitadelArtRevision/'+n+'.asset')),generatedAssets:[]};
fs.writeFileSync(out+'/survey.json',JSON.stringify(result,null,2));fs.writeFileSync(out+'/existing-citadel-instances.json',JSON.stringify(instances,null,2));
console.log(JSON.stringify({instances:instances.length,npcs:npc.length,regionContracts:result.regionContracts,bounds:result.positionOverrideBounds,kit:kit.filter(r=>r.exists).length}));
