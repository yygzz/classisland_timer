# 插件图标生成脚本：在仓库根目录运行 `python gen_icon.py` 即可生成 icon.png（需 Pillow）
from PIL import Image, ImageDraw

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 背景圆角方块
d.rounded_rectangle([8, 8, S-8, S-8], radius=56, fill=(34, 34, 51, 240))

# 进度环
cx, cy, r = S//2, S//2, 88
w = 18
d.ellipse([cx-r, cy-r, cx+r, cy+r], outline=(51, 85, 102, 255), width=w)
# 高亮弧：从 -90 度顺时针 270 度
d.arc([cx-r, cy-r, cx+r, cy+r], start=-90, end=180, fill=(76, 194, 255, 255), width=w)

# 表盘指针
d.line([(cx, cy), (cx+38, cy-52)], fill=(255, 255, 255, 255), width=14)
d.line([(cx, cy), (cx-6, cy+34)], fill=(255, 255, 255, 255), width=14)
for px, py in [(cx, cy), (cx+38, cy-52), (cx-6, cy+34)]:
    d.ellipse([px-8, py-8, px+8, py+8], fill=(255, 255, 255, 255))

img.save("icon.png")
print("icon.png saved")
