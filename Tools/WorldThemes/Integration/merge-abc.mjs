import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const out='output/WorldThemesABCIntegration', root='Assets/10.Datas/World/OpenWorld/WorldThemesABC';
const names=['QuarryPassR1','CloudridgeR1','HarborReedR1'];
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n');
const json=p=>JSON.parse(read(p));
const write=(p,j)=>fs.writeFileSync(p,JSON.stringify(j,null,2));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const canonical='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
assert(!fs.existsSync(out+'/published.json'),'적용 완료 기록: 재실행 금지');
fs.mkdirSync(out+'/backup',{recursive:true});
const previous=fs.existsSync(out+'/resource-map.json')?json(out+'/resource-map.json'):null;
const backupFiles=[canonical,canonical+'.meta','Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset','Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset.meta','Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/NavigationR3BankFinal.asset','Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/NavigationR3BankFinal.asset.meta'];
write(out+'/backup-list.json',backupFiles.map(p=>{let dest=out+'/backup/'+p;fs.mkdirSync(dest.slice(0,dest.lastIndexOf('/')),{recursive:true});fs.copyFileSync(p,dest);return {path:p,backup:dest,sha256:sha(p)};}));
const mapping={}, resources=[];
for(const n of names){const src='Assets/10.Datas/World/ThemeStudies/'+n;for(const e of fs.readdirSync(src,{recursive:true,withFileTypes:true})){if(!e.isFile())continue;const p=(e.parentPath||e.path).replaceAll('\\','/')+'/'+e.name;if(/(?:Terrain|Navigation|Baseline)[^/]*\.asset(?:\.meta)?$/i.test(p))continue;const dest=p.replace(src,root+'/'+n);if(p.endsWith('.meta')){const g=read(p).match(/^guid: ([a-f0-9]+)/m)?.[1];if(g)mapping[g]=previous?.mapping[g]||crypto.randomBytes(16).toString('hex');}resources.push({source:p,path:dest});}}
const remap=t=>t.replace(/\b[a-f0-9]{32}\b/g,g=>mapping[g]||g);
for(const r of resources){fs.mkdirSync(r.path.slice(0,r.path.lastIndexOf('/')),{recursive:true});const b=fs.readFileSync(r.source);if(!b.includes(0))fs.writeFileSync(r.path,remap(b.toString('utf8')));else fs.copyFileSync(r.source,r.path);}
write(out+'/resource-map.json',{mapping,resources});
function parse(t){const a=[...t.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n/gm)], m=new Map();for(let i=0;i<a.length;i++){assert(!m.has(a[i][2]));m.set(a[i][2],{type:a[i][1],text:t.slice(a[i].index,a[i+1]?.index??t.length)});}return m;}
assert.equal(parse('--- !u!1 &1\nA\n--- !u!4 &2\nB\n').size,2);
const baseText=read('output/WorldThemesSharedBaseline/'+canonical),base=parse(baseText),latestText=read(canonical),current=parse(latestText),conflicts=[],applied=[];
const owner=(t,b=t)=>{const rx=/(?:_ownerKey: |propertyPath: _ownerKey\n\s+value: )([^\n]+)/g;const values=[...b.matchAll(rx)].map(m=>m[1]);let i=0;return t.replace(rx,(m,v)=>m.slice(0,-v.length)+(values[i++]||v));};
for(const n of names){const review=parse(read('Assets/01.Scenes/ArtReview/WorldThemes/'+n+'.unity'));let count=0;for(const [id,r]of review){const b=base.get(id),c=current.get(id);let after=owner(r.text,b?.text);if(b&&owner(b.text)===after)continue;if(['154','218','1660057539'].includes(r.type)||after.includes('Unity.AI.Navigation.NavMeshSurface'))continue;
 if(!b){if(c){conflicts.push({region:n,id,kind:'added-fileID-collision'});continue;}current.set(id,{...r,text:after});count++;continue;}
 if(!c){conflicts.push({region:n,id,kind:'latest-removed'});continue;}
 if(c.text===b.text){current.set(id,{...r,text:after});count++;continue;}
 // 최신 사용자 속성과 다른 지역의 변경이 같은 문서에 있으면 diff3로 충돌을 검출한다.
 for(const [key,text]of Object.entries({current:c.text,base:b.text,agent:after}))fs.writeFileSync(out+'/'+key+'.tmp',text);
 const result=spawnSync('git',['merge-file','-p',out+'/current.tmp',out+'/base.tmp',out+'/agent.tmp'],{encoding:'utf8',maxBuffer:20*1024*1024});
 if(result.status!==0){conflicts.push({region:n,id,kind:'property-conflict',detail:result.stdout});continue;}current.set(id,{...r,text:result.stdout});count++;
 }applied.push({region:n,sceneDocuments:count});}
write(out+'/merge-conflicts.json',{status:conflicts.length?'conflicts':'scene-clear',conflicts});assert.equal(conflicts.length,0,'충돌로 중단');
const header=latestText.slice(0,latestText.indexOf('--- !u!'));
fs.writeFileSync(out+'/scene-merged.unity',remap(header+[...current.values()].map(r=>r.text).join('')));
const plans=[];
for(const n of names){let h=[],a=[],d=[],before=[],after=[];if(n==='CloudridgeR1'){let j=json('output/'+n+'/terrain-height-delta.json');h=j.rows; j=json('output/'+n+'/terrain-surface-delta.json');a=j.rows.map(r=>[r.x,r.z,...r.before,...r.after]);before=j.beforeLayerGuids;after=j.afterLayerGuids;const dt=json('output/'+n+'/terrain-detail-delta.json');write(out+'/'+n+'-details.json',dt);}
 else {const j=json('output/'+n+'/terrain-delta.json');if(n==='QuarryPassR1'){before=j.beforeLayers;after=j.afterLayers;a=j.cells.map(r=>[r.x,r.z,...r.before,...r.after]);}else{before=j.layerMap.slice(0,20).map(r=>r.guid);after=j.layerMap.map(r=>r.guid);h=j.heightChanges.map(r=>[r.xIndex,r.zIndex,r.before,r.after]);a=j.alphaChanges.map(r=>[r.xIndex,r.zIndex,...r.before.slice(0,20),...r.after]);}}
 write(out+'/'+n+'-cells.json',{height:h,alpha:a});fs.writeFileSync(out+'/'+n+'-before-layers.txt',before.join('\n'));fs.writeFileSync(out+'/'+n+'-after-layers.txt',after.map(g=>mapping[g]||g).join('\n'));plans.push({region:n,height:h.length,alpha:a.length,beforeLayers:before.length,afterLayers:after.length});}
write(out+'/merge-plan.json',{canonicalSha256:sha(canonical),applied,plans,resourceCount:resources.length,baseEqualsLatest:baseText===latestText});
console.log(JSON.stringify({applied,plans,resources:resources.length}));


