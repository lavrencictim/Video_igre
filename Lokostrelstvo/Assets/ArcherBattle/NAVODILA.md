# 🏹 Archer Battle 2D – navodila

2D lokostrelska igra (igralec proti AI) za Unity 6 + URP. Vsa grafika, zvoki in UI
se ustvarijo s kodo ob zagonu, zato ni treba uvažati nobenih slik ali zvokov.

## Zagon
1. Odpri projekt **Lokostrelstvo** v Unityju (6000.6.x).
2. Počakaj, da se skripte v `Assets/ArcherBattle/Scripts` prevedejo (spodaj desno se vrti krogec).
3. Odpri `Assets/Scenes/SampleScene` (ali katerokoli sceno) in pritisni **Play**.
   `GameBootstrap` sam ustvari igro, kamero preklopi v 2D (ortografsko) pogled.
4. Za najboljši izgled v oknu **Game** izberi razmerje **16:9** (Full HD).

Ničesar ni treba vleči v sceno. Če želiš, lahko na prazen GameObject ročno dodaš
komponento `GameManager` – potem se samodejni zagon ne izvede še enkrat.

## Kontrole
| Tipka | Akcija |
|---|---|
| A / D (ali puščici) | premikanje levo / desno |
| SPACE / W / puščica gor | skok (preskoči nizke puščice) |
| Miška | merjenje |
| Drži levi klik | napenjanje loka (močnejši strel, prikaže se začetek loka puščice) |
| Spusti levi klik | strel |
| ESC | pavza |

## Pravila
- Oba imata 100 HP. Zadetek v telo = 20 škode, v glavo = 40 škode (HEADSHOT efekt).
- Puščice letijo po balistični krivulji (gravitacija), zabodejo se v tla, stolpe in lokostrelce.
- Ko nekdo pade na 0 HP: počasen posnetek, nato **VICTORY!** ali **DEFEAT!** in gumb **PLAY AGAIN**.
- Zmage se štejejo in shranijo (PlayerPrefs).

## AI težavnosti
| | Easy | Medium | Hard |
|---|---|---|---|
| Reakcija na puščico | 0,65 s | 0,35 s | 0,14 s |
| Verjetnost izmikanja | 25 % | 55 % | 85 % |
| Napaka pri merjenju | ±6,5° | ±3,2° | ±1,3° |
| Pavza med streli | 2,4–3,6 s | 1,6–2,5 s | 0,9–1,6 s |
| Predvidevanje gibanja | ne | delno | da |

Vrednosti spreminjaš v `Difficulty.cs`.

## Datoteke
| Skripta | Kaj dela |
|---|---|
| `GameBootstrap.cs` | samodejni zagon igre ob Play |
| `GameManager.cs` | potek igre: meni → odštevanje → boj → zmaga/poraz, štetje zmag |
| `Archer.cs` | lokostrelec: premikanje, napenjanje, strel, animacije hoje/merjenja/strela, zadetki |
| `PlayerController.cs` | kontrole igralca + prikaz smeri strela |
| `AIController.cs` | AI: premikanje, izmikanje (tek ali skok), balistično merjenje z napako |
| `Arrow.cs` | let puščice, zaznavanje zadetkov (glava / telo / tla / stena) |
| `Arena.cs` | srednjeveška arena: grad, stolpi, zastave, bakle, publika |
| `GameUI.cs`, `ButtonFx.cs` | meni, izbira težavnosti, HUD, VICTORY/DEFEAT, pavza |
| `HealthBar.cs` | vrstica življenja nad lokostrelcem |
| `Fx.cs`, `CameraRig.cs` | delci, iskre, zvezdice ob headshotu, tresenje kamere |
| `Sfx.cs` | sintetizirani zvoki (lok, strel, zadetki, publika, melodiji) |
| `Art.cs` | proceduralno ustvarjeni sprite-i |
| `GameInput.cs` | podpora za novi Input System in stari Input Manager |

## Build (.exe)
File → Build Profiles → Windows → Build. Če bi bili sprite-i v buildu nevidni, v
Project Settings → Graphics → Always Included Shaders dodaj
`Universal Render Pipeline/2D/Sprite-Unlit-Default`.
