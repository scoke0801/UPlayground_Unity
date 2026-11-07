import fs from 'node:fs';
import crypto from 'node:crypto';

const scenePath = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const text = fs.readFileSync(scenePath, 'utf8');
const targets = /^(LakeWater|DeepLake|OverflowWater|LakeAbundanceArt|Lake_ViewGrass|R08_shrine_link_root|R07_prepare_shrine_link|R07_cut_shrine_link)$/;
const objects = [];
const transforms = new Map();
const starts = [...text.matchAll(/^--- !u!(\d+) &(\d+)(?: stripped)?\r?$/gm)];
const scalar = (s, key) => s.match(new RegExp(`^  ${key}: (.*)$`, 'm'))?.[1]?.trim();
for (let i = 0; i < starts.length; i++) {
  const h = starts[i];
  const s = text.slice(h.index, starts[i + 1]?.index ?? text.length);
  if (h[1] === '1') {
    const name = scalar(s, 'm_Name');
    if (targets.test(name ?? '')) objects.push({fileId:h[2], name, active:scalar(s,'m_IsActive'), transformId:s.match(/component: \{fileID: (\d+)\}/)?.[1]});
  }
  if (h[1] === '4') transforms.set(h[2], {fileId:h[2], gameObject:scalar(s,'m_GameObject'), position:scalar(s,'m_LocalPosition'), rotation:scalar(s,'m_LocalRotation'), scale:scalar(s,'m_LocalScale'), parent:scalar(s,'m_Father')});
}
for (const o of objects) {
  o.transform = transforms.get(o.transformId);
  const chain = [];
  let p = o.transform?.parent?.match(/fileID: (\d+)/)?.[1];
  while (p && p !== '0' && chain.length < 20) {
    const t = transforms.get(p); if (!t) break;
    chain.push(t); p=t.parent?.match(/fileID: (\d+)/)?.[1];
  }
  o.parentChain = chain;
  o.components = [];
  for (let i = 0; i < starts.length; i++) {
    const h=starts[i];
    if (h[1] === '1' || h[1] === '4') continue;
    const s=text.slice(h.index,starts[i+1]?.index ?? text.length);
    if (scalar(s,'m_GameObject') === `{fileID: ${o.fileId}}`) o.components.push({typeId:h[1],fileId:h[2],yaml:s});
  }
}
const output='output/WorldThemeProduction/LakeShrine';
fs.mkdirSync(output,{recursive:true});
const waterRendererId = objects.find(o=>o.name==='LakeWater')?.components.find(c=>c.typeId==='23')?.fileId;
const waterRendererReferences=[];
for(let i=0;i<starts.length;i++) {
  const s=text.slice(starts[i].index,starts[i+1]?.index ?? text.length);
  if (s.includes(`{fileID: ${waterRendererId}}`) && starts[i][1]==='114' && !s.includes('FootstepWaterSurface')) waterRendererReferences.push(s);
}
const result={status:'디스크 정적 조사 — Editor 위치/렌더/접지 검증 아님',scenePath,sceneHash:crypto.createHash('sha256').update(text).digest('hex'),objects,waterRendererReferences};
fs.writeFileSync(`${output}/disk-water-survey.json`,JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
