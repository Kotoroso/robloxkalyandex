# Промпты для картинок — Укради Дракона

## Как пользоваться
1. Бери промпты по порядку: номер → картинка. Копируй блок целиком — стиль уже внутри.
2. Картинки нужны в PNG с прозрачным фоном. Если генератор прозрачный фон не умеет, пусть делает белый: фон я уберу сам.
3. Файл называй как написано после стрелки (например `ui_button.png`) и присылай мне. Можно пачкой и без переименования, просто подпиши номера.
4. Игра подхватит картинки автоматически: если файл лежит в `Assets/_Game/Resources/Art/`, он заменит нарисованное кодом.
5. Где сказано «БЕЛАЯ, игра красит сама», картинка должна быть бело-серой: одна и та же кнопка становится зелёной, красной или синей в разных местах.
6. «9-slice» значит, что картинка растягивается: углы и рамка одинаковые со всех сторон, а середина пустая.

Порядок важности: сначала 1–17 (интерфейс), потом 18–31 (иконки и баннеры), потом драконы (донатные 89–94 в первую очередь), яйца и трейлы.

---

## Интерфейс (1–17)

1. **Базовая панель / карточка (БЕЛАЯ, игра красит сама, 9-slice)** → файл `ui_panel.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a plain rounded rectangle panel, corner radius about 20% of the height, soft vertical gradient from pure white at the top to light grey (#D0D0D0) at the bottom, very subtle inner top highlight line, NO outline. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. Square 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

2. **Кнопка (БЕЛАЯ, игра красит сама, 9-slice)** → файл `ui_button.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a chunky glossy 3D game button, rounded rectangle, thick dark navy outline (#141220) about 6% of the height, a glossy white highlight band on the upper half, a darker bottom lip (3D push-button look, bottom edge thicker), faint diagonal diamond pattern inside. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. 512x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

3. **Круглая кнопка HUD (БЕЛАЯ, игра красит сама)** → файл `ui_button_round.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a round glossy 3D game button, perfect circle, thick dark navy outline (#141220), glossy crescent highlight on the top, slightly darker bottom rim for 3D depth, empty center (the icon is placed on top by the game). The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. Square 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

4. **Фон окна (магазин, драконы, трейлы — цветная, 9-slice)** → файл `ui_window.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a large game window background, rounded rectangle, deep navy blue (#1E2650) with a very subtle lighter diamond lattice pattern, thick dark outline (#141220), soft inner glow along the edges, slightly lighter at the top. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. Square 1024x1024 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

5. **Шапка окна (БЕЛАЯ, игра красит сама в цвет раздела, 9-slice)** → файл `ui_header.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a wide header bar for a game window, rounded rectangle, thick dark navy outline, big glossy highlight across the upper half, subtle bottom shadow band, empty center (the title text is added by the game). The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. 1024x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

6. **Кнопка закрытия ✕ (цветная)** → файл `ui_close.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a square red close button with rounded corners, bright red (#EE3845) with glossy top highlight, thick dark navy outline, a big bold white X in the center with a dark outline. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no watermark, no frame, no drop shadow on the ground, centered with small margins
```

7. **Ячейка инвентаря / хотбара (БЕЛАЯ, игра красит сама, 9-slice)** → файл `ui_slot.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a square inventory slot with rounded corners, thick dark outline, slightly recessed inner area (inner shadow at the top, light edge at the bottom), empty center. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

8. **Плашка подсказки «Удерживай E» (БЕЛАЯ, игра красит в тёмный, 9-slice)** → файл `ui_prompt.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a wide pill-shaped rounded rectangle tooltip plate, thin dark outline, soft gradient, empty center. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. 512x192 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

9. **Клавиша (под букву E — букву ставит игра)** → файл `ui_key.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a single mechanical keyboard keycap seen from the front, white keycap with light grey sides, rounded, soft shadow at the bottom edge, thin dark outline, EMPTY top face with no letter. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

10. **Всплывающее уведомление (БЕЛАЯ, игра красит, 9-slice)** → файл `ui_toast.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a wide rounded notification banner, soft gradient, thin dark outline, subtle glossy top edge, empty center. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. 9-slice friendly: the corners and border must be identical on all four sides, the middle area must be plain and uniform (no icons, no text, no details in the center), so it can be stretched to any size. 512x128 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

11. **Бейдж «NEW!» (цветной, текст на картинке)** → файл `ui_badge_new.png`
```
Roblox game UI sticker badge with the text "NEW!" in bold chunky white letters with thick dark outline, on a bright red-to-orange rounded tag, glossy, slightly tilted, small sparkles. 256x128 PNG, transparent background, no watermark
```

12. **Бейдж «x2» (цветной, текст на картинке)** → файл `ui_badge_x2.png`
```
Roblox game UI sticker badge with the text "x2" in huge bold chunky yellow-white letters with thick dark outline, on a hot pink / red starburst shape, glossy, energetic. 256x160 PNG, transparent background, no watermark
```

13. **Основа джойстика на телефоне (полупрозрачная)** → файл `ui_joystick_bg.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a mobile virtual joystick base: a semi-transparent white ring (about 25% opacity inside, 60% opacity rim) with four small arrow chevrons pointing up, down, left, right. Square 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

14. **Ручка джойстика** → файл `ui_joystick_knob.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a mobile virtual joystick knob: a glossy white circle, 70% opacity, soft grey shading and a small highlight at the top, thin outline. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

15. **Кнопка прыжка на телефоне** → файл `ui_jump.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a round mobile jump button, semi-transparent white circle (40% opacity) with a thick white rim and a big bold white upward arrow in the center. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```

16. **Узор-подложка (бесшовный)** → файл `ui_pattern.png`
```
seamless tileable pattern texture: thin white diagonal diamond lattice lines on a fully transparent background, lines about 10% opacity, perfectly repeating on all edges. Square 128x128 PNG, no text
```

17. **Круг (БЕЛЫЙ, для кружков и точек)** → файл `ui_circle.png`
```
Roblox simulator game UI asset, bold cartoon style, clean vector-like shapes, glossy plastic look, crisp edges, flat front view (no perspective), a perfect flat white circle with a very soft light grey radial shading at the bottom. The asset must be WHITE / very light grey only (no color at all) because the game tints it by code; use only light-to-mid grey shading for volume. Square 256x256 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins
```


---

## Иконки интерфейса (18–29)

18. **Монета (деньги)** → файл `icon_coin.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a shiny thick gold coin with a star embossed in the center and a white highlight
```

19. **Молния (скорость)** → файл `icon_bolt.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a cyan-blue lightning bolt with glow
```

20. **Звезда (ежедневная награда)** → файл `icon_star.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a pink-magenta glossy star with a small white shine
```

21. **Шестерёнка (настройки)** → файл `icon_gear.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a chunky silver-grey settings gear
```

22. **Сумка (магазин улучшений)** → файл `icon_bag.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a green shopping bag with a gold coin emblem
```

23. **Яйцо (кнопка яиц)** → файл `icon_egg.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: an orange dragon egg with yellow spots
```

24. **Дракон (раздел драконов)** → файл `icon_dragon.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a cute chunky orange baby dragon head, blocky toy style, big eyes, small horns
```

25. **Лапа (драконы на красной кнопке)** → файл `icon_paw.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: an orange dragon paw print with three claws
```

26. **Кроссовок (трейлы / скорость)** → файл `icon_shoe.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a blue-white running sneaker with motion speed lines
```

27. **Книга** → файл `icon_book.png`
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a blue book with a gold emblem
```

28. **Валюта Ян (донат)** → файл `icon_yan.png` — на будущее, можно пропустить
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a round golden-yellow token with a stylized letter Y, like premium game currency
```

29. **Замок (закрыто)** → файл `icon_lock.png` — на будущее, можно пропустить
```
bold cartoon game UI icon, Roblox simulator style, thick dark navy outline, glossy highlights, vibrant saturated colors, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 512x512 PNG. Object: a golden padlock, closed
```


---

## Баннеры магазина (30–31)

30. **Донат «x2 доход»** → файл `store_x2_income.png`
```
Roblox simulator game store icon: a big pile of shiny gold coins and green cash bills with a big bold red "x2" badge in the corner, glossy cartoon style, thick dark outline, vibrant, 512x512 PNG, transparent background, no other text
```

31. **Донат «x2 рост яиц»** → файл `store_x2_grow.png`
```
Roblox simulator game store icon: a rainbow stopwatch with fast-forward arrows next to a glowing dragon egg, big bold "x2" badge, glossy cartoon style, thick dark outline, 512x512 PNG, transparent background, no other text
```


---

## Трейлы (32–39)

32. **Серый трейл** → файл `trail_0.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: silver-grey soft trail
```

33. **Зелёный трейл** → файл `trail_1.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: lime-green trail
```

34. **Синий трейл** → файл `trail_2.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: bright blue trail
```

35. **Фиолетовый трейл** → файл `trail_3.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: purple trail with sparkles
```

36. **Золотой трейл** → файл `trail_4.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: golden trail with sparkles
```

37. **Огненный трейл** → файл `trail_5.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: fire trail of yellow-orange-red flames with sparks
```

38. **Радужный трейл** → файл `trail_6.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: rainbow gradient trail with sparkles
```

39. **Космический трейл** → файл `trail_7.png`
```
Roblox blocky character (LEGO-like, faceless simple) running to the right with a long glowing speed trail streaming behind him, side view, game shop icon, glossy cartoon, thick dark outline, 512x512 PNG, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins. Trail: cosmic cyan-to-violet trail full of tiny stars
```


---

## Яйца (40–52)

40. **Драконье яйцо (донат)** → файл `egg_premium.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: golden premium egg with orange spots, a shining gold band around the middle, glowing cracks of light, sparkles around, premium loot-box feeling
```

41. **Яйцо Обычный** → файл `egg_0.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: light grey
```

42. **Яйцо Необычный** → файл `egg_1.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: green
```

43. **Яйцо Редкий** → файл `egg_2.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: blue
```

44. **Яйцо Эпический** → файл `egg_3.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: purple
```

45. **Яйцо Легендарный** → файл `egg_4.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: golden yellow
```

46. **Яйцо Мифический** → файл `egg_5.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: red
```

47. **Яйцо Божественный** → файл `egg_6.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: pale gold and white with a soft glow
```

48. **Яйцо Секретный** → файл `egg_7.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: cyan with glowing lines
```

49. **Яйцо Небесный** → файл `egg_8.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: violet with tiny stars
```

50. **Яйцо Древний** → файл `egg_9.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: bronze with carved glowing runes
```

51. **Яйцо Галактический** → файл `egg_10.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: deep blue galaxy with stars and nebula
```

52. **Яйцо Омега** → файл `egg_11.png`
```
cute glossy 3D game icon of a dragon egg, Roblox / LEGO toy style, chunky smooth shape, soft studio light, big lighter spots on the shell, transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG. Egg: magenta and black with a pink glow
```


---

## Драконы (53–94)

Каждый дракон — копия 3D-модели из игры. Стиль и строение одинаковые у всех, меняются только цвета и детали по редкости.
Строение модели (уже вписано в каждый промпт):
- кубическое тело, светлое пузо выступает спереди, на груди две полоски-пластины;
- шея под наклоном, кубическая голова с мордочкой и светлой нижней челюстью;
- большие глаза: белок, цветная радужка, белый блик; розовые щёчки;
- рожки загнуты назад;
- крылья: кость по переднему краю, светлая перепонка, одно ребро и ромбик на кончике;
- хвост из 3 сегментов загибается вверх и кончается ромбом-плавником;
- 4 короткие лапы, передние с цветными когтями;
- на спине цветные полосы и шипы.

53. **Lizzy / Ящерок (Обычный)** → файл `dragon_0.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #8FBF5A, belly #E8E0A0, wings #6E9A40, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

54. **Ashling / Пепельный (Обычный)** → файл `dragon_1.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #9A9A9A, belly #D0D0D0, wings #707070, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

55. **Sandy / Песчаник (Обычный)** → файл `dragon_2.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #D9B870, belly #F5E6B8, wings #B08A40, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

56. **Leaftail / Листохвост (Необычный)** → файл `dragon_3.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #3FBF4F, belly #B5F07A, wings #2A8A38, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

57. **Swampy / Болотник (Необычный)** → файл `dragon_4.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #4E7A3A, belly #9AB06A, wings #33552A, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

58. **Coral / Коралл (Необычный)** → файл `dragon_5.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FF7F7F, belly #FFD0C0, wings #E05050, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

59. **Frostfang / Ледяной Клык (Редкий)** → файл `dragon_6.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #6EC8FF, belly #E6F7FF, wings #3A8FD0, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. Effect: icy frost mist and little snowflakes floating around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

60. **Stormy / Грозовик (Редкий)** → файл `dragon_7.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #3050C0, belly #A0B8FF, wings #FFE040, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

61. **Sapphire / Сапфир (Редкий)** → файл `dragon_8.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #2060FF, belly #80C0FF, wings #1030A0, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 2 spikes along the spine in the wing color. 2 stripe bands on the back. Effect: icy frost mist and little snowflakes floating around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

62. **Amethyst / Аметистовый (Эпический)** → файл `dragon_9.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #A040F0, belly #E0B0FF, wings #6A20B0, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 3 spikes along the spine, glowing. 2 stripe bands on the back. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

63. **Shadow / Теневой (Эпический)** → файл `dragon_10.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #2A2438, belly #6A5A8A, wings #A040F0, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 3 spikes along the spine, glowing. 2 stripe bands on the back. Effect: dark purple smoke wisps swirling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

64. **Toxic / Токсик (Эпический)** → файл `dragon_11.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #70FF40, belly #D0FF80, wings #308020, contrasting accent color for claws, stripes and tail fin. Small pale cream horns. 3 spikes along the spine, glowing. 2 stripe bands on the back. Effect: a soft glowing aura around its body in the accent color. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

65. **Golden Emperor / Золотой Император (Легендарный)** → файл `dragon_12.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFC820, belly #FFF0A0, wings #E08A10, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Effect: a glowing golden ring halo floating above its head. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

66. **Sun Phoenix / Солнечный Феникс (Легендарный)** → файл `dragon_13.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FF8A20, belly #FFE060, wings #FF3A10, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

67. **Thunder Roar / Молниевый Рык (Легендарный)** → файл `dragon_14.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFE640, belly #FFFFFF, wings #3060FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

68. **Volcanohorn / Вулканорог (Мифический)** → файл `dragon_15.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #D02020, belly #FF9040, wings #401010, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

69. **Blood Moon / Кровавая Луна (Мифический)** → файл `dragon_16.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #8A0A2A, belly #FF5070, wings #200008, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Effect: a soft glowing aura around its body in the accent color. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

70. **Emerald Warden / Изумрудный Страж (Мифический)** → файл `dragon_17.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #10C080, belly #A0FFD0, wings #086040, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 3 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Effect: a soft glowing aura around its body in the accent color. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

71. **Archangel / Архангел (Божественный)** → файл `dragon_18.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFFFFF, belly #FFF4C0, wings #FFD860, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. A glowing gem on the forehead. Effect: a glowing golden ring halo floating above its head. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

72. **Thunder God / Громовержец (Божественный)** → файл `dragon_19.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #F0E0A0, belly #FFFFFF, wings #60A0FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. A glowing gem on the forehead. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

73. **Sun God / Солнцебог (Божественный)** → файл `dragon_20.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFB020, belly #FFF080, wings #FF6000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. A glowing gem on the forehead. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

74. **Cosmic / Космический (Секретный)** → файл `dragon_21.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #1A1060, belly #50F0FF, wings #FF40E0, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: tiny yellow and white stars orbiting around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

75. **Rainbow God / Радужный Бог (Секретный)** → файл `dragon_22.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFFFFF, belly #FF60A0, wings #40FFB0, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: rainbow iridescent shimmer on its scales and rainbow sparkles. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

76. **Voidling / Пустотник (Секретный)** → файл `dragon_23.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #100818, belly #8030FF, wings #000000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: dark purple smoke wisps swirling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

77. **Star Serpent / Звёздный Змей (Небесный)** → файл `dragon_24.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #3040C0, belly #C0D0FF, wings #FFFFFF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: tiny yellow and white stars orbiting around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

78. **Galaxion / Галактион (Небесный)** → файл `dragon_25.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #6020A0, belly #FF80FF, wings #20E0FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: rainbow iridescent shimmer on its scales and rainbow sparkles. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

79. **Infinity / Бесконечность (Небесный)** → файл `dragon_26.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #000000, belly #FFFFFF, wings #FFD700, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: a glowing golden ring halo floating above its head. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

80. **Fossil Titan / Окаменелый Титан (Древний)** → файл `dragon_27.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #A08060, belly #E0D0B0, wings #604020, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: small white sparkles twinkling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

81. **Rune Wyrm / Руный Змей (Древний)** → файл `dragon_28.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #406080, belly #80FFFF, wings #203040, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: a soft glowing aura around its body in the accent color. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

82. **Primordial / Первородный (Древний)** → файл `dragon_29.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #C06020, belly #FFD080, wings #FF2000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

83. **Nebula / Туманность (Галактический)** → файл `dragon_30.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #4020C0, belly #FF80E0, wings #20C0FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: tiny yellow and white stars orbiting around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

84. **Quasar / Квазар (Галактический)** → файл `dragon_31.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFFFFF, belly #80C0FF, wings #FFFF80, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: a glowing golden ring halo floating above its head. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

85. **Black Hole / Чёрная Дыра (Галактический)** → файл `dragon_32.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #050008, belly #6000FF, wings #FF6000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: dark purple smoke wisps swirling around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

86. **Omega Prime / Омега Прайм (Омега)** → файл `dragon_33.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FF1060, belly #FFD0E0, wings #400010, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

87. **Chronos / Хронос (Омега)** → файл `dragon_34.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #E0C060, belly #FFFFFF, wings #2040FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: tiny yellow and white stars orbiting around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

88. **The Absolute / Абсолют (Омега)** → файл `dragon_35.png`
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFFFFF, belly #000000, wings #FF00FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 5 spikes along the spine, glowing. 3 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. A glowing gem on the forehead. Effect: rainbow iridescent shimmer on its scales and rainbow sparkles. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

89. **Crystal Guardian / Кристальный Страж (Мифический)** → файл `dragon_36.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #60E0FF, belly #E0FFFF, wings #20A0FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: icy frost mist and little snowflakes floating around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

90. **Lava Titan / Лавовый Титан (Божественный)** → файл `dragon_37.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FF4010, belly #FFC040, wings #300800, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: small cartoon flames flickering on its back spikes and tail tip. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

91. **Neon Cyberdragon / Неоновый Кибердракон (Секретный)** → файл `dragon_38.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #101020, belly #00FFC8, wings #FF00C8, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: a soft glowing aura around its body in the accent color. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

92. **Solar Deity / Солнечный Бог (Небесный)** → файл `dragon_39.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFD020, belly #FFFFFF, wings #FF8000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: a glowing golden ring halo floating above its head. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

93. **Galaxy Leviathan / Галактический Кит (Галактический)** → файл `dragon_40.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #200050, belly #80FFFF, wings #FF60FF, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: tiny yellow and white stars orbiting around it. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```

94. **Infinity Dragon / Дракон Бесконечности (Омега)** → файл `dragon_41.png` — ДОНАТНЫЙ, самое важное
```
cute chunky 3D baby dragon, Roblox / LEGO blocky toy style made of slightly rounded boxes, glossy plastic, soft studio lighting, three-quarter view facing right, whole body visible and centered. Anatomy: boxy body, a lighter protruding belly with two horizontal chest plates, short tilted neck, cube-shaped head with a short square snout and a lighter lower jaw, big glossy eyes (white eye, colored iris, white highlight), pink blush cheeks, two horns angled backwards, two bat wings (thick bone on the front edge, lighter membrane, one rib, small diamond tip), a 3-segment tail curling upward ending in a diamond-shaped fin, four short stubby legs, front legs with colored claws, colored stripe bands across the back, spikes along the spine. Colors: body #FFFFFF, belly #FFD700, wings #000000, contrasting accent color for claws, stripes and tail fin. Big shiny GOLD horns. 4 spikes along the spine, glowing. 2 stripe bands on the back. Glowing tail fin. Glowing claws. Glowing colored irises. PREMIUM EXCLUSIVE: a golden crown with three points (the middle one has a glowing gem), a second smaller pair of glowing translucent wings, glowing crystal spikes on the back, glowing back stripes, slightly bigger and more epic, extra shiny. Effect: rainbow iridescent shimmer on its scales and rainbow sparkles. transparent background (if transparency is impossible — pure flat white #FFFFFF background), no text, no watermark, no frame, no drop shadow on the ground, centered with small margins, square 1024x1024 PNG.
```


---

## Что будет дальше
Присылай готовые картинки (можно пачкой, с номерами). Я:
1. уберу фон и обрежу края;
2. переименую файлы и положу их в `Resources/Art`;
3. поправлю 3D-модели драконов в игре по твоим картинкам: пропорции, цвета, рога, крылья.
