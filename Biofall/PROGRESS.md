# BIOFALL — что сделано

Top-down зомби-шутер. Unity 6 (URP), namespaces `Biofall.Core / Gameplay / Net / UI`, код в `Assets/Code/`.
Архитектура: EventBus (события), PoolService (пулинг), PlayerRegistry, ScriptableObject-данные, SOLID.
Режимы: **SOLO** (миссия + волновой), **CO-OP** (Netcode for GameObjects, LAN).

## Сцены и поток
- **Boot → MainMenu → геймплей.** Геймплейные сцены: `Mission_1`, `Mission_1_Coop`, `CoopArena`, `CoopTest`, `SOLO/Mission_1 SOLO`, `SOLO/WaveMode`.
- Главное меню (Play / Credits «Bekbolat Aldiyarov» / Exit), стиль Alien Shooter (тёмный фон + циановые рамки), пульсация заголовка, музыка меню.
- Пауза по Escape (Resume / Restart / Settings / Main Menu), настройки, экран Game Over, экран завершения миссии.

## Игрок
- Движение WASD, прицел мышью, top-down камера (наклон, follow + lookahead, тряска при уроне).
- HUD: HP, патроны, гранаты, FPS, Bio Samples, цели миссии, прогресс-бар, прицел, бордовая виньетка урона/низкого HP.
- Фонарик (конус по направлению прицела). Смерть: анимация + блок управления + Game Over.

## Оружие и бой
- Переключение **1 = пистолет, 2 = M4, 3 = SG-12 (дробовик)**, раздельные патроны, HUD показывает активное.
- Пистолет — одиночный; **M4 — клик одиночный, зажатие очереди**; **SG-12 — одиночный с разбросом** (8 дробин конусом за 1 патрон, поля `pelletsPerShot`/`spreadAngle` в `WeaponData`, цикл рейкастов в `Weapon.TryFire`). Хитскан по прицелу, трейсер-пуля, muzzle flash из пула, звуки выстрела/перезарядки.
- **Гранаты**: бросок по прицелу, инвентарь с ёмкостью, взрыв (AoE-урон + VFX), подбор гранат с дропа.
- Стратегии огня вынесены в `IFireStrategy` (SingleFire и т.д.).

## Враги (5 типов)
- **Zombie** (база, 50 HP), **Runner** (быстрый), **Tank** (толстый), **Screamer** (крик — AoE-волна `ScreamWaveAttack`/`ScreamWaveVFX`), **Spitter/зонёр** (стационарный, moveSpeed 0 + дальний attackRange; плюётся кислотой). Данные в `EN_*` ScriptableObjects.
- **Spitter** (`SpitterData`/`SpitAcidAttack`): по тому же animator-хуку "Attack", что и Screamer, **плюётся летящим глобом** (`AcidProjectile`/`Acid_Projectile.prefab` — светящийся зелёный шар, летит дугой от пасти в игрока), который при попадании разливается в **лежащую кислотную лужу** (`AcidPool` + шейдер `Biofall/AcidPool` — едкая зелёная зона с DoT, server-авторитетный урон, ложится на пол по ногам цели). Модель Screamer с болотным материалом `M_SpitterBody` (эмиссия), глоб — `M_AcidGlob`. Префабы `Spitter`, `Decal_AcidPool`, `Acid_Projectile`. Всё data-driven через `EN_Spitter` — одинаково во всех режимах.
- Погоня (гибрид: стиринг + NavMesh при препятствии), атака по событию анимации, смерть (анимация + звук).
- Пул, спавн вне обзора камеры, boids-расталкивание, health-bar над врагом.
- Реакция на попадание: кровь-брызги (партиклы), вспышка материала, отброс, **лужи крови-декали** (`BloodPool`). Дроп: патроны/аптечки/гранаты (шанс) + Bio Samples через `LootService` + `LootConfig`.
- Общий Update-цикл в `EnemyManager`. Спавнеры: `EnemySpawner`, `WaveSpawner` (волны), `CoopEnemySpawner` (сетевой).

## Миссия (Mission_1)
- Фазы (`MissionDirector` + EventBus): **Найти генератор → активировать маяк → оборона маяка (волны) → эвакуация**.
- Интерактивные объекты (`IInteractable`): `GeneratorStation`, `BeaconStation`, `ExtractionPoint`; подсказка взаимодействия в HUD.
- Работает в SOLO и CO-OP (на сервере волны гонит `CoopEnemySpawner`).

## Волновой режим (WaveMode)
- Отдельная сцена `SOLO/WaveMode` + `WaveSpawner` + `WaveHud` + `HUD_WaveMode.prefab`. Эндлесс-волны.
- Набор врагов нарастает по волнам: зомби (всегда), раннеры с волны 2, **скримеры и Spitter'ы с волны 3** (Spitter — до 4 одновременно), танки с волны 4. Стартовые волны/капы настраиваются в `WaveSpawner`.

## Экономика и мета-прогрессия
- Bio Samples: дроп с врагов → подбор → `CurrencyWallet` (HUD-счётчик) → при завершении миссии `RunSampleBanker` кладёт всё в банк.
- **Магазин апгрейдов** (`UpgradeShopUI`/`UpgradeRowUI`): тратим банк на 6 статов — `MaxHealth, MoveSpeed, HealthRegen, ReviveSpeed, GrenadeCapacity, PickupRadius`. Тиры со стоимостью в `UpgradeData`, каталог в `Resources/UpgradeCatalog`.
- Сохранение через **PlayerPrefs** (`PlayerProgression`): банк + уровни апгрейдов персистятся между запусками. Есть сброс прогресса.

## Co-op (Netcode for GameObjects)
- Стек: `com.unity.netcode.gameobjects` 2.12 + Multiplayer Center. Хостинг + **LAN-дискавери** (`LanDiscovery`).
- `NetworkBootstrap`, `CoopSession`, `NetSession` (InCoop/IsServer), сетевые `CoopPlayer`, `CoopEnemy`, `CoopPickup`, `CoopLootService`, `CoopMission`.
- Синхронизация: `ClientNetworkTransform`, `OwnerNetworkAnimator`. **Механика downed/revive**: `CoopPlayerLife`, `CoopReviveInteractor`, UI `CoopDownedUI`/`CoopDownedMarker`, `CoopSquadHUD` (статус сквада). Dev-HUD: `CoopDevHud`.
- Co-op префабы-варианты всех врагов и пикапов в `Assets/Prefabs/Net/`.

## Атмосфера
- Мрачный URP post-process (vignette, color grading, bloom), туман, тёмный скайбокс. Дождь (партиклы, `WeatherFollow`). Игровая/меню музыка.

## Ключевые ассеты
- Оружие: `WD_Pistol`, `WD_M4`, `WD_SG12`. Враги: `EN_Zombie`, `EN_Runner`, `EN_Tank`, `EN_Screamer`, `EN_Spitter`. Апгрейды: `UPG_*` (6 шт).
- Префабы: `Assets/Prefabs/{Weapon,Enemies,GameProps,Player,Net}/`. VFX: muzzle flash, tracer, blood splatter, blood pool decal, explosion, scream wave.
- Аниматоры игрока (+rifle) и зомби.

## Дальше (идеи)
Счёт/рекорды в WaveMode, больше оружия (AR и т.д.), больше миссий, баланс волн, полировка co-op (squad HUD, сетевой лаг), реальный арт-уровень вместо greybox.
