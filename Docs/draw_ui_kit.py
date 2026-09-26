# Набор интерфейса в стиле роблокс-симуляторов (рисуется кодом в высоком разрешении, суперсэмплинг x4)
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops
import math
OUT="/home/user/robloxkalyandex/Assets/_Game/Resources/Art/"
FONT="/tmp/claude-0/-home-user/86ac1269-ff02-5b52-ae58-50e228fe329f/scratchpad/assets/Rubik-ExtraBold.ttf"
S=4
DARK=(20,18,32,255)

def canvas(w,h): return Image.new("RGBA",(w*S,h*S),(0,0,0,0))
def rr(img,box,r,fill):
    ImageDraw.Draw(img).rounded_rectangle([v*S for v in box],radius=r*S,fill=fill)
def mask_rr(w,h,box,r):
    m=Image.new("L",(w*S,h*S),0); ImageDraw.Draw(m).rounded_rectangle([v*S for v in box],radius=r*S,fill=255); return m
def vgrad(w,h,top,bot,y0=0,y1=None):
    y1=h if y1 is None else y1
    g=Image.new("RGBA",(w*S,h*S))
    px=[]
    for y in range(h*S):
        t=min(1,max(0,(y/S-y0)/max(1,(y1-y0))))
        c=tuple(int(top[i]+(bot[i]-top[i])*t) for i in range(4))
        px.append(c)
    d=ImageDraw.Draw(g)
    for y,c in enumerate(px): d.line([(0,y),(w*S,y)],fill=c)
    return g
def paste_masked(img,src,m):
    tmp=Image.new("RGBA",img.size,(0,0,0,0)); tmp.paste(src,(0,0),m); img.alpha_composite(tmp)
def done(img,w,h,name):
    img.resize((w,h),Image.LANCZOS).save(OUT+name); print(name,w,h)

def grey(v,a=255): return (v,v,v,a)

def button(w,h,r,name,lip=16,t=9):
    img=canvas(w,h)
    rr(img,(0,0,w,h),r,DARK)                                   # толстая тёмная обводка
    rr(img,(t,t,w-t,h-t),r-t,grey(150))                        # нижняя "губа" (объём)
    face=mask_rr(w,h,(t,t,w-t,h-t-lip),r-t)
    paste_masked(img,vgrad(w,h,grey(255),grey(212),t,h-t-lip),face)
    # глянец на верхней половине
    gl=mask_rr(w,h,(t+8,t+6,w-t-8,t+(h-2*t-lip)*0.48),max(4,r-t-6))
    paste_masked(img,vgrad(w,h,(255,255,255,150),(255,255,255,30),t+6,t+(h-2*t-lip)*0.48),gl)
    # светлая кромка сверху внутри
    d=ImageDraw.Draw(img)
    # блик-капля слева сверху
    done(img,w,h,name)

def round_button(n,name,lip=12,t=9):
    img=canvas(n,n); d=ImageDraw.Draw(img)
    d.ellipse([0,0,n*S,n*S],fill=DARK)
    d.ellipse([t*S,t*S,(n-t)*S,(n-t)*S],fill=grey(150))
    m=Image.new("L",img.size,0); ImageDraw.Draw(m).ellipse([t*S,t*S,(n-t)*S,(n-t-lip)*S],fill=255)
    paste_masked(img,vgrad(n,n,grey(255),grey(210),t,n-t-lip),m)
    m2=Image.new("L",img.size,0); ImageDraw.Draw(m2).ellipse([(t+10)*S,(t+6)*S,(n-t-10)*S,(n*0.52)*S],fill=255)
    paste_masked(img,vgrad(n,n,(255,255,255,150),(255,255,255,20),t+6,n*0.52),m2)
    d.ellipse([(n*0.24)*S,(n*0.17)*S,(n*0.36)*S,(n*0.25)*S],fill=(255,255,255,210))
    done(img,n,n,name)

def panel(w,h,r,name):
    img=canvas(w,h)
    m=mask_rr(w,h,(0,0,w,h),r)
    paste_masked(img,vgrad(w,h,grey(255),grey(222)),m)
    # внутренняя светлая рамка и мягкая тень снизу
    inner=mask_rr(w,h,(5,5,w-5,h-5),r-5)
    ring=ImageChops.subtract(m,inner)
    paste_masked(img,Image.new("RGBA",img.size,grey(255)),ring)
    gl=mask_rr(w,h,(8,8,w-8,h*0.4),r-8)
    paste_masked(img,vgrad(w,h,(255,255,255,90),(255,255,255,0),8,h*0.4),gl)
    done(img,w,h,name)

def slot(n,r,name,t=8):
    img=canvas(n,n)
    rr(img,(0,0,n,n),r,DARK)
    m=mask_rr(n,n,(t,t,n-t,n-t),r-t)
    paste_masked(img,vgrad(n,n,grey(170),grey(235),t,n-t),m)      # утопленная ячейка: сверху темнее
    sh=mask_rr(n,n,(t,t,n-t,t+18),r-t)
    paste_masked(img,vgrad(n,n,(0,0,0,70),(0,0,0,0),t,t+18),sh)
    ring=ImageChops.subtract(mask_rr(n,n,(t,t,n-t,n-t),r-t),mask_rr(n,n,(t+4,t+4,n-t-4,n-t-4),r-t-4))
    paste_masked(img,Image.new("RGBA",img.size,grey(250,200)),ring)
    done(img,n,n,name)

def window(n,r,name,t=12):
    img=canvas(n,n)
    rr(img,(0,0,n,n),r,DARK)
    rr(img,(t,t,n-t,n-t),r-t,(78,98,190,255))                        # светлая внутренняя кромка
    m=mask_rr(n,n,(t+5,t+5,n-t-5,n-t-5),r-t-5)
    paste_masked(img,vgrad(n,n,(40,52,118,255),(26,32,78,255),t,n-t),m)
    sh=mask_rr(n,n,(t+5,t+5,n-t-5,t+40),r-t-5)
    paste_masked(img,vgrad(n,n,(0,0,0,90),(0,0,0,0),t+5,t+40),sh)
    done(img,n,n,name)

def sticker(w,h,text,name,c1,c2,fs,rot=-6,burst=False):
    img=canvas(w,h); d=ImageDraw.Draw(img)
    pad=10
    if burst:
        cx,cy=w/2*S,h/2*S; pts=[]
        for i in range(32):
            a=i/32*2*math.pi; rad=(min(w,h)/2-pad)*(1 if i%2==0 else 0.8)*S
            pts.append((cx+math.cos(a)*rad*w/min(w,h)*0.92,cy+math.sin(a)*rad))
        d.polygon(pts,fill=DARK)
        pts2=[(cx+(x-cx)*0.9,cy+(y-cy)*0.86) for x,y in pts]
        m=Image.new("L",img.size,0); ImageDraw.Draw(m).polygon(pts2,fill=255)
        paste_masked(img,vgrad(w,h,c1,c2),m)
    else:
        rr(img,(pad,pad,w-pad,h-pad),(h-2*pad)/2.2,DARK)
        m=mask_rr(w,h,(pad+7,pad+7,w-pad-7,h-pad-7),(h-2*pad)/2.2-7)
        paste_masked(img,vgrad(w,h,c1,c2,pad,h-pad),m)
        gl=mask_rr(w,h,(pad+14,pad+10,w-pad-14,h*0.48),(h-2*pad)/3)
        paste_masked(img,vgrad(w,h,(255,255,255,120),(255,255,255,10),pad+10,h*0.48),gl)
    f=ImageFont.truetype(FONT,fs*S)
    bb=d.textbbox((0,0),text,font=f,stroke_width=int(fs*0.16*S))
    tx=(w*S-(bb[2]-bb[0]))/2-bb[0]; ty=(h*S-(bb[3]-bb[1]))/2-bb[1]
    d.text((tx,ty),text,font=f,fill=(255,255,255,255),stroke_width=int(fs*0.16*S),stroke_fill=DARK)
    img=img.rotate(rot,resample=Image.BICUBIC,expand=False)
    done(img,w,h,name)

button(420,150,40,"ui_button.png")
round_button(160,"ui_button_round.png")
button(1024,190,44,"ui_header.png",lip=14,t=10)
panel(256,256,40,"ui_panel.png")
slot(160,34,"ui_slot.png")
window(512,56,"ui_window.png")
sticker(260,120,"NEW!","ui_badge_new.png",(255,110,90,255),(226,30,60,255),52)
sticker(220,170,"x2","ui_badge_x2.png",(255,238,90,255),(255,140,20,255),78,rot=-10,burst=True)
