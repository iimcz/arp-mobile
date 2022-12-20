# AR aplikace pro přípravu a náhled virtuální scény

Rozšířená realita (AR) umožňuje zobrazení virtuálního obsahu (3D modely, videa, obrázky,aj.) v reálném prostředí, např. pomocí mobilního zařízení, projektorů, a dalších. Pozorovatel tento virtuální obsah vidí pomocí daného zařízení umístěný na konkrétním místě, může jej obcházet, prohlížet si jej z více směrů a někdy s ním i interagovat. V rámci této práce se soustředíme na zobrazení AR obsahu pomocí průhledových brýlí pro rozšířenou realitu a mobilních zařízení (mobil, tablet) na platformě Android. Vytvořili jsme nástroj pro přípravu těchto scén přímo na místě (tj. v místnosti s expozicí), kdy více uživatelů se může podílet na přípravě a současném prohlížení virtuální scény. Použití demonstrujeme současným využitím AR brýlí pro rozmístění virtuálních objektů po místnosti a pomocí AR mobilního telefonu dochází k verifikaci umístění. Testování aplikace probíhalo v Národním Muzeu za plného provozu.

Aplikace je řešena jako nativní aplikace pro mobilní zařízení na platformě Android. Pro vývoj bylo použito prostředí Unity, skripty jsou implementovány v jazyce C#. Aplikace musí být připojena k internetu (kvůli synchronizaci scény). Využívá knihovny Immersal pro tvorbu virtuálních map prostředí, ARcore pro podporu rozšířené reality a Google Firebase pro synchronizaci stavu scén přes zařízení. Aplikace obsahuje jak nástroje pro správu AR scén, tak i pro jejich prohlížení. Vytváření map prostředí se řeší přes standardní aplikaci knihovny Immersal, která je volně dostupná pro mobilní platformy (Android i iOS). 

## Dokumentace

Celková dokumentace Správce obsahu je dostupná zde:

[AR aplikace pro přípravu a náhled virtuální scény - dokumentace](./docs/ar_dokumentace.pdf)
