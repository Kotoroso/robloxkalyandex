# Иконка "Навыки": бегущий блочный человечек + зелёная стрелка вверх (стиль роблокс-симулятора)
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import math
S=4; N=512; W=N*S
DARK=(20,18,32,255)
img=Image.new("RGBA",(W,W),(0,0,0,0))

def poly_rect(cx,cy,w,h,ang):
    a=math.radians(ang); c,s=math.cos(a),math.sin(a)
    pts=[]
    for dx,dy in [(-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2)]:
        pts.append((cx+dx*c-dy*s, cy+dx*s+dy*c))
    return pts
def grad_fill(mask,top,bot):
    g=Image.new("RGBA",(W,W)); d=ImageDraw.Draw(g)
    bb=mask.getbbox() or (0,0,W,W)
    for y in range(W):
        t=min(1,max(0,(y-bb[1])/max(1,bb[3]-bb[1])))
        d.line([(0,y),(W,y)],fill=tuple(int(top[i]+(bot[i]-top[i])*t) for i in range(3))+(255,))
    out=Image.new("RGBA",(W,W),(0,0,0,0)); out.paste(g,(0,0),mask); return out
def outline(mask,px):
    return mask.filter(ImageFilter.GaussianBlur(px*0.5)).point(lambda v:255 if v>10 else 0)

k=S*N/512
def P(x,y): return (x*k,y*k)
# части человечка: (центр, размер, угол, цвет верх, цвет низ)
parts=[
 # задняя нога, задняя рука (темнее)
 ((150,352),(46,120),38,(40,120,40),(25,85,30)),
 ((250,238),(40,110),-60,(230,190,40),(190,140,20)),
 # туловище
 ((205,262),(92,112),12,(70,160,255),(30,95,220)),
 # передняя нога
 ((236,360),(46,124),-28,(80,200,70),(40,140,40)),
 # передняя рука
 ((160,258),(40,108),55,(255,220,70),(230,165,25)),
 # голова
 ((222,168),(84,84),12,(255,222,80),(235,175,30)),
]
layers=[]
full=Image.new("L",(W,W),0)
for (c,sz,ang,t,b) in parts:
    m=Image.new("L",(W,W),0)
    ImageDraw.Draw(m).polygon([P(*p) for p in poly_rect(c[0],c[1],sz[0],sz[1],ang)],fill=255)
    m=m.filter(ImageFilter.GaussianBlur(6*S)).point(lambda v:255 if v>128 else 0)  # скругление углов
    layers.append((m,t,b)); full=ImageChops.lighter(full,m)
# стрелка вверх справа
arrow=[P(360,110),P(470,250),P(410,250),P(410,420),P(310,420),P(310,250),P(250,250)]
am=Image.new("L",(W,W),0); ImageDraw.Draw(am).polygon(arrow,fill=255)
am=am.filter(ImageFilter.GaussianBlur(5*S)).point(lambda v:255 if v>128 else 0)
full=ImageChops.lighter(full,am)
# общая обводка
o=outline(full,26*S)
img.paste(Image.new("RGBA",(W,W),DARK),(0,0),o)
# стрелка (градиент зелёный) под человечком — сначала стрелка, потом фигура поверх
img.alpha_composite(grad_fill(am,(140,255,110),(30,170,60)))
ao=ImageChops.subtract(outline(am,0),am)
for (m,t,b) in layers:
    # тонкая обводка каждой части, чтобы части читались
    ring=ImageChops.subtract(outline(m,12*S),m)
    img.paste(Image.new("RGBA",(W,W),DARK),(0,0),ring)
    img.alpha_composite(grad_fill(m,t,b))
# лицо: глаза
d=ImageDraw.Draw(img)
for ex in (212,240):
    d.ellipse([P(ex-6,152),P(ex+6,170)],fill=DARK)
d.arc([P(208,166),P(246,190)],start=20,end=160,fill=DARK,width=int(5*k))
# блики
for (m,t,b) in layers[-1:]:
    pass
d.ellipse([P(330,190),P(350,230)],fill=(255,255,255,170))
d.ellipse([P(190,140),P(206,152)],fill=(255,255,255,200))
img=img.resize((N,N),Image.LANCZOS)
img.save("/home/user/robloxkalyandex/Assets/_Game/Resources/Art/icon_skills.png")
bg=Image.new("RGBA",(300,160),(82,59,46,255)); t=img.resize((130,130),Image.LANCZOS); bg.alpha_composite(t,(10,15)); bg.alpha_composite(img.resize((64,64),Image.LANCZOS),(180,50))
bg.save("/tmp/claude-0/-home-user/86ac1269-ff02-5b52-ae58-50e228fe329f/scratchpad/skills_prev.png")
