# Картинки товаров для консоли Яндекс Игр (512x512): градиентный фон, иконка из игры, наклейка с количеством
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math
ART="/home/user/robloxkalyandex/Assets/_Game/Resources/Art/"
OUT="/home/user/robloxkalyandex/ЯНДЕКС_ЗАГРУЗКА/картинки_покупок/"
FONT="/tmp/claude-0/-home-user/86ac1269-ff02-5b52-ae58-50e228fe329f/scratchpad/assets/Rubik-ExtraBold.ttf"
N=512; DARK=(20,18,32,255)
def bg(c1,c2):
    im=Image.new("RGBA",(N,N)); d=ImageDraw.Draw(im)
    for y in range(N):
        t=y/N; d.line([(0,y),(N,y)],fill=tuple(int(c1[i]+(c2[i]-c1[i])*t) for i in range(3))+(255,))
    # лучи
    rays=Image.new("L",(N,N),0); rd=ImageDraw.Draw(rays)
    for k in range(12):
        a=k*math.pi/6; rd.polygon([(N/2,N/2),(N/2+math.cos(a)*800,N/2+math.sin(a)*800),(N/2+math.cos(a+0.18)*800,N/2+math.sin(a+0.18)*800)],fill=40)
    im.paste(Image.new("RGBA",(N,N),(255,255,255,255)),(0,0),rays)
    return im
def text(im,s,xy,size,fill=(255,255,255,255),stroke=10):
    d=ImageDraw.Draw(im); f=ImageFont.truetype(FONT,size)
    bb=d.textbbox((0,0),s,font=f,stroke_width=stroke); w=bb[2]-bb[0]; h=bb[3]-bb[1]
    d.text((xy[0]-w/2-bb[0],xy[1]-h/2-bb[1]),s,font=f,fill=fill,stroke_width=stroke,stroke_fill=DARK)
def icon(im,name,size,center=(N/2,N/2-10)):
    ic=Image.open(ART+name).convert("RGBA"); ic.thumbnail((size,size),Image.LANCZOS)
    im.alpha_composite(ic,(int(center[0]-ic.width/2),int(center[1]-ic.height/2)))
def badge(im,s,c=(235,40,60)):
    d=ImageDraw.Draw(im); d.rounded_rectangle([300,360,500,470],radius=40,fill=DARK); d.rounded_rectangle([308,368,492,462],radius=34,fill=c+(255,))
    text(im,s,(400,413),64,stroke=8)
def frame(im):
    m=Image.new("L",(N,N),0); ImageDraw.Draw(m).rounded_rectangle([0,0,N-1,N-1],radius=70,fill=255)
    out=Image.new("RGBA",(N,N),(0,0,0,0)); out.paste(im,(0,0),m); return out
def save(im,name): frame(im).save(OUT+name+".png"); print(name)

for n,cnt in (("dragon_egg_1","x1"),("dragon_egg_3","x3"),("dragon_egg_10","x10")):
    im=bg((255,190,60),(230,90,20)); icon(im,"egg_premium.png",400); badge(im,cnt); save(im,n)
im=bg((255,230,120),(220,150,20)); icon(im,"icon_egg.png",330); badge(im,"+1",(240,160,20)); save(im,"legend_egg")
im=bg((120,230,120),(30,150,60)); icon(im,"icon_coin.png",300,(230,250)); icon(im,"icon_coin.png",240,(320,300)); badge(im,"x2"); save(im,"x2_income")
im=bg((140,230,255),(40,120,220)); icon(im,"icon_egg.png",300,(236,240)); badge(im,"x2"); save(im,"x2_grow")
im=bg((130,170,255),(50,70,210)); icon(im,"icon_skills.png",330); badge(im,"x2"); save(im,"x2_train")
im=bg((190,150,255),(100,50,200)); icon(im,"icon_noads.png",320); badge(im,"2ч",(90,60,200)); save(im,"noads_2h")
im=bg((255,140,170),(210,40,90)); icon(im,"icon_noads.png",330,(N/2,N/2-30))
d=ImageDraw.Draw(im); d.rounded_rectangle([60,370,452,470],radius=40,fill=DARK); d.rounded_rectangle([68,378,444,462],radius=34,fill=(200,30,60,255))
text(im,"НАВСЕГДА",(N/2,420),58,stroke=8); save(im,"noads_forever")
trails=[("trail_gray",(200,200,210),(130,130,145)),("trail_green",(130,255,90),(40,170,40)),("trail_blue",(110,190,255),(30,90,255)),
        ("trail_purple",(220,120,255),(130,30,230)),("trail_gold",(255,240,130),(255,160,30)),("trail_fire",(255,220,60),(255,40,10)),
        ("trail_rainbow",None,None),("trail_cosmic",(80,230,255),(150,30,255))]
for name,a,b in trails:
    im=bg((40,50,100),(20,24,60)); d=ImageDraw.Draw(im)
    for i in range(6):
        y=190+i*28
        for x in range(40,360):
            t=(x-40)/320
            if a is None:
                import colorsys; r,g,bb=colorsys.hsv_to_rgb((t+i*0.08)%1,0.8,1); col=(int(r*255),int(g*255),int(bb*255))
            else: col=tuple(int(b[k]+(a[k]-b[k])*t) for k in range(3))
            w=int(2+t*14); d.ellipse([x,y-w/2+(abs(i-2.5)*4),x+3,y+w/2+(abs(i-2.5)*4)],fill=col+(int(80+175*t),))
    icon(im,"icon_shoe.png",230,(380,270)); save(im,name)
