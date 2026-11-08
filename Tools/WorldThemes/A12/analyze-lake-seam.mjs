import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// 5m 표본은 후보 분석용이다. 실제 alpha 셀의 수면/수심/길 검사는 A00 Editor 단계에서 수행한다.
const out = 'output/WorldThemeProduction/A12';
const ownPath = 'Tools/WorldThemes/Integration/ownership.json';
const planPath = 'Tools/WorldThemes/A02/lake-study.json';
const csvPath = 'output/WorldThemeProduction/LakeShrine/live-water-depth.csv';
const own = JSON.parse(fs.readFileSync(ownPath,'utf8'));
const plan = JSON.parse(fs.readFileSync(planPath,'utf8'));
const live = JSON.parse(fs.readFileSync('output/WorldThemeProduction/A00/live-baseline.json','utf8'));
const p = own.themes.find(t=>t.themeId==='LakeShrine').corePolygon;
const painted = JSON.parse(fs.readFileSync('output/WorldThemeProduction/LakeShrine/painted-cells.json','utf8'));
const rows = fs.readFileSync(csvPath,'utf8').trim().split(/\r?\n/).slice(1).map(line=>{
  const [x,z,waterY,terrainY,depth,insideWaterMesh]=line.split(',');
  return {x:+x,z:+z,waterY:+waterY,terrainY:+terrainY,depth:+depth,insideWaterMesh:insideWaterMesh==='true'};
});
const outer = 15, inner = 4, radius = plan.protectedRadius;
const anchors = [...live.ids.map(r=>[r.position[0],r.position[2]]),...plan.protectedAnchors.map(r=>[r[0],r[2]])];
function inside(x,z) {
  let c=false;
  for(let i=0,j=p.length-1;i<p.length;j=i++)
    if(((p[i][1]>z)!==(p[j][1]>z))&&(x<(p[j][0]-p[i][0])*(z-p[i][1])/(p[j][1]-p[i][1])+p[i][0]))c=!c;
  return c;
}
function edgeDistance(x,z) {
  let d=Infinity,index=-1;
  for(let i=0;i<p.length;i++) {
    const a=p[i],b=p[(i+1)%p.length],dx=b[0]-a[0],dz=b[1]-a[1];
    const t=Math.max(0,Math.min(1,((x-a[0])*dx+(z-a[1])*dz)/(dx*dx+dz*dz)));
    const q=Math.hypot(x-a[0]-dx*t,z-a[1]-dz*t);
    if(q<d){d=q;index=i;}
  }
  return {distance:d,edge:index};
}
const smooth=t=>{t=Math.max(0,Math.min(1,t));return t*t*(3-2*t);};
const candidateRows=[];
for(const r of rows) {
  const e=edgeDistance(r.x,r.z),inCore=inside(r.x,r.z);
  if((inCore&&e.distance>inner)||(!inCore&&e.distance>outer))continue;
  const protectedDistance=Math.min(...anchors.map(a=>Math.hypot(a[0]-r.x,a[1]-r.z)));
  const signed=inCore?e.distance:-e.distance;
  candidateRows.push({...r,insideCore:inCore,signedCoreDistance:signed,nearestEdge:e.edge,
    eastPriority:e.edge>=2&&e.edge<=4,protectedDistance,protectedByExistingRadius:protectedDistance<radius,
    proposedShapeBlend:smooth((signed+outer)/(outer+inner)),
    existingCoreShapeBlend:inCore?smooth(e.distance/inner):0,
    requiresExactAlphaCellResample:true,roadClearanceStatus:'NOT_VERIFIED'});
}
const wet=candidateRows.filter(r=>r.depth>0&&!r.protectedByExistingRadius);
const summaries={};
for(const [name,a] of Object.entries({all:candidateRows,wet,outerWet:wet.filter(r=>!r.insideCore),innerCollarWet:wet.filter(r=>r.insideCore),eastOuterWet:wet.filter(r=>!r.insideCore&&r.eastPriority)}))
  summaries[name]={count:a.length,boundsXZ:a.length?[Math.min(...a.map(r=>r.x)),Math.min(...a.map(r=>r.z)),Math.max(...a.map(r=>r.x)),Math.max(...a.map(r=>r.z))]:null,minDepth:a.length?Math.min(...a.map(r=>r.depth)):null,maxDepth:a.length?Math.max(...a.map(r=>r.depth)):null};
const nearPaint=painted.filter(r=>edgeDistance(r.worldX,r.worldZ).distance<=inner);
const globalScope={worldCoordinates:true,heightChangeAllowed:false,detailChangeAllowed:false,treeChangeAllowed:false,
  sourceHostIntegrationAllowed:false,existingCoreMask:p,outerSeamMeters:outer,innerCollarMeters:inner,
  innerCollarRequiresA00CoordinatedOwnership:true,protectedRadiusMeters:radius,
  exactWaterTriangleAndDepthRequired:true,protectedFootprintsAndExistingRouteExclusionRequired:true,
  cloneOnly:true,fullOriginalBaselineRemapRequired:true,waterMaterialChangesAllowed:false,
  outputArrayBase:'Copy current A02-final-v2 alpha; overwrite only explicitly authorized collar/seam cells using remapped original Host alpha as Lerp baseline. Preserve all other existing core cells.',
  preservationComparisons:['Versus A02-final-v2: zero alpha differences outside newly authorized collar+seam','Versus original Host: zero alpha differences outside full existing core+new seam'],
  formula:'w = SmoothStep01((signedDistanceToCoreBoundary + 15) / 19); signed distance positive inside core. Multiply by SmoothStep01(depth / 0.35), zero at depth <= 0. Alpha = normalize(lerp(originalSourceAlphaRemappedByGuid, existingRiverBedTarget(depth), w)).',
  seamOnlyWarning:'Painting exterior only leaves the existing 4m fade-to-original grass ring in the core. Recompute the coordinated inner 4m collar together; do not repeatedly lerp the previous clone.'};
const evidenceFiles=[ownPath,planPath,csvPath,'output/WorldThemeProduction/LakeShrine/painted-cells.json'];
const evidence=evidenceFiles.map(path=>({path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex')}));
const result={status:'PROPOSAL_ONLY_NOT_AUTHORIZED_CELL_MASK',createdUtc:new Date().toISOString(),summaries,
  existingPaintCandidates:painted.length,existingPaintInInnerCollar:nearPaint.length,
  minimumProtectedDistanceWet:wet.length?Math.min(...wet.map(r=>r.protectedDistance)):null,
  exactRoadClearance:'NOT_VERIFIED_FROM_CSV',sampling:'Existing CSV only includes water-mesh-interior 5m samples; it is not an alpha-cell authorization mask',
  sourceTerrainOrigin:[0,0,-700],sourceTerrainSize:[2400,400,2800],alphaResolution:1024,
  alphaCellSize:[2400/1024,2800/1024],proposal:globalScope,evidence};
assert(rows.length>0&&painted.length===649);
assert.equal(smooth(0),0);assert.equal(smooth(1),1);
assert(candidateRows.every(r=>Math.abs(r.signedCoreDistance)<=(r.insideCore?inner:outer)+1e-7));
fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(`${out}/lake-seam-analysis.json`,JSON.stringify(result,null,2));
fs.writeFileSync(`${out}/lake-seam-5m-candidates.json`,JSON.stringify(candidateRows,null,2));
console.log(JSON.stringify({summaries,existingPaintInInnerCollar:nearPaint.length,minimumProtectedDistanceWet:result.minimumProtectedDistanceWet}));
