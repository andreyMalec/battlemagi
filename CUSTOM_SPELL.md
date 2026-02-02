[How to](HOW_TO.md)

### 1️⃣ Core (ядро) — “что вообще кастуется”

Обязательный модуль (1 на заклинание)

- #### Projectile — летящий снаряд

  Отдельный entity, летящий во времени.

  Spawn → Update → Despawn

  Имеет position, velocity, lifetime

  Коллизии
    + Raycast per frame или
    + Rigidbody + collider

  Типовые триггеры
    + OnSpawn
    + OnHit
    + OnPierce
    + OnBounce
    + OnLifetimeEnd

- #### Beam — луч

  Непрерывный эффект между кастером и точкой.

  Живёт пока удерживается каст

  Не имеет velocity

  Коллизии
    - Continuous raycast
    - Может быть multi-hit per tick

  Типовые триггеры
    - OnBeamStart
    - OnBeamTick
    - OnBeamEnd
    - OnTargetEnter
    - OnTargetExit

- #### Zone — область на земле

  Область в мире, действующая во времени.

  Spawn → Persist → Expire

  Обычно статична или медленно движется

  Коллизии

    - Overlap checks
    - Spatial queries

  Типовые триггеры

    - OnZoneCreate
    - OnEnter
    - OnStay
    - OnExit
    - OnZoneExpire

- #### SelfCast — на себя
  Эффект без сущности в мире.

  Выполняется сразу

  Не имеет update loop

  Коллизии
    - Нет (или моментальный sphere check)

  Типовые триггеры
    - OnCast
    - OnAfterDelay
- #### Summon — Полноценная сущность
  Spawn → Think → Act → Die

  Коллизии
    - Как у обычного персонажа

  Типовые триггеры
    - OnSummon
    - OnSummonHit
    - OnSummonDeath
    - OnOwnerDeath

### 2️⃣ Shape — “как это доставляется” (реализация зависит от Core)

Правило пространственного распределения воздействия во времени

#### 1️⃣ Projectile

- Linear
- Arc
- Homing
- Spiral
- Bounce
- Split
- Wave

`Shape = Update(position, time)`

#### 2️⃣ Beam

У Beam нет velocity, но есть:

- origin
- direction
- length
- sampling

Примеры Beam Shape:

- Straight beam
- Cone
- Sweeping beam (вращается)
- Pulsed beam (вкл/выкл по тикам)
- Forked beam (раздвоение)
- Chain beam

`BeamShape.sampleRays(t) → List<Ray>`

#### 3️⃣ Zone

Примеры Zone Shape:

- Circle
- Ring
- Donut
- Line strip
- Expanding circle
- Moving zone
- Fractal / random pockets

Shape управляет:

- формой overlap
- ростом/сжатием
- перемещением

`ZoneShape.contains(point, t)`

#### 4️⃣ Self

Здесь Shape — это pattern применения:
Примеры:

- Self → Forward cone
- Blink → Line cast
- Shockwave → Radial wave

Это скорее:

- spatial selector
- target pattern

### 3️⃣ Payload — “что происходит при попадании”

🩸 Combat Effects

- Deal damage
- Apply DOT / HOT
- Apply CC (slow, root, silence)
- Knockback / pull
- Break shields

🔥 Spell Mutation (изменение заклинания)

- Spawn secondary projectile
- Split projectile
- Change element
- Increase radius
- Convert damage type
- Extend duration

🧠 Resource / Risk

- Restore mana
- Drain mana
- Deal self-damage
- Increase overheat
- Reduce cooldown
- Lock spell temporarily

🧿 State / Tag Effects

- Apply status (Burn, Marked, Echo)
- Remove status
- Add stack
- Consume stacks

### 4️⃣ Modifiers — “как именно это работает”

- +Radius / −Speed
- +Crit chance
- Damage scales with distance
- Damage scales with missing HP
- Cast time → charge
- Consumes HP instead of mana

### 5️⃣ Triggers / Conditions — глубина 💀

Trigger — когда проверяем. Это событие. Оно просто говорит: «что-то произошло»

- OnHit
- OnKill
- OnCastStart
- OnCastEnd
- OnProjectileBounce
- AfterDelay(0.5s)
- EveryTick
- OnVoicePeak
- OnOwnerDamaged

Condition — применять или нет

- Target has Burn
- Distance > 15m
- Target HP < 30%
- Caster is airborne
- Mana > 50%
- Combo counter ≥ 3
- Voice loudness > threshold

## 1

## 2

## 3

## 4

## 5

