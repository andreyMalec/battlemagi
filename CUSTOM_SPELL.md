[How to](HOW_TO.md)

### 1 Core (ядро) — “что вообще кастуется”

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

### 2 Transform — “как это распространяется” (реализация зависит от Core)

- Linear
- Arc
- Homing
- Spiral
- Bounce
- Split
- Wave

Transform управляет:

- ростом/сжатием
- перемещением

### 2.5 Shape — “какой это обладает” (реализация зависит от Core)

#### 1️⃣ Projectile

- Capsule
- Sphere

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

#### 3️⃣ Zone

Примеры Zone Shape:

- Circle
- Ring
- Donut
- Line strip

Shape управляет:

- формой overlap

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

### 3 Payload — “что происходит при попадании”

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

### 4 Modifiers — “как именно это работает”

- +Radius / −Speed
- +Crit chance
- Damage scales with distance
- Damage scales with missing HP
- Cast time → charge
- Consumes HP instead of mana

### 5 Triggers / Conditions — глубина 💀

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

внешка

- Огонь
    - огненный - взрывается при ударе (1 раз урон)
    - пламенный - оставляет горящую зону (dot)
- Лёд
    - ледяной - взрывается при ударе (1 раз урон)
    - морозный - замедляет врагов в зоне (slow)
- Электричество
    - электрический - дот
    - штормовой - дот
    - громовой - отталкивает врагов в зоне (knockback)
- Яд
    - ядовитый - дот
    - токсичный - дот ?
- Воздух
    - воздушный - отталкивает врагов в зоне (knockback)
    - эфирный - дот
- Земля
    - земляной
    - каменный
    - песочный
- Тьма
    - теневой

Мувмент
- [Снаряд] осколок (шип) - летит от себя без гравитации,
- [Зона] туман - летит от себя без гравитации,
- [Зона] поле - статично,
- [Снаряд] шар - летит от себя с гравитацией,

Форма
- дождь - спавн сверху вниз
- конус - спавн в конусе
- дуги - спавн по дуге
- кольцо - спавн под себя
- шаг - спавн каждые N метров
- направленный - спавн в точке


?
- луч - непрерывный эффект между кастером и точкой
- ловушка - суммон


Префикс
- Самонаводящийся 
- Спиральный




### Дождь ледяных осколков








## 1

## 2

## 3

## 4

## 5

