const fs=require('fs'),path=require('path'),crypto=require('crypto');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const files=new Set(read('output/CloudridgeR1/survey.json').map(o=>o.prefab).filter(Boolean));
function walk(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=(dir+'/'+e.name).replaceAll('\\','/');if(e.isDirectory())walk(p);else if(/\.(cs|asmdef)$/.test(p))files.add(p);}}
walk('Assets/02.Scripts');for(const p of ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/QualitySettings.asset','ProjectSettings/GraphicsSettings.asset','Assets/Settings/LakeVillage_RPAsset.asset','Assets/Settings/PC_RPAsset.asset','Assets/10.Datas/Camera/CameraOccluderNameRules.asset'])if(fs.existsSync(p))files.add(p);
const rows=[...files].sort().map(p=>({path:p,sha256:crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex'),meta:fs.existsSync(p+'.meta')?crypto.createHash('sha256').update(fs.readFileSync(p+'.meta')).digest('hex'):null}));
fs.writeFileSync('output/CloudridgeR1/shared-resources-before.json',JSON.stringify({observedAt:new Date().toISOString(),scope:'B 실제 제작 직전 코드·지역 프리팹·공용 설정 상태. 공통 기준선 자체는 baseline.json을 참조',rows},null,2));console.log(JSON.stringify({files:rows.length}));
