const fs=require('fs'),assert=require('assert/strict'),crypto=require('crypto');
const root='Logs/StartupProfiling/StaticBatchCanonicalR1/';
const u32=x=>BigInt.asUintN(32,BigInt(x));
const key=(guid,id)=>guid.toLowerCase()+':'+id;
const flagMod=/^    - target: \{fileID: (-?\d+), guid: ([a-f\d]{32}), type: 3\}\n      propertyPath: m_StaticEditorFlags\n      value: (-?\d+)\n      objectReference: \{fileID: 0\}\n/gim;
function strip(text,allowed,phase,seen){return text.replace(flagMod,(whole,id,guid,value)=>{const k=key(guid,id),entry=allowed.get(k);if(!entry)return whole;if(seen.has(k))throw Error('중복 플래그 override '+k);seen.add(k);assert.equal(u32(value),u32(entry[phase]),'허용하지 않은 플래그 값');return '';});}
// 64비트 참조 보존·한 비트 변경·다른 속성 보존을 작은 입력으로 먼저 검사한다.
const testGuid='0123456789abcdef0123456789abcdef',testId='9007199254740993';
const sample='    - target: {fileID: '+testId+', guid: '+testGuid+', type: 3}\n      propertyPath: m_StaticEditorFlags\n      value: 4294967291\n      objectReference: {fileID: 0}\n';
assert.equal(strip(sample,new Map([[key(testGuid,testId),{after:4294967291}]]),'after',new Set()),'');
assert.equal(strip(sample,new Map(),'after',new Set()),sample);
assert.throws(()=>strip(sample,new Map([[key(testGuid,testId),{after:0}]]),'after',new Set()));
if(process.argv[2]==='--self-test'){console.log('플래그 검사 자체 검증 3건 통과');process.exit(0);}
const plan=JSON.parse(fs.readFileSync(root+'staging-manifest.json'));
function documents(file){const raw=fs.readFileSync(file),s=raw.toString('utf8'),matches=[...s.matchAll(/^--- !u!(\d+) &(-?\d+)([^\r\n]*)\r?\n/gm)],map=new Map();for(let i=0;i<matches.length;i++){const m=matches[i],k=m[1]+':'+m[2];if(map.has(k))throw Error('중복 문서 '+k);map.set(k,{type:m[1],id:m[2],text:s.slice(m.index,i+1<matches.length?matches[i+1].index:s.length)});}return{map,hash:crypto.createHash('sha256').update(raw).digest('hex').toUpperCase(),preamble:s.slice(0,matches[0].index)};}
// 적용 후에는 ZIP에서 별도 경로로 꺼낸 원문과 정규 씬을 인수로 전달한다.
const a=documents(process.argv[2]||plan.sourcePath),b=documents(process.argv[3]||plan.stagingPath);
assert.equal(a.hash,plan.sourceHash,'정규 입력이 달라졌습니다.');assert.equal(b.hash,plan.stagingHash,'사본 입력이 달라졌습니다.');
assert.equal(a.preamble,b.preamble);assert.equal(a.map.size,b.map.size);
for(const k of a.map.keys())assert.ok(b.map.has(k),'문서 ID 제거/교체 '+k);
const direct=new Map(),prefabs=new Map(),covered=new Set();
for(const t of plan.targets){assert.equal(u32(t.after),u32(u32(t.before)&~4n));assert.notEqual(u32(t.before),u32(t.after));if(!t.targetGuid){assert.ok(!direct.has(t.sceneId));direct.set(t.sceneId,t);continue;}
 const candidates=[...new Set([t.instanceId,t.handleId].filter(x=>x&&a.map.has('1001:'+x)))];assert.equal(candidates.length,1,'PrefabInstance 식별이 모호합니다: '+t.globalId);
 const instance=candidates[0];if(!prefabs.has(instance))prefabs.set(instance,new Map());const map=prefabs.get(instance),k=key(t.targetGuid,t.targetId);assert.ok(!map.has(k),'대상 Prefab 참조 중복');map.set(k,t);
}
let directChanges=0,prefabChanges=0,unchanged=0;
for(const[k,x]of a.map){const y=b.map.get(k);if(x.text===y.text){unchanged++;continue;}
 if(x.type==='1'){
  const t=direct.get(x.id);assert.ok(t,'계획 밖 GameObject 변경 '+k);
  const rx=/^  m_StaticEditorFlags: (-?\d+)$/m,old=x.text.match(rx),next=y.text.match(rx);assert.ok(old&&next,'플래그 필드 누락');
  assert.equal(u32(old[1]),u32(t.before));assert.equal(u32(next[1]),u32(t.after));
  assert.equal(x.text.replace(rx,'  m_StaticEditorFlags: 허용 값'),y.text.replace(rx,'  m_StaticEditorFlags: 허용 값'),'다른 GameObject 필드 변경 '+k);
  covered.add(t.globalId);directChanges++;continue;
 }
 assert.equal(x.type,'1001','허용 밖 문서 변경 '+k);const allowed=prefabs.get(x.id);assert.ok(allowed,'계획 밖 PrefabInstance 변경 '+k);
 const before=new Set(),after=new Set();const left=strip(x.text,allowed,'before',before),right=strip(y.text,allowed,'after',after);
 assert.equal(left,right,'플래그 외 Prefab 데이터 변경 '+k);
 for(const q of new Set([...before,...after]))covered.add(allowed.get(q).globalId);
 prefabChanges++;
}
assert.equal(covered.size,plan.targets.length,'일부 대상의 직렬화 변경을 확인하지 못했습니다.');
const result={passed:true,documents:a.map.size,unchanged,directChanges,prefabChanges,targets:plan.targets.length,covered:covered.size,sourceHash:a.hash,stagingHash:b.hash,allowed:'대상 GameObject의 BatchingStatic 1비트 및 해당 Prefab override만 변경',selfTests:3};
fs.writeFileSync(root+'scene-validation.json',JSON.stringify(result,null,2));console.log(result);

