# PRT - Portable Radio Transmitter (for FIKA only)
**v0.9.7**
- **Made by _Suomi & makshepard**
- **Developed with the help of AI**

Realistic portable radio communication for EFT + FIKA co-op. 13 radios (from Realistic TRC-83 to Harris AN/PRC-152 and the R-187P1), purchasable from traders or found in the world, with a full custom voice channel: range-based signal degradation, static, and a unique "voice" per radio.

> **DO NOT FORGET TO ENABLE VOIP IN THE ESCAPE FROM TARKOV SETTINGS AND IN THE F12 FIKA SETTINGS MENU, OTHERWISE THE MOD WILL NOT WORK**

## Features

* 13 radios, each with its own range, sound quality, and transmission character;
* Radios are available for purchase and barter from traders at various loyalty levels, and can also be found as loot in the world (on the ground and in containers, split between civilian and military locations);
* On‑screen status indicators in raid (radio on/off, transmit state, channel busy, duplex mode, signal strength) with customisation options;
* A dedicated notification overlay with simple configuration options;
* Item names, descriptions, and on-screen notifications localized into 8 languages (Russian, English, German, Spanish, French, Polish, Italian, Czech), auto-detected from your game's language;
* If Project Fika is not installed, radios automatically fall back to being collectible-only items (no radio functionality) and can be displayed in the Hall of Fame instead;
* All colors and indicator styles configurable in the BepInEx Configuration Manager (F12).

---

## Requirements

[Project Fika](https://forge.sp-tarkov.com/mod/2326/project-fika)

---

## Installation

1. Open the downloaded archive with 7zip (recommended)
2. Locate the mod folder inside the 7zip archive
3. Drag the selected folders into your SPT folder

**Installation demonstration** (*many thanks to DrakiaXYZ for the demo*):

![Demonstration](https://i.imgur.com/34vXXDj.gif)

---

## Controls

The radio uses key combinations, and each of them includes the VOIP key. By default this is the "K" key. If you have a different one, use the key you assigned – the combinations below are based on the original:

`Right Ctrl + K` — turn the radio on/off;

`Right Shift + K` — switch to another radio (if you have several in valid slots);

`Enter + K` — toggle Half‑Duplex/Duplex* mode (while the radio is on);

> ##### *Half‑Duplex is a transmission mode in which devices can exchange information in both directions, but only one at a time, not simultaneously*
> ##### *Duplex is a radio mode in which the device can both transmit and receive signals at the same time. Not all radios in the game support this mode (it will be indicated in the description).*

---

## Settings

**DO NOT FORGET TO ENABLE VOIP IN THE ESCAPE FROM TARKOV SETTINGS AND IN THE F12 FIKA SETTINGS MENU, OTHERWISE THE MOD WILL NOT WORK**
**Enabling VOIP in FIKA settings**

![AllowVOIP_inSettingsFIKA](https://i.imgur.com/K5lRuJF.png)

The mod settings are located in‑game in the F12 menu under Suomi-PRT *.*.* (see image below):

![SettingsPRT](https://i.imgur.com/n4a0WBA.png)

---

## Known Issues

* **When broadcasting inside buildings, sound quality degrades significantly due to the audio engine's behaviour** (currently unfixable);
* While in raid, if you open the menu with the ESC key, the radio indicators remain visible (currently unfixable);
* When switching a radio from an invalid slot to a valid one (*e.g. from backpack to rig*), it does not get selected automatically (we are working on this);


> If you encounter any errors, please write in the comments or on Discord with a description of the issue and attach the logs: `\BepInEx\plugins\prt-fika\prt-fika.log`

---

## Credits

- _Suomi_ handled the engine‑side work: sourcing and adapting 3D radio models, bundling them into Unity AssetBundles, reverse‑engineering EFT's internal classes/APIs with dnSpy, finding and organising sound assets, extensive in‑game testing across multiple sessions, and designing the on‑screen status indicator system (layout, behaviour, style);
- _makshepard_ provided guides, reference material, design feedback, and testing throughout development;
- **Developed with the help of AI**.

## AI Disclosure

This project was developed with AI assistance for most of the C# code. The author has practically no prior C# experience — this was their first real hands-on project with the language, so AI help with writing and explaining the code was necessary.

The author handled the engine-side work described in Credits above, plus extensive in-game testing and QA across multiple sessions.

AI was used as an implementation tool under the author's direction and testing — not as an autonomous replacement for their own engineering and QA work.
