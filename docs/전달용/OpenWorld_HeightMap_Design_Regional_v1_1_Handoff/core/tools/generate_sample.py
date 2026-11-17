from pathlib import Path
import numpy as np, json, hashlib, shutil
from scipy.ndimage import gaussian_filter, zoom
from PIL import Image
# Offline sample authoring utility. Not a Unity runtime dependency.
# The AI reference image is copied only when it is available in this handoff package.
import argparse
parser=argparse.ArgumentParser(description="Regenerate the numerical WorldBuilder sample.")
parser.add_argument('--output', type=Path, default=Path(__file__).resolve().parents[1]/'samples_regenerated')
parser.add_argument('--overwrite', action='store_true', help='Allow overwriting existing generated sample files.')
args=parser.parse_args()
P=args.output.resolve()
if P.exists() and any(P.iterdir()) and not args.overwrite:
    parser.error('Output directory is not empty. Choose another --output or explicitly use --overwrite.')
P.mkdir(parents=True,exist_ok=True)
(P/'tiles').mkdir(exist_ok=True)
N=2049; size=2048.0; y0=-80.; yrange=480.; seed=20261116
rng=np.random.default_rng(seed)
a=np.linspace(0,1,N,dtype=np.float32); x,z=np.meshgrid(a,a)

def noise(g):
    v=rng.normal(0,1,(g,g)).astype(np.float32)
    v=zoom(v,(N/g,N/g),order=3,prefilter=True)[:N,:N]
    return (v-v.mean())/(v.std()+1e-6)

def bump(cx,cz,sx,sz): return np.exp(-0.5*(((x-cx)/sx)**2+((z-cz)/sz)**2))
low=noise(7); mid=noise(21); fine=noise(57)
# Analytic macro landforms: broad playable basin, northern and eastern ridges, SW coast.
h=28+9*low+3*mid
north=np.exp(-((z-(.80+.035*np.sin(x*11)))/.115)**2)
east=np.exp(-((x-(.86+.035*np.sin(z*9)))/.095)**2)
mount=np.maximum(north,east)*np.clip(.65+.24*mid+.055*fine,.1,1.5)
h+=195*mount + 60*bump(.47,.84,.12,.075)+65*bump(.9,.48,.055,.14)
h+=30*bump(.22,.56,.10,.15)+24*bump(.56,.26,.12,.09)
# Coastal falloff keeps open terrain in the center and a low corner to the SW.
coast=(x*.65+z*.9)
w=np.clip((.37-coast)/.22,0,1); w=w*w*(3-2*w)
h=h*(1-w)+(-40+4*low)*w
# Broad traversable loops, not a fully walkable claim.
for cx,cz,sx,sz,target,weight in [(.38,.40,.15,.17,26,.65),(.39,.66,.14,.09,43,.60),(.66,.32,.12,.12,41,.60)]:
    k=bump(cx,cz,sx,sz)*weight; h=h*(1-k)+target*k
# Designer-guided river course; levels are separately saved, never inferred from dark pixels.
river=np.array([[.72,.79,119,12],[.71,.73,100,14],[.69,.65,78,16],[.66,.59,75,18],[.62,.54,68,18],[.58,.50,59,18],[.54,.46,49,19],[.535,.445,48,19],[.53,.435,26,19],[.49,.40,22,20],[.45,.36,18,20],[.40,.31,13,20],[.34,.26,8,22],[.29,.20,4,22],[.23,.15,1,26],[.18,.10,0,28]],dtype=float)
# Nearest point to each centerline segment and analytically carved riverbed.
nearest=np.full((N,N),1e9,np.float32); bed=np.zeros_like(h); width=np.zeros_like(h)
for p,q in zip(river[:-1],river[1:]):
    dx,dz=q[0]-p[0],q[1]-p[1]
    t=np.clip(((x-p[0])*dx+(z-p[1])*dz)/(dx*dx+dz*dz),0,1)
    dist=np.sqrt((x-(p[0]+t*dx))**2+(z-(p[1]+t*dz))**2)*size
    sel=dist<nearest
    nearest[sel]=dist[sel]
    bed[sel]=(p[2]+t*(q[2]-p[2])-3)[sel]
    width[sel]=((p[3]+t*(q[3]-p[3]))*.5)[sel]
blend=np.clip(1-(nearest-width)/22,0,1); blend=blend*blend*(3-2*blend)
# Ensure river valley at planned levels, embedding bank formation as a sample operation.
valley=bed+3.2*(np.maximum(nearest-width,0)/22)**2
h=h*(1-blend)+valley*blend
# Lake bottom (surface is 78m); basin blends to surrounding ground. Not a water render.
lake=((x-.69)/.055)**2+((z-.65)/.045)**2
lk=np.clip((1.75-lake)/.75,0,1); lk=lk*lk*(3-2*lk)
lb=68+7*np.clip(lake,0,1)
h=h*(1-lk)+lb*lk
# Two example reserved POI plots, flattened for sample only.
for cx,cz,r,target in [(.38,.42,.027,28),(.41,.67,.027,46)]:
    d=np.sqrt((x-cx)**2+(z-cz)**2)
    k=np.clip((r+.018-d)/.018,0,1); k=k*k*(3-2*k)
    h=h*(1-k)+target*k
h=gaussian_filter(h,0.6).astype(np.float32)
h=np.clip(h,y0,y0+yrange)
u=(h-y0)/yrange
raw=np.rint(u*65535).astype('<u2')
raw.tofile(P/'openworld_2049_r16le.raw')
# Display and PNG coordinates: top-left is NW; raw row0 is SW (Unity z=0).
Image.fromarray(raw[::-1]).save(P/'openworld_2049_gray16.png')
prev=Image.fromarray(np.rint(u[::-1]*255).astype('uint8'),'L')
prev.resize((1024,1024),Image.Resampling.LANCZOS).save(P/'openworld_height_preview.png')
concept=Path(__file__).resolve().parents[1]/'samples'/'heightmap_concept_ai.png'
if concept.exists() and concept.resolve() != (P/'heightmap_concept_ai.png').resolve():
    shutil.copy(concept, P/'heightmap_concept_ai.png')
# Derive an illustrative shaded view; this file must NEVER be used as height data.
gz,gx=np.gradient(h,1.,1.); normal=np.dstack((-gx,np.ones_like(h),-gz)); normal/=np.linalg.norm(normal,axis=2)[...,None]
light=np.array([-.45,.82,-.35]);light/=np.linalg.norm(light)
shade=np.clip((normal*light).sum(axis=2),0,1)*.60+.40
# Height-based colors for an explicitly illustrative landscape preview (not a heightmap).
knots=np.array([-80,0,6,30,90,180,300,400]); palette=np.array([[27,54,63],[46,84,91],[145,157,110],[125,149,112],[138,154,123],[143,141,128],[190,187,176],[231,231,224]],float)
rgb=np.stack([np.interp(h,knots,palette[:,i]) for i in range(3)],axis=2)*shade[...,None]
Image.fromarray(np.uint8(np.clip(rgb[::-1],0,255)),'RGB').resize((1024,1024),Image.Resampling.LANCZOS).save(P/'openworld_relief_preview.jpg',quality=92)
files=[]
for tz in range(4):
    for tx in range(4):
        arr=raw[tz*512:tz*512+513,tx*512:tx*512+513].copy()
        fname=f'tiles/tile_{tx:02d}_{tz:02d}_513_r16le.raw'; arr.tofile(P/fname)
        files.append(dict(tile=[tx,tz],file=fname,originMeters=[tx*512,y0,tz*512],sha256=hashlib.sha256((P/fname).read_bytes()).hexdigest()))
# Four-corner diagnostic fixture to detect flips/transposes with the importer.
R=33; qx,qz=np.meshgrid(np.linspace(0,1,R),np.linspace(0,1,R)); diag=.1+.2*qx+.5*qz
np.rint(diag*65535).astype('<u2').tofile(P/'orientation_test_33_r16le.raw')
manifest={
 'schemaVersion':1,'sampleId':'openworld-heightmap-20261116','seed':seed,
 'purpose':'Technical sample: analytic landforms and authored water/plot stamps. Not a validated game level or hydrology simulation.',
 'heightFile':'openworld_2049_r16le.raw','encoding':'R16_UNORM','byteOrder':'little-endian','width':N,'height':N,'layout':'row-major','row0':'south (z=0)','xDirection':'west to east (+X)','zDirection':'south to north (+Z)',
 'worldOriginMeters':[0,y0,0],'worldSizeMeters':[size,yrange,size],'sampleSpacingMeters':1.0,'seaLevelMeters':0.,
 'pngFile':'openworld_2049_gray16.png','pngRow0':'north; vertically flip on decode to canonical south-first grid',
 'heightEquation':'worldY = -80 + (ushort/65535.0)*480','tileCells':512,'tileSamples':513,'tileCount':[4,4],
 'quantizationStepMeters':yrange/65535.,'statistics':{'minWorldY':float(h.min()),'maxWorldY':float(h.max()),'uniqueUInt16':int(np.unique(raw).size)},
 'sha256':hashlib.sha256((P/'openworld_2049_r16le.raw').read_bytes()).hexdigest(),
 'tiles':files,
 'waterGuides':{'units':'meters; x,z positions in world space; surfaceY is absolute worldY','riverPoints':[{'x':round(float(p[0]*size),4),'z':round(float(p[1]*size),4),'surfaceY':float(p[2]),'width':float(p[3])} for p in river],
 'lake':{'centerXZ':[.69*size,.65*size],'radiiXZ':[.055*size,.045*size],'surfaceY':78.0},
 'waterfallHint':{'upstreamXZ':[.535*size,.445*size],'upstreamY':48.,'downstreamXZ':[.53*size,.435*size],'downstreamY':26.}},
 'poiHints':[{'id':'settlement-start','xz':[.38*size,.42*size],'padY':28.,'type':'settlement'},{'id':'landmark-north','xz':[.41*size,.67*size],'padY':46.,'type':'landmark'}],
 'orientationFixture':{'file':'orientation_test_33_r16le.raw','resolution':33,'normalizedCorners':{'SW':.1,'SE':.3,'NW':.6,'NE':.8}},
 'notIncluded':['water meshes','cave meshes','prefabs','navmesh','validated traversal','hydraulic erosion','Unity import verification']
}
(P/'sample_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
# Structural validation of distributed numerical assets, not Unity runtime tests.
checks=[]
checks.append(('raw_byte_count',(P/'openworld_2049_r16le.raw').stat().st_size==N*N*2))
checks.append(('png_uint16_roundtrip',np.array_equal(np.array(Image.open(P/'openworld_2049_gray16.png'))[::-1],raw)))
checks.append(('height_range',bool(np.all((u>=0)&(u<=1)))))
checks.append(('raw_reconstruction_error',float(np.max(np.abs((raw.astype(float)/65535)*yrange+y0-h))) <= yrange/65535*.51))
for tz in range(4):
 for tx in range(4):
  c=np.fromfile(P/f'tiles/tile_{tx:02d}_{tz:02d}_513_r16le.raw',dtype='<u2').reshape(513,513)
  if tx<3:
   r=np.fromfile(P/f'tiles/tile_{tx+1:02d}_{tz:02d}_513_r16le.raw',dtype='<u2').reshape(513,513)
   checks.append((f'seam_E_{tx}_{tz}',np.array_equal(c[:,-1],r[:,0])))
  if tz<3:
   t=np.fromfile(P/f'tiles/tile_{tx:02d}_{tz+1:02d}_513_r16le.raw',dtype='<u2').reshape(513,513)
   checks.append((f'seam_N_{tx}_{tz}',np.array_equal(c[-1,:],t[0,:])))
(P/'validation_report.json').write_text(json.dumps({'checks':[{'name':n,'pass':bool(v)} for n,v in checks],'scope':'File-level numerical checks only. Unity compilation/import/PlayMode/PlayerBuild NOT run.','allPass':all(v for _,v in checks)},indent=2),encoding='utf-8')
print(json.dumps(manifest['statistics'],indent=2));print('checks:',len(checks),'allPass:',all(v for _,v in checks))
