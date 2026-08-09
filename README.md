# S&M-PRT — Portable Radio Transmitter (for FIKA)

Adds 13 real-world radios to EFT + FIKA co-op, each with its own range, audio quality and distinctive "voice". Talk to your squad over a dedicated radio channel, with signal degrading realistically over distance until it drops into static.

**Built for SPT 4.0.13.**

## Requirements

For full functionality, the mod requires [Project Fika](https://forge.sp-tarkov.com/mod/2326/project-fika), otherwise the radio interaction functionality is unavailable. Radios act as additional items that also spawn in locations and can be bought from traders. In addition, they can be placed in the Hall of Fame.

## Features

* A dozen or so radios, each with its own range, sound quality, and transmission character;
* Spawning of radios in the world and their purchase from traders (at different loyalty levels);
* On‑screen status indicators in raid (radio on/off, transmit state, channel busy, duplex/simplex mode, signal strength) with detailed configuration options;
* A dedicated notification overlay with detailed configuration options;
* Recording of in‑raid conversations with save support and auto‑cleanup;
* Full localization into 8 languages (Russian, English, German, Spanish, French, Polish, Italian, and Czech);
* Support for the [Batteries Not Included](https://forge.sp-tarkov.com/mod/2513/batteries-not-included) mod.

## Installation

1. Open the downloaded archive with 7zip (recommended)
2. Locate the mod folder inside the 7zip archive
3. Drag the selected folders (`BepInEx` and `SPT`) into your SPT client folder

**Installation demonstration** (*many thanks to DrakiaXYZ for the demo*):

![Demonstration](https://i.imgur.com/34vXXDj.gif)

## Controls

The radio uses key combinations, and each of them includes the VOIP key. By default this is the "K" key. If you have a different one, use the key you assigned – the combinations below are based on the original:

`Right Ctrl + K` — turn the radio on/off;

`Right Shift + K` — switch to another radio (if you have several in valid slots);

`Enter + K` — toggle Half‑Duplex/Duplex* mode (while the radio is on);

To view radio recordings, the default key is `F9`, but this can be changed in the F12 settings.

> ##### *Half‑Duplex is a transmission mode in which devices can exchange information in both directions, but only one at a time, not simultaneously*
> ##### *Duplex is a radio mode in which the device can both transmit and receive signals at the same time. Not all radios in the game support this mode (it will be indicated in the description).*

## Raid Recordings

Optionally record radio traffic during a raid and listen back afterwards in a built-in browser — playback controls, waveform or spectrogram view, filtering, and auto-cleanup.

**Off by default; enable it in F12.**

## Settings

**DO NOT FORGET TO ENABLE VOIP IN THE ESCAPE FROM TARKOV SETTINGS AND IN THE F12 FIKA SETTINGS MENU, OTHERWISE THE MOD WILL NOT WORK**

**Enabling VOIP in FIKA settings**

![AllowVOIP_inSettingsFIKA](https://i.imgur.com/K5lRuJF.png)

The mod settings are located in‑game in the F12 menu under S&M-PRT 1.0.* (the image below shows an older version, for reference):

![SettingsPRT](https://i.imgur.com/n4a0WBA.png)

Currently the settings are split into 8 sections:

* *Hotkeys* – keybind settings for the radio
* *Volume* – volume settings for receive signal, interference, and notifications
* *Radio* – language settings, operation in the Labyrinth, and an experimental feature to transmit the sounds of gunfire near you
* *Notifications* – notification style, opacity, and size settings
* *Indicators* – indicator style, opacity, and size settings
* *Colors* – indicator colour settings
* *Raid Recordings* – theme settings for the recordings window, auto‑cleanup, and manual file cleaning
* *Developer* – toggle for verbose logging and battery drain rate adjustment (for the battery mod)

## Known Issues

* **When broadcasting inside buildings, sound quality degrades significantly due to the audio engine's behaviour** (currently unfixable);
* While in raid, if you open the menu with the ESC key, the radio indicators remain visible (currently unfixable);

> If you encounter any errors, please write in the comments or on Discord with a description of the issue and attach the logs: `\BepInEx\plugins\prt-fika\prt-fika.log`

## Credits

- _Suomi_ handled the engine‑side work: sourcing and adapting 3D radio models, bundling them into Unity AssetBundles, reverse‑engineering EFT's internal classes/APIs with dnSpy, finding and organising sound assets, extensive in‑game testing across multiple sessions, and designing the on‑screen status indicator system (layout, behaviour, style);
- _makshepard_ provided guides, reference material, design feedback, suggested and jointly reviewed ideas, participated in testing at all stages of development, and also drowned one radio in a puddle;
- **Developed with the help of AI**.

## License

Copyright (c) 2026 Suomi & makshepard — released under the MIT License, see `LICENSE`.
