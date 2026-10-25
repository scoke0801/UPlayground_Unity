const fs = require('fs');
const crypto = require('crypto');
const folder = 'Assets/10.Datas/Actor/ScenarioResidents/ToonLook';
const profile = fs.readFileSync(`${folder}/P09_Residents_ToonLook.asset`, 'utf8');
const replacements = [...profile.matchAll(/source: \{fileID: \d+, guid: (\w+), type: \d+\}\s+replacement: \{fileID: \d+, guid: (\w+), type: \d+\}/g)].map(m=>[m[2],m[1]]);
function canonical(text) {
  // Variant의 신규 재질 override는 기반 프리팹의 슬롯을 가리킨다. 슬롯 수는 Unity 검사로 별도 확인한다.
  text = text.replace(/    - target: [^\n]*\r?\n      propertyPath: '?m_Materials\.Array\.data\[\d+\]'?\r?\n      value: [^\n]*\r?\n      objectReference: [^\n]*\r?\n/g, '');
  for (const [replacement,source] of replacements) text = text.replaceAll(replacement,source);
  return text.replaceAll('\r\n','\n');
}
const results = [];
for (const p of fs.readFileSync('output/P09ToonLook/used-npcs.txt','utf8').trim().split(/\r?\n/)) {
  const before = fs.readFileSync(`output/P09ToonLook/Backup/${p}`,'utf8');
  const after = fs.readFileSync(p,'utf8');
  const preserved = canonical(before) === canonical(after);
  let mismatch;
  if (!preserved) {
    const a=canonical(before), b=canonical(after);
    let i=0; while(i<a.length && a[i]===b[i]) i++;
    mismatch={before:a.slice(Math.max(0,i-70),i+160), after:b.slice(Math.max(0,i-70),i+160)};
  }
  results.push({path:p, preserved, rawChanged:before!==after, mismatch});
}
const src='Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Textures/Female_Body/P09_Female_Body_Diff.png';
const dst=`${folder}/Textures/P09_Female_Body_Diff_4K.png`;
const hash = p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const report = {prefabs:results,textureSourceIdentical:hash(src)===hash(dst),textureSHA256:hash(src)};
fs.writeFileSync('output/P09ToonLook/preservation.json', JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));
if(results.some(x=>!x.preserved)||!report.textureSourceIdentical) process.exitCode=1;
