"""Enemy sprites for AntiBacteria in palette indices: '.' clear, 'k' background (opaque), '#' foreground, 'R' accent."""
import math
from PIL import Image

class Spr:
    def __init__(s, w, h=None):
        s.w=w; s.h=h or w; s.p=[['.']*s.w for _ in range(s.h)]
    def put(s,x,y,c):
        x=int(round(x)); y=int(round(y))
        if 0<=x<s.w and 0<=y<s.h: s.p[y][x]=c
    def get(s,x,y): return s.p[y][x] if 0<=x<s.w and 0<=y<s.h else '.'
    def disc(s,cx,cy,r,c,dither=None):
        for y in range(s.h):
            for x in range(s.w):
                if (x-cx)**2+(y-cy)**2<=r*r+0.3:
                    if dither is None or (x+y)%2==dither: s.put(x,y,c)
    def ring(s,cx,cy,r,c,step=1):
        n=max(8,int(2*math.pi*r*3))
        for i in range(n):
            if (i//3)%step: continue
            a=2*math.pi*i/n; s.put(cx+r*math.cos(a), cy+r*math.sin(a), c)
    def line(s,x0,y0,x1,y1,c):
        n=int(max(abs(x1-x0),abs(y1-y0)))+1
        for i in range(n+1):
            t=i/max(1,n); s.put(x0+(x1-x0)*t, y0+(y1-y0)*t, c)
    def poly_fill(s,pts,c):
        for y in range(s.h):
            for x in range(s.w):
                if inside(pts,x+0.0,y+0.0): s.put(x,y,c)
    def outline(s,fill,c):
        """draw c on clear pixels 4-adjacent to any pixel whose value is in fill"""
        out=[]
        for y in range(s.h):
            for x in range(s.w):
                if s.p[y][x]=='.' and any(s.get(x+dx,y+dy) in fill for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))): out.append((x,y))
        for x,y in out: s.p[y][x]=c
    def stamp(s,rows,ox=0,oy=0):
        for y,row in enumerate(rows):
            for x,ch in enumerate(row):
                if ch!='.': s.put(ox+x,oy+y,ch)
    def mirror_check(s): pass
    def png(s,path):
        im=Image.new('RGBA',(s.w,s.h),(0,0,0,0)); px=im.load()
        col={'k':(0,0,0,255),'#':(255,255,255,255),'R':(255,0,0,255)}
        for y in range(s.h):
            for x in range(s.w):
                if s.p[y][x] in col: px[x,y]=col[s.p[y][x]]
        im.save(path)

def inside(pts,x,y):
    c=False; n=len(pts)
    for i in range(n):
        x1,y1=pts[i]; x2,y2=pts[(i+1)%n]
        if (y1>y)!=(y2>y) and x < (x2-x1)*(y-y1)/(y2-y1+1e-9)+x1: c=not c
    return c

S={}
# ---- eyes (red pupils that slide toward the player; drawn on top of a hollow body)
e=Spr(3); e.stamp(['.R.','RRR','.R.']); S['eye_small']=e
e=Spr(5); e.stamp(['.RRR.','RRRRR','RRRRR','RRRRR','.RRR.']); S['eye_big']=e

# ---- 1 Loctopus: round cell + 4 curly tentacles (connected lines), hollow middle for the eye
s=Spr(17); c=8
s.disc(c,c,4.6,'k'); s.ring(c,c,4.6,'#')
for k in range(4):
    a=math.pi/4+k*math.pi/2
    pts=[(c+r*math.cos(a+0.28*math.sin(r*1.3)), c+r*math.sin(a+0.28*math.sin(r*1.3))) for r in (4.8,5.8,6.8,7.8)]
    for (x0,y0),(x1,y1) in zip(pts,pts[1:]): s.line(x0,y0,x1,y1,'#')
S['enemy_loctopus']=s

# ---- 2 RedNoise: jagged red static ring, white rim pixels, eye
import random; random.seed(7)
s=Spr(15); c=7
s.disc(c,c,4.4,'k')
for i in range(36):
    a=2*math.pi*i/36; r=5.2+random.choice([-0.8,0,0,0.8,1.6])
    s.put(c+r*math.cos(a), c+r*math.sin(a), 'R')
for i in range(0,36,3):
    a=2*math.pi*i/36; s.put(c+4.6*math.cos(a), c+4.6*math.sin(a), '#')
S['enemy_rednoise']=s

# ---- 3 SpiralCorosia: cell with a swirl arm, red core
s=Spr(15); c=7
s.disc(c,c,6,'k'); s.ring(c,c,6,'#')
for i in range(40):
    t=i/40*2.2*math.pi; r=1.2+t*0.68
    s.put(c+r*math.cos(t), c+r*math.sin(t), '#')
s.disc(c,c,1.2,'R')
S['enemy_spiral']=s

# ---- 5 BigHunterus: big dithered body, thick rim, hollow centre for the big eye
s=Spr(33); c=16
s.disc(c,c,15.5,'#',dither=0); s.ring(c,c,15.5,'#'); s.ring(c,c,14.7,'#')
s.disc(c,c,5.5,'k'); s.ring(c,c,6,'#')
S['enemy_bighunter']=s

# ---- 7 ShootingCorosia: big dithered blob + 4 red-tipped nozzles
s=Spr(31); c=15
s.disc(c,c,11.5,'#',dither=1); s.ring(c,c,11.5,'#')
for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
    for t in range(11,15):
        for w in (-1,0,1): s.put(c+dx*t+dy*w, c+dy*t+dx*w, '#')
    s.put(c+dx*15, c+dy*15, 'R'); s.put(c+dx*14, c+dy*14, 'R')
s.disc(c,c,3.5,'R'); s.ring(c,c,4.3,'k')
S['enemy_shooting']=s

# ---- 8 Sunux: solid sun core + 8 rays (cardinal long, diagonal short), red centre
s=Spr(31); c=15
s.disc(c,c,7.5,'#'); s.disc(c,c,3.2,'R'); s.ring(c,c,8.6,'k')
for k in range(8):
    a=k*math.pi/4; L=14 if k%2==0 else 11.5
    s.line(c+10*math.cos(a), c+10*math.sin(a), c+L*math.cos(a), c+L*math.sin(a), '#')
S['enemy_sunux']=s

# ---- 9 Adrenalium: sharp diamond with speed notches, red core
s=Spr(15); c=7
pts=[(c,0.2),(c+6.8,c),(c,14.8),(c-6.8,c)]
s.poly_fill(pts,'k')
for (x0,y0),(x1,y1) in zip(pts,pts[1:]+pts[:1]): s.line(x0,y0,x1,y1,'#')
s.disc(c,c,1.5,'R'); s.put(c,c-4,'#'); s.put(c,c+4,'#'); s.put(c-4,c,'#'); s.put(c+4,c,'#')
S['enemy_adrenalium']=s

# ---- 11 Chorux: hexagon shell, red core
s=Spr(15); c=7
pts=[(c+6.5*math.cos(math.pi/6+k*math.pi/3), c+6.5*math.sin(math.pi/6+k*math.pi/3)) for k in range(6)]
s.poly_fill(pts,'k')
for (x0,y0),(x1,y1) in zip(pts,pts[1:]+pts[:1]): s.line(x0,y0,x1,y1,'#')
s.disc(c,c,2.3,'R')
S['enemy_chorux']=s

# ---- 12 Chorux defender: small shield (kite) with a white dot, no red (invulnerable while shielding)
s=Spr(11); c=5
pts=[(c,0.3),(c+4.6,c-1.5),(c+3.2,c+3.2),(c,10.6),(c-3.2,c+3.2),(c-4.6,c-1.5)]
s.poly_fill(pts,'k')
for (x0,y0),(x1,y1) in zip(pts,pts[1:]+pts[:1]): s.line(x0,y0,x1,y1,'#')
s.put(c,c,'#'); s.put(c,c-1,'#'); s.put(c,c+1,'#')
S['enemy_defender']=s
s2=Spr(11); s2.p=[row[:] for row in s.p]; s2.put(c,c,'R'); s2.put(c,c-1,'R'); s2.put(c,c+1,'R'); S['enemy_defender_open']=s2

# ---- 13 Spawner: big dotted ring body with red eggs inside
s=Spr(31); c=15
s.disc(c,c,13,'k'); s.ring(c,c,13,'#'); s.ring(c,c,11.2,'#',step=2)
for (x,y) in ((c,c),(c-5,c-3),(c+5,c-3),(c-3,c+5),(c+4,c+4)):
    s.disc(x,y,1.6,'R')
S['enemy_spawner']=s

# ---- 17 Tentacle: solid white blob with 6 curling arms, NO red (cannot be killed)
s=Spr(17); c=8
s.disc(c,c,3.6,'#')
for k in range(6):
    a=k*math.pi/3
    pts=[(c+r*math.cos(a+0.12*(r-3.5)**1.3), c+r*math.sin(a+0.12*(r-3.5)**1.3)) for r in (3.5,4.7,5.9,7.0,7.8)]
    for (x0,y0),(x1,y1) in zip(pts,pts[1:]): s.line(x0,y0,x1,y1,'#')
s.put(c,c,'k'); s.put(c-1,c,'k'); s.put(c+1,c,'k'); s.put(c,c-1,'k'); s.put(c,c+1,'k')
S['enemy_tentacle']=s

# ---- 18 ZigZagus: saw-tooth rim, hollow for eye
s=Spr(15); c=7
s.disc(c,c,4.5,'k')
for i in range(24):
    a=2*math.pi*i/24; r=5.0 if i%2==0 else 6.6
    a2=2*math.pi*(i+1)/24; r2=6.6 if i%2==0 else 5.0
    s.line(c+r*math.cos(a), c+r*math.sin(a), c+r2*math.cos(a2), c+r2*math.sin(a2), '#')
S['enemy_zigzag']=s

# ---- 20 Gnida: bug - oval body, 6 legs, hollow for eye
s=Spr(15); c=7
for y in range(15):
    for x in range(15):
        if ((x-c)/4.3)**2+((y-c)/5.3)**2<=1: s.put(x,y,'k')
for y in range(15):
    for x in range(15):
        v=((x-c)/4.3)**2+((y-c)/5.3)**2
        if 0.72<v<=1.15: s.put(x,y,'#')
for dy in (-3,0,3):
    for sx in (-1,1):
        s.line(c+sx*5,c+dy,c+sx*7,c+dy+(1 if dy>=0 else -1)*1.5,'#')
s.put(c-2,0,'#'); s.put(c+2,0,'#'); s.put(c-1,1,'#'); s.put(c+1,1,'#')
S['enemy_gnida']=s

# ---- 21 ZamriGnida: gnida with a spiral back pattern and bigger feelers
s=Spr(17); c=8
for y in range(17):
    for x in range(17):
        v=((x-c)/5.0)**2+((y-c)/6.0)**2
        if v<=1: s.put(x,y,'k')
        if 0.74<v<=1.12: s.put(x,y,'#')
for dy in (-4,0,4):
    for sx in (-1,1): s.line(c+sx*5.6,c+dy,c+sx*8,c+dy+(1 if dy>=0 else -1)*2,'#')
for sx in (-1,1): s.line(c+sx*1,c-6,c+sx*3,c-8,'#')
for i in range(12):
    a=i*math.pi/6
    if i%3!=2: s.put(c+3.2*math.cos(a), c+3.8*math.sin(a), '#')  # dotted inner ring = the 'spiral' marking
S['enemy_zamri']=s

# ---- 10 CaterPillar: head (solid, red eyes) and small dotted segment
s=Spr(9); c=4
s.disc(c,c,4,'#'); s.put(c-2,c-1,'R'); s.put(c+2,c-1,'R'); s.put(c,c+2,'k')
S['cat_head']=s
s=Spr(7); c=3
s.disc(c,c,3,'k'); s.ring(c,c,3,'#'); s.put(c,c,'#')
S['cat_segment']=s

# ---- 15 LotusBacterius: flower head (5 petals, red centre) and petal segments
s=Spr(15); c=7
for k in range(5):
    a=-math.pi/2+k*2*math.pi/5
    s.disc(c+3.8*math.cos(a), c+3.8*math.sin(a), 2.8,'#')
s.disc(c,c,2.6,'k'); s.disc(c,c,1.6,'R')
S['lotus_head']=s
s=Spr(9); c=4
pts=[(c,0.2),(c+3.6,c),(c,8.8),(c-3.6,c)]
s.poly_fill(pts,'k')
for (x0,y0),(x1,y1) in zip(pts,pts[1:]+pts[:1]): s.line(x0,y0,x1,y1,'#')
s.put(c,c,'R')
S['lotus_petal']=s

# ---- 19 ShootingSerpentus head: snake head with a red muzzle ring
s=Spr(11); c=5
s.disc(c,c,5,'#'); s.put(3,4,'R'); s.put(3,6,'R')
s.ring(c+2.2,c,1.6,'R'); s.put(c+2,c,'k')
S['snake_head_shooter']=s

if __name__=='__main__':
    import os
    os.makedirs('out',exist_ok=True)
    for n,sp in S.items(): sp.png(f'out/{n}.png')
    print(len(S),'sprites')
