# BIOFALL — что сделано

Top-down зомби-шутер. Unity 6 (URP), без asmdef, namespaces `Biofall.Core / Gameplay / UI`.
Архитектура: EventBus (события), PoolService (пулинг), PlayerRegistry, ScriptableObject-данные, SOLID.

## Сцены и поток
- **Boot → MainMenu → Test** (геймплей). Есть копия `Blockout` с greybox-уровнем (платформы/рампы/стены) и ~75 зомби.
- Главное меню (Play / Credits «Bekbolat Aldiyarov» / Exit), стиль Alien Shooter (тёмный фон + циановые рамки).
- Пауза по Escape (Resume / Restart / Main Menu), экран Game Over (Restart / Main Menu).

## Игрок
- Движение WASD, прицел мышью, top-down камера (наклон 60°, follow + lookahead, тряска при уроне).
- HUD: HP, патроны, FPS, Bio Samples («BIO N»), бордовая виньетка урона/низкого HP.
- Смерть игрока: анимация + блок управления + Game Over.

## Оружие (арсенал)
- Переключение **1 = пистолет, 2 = M4**, у каждого свои патроны (12/48 и 30/120), HUD показывает активное.
- Пистолет — одиночный; **M4 — клик одиночный, зажатие очереди по 3**, урон 12/пуля.
- Хитскан по прицелу, трейсер-пуля и **muzzle flash** из пула, звуки выстрела/перезарядки.
- Rifle-анимации игрока (idle/run/back/reload/fire) на базовом слое по параметру `Weapon`.

## Зомби
- 50 HP, погоня (гибрид: стиринг + NavMesh при препятствии), атака по событию анимации, смерть (анимация + звук).
- Пул, спавн **вне обзора** камеры, boids-расталкивание (не слипаются), случайные тихие гроулы.
- Реакция на попадание: **кровь-брызги** (партиклы), вспышка материала, отброс. Дроп: патроны (шанс) + Bio Samples.
- Архитектура готова к 100–150 (один общий Update-цикл в `EnemyManager`).

## Экономика
- Bio Samples: дроп с зомби → подбор → счётчик на HUD. (Пока без траты — следующий шаг: апгрейды.)

## Атмосфера
- Мрачный URP post-process (vignette, color grading, bloom), туман, тёмный скайбокс — «мрачно, но видно».
- **Фонарик** игрока: конус светит туда, куда целишься.
- **Дождь** в Test (партиклы над игроком, симуляция в world).

## Ключевые ассеты
- Данные: `WD_Pistol`, `WD_M4`, `EN_Zombie` (ScriptableObjects).
- VFX-префабы (в `Assets/Prefabs/`): `VFX_MuzzleFlash`, `VFX_BulletTracer`, `VFX_BloodSplatter`, `Pickup_Ammo`, `Pickup_BioSample`.
- Аниматоры: `CharacterController` (игрок, +rifle), `EnemyController` (зомби).

## Дальше (идеи)
Волны + счёт, трата Bio Samples на апгрейды, ещё оружие (Shotgun/AR), реальный уровень.
