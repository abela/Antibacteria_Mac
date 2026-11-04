# Steam store localization

Steam languages: french, german, italian, spanish (Spain), portuguese (Portugal), czech, dutch, polish, russian,
japanese, koreana, schinese, tchinese. (Georgian is not a Steam store language.)

- `ShortDescriptions.md`: short description and the five headline texts per language. Source: `Tools/store_texts.py`.
- `HeadlineBanners/<lang>/headline_<name>_<lang>.png`: localized About This Game banners.
  Upload in Store Page Admin > Description > Upload Custom Images. Steam groups each one with the English
  `headline_<name>.png` by its language suffix, so the same `[img]` tags show the right language.
  Redraw: `python3 Tools/headline_banners_loc.py Localization/HeadlineBanners <m6x11cyrillic.ttf> <m6x11_extras.ttf>`.
- About This Game: the same BBCode as English in every language (the banners swap by themselves).
- Localized capsules (Japanese, Korean, Simplified and Traditional Chinese):
  - Pixel set (in use): `Art/Localized/<lang>/`, redraw with `python3 Tools/make_loc.py`.
  - Illustrated set (for the A/B test): `Illustrated/Art/Localized/<lang>/`, redraw with `Illustrated/Tools/loc_capsules.py`.
  Upload in Graphical Assets after picking the language at the top of the page.
  Both CJK tools need Noto Sans CJK (installed on Linux at /usr/share/fonts/opentype/noto).
