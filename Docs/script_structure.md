# Peta Script Gameplay — Maskbound Jinosi

> Sumber: hasil scan langsung `Assets/@ssets/Scripts/` (~141 file, 12 folder).
> Baseline: Unity `6000.3.16f1`, URP `17.3.0`, Cinemachine `3.1.2`, Timeline `1.8.12`, CorgiEngine + Fungus + InControl. Tanpa `.asmdef` (satu assembly `Assembly-CSharp`).

Prinsip proyek ini (dari `Docs/README.md`):

```text
Corgi Engine mengurus platformer, Maskbound mengurus aturan game.
```

---

## 1. Diagram Modul Besar

```mermaid
graph TD
    subgraph Framework["Framework (vendor, jangan diedit)"]
        Corgi[CorgiEngine<br/>Character / Health / Weapon / AIBrain]
        Fungus[Fungus<br/>Flowchart / Block / Say]
        InControl[InControl<br/>InputDevice]
    end

    subgraph Maskbound["Scripts Maskbound (@ssets/Scripts)"]
        Input[Input/<br/>binding + bridge]
        Skills[Skills/<br/>data + caster + slot + input]
        Combat[Combat/<br/>senjata + damage window + hitstop]
        AI[AI/<br/>otak boss + spawner skill]
        Gameplay[Gameplay/<br/>dialog trigger + scene + toggles]
        UI[UI/<br/>shop + HUD + overlay]
        Soul[Soul/<br/>wallet + pickup]
        Stats[Stats/<br/>stat player]
        Breakables[Breakables/<br/>prop hancur + drop soul]
        FX[FX/<br/>VFX facing + destroy]
        Debug[Debug/ + Editor/<br/>test hub + prefab builder]
    end

    Input --> Skills
    Skills --> Combat
    Skills --> Soul
    AI --> Combat
    Gameplay --> Skills
    Gameplay --> Fungus
    Gameplay --> Corgi
    UI --> Skills
    UI --> Soul
    Breakables --> Soul
    Stats --> Skills
    Combat --> Corgi
    AI --> Corgi
```

---

## 2. Alur Skill Player (4 jalur input → 1 caster)

```mermaid
flowchart LR
    subgraph InputJalur["4 jalur input (semua manggil slot/caster)"]
        SEL[CharacterSkillSelectionInput<br/>LB/RB pilih, LT/Q aktif]
        KEY[CharacterSkillKeyboardInput<br/>tombol 1-4 langsung]
        JUMP[CharacterJumpSkillInput<br/>tombol Jump, khusus JejakSukma]
        RELAY[SkillAnimationEventRelay<br/>event dari animasi]
    end

    CASTER[CharacterSkillCaster<br/>CanCast / Cast / cooldown / animator]
    SLOTS[SkillSlotManager<br/>6 slot: 0-2 active, 3-5 passive]
    DATA[ActiveSkillData / PassiveSkillData<br/>ScriptableObject]
    FXS[Effects/: Projectile / Hitbox /<br/>Burst / GroundImpact / Timed]
    PAS[PlayerPassiveSkillController<br/>modifier damage/cooldown/speed]
    SAVE[SkillSaveStore<br/>PlayerPrefs antar scene+sesi]

    SEL --> CASTER
    KEY --> CASTER
    JUMP --> SLOTS
    RELAY --> CASTER
    CASTER --> SLOTS
    SLOTS --> DATA
    CASTER --> FXS
    DATA --> PAS
    SLOTS --> SAVE
```

Catatan penting:

- Boss **tidak** lewat jalur ini. Boss pakai Corgi `Weapon` + Animation Event (lihat §3).
- Disable skill player = matikan **4 komponen**: `CharacterSkillCaster` + 3 input (`Selection`, `Keyboard`, `Jump`). Lihat `PlayerControlToggles.DisableSkills()` dan `BossFightTrigger.DisablePlayerSkills()` — keduanya harus sinkron.

---

## 3. AI Boss PrabuKlana (state + pemilih senjata)

```mermaid
stateDiagram-v2
    [*] --> Chase
    Chase --> Attack : Jarak <= 16<br/>(AIDecisionDistanceToTarget)
    Attack --> Chase : AttackDone / ShouldChase<br/>(AIDecisionPhaseAttackDone)
    Chase --> Die : HP 0
    Attack --> Die : HP 0

    state Attack {
        [*] --> PilihSenjata
        PilihSenjata --> Attack1 : jauh, P1/P3
        PilihSenjata --> Attack2 : dekat <=14.5, P1/P2/P3
        PilihSenjata --> RainHammer : jauh, P2/P3 (HP<=0.7)
        PilihSenjata --> LaserBeam : jauh, P3 random (HP<=0.5)
        PilihSenjata --> MaskRage : dekat, P3 random
    }
```

```mermaid
flowchart TD
    BRAIN[AIBrain<br/>Chase / Attack / Die] --> PHASE[AIActionPhaseAttack<br/>pilih Weapon by HP% + jarak]
    PHASE --> W1[MeleeAttack1/2<br/>AnimationEventMeleeWeapon]
    PHASE --> W2[RainHammer<br/>AnimationEventBossSkillPointSpawner]
    PHASE --> W3[LaserBeam / MaskRage<br/>SkillProjectileSpawner]
    PHASE --> TEL[TeleportBehindPlayer<br/>event animasi MaskRage]
    W1 --> RELAY[MeleeDamageWindowAnimationRelay<br/>Open/CloseDamageWindow]
    W2 --> HAMMER[GroundImpactSkillObject2D<br/>+ HammerRainBombDamageable]
    W3 --> MASK[MaskRageProjectileController<br/>idle 5 dtk / kena sentuh → Explode]
    HAMMER --> STUN[BossStunReceiver<br/>StunFor 7 dtk]
    MASK --> DIE[BossDeathBrainState<br/>+ AIActionBossDeathCameraShot]
```

---

## 4. Sequence Dialog Intro Boss (skill mati selama dialog saja)

```mermaid
sequenceDiagram
    participant T as BossFightTrigger
    participant P as Player (Character.Freeze)
    participant S as Skill player (caster+3 input)
    participant TL as Intro Timeline (director)
    participant F as Fungus Flowchart
    participant B as Boss (AIBrain+Damage)

    T->>B: Start: FreezeBoss (AI+damage off)
    T->>T: ActivateTrigger (signal timeline / chain dialog)
    T->>P: FreezePlayer + ForcePlayerIdle
    T->>S: DisablePlayerSkills (caster + selection + keyboard + jump)
    T->>TL: PauseIntroTimeline
    T->>F: ExecuteBlock(BossIntro)
    F-->>T: HasExecutingBlocks == false (dialog selesai)
    T->>S: RestorePlayerSkills
    T->>P: UnfreezePlayer
    T->>B: UnfreezeBoss → TransitionToState(Chase)
    T->>TL: ResumeIntroTimeline
```

---

## 5. Persistensi (PlayerPrefs)

```mermaid
graph LR
    subgraph Save["Disimpan di PlayerPrefs"]
        SKILL[SkillSaveStore<br/>owned + layout 6 slot]
        TUT[PlayerControlToggles<br/>unlock per-ability]
        DLG[NPCDialogTrigger / BossFightTrigger<br/>once-flag dialog]
    end
    NG[New Game] -->|DeleteAll| Save
    SKILL --> SLOT[SkillSlotManager.Awake<br/>RestoreFromSaveStore]
    TUT --> LOCK[ApplyStartLock + MaintainLock<br/>tiap frame]
    DLG --> SKIP[skip bila flag == 1]
```

---

## 6. Isi Tiap Folder (file → peran satu baris)

### 6.1 `AI/` — 13 file, otak + alat cast boss

| File | Peran |
|---|---|
| `AIActionPhaseAttack.cs` | Otak utama: pilih Weapon by HP% + jarak |
| `AIDecisionPhaseAttackDone.cs` | Syarat keluar state Attack |
| `AIActionMoveToBossSkillPoint.cs` + `AIDecisionBossSkillPointReached.cs` | Navigasi ke skill point (cadangan, tidak dipakai di state aktif) |
| `AIActionCallDialogTrigger.cs` | AI memanggil dialog trigger lain |
| `AIActionToggleDamageOnTouch.cs` | Nyalakan/matikan damage dari AI state |
| `AnimationEventBossSkillPointSpawner.cs` | Cast RainHammer dari animation event |
| `AnimationEventTeleportBehindPlayer.cs` | Teleport MaskRage ke belakang player |
| `BossSkillSpawnPointGroup.cs` | Registry 5 titik spawn `prabu_klana_skill` |
| `BossSkillPointGroupFacingMirror.cs` | Mirror spawn point saat boss balik arah |
| `BossStunReceiver.cs` | Terima weakness dari bom → stun AI |
| `BossDeathBrainState.cs` | Paksa transisi ke state Die |
| `AIActionBossDeathCameraShot.cs` | Sinematik mati + kembali ke menu |

### 6.2 `Skills/` — 20 file inti + `Effects/` (12) + `Passives/` (6)

| File | Peran |
|---|---|
| `Skill.cs` / `SkillType.cs` | Base ScriptableObject + enum Active/Passive |
| `ActiveSkillData.cs` | Data skill aktif (prefab, cooldown, SFX, flag Jump-button) |
| `PassiveSkillData.cs` + `PassiveEffectData.cs` | Data pasif + efeknya |
| `SkillSlot.cs` / `SkillSlotManager.cs` | 1 slot + manager 6 slot (equip/unequip/activate) |
| `SkillContext.cs` / `SkillRuntimeContext.cs` / `ISkillRuntimeReceiver.cs` | Konteks equip + konteks runtime untuk efek |
| `CharacterSkillCaster.cs` | Cast, cooldown global, kunci gerakan, animator |
| `CharacterSkillSelectionInput.cs` | Pilih via LB/RB, aktif via LT/Q |
| `CharacterSkillKeyboardInput.cs` | Aktif langsung via 1-4 (sekarang disabled di prefab) |
| `CharacterJumpSkillInput.cs` | Cast via tombol Jump (JejakSukma) |
| `SkillAnimationEventRelay.cs` | Jembatan animation event → caster |
| `SkillSaveStore.cs` | Save/load owned + layout slot |
| `SkillCooldownFeedback.cs` / `SkillSelectionIconFeedback.cs` / `SkillIconFloatingPopup.cs` / `CooldownFloatingText.cs` | Feedback cooldown + seleksi |
| `PlaceholderActiveSkill.cs` / `PlaceholderPassiveSkill.cs` | Skill dummy untuk tes/dev |

`Effects/` (12): `SkillProjectile2D`, `SkillHitbox2D`, `SkillBurstSpawner2D`, `SkillProjectileSpawner`, `SkillSummonObject`, `TimedSkillObject`, `GroundImpactSkillObject2D` (+AnimationRelay), `HammerRainBombDamageable`, `MaskRageProjectileController`, `JejakSukmaJumpEffect`, `AnimationEventVFXSpawner` — semua pelaku efek yang di-spawn caster/boss.

`Passives/` (6): `PlayerPassiveSkillController` (orkestrasi modifier), `ConsecutiveHitPassiveEffect`, `HealOnHitPassiveEffect`, `StatModifierPassiveEffect`.

### 6.3 `Combat/` — 18 file, senjata + damage

| Kelompok | File |
|---|---|
| Base & relay boss | `AnimationEventMeleeWeapon.cs`, `MeleeDamageWindowAnimationRelay.cs`, `DamageOnTouchAnimationRelay.cs`, `AnimationEventLunge2D.cs` |
| Player | `MaskboundSpecialAttackAbility.cs` + `MaskboundSpecialAttackData.cs` + `MaskboundSpecialHitbox.cs`, `CharacterBlock.cs`, `CharacterMeditation.cs`, `SingleAirAttackLimiter.cs`, `WeaponFacingTransformSync.cs` |
| Feedback | `HitstopTrigger.cs`, `MeleeWeaponHitstop.cs`, `DamageNumberManager.cs` + `DamageNumberPopup.cs`, `FaceDamageDirectionOnHit.cs` |
| Debug | `MaskboundMeleeWeaponStateDebug.cs`, `MaskboundWeaponDirectInputDebug.cs` |

### 6.4 `Gameplay/` — 20 file

- `Dialogue/` (5): `BossFightTrigger.cs` (intro + freeze + dialog + start fight), `NPCDialogTrigger.cs` (dialog NPC + once-flag + chain), `FungusPlayAnimation.cs`, `GameOverFungusBridge.cs`, `TutorialViews.cs`
- `Scene/` (7): `GameFlowManager.cs`, `BootstrapSceneLoader.cs`, `FountainInteractable.cs` (buka shop), `MapTransitionDoor.cs` (pintu antar scene), `SpiritualSoulTarget.cs`, `SceneBgmManager.cs`, `PlayerGameOverOverlayController.cs`
- Root (8): `PlayerControlToggles.cs` (toggle per-ability + tutorial lock + context-menu tes), `LandingMovementLock.cs`, `LocomotionAnimationSpeedSync.cs`, `MaskboundLandingAnimatorTrigger.cs`, `CharacterFacingVisualOffset.cs`, `PlayerDeathKnockback.cs`, `HitstopBootstrap.cs`, `DemoBossChallengeTimer.cs`

### 6.5 `UI/` — 20 file

| File | Peran |
|---|---|
| `SkillShopPanel.cs` | Shop buy/equip (polling F/A di Update, belum onClick) |
| `SkillCooldownSlotUI.cs` | Tampilan slot + cooldown (display only) |
| `BossHealthTarget.cs` + `BossHealthUIBinder.cs` | Data nama boss + binder bar HP |
| `BossVictoryOverlay.cs` / `GameOverOverlay.cs` / `OverlayConfirmInput.cs` | Overlay menang/mati + konfirmasi input |
| `HealthProgressBarDisplay.cs` / `HealthSpriteStateDisplay.cs` / `HealthTextDisplay.cs` | Tampilan HP (3 varian) |
| `SoulTextDisplay.cs` / `LivesUIBinder.cs` / `InteractionPrompt.cs` | Teks soul, nyawa, prompt interact |
| MainMenu (`MainMenuController`/`Page`/`FolderButton`), `InControlMenuSelectionInput.cs`, `CreditPager.cs`, `TutorialUIOpener.cs`, `InputTutorialTextDisplay.cs` | Menu + tutorial UI |

### 6.6 `Input/` — 6 file

`MaskboundInControlInputManager.cs`, `MaskboundInControlWeaponInput.cs`, `MaskboundInputBindings.cs` (+ `.asset`), `MaskboundBasicAttackInputBridge.cs`, `InControlDuplicateDestroyer.cs`, `MaskboundInputInspectorMonitor.cs` — satu jalur input InControl untuk Player1.

### 6.7 Pendukung — `Soul/` (3), `Stats/` (2), `Breakables/` (4), `FX/` (4)

- `Soul/`: `SoulWallet.cs`, `SoulPickup.cs`, `SoulCurrencyData.cs`
- `Stats/`: `CharacterStats.cs`, `CharacterStatData.cs` (+ `.asset` player default)
- `Breakables/`: `BreakableObject.cs` (base: HP + flash + drop soul acak), `BreakableStoneObject.cs`, `BreakableSoulObject.cs`, `BreakableBurstEffect.cs`
- `FX/`: `FacingSpawnPoint2D.cs`, `SpawnedVFXFacing2D.cs`, `AnimatorIntOnAwake.cs`, `DestroyAfterAnimation.cs`

### 6.8 `Debug/` (9+1) + `Editor/` (2)

`DevTestHub.cs`, DevModMenu (`Controller`/`Page`/`FolderButton`/`ActionButton`/`ToggleItem`), `DevTestPanelController.cs`, `DevTestStatusDisplay.cs`, `EnemyDistanceDebugVisualizer.cs`; Editor: `SkillShopPrefabBuilder.cs` (bangun + wiring prefab shop), `BreakableSaveIdAssigner.cs`, `DevModMenuBootstrapBuilder.cs`, `MainMenuBootstrapBuilder.cs`.

---

## 7. Aturan Arsitektur (dari skill unity-architecture)

- **Tier proyek:** long-lived (bukan prototipe) — 141 file, save antar sesi, banyak sistem saling kunci.
- **Data ownership:** authored config di `ScriptableObject` (`Skills/*.asset`, `Soul_DefaultCurrency`, `Player_DefaultStats`, `MaskboundInputBindings`); runtime state di komponen + `SkillSaveStore`; scene object hanya komposisi.
- **Komunikasi:** direct ref untuk parent→child yang pasti ada; event (`SkillEquipped`, `MMDamageTakenEvent`, `CorgiEngineEvent.LevelStart`) untuk antar sistem; Timeline Signal → public method untuk sinematik; Fungus `Call Method` untuk trigger dari dialog (jangan bikin custom command baru bila built-in cukup).
- **Bootstrap:** player di-spawn runtime oleh LevelManager — semua skrip wajib auto-find + guard `null` + tunggu `AbilityInitialized`, bukan drag-and-drop di Inspector.
- **Risiko performa (hot path):** `MaintainLock()` tiap frame (murah, hanya cek `enabled`), `PlayerPassiveSkillController.Update()` (tick semua runtime), polling input di `Update()` shop/selection — ketiganya O(kecil), aman untuk target PC.
- **Do now / skip now:** pertahankan pola yang ada (atomic `DisableX/EnableX` + wrapper, context-menu Testing, field Inspector opt-in default OFF). Jangan tambah framework/layer baru; jangan pecah asmdef sebelum ada kebutuhan build yang nyata.

## 8. Risiko & Inkonsistensi yang Diketahui

1. Dua jalur disable skill tidak sinkron (`PlayerControlToggles` vs `BossFightTrigger`) — `BossFightTrigger` baru dilengkapi caster (perubahan terakhir).
2. `BuyButton` + entry shop `onClick` kosong — interaksi hanya keyboard/gamepad.
3. Keyboard 1-4 di-disable di prefab tapi kodenya masih hidup — putuskan: hapus atau pertahankan sebagai fallback.
4. Tanpa `.asmdef` — kompilasi melambat seiring file bertambah.

## 9. Unknowns (perlu konfirmasi)

- Apakah 4 jalur input skill memang disengaja dipertahankan semua?
- Scene mana pakai tutorial lock vs hanya `BossFightTrigger`?

## 10. Cara Baca Lanjutan

1. Mau paham shop → `UI/SkillShopPanel.cs` (`TryBuySelected`, `EquipSelectedSkill`, `SelectIndex`).
2. Mau paham cast → `Skills/CharacterSkillCaster.cs` (`CanCast`, `Cast`, `ActivateSkillSlot`).
3. Mau paham boss → `AI/AIActionPhaseAttack.cs` (`PerformPhaseAttack`, `PickRandomPhase3Weapon`).
4. Mau paham intro → `Gameplay/Dialogue/BossFightTrigger.cs` (`ActivateTrigger`, `DisablePlayerSkills`, `PlayDialog`, `EndSequence`).
5. Data skill → `ScriptableObjects/Skills/` (4 active + 6 passive + 7 effect).
