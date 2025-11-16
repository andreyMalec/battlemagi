# How to

## Добавить заклинание

1. Добавить картинку для книги заклинаний в папку [Assets/Prefabs/Book/](Assets/Prefabs/Book/) по шаблону
2. Создать папку [Assets/Prefabs/Spell/$Название Заклинания](Assets/Prefabs/Spell/)`
3. Создать префаб для основного заклинания с компонентом `BaseSpell`. Вместо наследования используем композицию
4. Создать новый экземпляр SpellData
5. Заполнить поля SpellData: <strong>*</strong> - обязательные поля
    - id - <strong>*</strong> порядок в `SpellDatabase`
    - name - <strong>*</strong> название в книге заклинаний
    - description - <strong>*</strong> описание в книге заклинаний
    - spellWords - <strong>*</strong> тригер слова для распознавания
    - manaCost - <strong>*</strong> стоимость маны (шаг 25)
    - bookImage - <strong>*</strong> картинка в книге заклинаний
    - mainSpellPrefab - <strong>*</strong> основной префаб
    - spellInHandPrefab - префаб для превью в руке (компонент `SpellInHand`)
    - spellBurstPrefab - префаб для эффекта при касте (появляется только локально у заклинателя)
    - impactPrefab - префаб для эффекта при столкновении (`NetworkObject`)
    - impactEffects - список эффектов при столкновении (наследники `ImpactEffect`; `GroundImpactEffect` и
      `VisualImpactEffect` исключают друг друга)
    - buffs - список баффов, накладываемых на заклинателя при касте (наследники `StatusEffectData`)
    - damageSound - звук нанесения урона (добавить новый AudioClip в `AudioManager`)
    - castWaitingIndex - 0 - обычный в левой руке, 1 - обе руки (для стрельбы)
    - invocationIndex - номер анимации в `Animator` на слое _Casting_
    - clearInHandBeforeAnim - очищать ли руку перед анимацией каста (по-умолчанию очищается после)
    - previewMainInHand - рисует первый MeshRenderer из mainSpellPrefab в точке спавна при показе в руке
    - disableWhileCarrying - отключать ли заклинание, когда персонаж несет флаг
    - echoCount - количество эхо (повторений без затрат маны)
    - instanceLimit - максимальное количество одновременно существующих mainSpellPrefab этого заклинания
    - lifeTime - время жизни mainSpellPrefab в секундах
    - baseDamage - базовый урон заклинания
    - canSelfDamage - может ли заклинание наносить урон своему заклинателю и его команде
    - useParticleCollision - использовать ли столкновения частиц для срабатывания попаданий
    - isDOT - наносит ли заклинание урон с течением времени
    - tickInterval - интервал между тиками урона с течением времени
    - hasAreaEffect - имеет ли заклинание урон по области
    - areaRadius - радиус урона по области (урон падает от центра к краям) (влияет на радиус отбрасывания)
    - knockbackForce - сила отбрасывания при столкновении
    - isProjectile - является ли заклинание снарядом (влияет только на поведение других полей)
    - piercing - умирает ли снаряд при столкновении
    - baseSpeed - базовая скорость снаряда
    - projCount - количество снарядов при касте
    - multiProjDelay - задержка между снарядами при множественной стрельбе
    - spawnMode - режим появления при множественных снарядах (применяет _multiProjDelay_)
    - arcAngleStep - угол между снарядами при режиме появления по дуге
    - raycastMaxDistance - максимальная дистанция луча при режиме появления с применением raycast
    - isBeam - режим перемещения - привязан к кастеру
    - isHoming - режим перемещения - автонаводка на ближайшую цель
    - homingRadius - радиус поиска цели для автонаводки
    - homingStrength - сила поворота для автонаводки
    - isChanneling - затраты маны применяются каждую секунду. Запрещает применять другие заклинания во время каста
    - channelDuration - максимальная длительность каста в секундах
6. Добавить созданный SpellData в `SpellDatabase` в нужный индекс
7. Добавить созданный SpellData в нужный ArchetypeData

## Добавить архетип

1. Создать папку [Assets/Player/Class $Название Архетипа](Assets/Player/)`
2. Создать префаб тела с компонентом `MeshController` и `Animator`
3. Добавить объект Invocation в левую руку модели
4. Включить Read/Write для модели в настройках импорта
5. Добавить `MeshBody` компонент к объекту _Renderer_ тела
6. (Опционально) Добавить `MeshCloak` компонент к объекту _Renderer_ плаща
7. Для всех Collider добавить компонент `ChildCollider` и применить физический материал _Body_
7. Отключить RB для ступней в `MeshController`
8. Применить настройки для Rig
9. Применить Ragdoll Builder к префабу тела (масса 2000)
10. Добавить материал _RuneOnPlayer_ к телу
11. Добавить шейдер тела с применением цвета
12. (Опционально) Добавить шейдер плаща с применением цвета
13. Создать новый экземпляр ArchetypeData
    - id - порядок в `ArchetypeDatabase`
    - archetypeName - название архетипа
    - avatarPrefab - префаб тела
    - bodyShader - шейдер тела с применением цвета
    - cloakShader - шейдер плаща с применением цвета
    - spells - список заклинаний
    - cameraOffset - дополнительное смещение камеры
14. Добавить созданный ArchetypeData в `ArchetypeDatabase` в нужный индекс
15. В префабах _LobbyMemberItem_ и _ScoreboardItem_ добавить иконку архетипа в список спрайтов под соответствующим индексом