import fs from 'node:fs';import crypto from 'node:crypto';
const root='output/ShadeCreekR3',hash=t=>crypto.createHash('sha256').update(t).digest('hex');
const baseline=JSON.parse(fs.readFileSync(`${root}/baseline.json`));const target=baseline.rows.find(r=>r.path==='ProjectSettings/QualitySettings.asset').sha256;
const original=fs.readFileSync('ProjectSettings/QualitySettings.asset','utf8');const matches=[];
// 현재 코드 SettingsApplier.ApplyQualityPreset의 값만 재현한다. 실제 설정 파일은 쓰지 않는다.
for(let preset=0;preset<4;preset++)for(let vsync=0;vsync<5;vsync++){
const values={globalTextureMipmapLimit:preset===0?1:0,anisotropicTextures:Math.min(preset,2),softParticles:preset>=1?1:0,realtimeReflectionProbes:preset>=2?1:0,shadowDistance:[20,40,60,100][preset],shadows:preset===0?1:2,shadowResolution:preset,antiAliasing:[0,2,4,8][preset],vSyncCount:vsync};
let candidate=original;for(const [field,value] of Object.entries(values))candidate=candidate.replace(new RegExp(`(${field}: )[^\\r\\n]+`),`$1${value}`);
for(const text of [candidate,candidate.replace(/\r?\n/g,'\r\n')])if(hash(text)===target)matches.push({preset,vsync,values,sha256:hash(text)});
}
const report={target,current:hash(original),matches,method:'현재 SettingsApplier 런타임 품질 프리셋 및 VSync 적용 결과와 작업 시작 파일 SHA256 정확 비교. 파일 변경 없음.',editorLogEvidence:{playExitExceptionLine:3793,changedQualityArtifactLine:3859,firstR3ImportLine:3863,initialQualityArtifact:'50a4e367f6dcef570643d1279ee4c2c6',postPlayQualityArtifact:'5029a9954441602322ab7381e256c30d'}};
fs.writeFileSync(`${root}/quality-play-hypothesis.json`,JSON.stringify(report,null,2));console.log(report);
