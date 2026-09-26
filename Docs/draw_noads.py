# Иконка "Без рекламы": табличка AD, перечёркнутая красным кругом (стиль роблокс-симулятора)
from PIL import Image, ImageDraw, ImageFilter, ImageFont
S=4; N=512; W=N*S; k=W/512
DARK=(20,18,32,255)
FONT="/tmp/claude-0/-home-user/86ac1269-ff02-5b52-ae58-50e228fe329f/scratchpad/assets/Rubik-ExtraBold.ttf"
img=Image.new("RGBA",(W,W),(0,0,0,0)); d=ImageDraw.Draw(img)
def P(*v): return [x*k for x in v]
# табличка AD
d.rounded_rectangle(P(96,146,416,366),radius=40*k,fill=DARK)
g=Image.new("RGBA",(W,W)); gd=ImageDraw.Draw(g)
for y in range(W):
    t=min(1,max(0,(y/k-166)/180)); gd.line([(0,y),(W,y)],fill=(int(255-40*t),int(255-40*t),int(255-30*t),255))
m=Image.new("L",(W,W),0); ImageDraw.Draw(m).rounded_rectangle(P(116,166,396,346),radius=26*k,fill=255)
img.paste(g,(0,0),m)
f=ImageFont.truetype(FONT,int(150*k))
bb=d.textbbox((0,0),"AD",font=f); tx=(W-(bb[2]-bb[0]))/2-bb[0]; ty=(W-(bb[3]-bb[1]))/2-bb[1]
d.text((tx,ty),"AD",font=f,fill=(60,70,110,255))
# красный круг с чертой
for col,wid in ((DARK,62),((235,40,50,255),40)):
    d.ellipse(P(56,56,456,456),outline=col,width=int(wid*k))
    d.line(P(126,126,386,386),fill=col,width=int(wid*k))
img=img.resize((N,N),Image.LANCZOS)
img.save("/home/user/robloxkalyandex/Assets/_Game/Resources/Art/icon_noads.png")
