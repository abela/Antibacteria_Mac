"""Boss sprites for AntiBacteria (palette indices). Each boss gets 4 damage stages: _0 healthy .. _3 badly eroded."""
import math, random
from draw import Spr, inside

def erode(base, frac, seed, colours='#'):
    """Damage stage: punch background-coloured holes into a share of the body pixels (deterministic)."""
    s=Spr(base.w,base.h); s.p=[r[:] for r in base.p]
    rnd=random.Random(seed)
    cells=[(x,y) for y in range(s.h) for x in range(s.w) if s.p[y][x] in colours]
    rnd.shuffle(cells)
    for x,y in cells[:int(len(cells)*frac)]:
        # keep the silhouette: only erode pixels that are not on the outer edge
        if all(s.get(x+dx,y+dy)!='.' for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))): s.p[y][x]='k'
    return s

B={}
# ---- BigBoss (22): huge spinning cell, dithered body, thick rim, 6 red nuclei + spikes
s=Spr(89); c=44
s.disc(c,c,38,'k'); s.disc(c,c,38,'#',dither=0); s.ring(c,c,38,'#'); s.ring(c,c,37,'#'); s.ring(c,c,36.2,'#')
for k in range(16):
    a=k*math.pi/8; L=44 if k%2==0 else 41.5
    for w in (-1,0,1): s.line(c+38*math.cos(a)+w*math.sin(a)*0.6, c+38*math.sin(a)-w*math.cos(a)*0.6, c+L*math.cos(a), c+L*math.sin(a), '#')
s.disc(c,c,11,'k'); s.ring(c,c,11,'#'); s.disc(c,c,6,'R')
for k in range(6):
    a=k*math.pi/3+math.pi/6; x,y=c+24*math.cos(a), c+24*math.sin(a)
    s.disc(x,y,4.6,'k'); s.ring(x,y,4.6,'#'); s.disc(x,y,2.2,'R')
B['boss_bigboss']=s

# ---- Inferno (23): wavy flame outline, dithered, big red core
s=Spr(65); c=32
def rad(a): return 25+3.5*math.sin(7*a)+2*math.sin(13*a+1)
for y in range(65):
    for x in range(65):
        dx,dy=x-c,y-c; r=math.hypot(dx,dy); a=math.atan2(dy,dx)
        if r<=rad(a): s.put(x,y,'#' if (x+y)%2==0 else 'k')
for i in range(720):
    a=2*math.pi*i/720; r=rad(a); s.put(c+r*math.cos(a), c+r*math.sin(a), '#')
    if i%9==0: s.line(c+r*math.cos(a), c+r*math.sin(a), c+(r+4+2*math.sin(5*a))*math.cos(a+0.08), c+(r+4+2*math.sin(5*a))*math.sin(a+0.08), '#')
s.disc(c,c,10,'k'); s.ring(c,c,10,'#'); s.disc(c,c,7,'R'); s.disc(c-2,c-2,1.5,'#')
B['boss_inferno']=s

# ---- BigRedNoise (24): giant jagged red static, white teeth, hollow centre for a big eye
s=Spr(69); c=34
random.seed(24)
s.disc(c,c,27,'k')
for i in range(220):
    a=2*math.pi*i/220; r=29+random.choice([-2,-1,0,0,1,2,3,4])
    s.put(c+r*math.cos(a), c+r*math.sin(a), 'R'); s.put(c+(r-1)*math.cos(a), c+(r-1)*math.sin(a), 'R')
for i in range(0,220,4):
    a=2*math.pi*i/220; s.put(c+26*math.cos(a), c+26*math.sin(a), '#')
for i in range(140):
    a=random.random()*2*math.pi; r=random.uniform(13,24)
    s.put(c+r*math.cos(a), c+r*math.sin(a), 'R' if random.random()<0.5 else '#')
s.disc(c,c,10,'k'); s.ring(c,c,10.5,'#')
B['boss_bigrednoise']=s
e=Spr(11); e.disc(5,5,5,'R'); e.disc(3.5,3.5,1.2,'#'); B['eye_boss']=e

# ---- Anakonda (25): big snake head (faces left) + big ring segments
s=Spr(17); c=8
s.disc(c,c,8,'#'); s.disc(4,5,1.6,'R'); s.disc(4,11,1.6,'R')
s.line(0,8,3,8,'k'); s.put(1,7,'k'); s.put(1,9,'k')
B['ana_head']=s
for name,r in (('ana_segment_r7',7),('ana_segment_r6',6),('ana_segment_r5',5)):
    s=Spr(2*r+1); c=r
    s.disc(c,c,r,'k'); s.ring(c,c,r,'#'); s.ring(c,c,r-1.2,'#',step=2); s.disc(c,c,1.6,'R')
    B[name]=s

# ---- Frunctus (26): the final boss - a seed/fruit cell with a leaf crown and a red heart, eye socket
s=Spr(27); c=13
for y in range(27):
    for x in range(27):
        v=((x-c)/10.5)**2+((y-c-1.5)/11.5)**2
        if v<=1: s.put(x,y,'k')
        if 0.78<v<=1.02: s.put(x,y,'#')
        elif v<=0.78 and (x*3+y)%5==0: s.put(x,y,'#')
for sx in (-1,1):
    s.line(c,c-9,c+sx*6,c-12.5,'#'); s.line(c+sx*6,c-12.5,c+sx*2,c-7,'#')
s.line(c,c-11,c,c-8,'#')
s.disc(c,c+1.5,4.5,'k'); s.ring(c,c+1.5,4.5,'#')
B['boss_frunctus']=s
e=Spr(5); e.stamp(['.RRR.','RRRRR','RR#RR','RRRRR','.RRR.']); B['eye_frunctus']=e

if __name__=='__main__':
    import os
    os.makedirs('out_boss',exist_ok=True)
    for n,sp in B.items():
        sp.png(f'out_boss/{n}.png')
        if n.startswith('boss_'):
            for k,frac in enumerate((0.22,0.42,0.62),start=1):
                erode(sp,frac,sum(map(ord,n))+k,'#R' if n=='boss_bigrednoise' else '#').png(f'out_boss/{n}_{k}.png')
    print('ok', sorted(os.listdir('out_boss')))
