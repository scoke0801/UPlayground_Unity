import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const plan=JSON.parse(fs.readFileSync('Tools/WorldThemes/A05/shadecreek-study.json','utf8'));
const hash=text=>crypto.createHash('sha256').update(text).digest('hex');
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const inspect=path=>{
 const text=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n'),heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)/gm)];
 const docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));
 const gos=new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,d]));
 const transforms=new Map([...docs.values()].filter(d=>d.type==='4').map(d=>[fid(scalar(d.text,'m_GameObject')),d]));
 const name=id=>scalar(gos.get(id)?.text??'','m_Name')?.replaceAll('FullTree_01_Red','FullTree_01');
 const goPath=id=>{let parts=[],go=id;for(let i=0;i<30&&go;i++){parts.unshift(name(go)??`unresolved:${go}`);const parent=docs.get(fid(scalar(transforms.get(go)?.text??'','m_Father')));go=parent?fid(scalar(parent.text,'m_GameObject')):null;}return parts.join('/');};
 const docKey=d=>d.type==='1'?'GO:'+goPath(d.id):d.type+':'+goPath(fid(scalar(d.text,'m_GameObject')));
 const keys=new Map([...docs.values()].map(d=>[d.id,docKey(d)]));
 const normalize=d=>d.text.replace(/^--- !u!\d+ &-?\d+/m,'--- '+docKey(d)).replaceAll('FullTree_01_Red','FullTree_01').replace(/\{fileID: (-?\d+)\}/g,(_,id)=>'{local:'+ (id==='0'?'null':keys.get(id)??'unresolved:'+id)+'}');
 const components=types=>[...docs.values()].filter(d=>types.includes(d.type)&&!d.text.split('\n')[0].includes('stripped')).map(d=>({key:docKey(d),normalized:normalize(d)})).sort((a,b)=>a.key.localeCompare(b.key));
 const materials=[...docs.values()].filter(d=>d.type==='23').map(d=>({renderer:goPath(fid(scalar(d.text,'m_GameObject'))),slots:[...(d.text.match(/  m_Materials:\n((?:  - .*\n)+)/)?.[1]??'').matchAll(/fileID: (-?\d+), guid: (\w+)/g)].map((m,slot)=>({slot,fileId:m[1],guid:m[2]}))})).sort((a,b)=>a.renderer.localeCompare(b.renderer));
 const materialIndependentRenderer=components(['23']).map(d=>({key:d.key,normalized:d.normalized.replace(/  m_Materials:\n(?:  - .*\n)+/,'  m_Materials: [leaf-comparison]\n')}));
 return {path,guid:fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1],fileHash:hash(fs.readFileSync(path)),transforms:components(['4']),meshes:components(['33']),lod:components(['205']),colliders:components(['64','65','135','136']),rendererSettings:materialIndependentRenderer,materials,nestedSources:[...text.matchAll(/m_SourcePrefab: \{fileID: (-?\d+), guid: (\w+)/g)].map(m=>({fileId:m[1],guid:m[2]}))};
};
const red=inspect(plan.sourceRedTree),green=inspect(plan.greenTreeCandidate);
const compare=key=>JSON.stringify(red[key])===JSON.stringify(green[key]);
const identical={transforms:compare('transforms'),meshes:compare('meshes'),lod:compare('lod'),colliders:compare('colliders'),rendererSettingsExceptMaterials:compare('rendererSettings')};
const slotDifferences=[];
for(let i=0;i<red.materials.length;i++){
 const a=red.materials[i],b=green.materials[i];assert.equal(a.renderer,b.renderer);assert.equal(a.slots.length,b.slots.length);
 for(let slot=0;slot<a.slots.length;slot++)if(a.slots[slot].guid!==b.slots[slot].guid)slotDifferences.push({rendererRelativePath:a.renderer,slot,redGuid:a.slots[slot].guid,greenGuid:b.slots[slot].guid});
}
if(!Object.values(identical).every(Boolean)){
 const differenceKeys=['transforms','meshes','lod','colliders','rendererSettings'].filter(k=>!compare(k));
 console.log(JSON.stringify({identical,differences:differenceKeys.map(key=>({key,red:red[key].slice(0,1),green:green[key].slice(0,1)}))},null,2));
}
assert.ok(Object.values(identical).every(Boolean),'mesh/LOD/collider/transform/renderer invariant differs');
assert.ok(slotDifferences.length>0&&slotDifferences.every(d=>d.slot>0),'leaf-only difference requires review');
const billboardPaths=[plan.sourceRedTree.replace('/PF_Farm_GenTree_C_FullTree_01_Red.prefab','/Billboards/PF_Farm_GenTree_C_FullTree_01_impostor_Red.prefab'),plan.greenTreeCandidate.replace('/PF_Farm_GenTree_C_FullTree_01.prefab','/Billboards/PF_Farm_GenTree_C_FullTree_01_impostor.prefab')];
const billboards=billboardPaths.map(path=>{
 const text=fs.readFileSync(path,'utf8').replace(/\r\n/g,'\n');
 const heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)/gm)];
 const docs=heads.map((h,i)=>({type:h[1],fileId:h[2],text:text.slice(h.index,heads[i+1]?.index??text.length)}));
 const meshes=docs.filter(d=>d.type==='43').map(d=>({fileId:d.fileId,serializedHash:hash(d.text.replace(/^--- !u!43 &-?\d+\n/,'').replace(/m_Name: .*\n/,'m_Name: [mesh]\n'))}));
 return {path,guid:fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1],fileHash:hash(fs.readFileSync(path)),meshes,materialSubassets:docs.filter(d=>d.type==='21').map(d=>({fileId:d.fileId,hash:hash(d.text)})),rendererSlots:docs.filter(d=>d.type==='23').map(d=>({fileId:d.fileId,materials:d.text.match(/  m_Materials:\n((?:  - .*\n)+)/)?.[1]}))};
});
const billboardMeshSerializedEqual=JSON.stringify(billboards[0].meshes)===JSON.stringify(billboards[1].meshes);
const output={status:'직접 직렬화된 LOD0~2 불변량만 통과. nested impostor는 별도 후보이며 whole-prefab 동일/시각 통과 아님',red,green,identical,slotDifferences,billboards,billboardMeshSerializedEqual,guard:'LOD0~2의 exact leaf slot과 별도 impostor material slot/atlas 호환을 Host에서 확인해야 한다. replaceObjects는 rendererGlobalId+slot·GUID/fileID·소유권 역참조·실물 화면 확인 뒤 채운다'};
fs.mkdirSync('output/WorldThemeProduction/ShadeCreek',{recursive:true});
fs.writeFileSync('output/WorldThemeProduction/ShadeCreek/tree-variant-pair.json',JSON.stringify(output,null,2));
console.log(JSON.stringify({identical,slotDifferences,billboardMeshSerializedEqual,billboards:billboards.map(b=>({path:b.path,guid:b.guid,meshes:b.meshes,materialSubassets:b.materialSubassets,rendererSlots:b.rendererSlots}))},null,2));
