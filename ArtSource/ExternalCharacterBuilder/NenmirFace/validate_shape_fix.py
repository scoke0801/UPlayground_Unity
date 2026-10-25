"""볼 축소·이목구비 재배치·목 접합면의 형상 회귀를 검사한다."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
exec((ROOT/'sculpt_face.py').read_text(encoding='utf-8-sig').split("with (ROOT/'repair_topology.bin')")[0])
previous=ROOT.parents[2]/'output/NenmirP09ShapeFix/backup_20261025_111310/ArtSource/ExternalCharacterBuilder/NenmirFace/repair_sculpt.bin'
with previous.open('rb') as f:
 n,sh=struct.unpack('<ii',f.read(8));before=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy()
assert before.shape==shaped.shape
upper=source[:,2]>=1.475
print('upper_change',np.max(np.linalg.norm(shaped[upper]-before[upper],axis=1)))
outer_faces=np.array([t for t in triangles[0] if int(welded[t[0]]) in skin_nodes])
def normals(points):
 t=points[outer_faces];n=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);n/=np.maximum(np.linalg.norm(n,axis=1)[:,None],1e-12);return n
old=normals(before);new=normals(shaped);original=normals(source)
print('flips source',np.sum((old*original).sum(1)<0),np.sum((new*original).sum(1)<0))
faces_neck=(source[outer_faces,2].max(1)<1.385)
edge_faces=defaultdict(list)
for k,tri in enumerate(outer_faces):
 if not faces_neck[k]:continue
 for a,b in zip(tri,np.roll(tri,-1)):edge_faces[tuple(sorted((int(welded[a]),int(welded[b]))))].append(k)
def angle_stats(normals):
 vals=[np.rad2deg(np.arccos(np.clip(np.dot(normals[a],normals[b]),-1,1))) for ids in edge_faces.values() if len(ids)==2 for a,b in [ids]]
 return [float(np.max(vals)),float(np.percentile(vals,95)),int(np.sum(np.array(vals)>60))]
print('neck_dihedral',angle_stats(old),angle_stats(new))
for z in [1.355,1.365,1.375,1.385]:
 ids=np.array([i for i in skin_indices if abs(source[i,2]-z)<.005 and source[i,1]<-.012])
 print('width',z,float(np.ptp(before[ids,0])),float(np.ptp(shaped[ids,0])))
assert np.max(np.linalg.norm(shaped[upper]-before[upper],axis=1))<1e-7, '정수리 형상이 바뀌었습니다.'
assert np.all((new*original).sum(1)>=0), '바깥 피부의 삼각형 뒤집힘'
assert angle_stats(new)[1]<angle_stats(old)[1], '목 영역의 급격한 각도가 줄지 않았습니다.'
widths=[]
for z in [1.355,1.365,1.375,1.385]:
 ids=np.array([i for i in skin_indices if abs(source[i,2]-z)<.005 and source[i,1]<-.012])
 old_width=float(np.ptp(before[ids,0]));new_width=float(np.ptp(shaped[ids,0]))
 assert new_width<old_width*.93
 widths.append({'height_m':z,'before_m':old_width,'after_m':new_width,'reduction_percent':100*(1-new_width/old_width)})
report={'upper_face_max_change_m':float(np.max(np.linalg.norm(shaped[upper]-before[upper],axis=1))), 'flipped_outer_triangles':int(np.sum((new*original).sum(1)<0)), 'neck_dihedral_before':angle_stats(old),'neck_dihedral_after':angle_stats(new),'widths':widths}
revision2=ROOT.parents[2]/'output/NenmirP09ProportionFix/backup_20261025_112242/ArtSource/ExternalCharacterBuilder/NenmirFace/repair_sculpt.bin'
with revision2.open('rb') as f:
 n,sh=struct.unpack('<ii',f.read(8));prior=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy()
eye_region=(source[:,2]>1.406)&(source[:,2]<1.425)&(np.abs(source[:,0])>.025)&(np.abs(source[:,0])<.05)&(source[:,1]<-.035)
assert eye_region.any()
eye_inset=np.abs(prior[eye_region,0])-np.abs(shaped[eye_region,0])
eye_down=prior[eye_region,2]-shaped[eye_region,2]
assert eye_inset.min()>.002 and eye_down.min()>.0005
report['eye_region_inset_mean_m']=float(eye_inset.mean())
report['eye_region_down_mean_m']=float(eye_down.mean())
revision3=ROOT.parents[2]/'output/NenmirP09ArtReview/backup_r03/ArtSource/ExternalCharacterBuilder/NenmirFace/repair_sculpt.bin'
with revision3.open('rb') as f:
 n,sh=struct.unpack('<ii',f.read(8));prior=np.frombuffer(f.read(n*12),'<f4').reshape(n,3).copy()
# 원본의 분리된 눈썹·안구·입술 메시를 기준으로 이전 버전과 비율을 비교한다.
links=defaultdict(set)
for a,b,c in triangles[0]:
 links[a].update((b,c));links[b].update((a,c));links[c].update((a,b))
unseen=set(links);parts=[]
while unseen:
 pending=[next(iter(unseen))];part=[]
 while pending:
  i=pending.pop()
  if i not in unseen:continue
  unseen.remove(i);part.append(i);pending.extend(links[i]&unseen)
 parts.append(np.array(part))
brows=np.concatenate([p for p in parts if len(p)==18 and source[p,2].min()>1.439])
eyes=np.concatenate([p for p in parts if len(p)==49 and source[p,2].min()>1.390 and source[p,1].min()<-.05])
lips=next(p for p in parts if len(p)==64 and source[p,2].max()<1.364)
skin=next(p for p in parts if len(p)==980)
chin=skin[(np.abs(source[skin,0])<.012)&(source[skin,1]<-.035)&(source[skin,2]<1.345)]
assert len(brows)==36 and len(eyes)==98 and len(chin)>0
def proportions(p):
 return {'brow_eye_gap_m':float(p[brows,2].mean()-p[eyes,2].mean()),'lip_chin_gap_m':float(p[lips,2].mean()-p[chin,2].min())}
before_proportions=proportions(prior);after_proportions=proportions(shaped)
assert after_proportions['brow_eye_gap_m']<before_proportions['brow_eye_gap_m']-.003
assert after_proportions['lip_chin_gap_m']<before_proportions['lip_chin_gap_m']-.004
report['r03_landmarks']=before_proportions;report['r04_landmarks']=after_proportions
print('proportions',before_proportions,after_proportions)
(ROOT/'shape_fix_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

