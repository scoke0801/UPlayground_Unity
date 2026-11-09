import fs from 'node:fs';import crypto from 'node:crypto';
const out='output/ShadeCreekR3/Integration',main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity',review='Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity';
const sha=s=>crypto.createHash('sha256').update(s).digest('hex');
const plan=JSON.parse(fs.readFileSync(`${out}/plan.json`));
const before=fs.readFileSync(`${out}/LakeVillageWorld.before.unity`,'utf8'),current=fs.readFileSync(main,'utf8'),source=fs.readFileSync(review,'utf8');
if(sha(current)!==plan.mainHash||sha(source)!==plan.reviewHash||!plan.canonicalUnchangedSinceReviewBaseline)throw Error('조사 이후 씬 변경 또는 기준선 불일치');
const parse=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],text:m[0]}]));
const a=parse(before),b=parse(source),map=fs.readFileSync(`${out}/asset-map.tsv`,'utf8').replace(/^\uFEFF/,'').trim().split(/\r?\n/).map(l=>l.split('\t'));
const normalize=s=>s.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/g,'$1SCENE');
const remap=s=>{for(const [old,next] of map)s=s.replaceAll(old,next);return s;};
const mod=/    - target: [^\n]+\n      propertyPath: ([^\n]+)\n      value: [^\n]*\n      objectReference: [^\n]+\n/g;
const allowed=p=>/^m_(Materials\.Array\.data\[\d+\]|LocalPosition\.[xyz]|LocalRotation\.[xyzw]|LocalScale\.[xyz]|LocalEulerAnglesHint\.[xyz]|Enabled|IsActive|Name|Size|LocalReferencePoint\.[xyz]|LODs\.Array\.data\[\d+\]\.renderers\.Array\.data\[\d+\]\.renderer)$/.test(p.replaceAll("'",''));
const stripPrefab=text=>normalize(text).replace(mod,(whole,p)=>allowed(p)?'':whole).replace(/  m_AddedComponents:[\s\S]*?(?=  m_SourcePrefab:)/,'  m_AddedComponents: VERIFIED\n');
const changed=[],added=[];let result=before.slice(0,before.indexOf('--- !u!'));
for(const [id,old] of a){const next=b.get(id);if(!next)throw Error('기존 객체 누락 '+id);
if(normalize(old.text)===normalize(next.text)){result+=old.text;continue;}
if(old.type==='114'){
 if(normalize(old.text).replace(/m_NavMeshData: [^\n]+/,'NAV')!==normalize(next.text).replace(/m_NavMeshData: [^\n]+/,'NAV'))throw Error('콘텐츠 변경 '+id);
}else if(['154','218'].includes(old.type)){
 if(old.text.replace(/m_TerrainData: [^\n]+/,'TERRAIN')!==next.text.replace(/m_TerrainData: [^\n]+/,'TERRAIN'))throw Error('Terrain 참조 외 변경 '+id);
}else if(old.type==='4'){
 const clean=t=>t.replace(/  m_Children:[\s\S]*?(?=  m_Father:)/,'CHILDREN\n');if(clean(old.text)!==clean(next.text))throw Error('기존 Transform 변경 '+id);
 const children=t=>[...t.matchAll(/^  - \{fileID: (-?\d+)\}/gm)].map(m=>m[1]);if(children(old.text).some(child=>!children(next.text).includes(child)))throw Error('기존 자식 제거 '+id);
}else if(old.type==='1001'){
 if(stripPrefab(old.text)!==stripPrefab(next.text)){fs.writeFileSync(`${out}/unexpected-before.txt`,stripPrefab(old.text));fs.writeFileSync(`${out}/unexpected-after.txt`,stripPrefab(next.text));throw Error('비환경 프리팹 변경 '+id);}
 for(const m of next.text.matchAll(/addedObject: \{fileID: (-?\d+)\}/g)){if(!a.has(m[1])&&b.get(m[1])?.type!=='23')throw Error('비렌더러 컴포넌트 추가 '+id);}
}else throw Error('예상 밖 기존 유형 변경 '+id+' '+old.type);
result+=remap(next.text);changed.push({id,type:old.type});
}
for(const [id,next] of b)if(!a.has(id)){if(!['1','4','23','33','1001'].includes(next.type))throw Error('예상 밖 새 컴포넌트 '+id);result+=remap(next.text);added.push({id,type:next.type});}
const reviewGuid=fs.readFileSync(review+'.meta','utf8').match(/guid: (\w+)/)[1];if(result.includes(reviewGuid))throw Error('검토 씬 owner 유입');
for(const [old] of map)if(result.includes(old))throw Error('검토 데이터 의존 잔존');
fs.writeFileSync(`${out}/LakeVillageWorld.merged.unity`,result);fs.writeFileSync(`${out}/merge-result.json`,JSON.stringify({beforeHash:sha(before),afterHash:sha(result),changed,added,contentChanges:0,ownerChanges:0},null,2));
console.log({changed:changed.length,added:added.length,contentChanges:0,ownerChanges:0});
if(process.argv.includes('--apply')){fs.writeFileSync(main,result);console.log('정규 씬 환경 블록 병합 적용');}
