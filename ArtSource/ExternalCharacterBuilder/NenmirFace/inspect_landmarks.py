"""얼굴 구성요소의 실제 좌표와 연결성을 조사한다."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
exec((ROOT/'sculpt_face.py').read_text(encoding='utf-8-sig').split('settings=json.loads')[0])
for slot,faces in enumerate(triangles):
 links=defaultdict(set)
 for a,b,c in faces:
  links[a].update((b,c));links[b].update((a,c));links[c].update((a,b))
 unseen=set(links)
 while unseen:
  pending=[next(iter(unseen))];component=[]
  while pending:
   i=pending.pop()
   if i not in unseen:continue
   unseen.remove(i);component.append(i);pending.extend(links[i]&unseen)
  if len(component)<8:continue
  p=source[component]
  print(slot,len(component),'range',np.round(p.min(0),5),np.round(p.max(0),5),'center',np.round(p.mean(0),5))
