# PRT - Portable Radio Transmitter (for FIKA only)
**v0.9.6**
- **Made by _Suomi & makshepard**
- **Developed with the help of AI**

Realistic portable radio communication for EFT + FIKA co-op. 13 radios (from Realistic TRC-83 to Harris AN/PRC-152 and the R-187P1), purchasable from traders, with a full custom voice channel: range-based signal degradation, static, and a unique "voice" per radio.

## Features
- 13 radios, each with distinct range, signal quality, and DSP character
- Trader integration (currency and/or barter, depending on radio)
- On-screen status indicators (power, talking, channel busy, duplex/simplex mode, signal strength) — fully configurable/toggleable
- Custom notification overlay
- All colors and indicator styles configurable in the BepInEx Configuration Manager (F12)

## Hotkeys
*(hold together with the game's VOIP key, default K)*

- **Right Ctrl** — toggle radio on/off
- **Right Shift** — select another radio (if carrying more than one)
- **Enter** — toggle Half-duplex/Simplex (not available on every radio, only while the radio is on)

All keybinds, volumes, indicators, and colors can be changed in the F12 menu.

## AI Disclosure

This project was developed with AI assistance for most of the C# code. The author has practically no prior C# experience — this was their first real hands-on project with the language, so AI help with writing and explaining the code was necessary.

The author handled the engine-side work: sourcing and adapting 3D radio models, bundling them into Unity AssetBundles, reverse-engineering EFT's internal classes/APIs with dnSpy, finding and organizing sound assets, extensive in-game testing across multiple sessions, and designing the on-screen status indicator system (layout, behavior, style).

Contributor makshepard provided guides, reference material, design feedback, and testing throughout development.

AI was used as an implementation tool under the author's direction and testing — not as an autonomous replacement for their own engineering and QA work.

---

# PRT - Portable Radio Transmitter (только для FIKA)
- **v0.9.6**
- **Создано _Suomi & makshepard**
- **Разработано с помощью ИИ**

Реалистичная портативная радиосвязь для EFT + FIKA co-op. 13 раций (от Realistic TRC-83 до Harris AN/PRC-152 и Р-187П1), покупка у торговцев, полноценный собственный голосовой канал: деградация сигнала по дальности, помехи, у каждой рации свой уникальный "голос".

## Особенности
- 13 раций, у каждой своя дальность, качество сигнала и характер звучания
- Покупка у торговцев (за деньги и/или бартер — зависит от рации)
- Статус-индикаторы на экране (питание, говорите ли вы, занятость канала, режим дуплекс/симплекс, сила сигнала) — полностью настраиваемые/отключаемые
- Собственный оверлей уведомлений
- Все цвета и стили индикаторов настраиваются в меню BepInEx Configuration Manager (F12)

## Горячие клавиши
*(зажимать вместе с клавишей VOIP игры, по умолчанию K)*

- **Right Ctrl** — включить/выключить рацию
- **Right Shift** — выбрать другую рацию (если их несколько)
- **Enter** — переключить Полудуплекс/Симплекс (не на всех рациях, только когда рация включена)

Все хоткеи, громкость, индикаторы и цвета можно поменять в меню F12.

## Раскрытие информации об ИИ

Этот проект был разработан с помощью ИИ для большей части кода на C#. Автор практически не имеет опыта работы с C# — это был его первый реальный практический проект на этом языке, поэтому помощь ИИ в написании и объяснении кода была необходима.

Автор занимался работой над движком: поиском и адаптацией 3D-моделей радио, их объединением в Unity AssetBundles, обратным проектированием внутренних классов/API EFT с помощью dnSpy, поиском и организацией звуковых ресурсов, обширным внутриигровым тестированием в нескольких сессиях и разработкой системы экранных индикаторов состояния (макет, поведение, стиль).

Участник makshepard предоставлял руководства, справочные материалы, отзывы о дизайне и тестирование на протяжении всей разработки.

ИИ использовался в качестве инструмента реализации под руководством и тестированием автора — а не как автономная замена его собственной инженерной и контрольной работы.
