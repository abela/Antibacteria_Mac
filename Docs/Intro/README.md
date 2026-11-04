# Intro (opening story)

**Story (v4):** late at night one lab still has its lights on. The doctor, who has searched for a cure for years,
finally calls it a day: he pulls the lamp chain and goes home, leaving a sample under the microscope. In the dark,
one cell in it wakes up (the player). The bacteria turn on it, the camera rushes in, white flash, and Level 1
starts with "YOU ARE THE CURE". No pills, drugs or injections.

| # | Shot | Caption (`WordIDs`) | Frames (15 fps) |
|---|---|---|---|
| 1 | Night city, one lit window | `IntroLine1` | 0-75 |
| 2 | The doctor at the microscope | `IntroLine2` | 75-150 |
| 3 | He stands, pulls the lamp chain, walks out, the door shuts | `IntroLine3` | 150-234 |
| 4 | The microscope alone in the moonlight, a sample on the slide | `IntroLine4` | 240-307 |
| 5 | Through the eyepiece: a cell wakes, the bacteria close in, rush, flash | `IntroLine5` | 307-388 |

427 frames (28.5 s); the flash is the last frames, where the game loads Level 1.

## Making it

The scripts render in Python (numpy, Pillow, scipy) and expect to sit together in one folder:

1. `animatic4.py` draws the shots (it reuses the shading, the lab and the doctor from `animatic3.py`, which uses
   `animatic2.py`). `python3 animatic4.py` writes a preview video; `python3 animatic4.py stills` a few frames.
2. `intro_stems4.py` synthesizes the music and effects tracks on the same timings, plus the Level 1 sting.
3. `export_frames4.py` packs the frames for the game (320x180, 2 bits a pixel) and prints each caption's frame range.

Copy `export4/intro_frames.bytes`, `intro_music.wav`, `intro_sfx.wav` and `intro_sting.wav` to
`Unity/AntiBacteria_Unity/Assets/_Project/Resources/Intro/`, and put the printed caption ranges in
Project Settings > AntiBacteria > Intro (and the defaults in `IntroSettings.cs`).

Captions are drawn by the game in its own font and translated (`IntroLine1`-`IntroLine5` in `LocalizationFile.json`).
If a new caption uses characters the pixel fonts don't have yet, rebuild them (see `Docs/Localization.md`).

The v3 scripts (`animatic3.py`, `intro_stems.py`, `export_frames.py`) made the earlier version (the cure and the
injection); v4 still imports `animatic3.py` for its drawing code.
