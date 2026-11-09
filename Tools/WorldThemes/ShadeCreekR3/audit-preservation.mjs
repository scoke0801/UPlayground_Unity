import fs from 'node:fs';import crypto from 'node:crypto';
const load=p=>JSON.parse(fs.readFileSync(`output/ShadeCreekR3/${p}.json`,'utf8').replace(/^\uFEFF/,''));
const baseline=load('baseline');const rows=baseline.rows.map(r=>{const current=fs.existsSync(r.path)?crypto.createHash('sha256').update(fs.readFileSync(r.path)).digest('hex'):null;return {path:r.path,expected:r.sha256,current,unchanged:current===r.sha256};});
const refs=load('references-final');const key=r=>JSON.stringify(r);
const old=new Set(refs.canonical.broken.map(key));const newMissing=refs.r3.broken.filter(r=>!old.has(key(r)));
const result={checkedAt:new Date().toISOString(),filesChecked:rows.length,changed:rows.filter(r=>!r.unchanged),rows,referenceComparison:{newMissing,canonicalBroken:refs.canonical.broken.length,r3Broken:refs.r3.broken.length,canonicalEmptyMaterials:refs.canonical.emptyMaterials,r3EmptyMaterials:refs.r3.emptyMaterials},scope:'開始時ディスク基準との比較。差分は自動的に環境作業の破損と判定せず確認する。'};
result.scope='작업 시작 디스크 기준과 비교. 차이는 자동으로 환경 작업의 훼손으로 판정하지 않고 기존·병행 작업 여부를 확인한다.';
fs.writeFileSync('output/ShadeCreekR3/preservation.json',JSON.stringify(result,null,2));console.log({files:rows.length,changed:result.changed.map(r=>r.path),newMissing,baselineMissing:refs.canonical.broken.length,empty:refs.r3.emptyMaterials});
