import fs from 'node:fs';
import {guidPaths,scalar,fid,ref} from './audit-streaming-a04-context.mjs';
const m=JSON.parse(fs.readFileSync('output/WorldThemeProduction/ShadeCreek/ConnectedProtection/manifest.json','utf8'));
const caches=new Map();
function scene(path){if(caches.has(path))return caches.get(path);const t=fs.readFileSync(path,'utf8'),heads=[...t.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)],d=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:t.slice(h.index,heads[i+1]?.index??t.length)}]));caches.set(path,d);return d;}
const rows=[];
for(const id of m.editModeIdentities.filter(r=>!r.poseComplete)){
 const docs=scene(id.sourceScene),component=docs.get(id.componentFileId),goId=component?fid(scalar(component.text,'m_GameObject')):null,go=docs.get(goId),instanceId=go?fid(scalar(go.text,'m_PrefabInstance')):id.ownerKey.match(/#prefab:(\d+)/)?.[1],instance=docs.get(instanceId);
 const modifications=[...(instance?.text??'').matchAll(/    - target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\r?\n      propertyPath: ([^\r\n]*)\r?\n      value: ([^\r\n]*)\r?\n      objectReference: ([^\r\n]*)/g)].map(m=>({targetFileId:m[1],targetGuid:m[2],targetPath:guidPaths.get(m[2]),property:m[3],value:m[4],reference:m[5]}));
 rows.push({identity:id.entityGuid,mapId:id.mapId,sourceScene:id.sourceScene,componentId:id.componentFileId,goId,go:go?.text,instanceId,parentTransform:fid(instance?.text.match(/m_TransformParent: (.*)/)?.[1]),sourcePrefabGuid:ref(scalar(instance?.text??'','m_SourcePrefab')),sourcePrefabPath:guidPaths.get(ref(scalar(instance?.text??'','m_SourcePrefab'))),instanceRootModifications:modifications.filter(m=>/m_LocalPosition|m_LocalRotation|m_LocalScale|m_Name|_guid/.test(m.property)),strippedTransforms:[...docs.values()].filter(d=>d.type==='4'&&fid(scalar(d.text,'m_PrefabInstance'))===instanceId).map(d=>d.text)});
}
fs.writeFileSync('output/WorldThemeProduction/ShadeCreek/ConnectedProtection/unresolved-prefab-source-inspection.json',JSON.stringify(rows,null,2));
console.log(JSON.stringify(rows.map(r=>({id:r.identity,source:r.sourcePrefabPath,go:r.go,rootMods:r.instanceRootModifications.filter(m=>m.property==='m_Name'||m.property==='m_LocalPosition.x'),strippedTransforms:r.strippedTransforms})),null,2));
