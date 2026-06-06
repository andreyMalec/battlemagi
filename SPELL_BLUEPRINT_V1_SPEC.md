# Runtime Blueprint спецификация

## Статус
- Blueprint существует только в рантайме и не хранится как постоянный asset.
- Уникальность заклинания определяется нормализованной фразой.
- Контракт опирается на текущие ограничения в `SpellDefinition`, `SpellSystem`, `SpellRecognizer`, `SpellCasterNet`.
- В `v1` действует dual-path: `Legacy` (старые заклинания) и `New` (только новые заклинания).

## 1. Идентичность заклинания

## 1.1 `spellKey`
`spellKey` формируется только из фразы:
1. Взять исходную `rawPhrase`.
2. Применить ту же токенизацию, что в `SpellRecognizer.TokenizePhrase` (`[\p{L}\p{Nd}']+`, `ToLowerInvariant()`).
3. Склеить токены через один пробел.

Пример:
- `"Адский   Огненный осколок!"` -> `"адский огненный осколок"`

`spellKey` используется как единственный идентификатор runtime blueprint, ключ кэша и сетевой идентификатор.

## 1.2 Runtime lifecycle
1. Сначала выбирается маршрут каста (`CastRoute`): `Legacy` или `New`.
2. `Legacy`: используется существующий путь `DefaultSpells.Get(name)` / `DefaultSpells.GetSubSpell(name)` и текущие `SpellDefinition` + `SpellPrefabDatabase` без изменений.
3. `New`: фраза нормализуется в `spellKey`, выполняется поиск/сборка runtime blueprint, затем адаптация в runtime `SpellDefinition`.
4. Оба маршрута сходятся в существующий `SpellSystem.CastSpell`.

Правило v1: новые заклинания идут только через `New`, старые продолжают работать через `Legacy`.

## 2. Runtime контракт blueprint

## 2.1 Корневой объект

| Поле | Тип | Обяз. | Источник/ограничение |
|---|---|---|---|
| `rawPhrase` | `string` | Да | Исходная фраза игрока |
| `spellKey` | `string` | Да | Нормализованная фраза (см. 1.1) |
| `semanticTags` | `string[]` | Нет | Семантические теги из лексикона (`fire`, `storm`, `aoe`, `dot`, `pierce`) |
| `visualHints` | `string[]` | Нет | Подсказки резолверу визуала (`heavy`, `thin`, `ritual`, `clean`) |
| `coreType` | `CoreType` | Да | Строго из `CoreType` |
| `spawn` | `SpawnNode` | Да | Поля `SpawnDefinition` |
| `common` | `CommonNode` | Да | Общие поля `SpellDefinition` |
| `damage` | `DamageNode` | Нет | `DamageDefinition` |
| `knockback` | `KnockbackNode` | Нет | `KnockbackDefinition` |
| `effects` | `EffectNode[]` | Нет | `EffectDefinition` |
| `projectile` | `ProjectileNode` | Условно | Только если `coreType=Projectile` |
| `zone` | `ZoneNode` | Условно | Только если `coreType=Zone` |
| `beam` | `BeamNode` | Условно | Только если `coreType=Beam` |
| `summon` | `SummonNode` | Условно | Только если `coreType=Summon` |
| `self` | `SelfNode` | Условно | Только если `coreType=Self` |

Правило exclusivity: в любой момент времени заполнен только один из блоков core-деталей (`projectile/zone/beam/summon/self`), как в `SpellDefinition.Validate()`.

## 2.2 Enum и значения (строго текущие)

- `CoreType`: `Projectile`, `Zone`, `Beam`, `Self`, `Summon`
- `SpellMovement`: `Static`, `Linear`, `Spiral`, `FollowCaster`, `LookAtPoint`, `Accelerated`
- `ZoneShapeType`: `Sphere`, `Plate`
- `BeamShapeType`: `Straight`, `Cone`
- `SpellDamageMode`: `Instant`, `DamageOverTime`, `OncePerLifetime`
- `SpellDamageBaseType`: `Flat`, `Percent`
- `SpellDamagePercentStat`: `Health`, `Mana`, `Armor`
- `SpellKnockbackMode`: `Impulse`, `Continuous`, `BeamSelfImpulse`
- `SpellKnockbackVectorMode`: `AwayFromPoint`, `TowardPoint`, `TowardPointAndUp`
- `StatusEffectType`: использовать текущий enum `StatusEffectType` из `EffectDefinition`
- `SummonMotion`: `NoMotion`, `Stationary`, `Floating`
- `SummonBrain`: `Dead`, `AlwaysAttack`, `Aggressive`, `Defensive`
- `SummonSensor`: `None`, `Radius`, `LineOfSight` (flags)
- `TargetFilter`: `Player`, `Spell`, `All`
- `FollowCasterTarget`: `Caster`, `Spawn`
- `SpawnMode`: использовать текущий enum `SpawnMode`
- `Preview`: `None`, `Mesh`, `Line`, `Disk`, `GroundPoint` (flags)

## 3. Ограничения Core/Transform/Shape по текущему коду

## 3.1 Core -> Shape

| Core | Shape |
|---|---|
| `Projectile` | Shape не настраивается, всегда `LineProjectileShape` |
| `Zone` | `ZoneShapeType.Sphere` или `ZoneShapeType.Plate` |
| `Beam` | `BeamShapeType.Straight` или `BeamShapeType.Cone` |
| `Summon` | Отдельного shape нет |
| `Self` | Отдельного shape нет |

## 3.2 Core -> Movement (фактическое исполнение)

| Core | Реально поддерживаемые `SpellMovement` в `SpellSystem.Move(...)` |
|---|---|
| `Projectile` | `Static`, `Linear`, `Accelerated`, `LookAtPoint`, `Spiral` |
| `Zone` | `Static`, `Linear`, `Accelerated`, `LookAtPoint`, `Spiral`, `FollowCaster` |
| `Beam` | `Static`, `Linear`, `Accelerated`, `LookAtPoint`, `FollowCaster` |
| `Summon` | Не использует `SpellMovement`, использует `SummonMotion` |
| `Self` | Movement отсутствует |

Важно: если в runtime blueprint задать movement, который не обрабатывается конкретным `Move(...)` switch, поведение деградирует в `StaticTransform`.

## 3.3 Дополнительные ограничения из текущих `Validate()`
- Homing (`enableHoming`) допускается только для `Linear`, `Spiral`, `Accelerated` в `Projectile/Zone`.
- `returnToCaster` допускается:
  - `Projectile/Zone`: только `Linear`, `Spiral`, `Accelerated`.
  - `Beam`: только `Linear`, `Accelerated`.
- `spawnAtStep` активируется только если задан соответствующий sub-spell (`atStepDistanceSpawn != null`).

## 3.4 Триггеры и действия (как сейчас в `SpellSystem`)
Триггеры в v1 runtime не задаются свободно, а вычисляются из заполненных полей:
- `Projectile`: `OnHit`, `OnMaxDistance`, `OnStepDistance`, `OnLifetimeHalf`, `OnLifetimeEnding`, опционально `OnLifetimeStart`.
- `Zone`: `OnZoneStay`, `OnEnemySpellKill`, `OnMaxDistance`, `OnStepDistance`, `OnLifetimeHalf`, `OnLifetimeEnding`, опционально `OnLifetimeStart`, `OnZoneEnter`.
- `Beam`: `OnHit`, `OnBeamTick` (для `BeamSelfImpulse`), `OnMaxDistance`, `OnLifetimeHalf`, `OnLifetimeEnding`, опционально `OnLifetimeStart`.
- `Summon`: `OnLifetimeEnding`, опционально `OnLifetimeStart`, `OnSummonAttack` (для `Trap`).
- `Self`: `OnLifetimeStart`.

## 3.5 Payload (как сейчас)
Payload формируется из блоков:
- `damage` -> урон (instant/dot/once-per-lifetime).
- `knockback` -> импульс/continuous/beam-self-impulse.
- `effects` -> статусные эффекты.
- sub-spells из core definition (`onHitSpawn`, `atMaxDistanceSpawn`, `onLifetimeEndSpawn` и т.д.).

## 4. Валидация runtime blueprint

## 4.1 Базовые правила
- `spellKey` не пустой.
- `coreType` обязателен.
- `spawn` обязателен.
- Ровно один core-блок заполнен и соответствует `coreType`.

## 4.2 Коды ошибок

| Код | Условие |
|---|---|
| `SPRB-0001` | Пустой `rawPhrase` |
| `SPRB-0002` | Пустой `spellKey` |
| `SPRB-0003` | Не задан `coreType` |
| `SPRB-0004` | Не задан `spawn` |
| `SPRB-0005` | Не соблюдена exclusivity core-блоков |
| `SPRB-0101` | `coreType=Zone`, но `zone` отсутствует |
| `SPRB-0102` | `coreType=Beam`, но `beam` отсутствует |
| `SPRB-0103` | `coreType=Projectile`, но `projectile` отсутствует |
| `SPRB-0104` | `coreType=Summon`, но `summon` отсутствует |
| `SPRB-0105` | `coreType=Self`, но `self` отсутствует |
| `SPRB-0201` | Недопустимый movement для выбранного core |
| `SPRB-0202` | Недопустимый shape для выбранного core |
| `SPRB-0203` | `returnToCaster=true` при недопустимом movement |
| `SPRB-0204` | `enableHoming=true` при недопустимом movement |

## 5. Phrase Lexicon (runtime)

## 5.1 Формат

`RuntimePhraseLexicon`:

```json
{
  "locale": "ru-RU",
  "unknownPolicy": "warn_drop",
  "entries": [
    {
      "token": "шар",
      "priority": 120,
      "mapsTo": [
        { "path": "coreType", "value": "Projectile" },
        { "path": "projectile.moveType", "value": "Linear" },
        { "path": "projectile.enableGravity", "value": "true" }
      ],
      "semanticTags": [
        { "group": "delivery", "value": "projectile", "priority": 120 }
      ],
      "visualHints": [
        { "group": "tone", "value": "heavy", "priority": 80 }
      ]
    }
  ],
  "implicitRules": [
    {
      "id": "storm_knockback",
      "priority": 90,
      "whenToken": "громовой",
      "apply": [
        { "path": "knockback.mode", "value": "Impulse" },
        { "path": "knockback.vectorMode", "value": "AwayFromPoint" }
      ]
    }
  ]
}
```

Поддерживаемые поля:
- `entries[].token`: нормализованный токен (`ToLowerInvariant`), точное совпадение.
- `entries[].priority`: приоритет токена в merge.
- `entries[].mapsTo[]`: список patch-операций `path/value`.
- `entries[].semanticTags[]`, `entries[].visualHints[]`: теги с группой и приоритетом.
- `implicitRules[]`: условные правила после прохода по токенам.
- `unknownPolicy`: в runtime `warn_drop`.

## 5.2 Patch-path для runtime

- `coreType`
- `spawn.spawnMode`, `spawn.instanceCount`, `spawn.instanceLimit`, `spawn.multiInstanceDelay`
- `common.scale`, `common.lifetime`, `common.manaCost`, `common.bloodMagic`
- `damage.mode`, `damage.baseType`, `damage.percentOf`, `damage.amount`, `damage.percent`, `damage.tickInterval`
- `knockback.mode`, `knockback.vectorMode`, `knockback.impulse`, `knockback.forcePerSecond`, `knockback.duration`
- `effects.add` (`StatusEffectType` или `StatMultiplier:<StatType>[:<EffectTarget>]`)
- `projectile.*`: `prefabId`, `moveType`, `moveSpeed`, `returnToCaster`, `enableHoming`, `enableGravity`, `enableBounce`, `maxBounces`, `enablePierce`, `maxPierces`, `enableFork`, `forkCount`
- `zone.*`: `prefabId`, `shapeType`, `moveType`, `moveSpeed`, `returnToCaster`, `enableHoming`, `destroyIncomingSpells`, `impassableForEnemies`, `teleportOnSpawn`
- `beam.*`: `prefabId`, `shapeType`, `moveType`, `moveSpeed`, `returnToCaster`, `enableBounce`, `maxBounces`, `enablePierce`, `maxPierces`, `enableFork`, `forkCount`
- `summon.*`: `prefabId`, `brain`, `targetFilter`, `motion`, `moveSpeed`
- `self.prefabId`

## 5.3 Политика коллизий
- Конфликт patch-поля: больше `priority`; при равенстве — последний токен в фразе.
- Конфликт `coreType`/shape/movement: та же политика.
- Конфликт `semanticTags`/`visualHints` внутри одной `group`: максимум по `priority`, при равенстве — последний токен.
- `implicitRules` применяются после token-entry и участвуют в тех же коллизиях.
- Невозможная комбинация после merge — ошибка `SPRB-02xx`.

## 5.4 Реализованные маппинги эффектов из `CUSTOM_SPELL_IMPL.md`

Прямо поддержано (gameplay patch):
- `взрыв` -> `Zone` + `damage.mode=Instant`
- `лужа` -> `Zone` + `damage.mode=DamageOverTime`
- `горение`, `яд` -> `effects.add=DamageOverTime`
- `заморозка` -> `effects.add=Freeze`
- `замедление` -> `effects.add=StatMultiplier:MoveSpeed:Enemies`
- `отталкивание`/`втягивание` -> `knockback.mode=Impulse` + `vectorMode`
- `отскок` -> `enableBounce`
- `пробивает`/`проходит сквозь` -> `enablePierce`
- `разветвление` -> `enableFork`
- `автонаводка` -> `projectile.enableHoming`
- `завеса` -> `zone.destroyIncomingSpells=true`
- `купол` -> `zone.impassableForEnemies=true`

Частично поддержано (через существующие поля/enum):
- `повышает защиту`, `дает армор` -> `effects.add=Armor` или `StatMultiplier:DamageReduction`
- `увеличение скорости` -> `StatMultiplier:MoveSpeed:Self`

Не поддержано в gameplay (сохраняется как `semanticTags`):
- `сайленс`, `слепота`, `вампиризм`, `инверт управления`, `замедление снарядов`

## 6. Резолв внешнего вида для новых заклинаний (`New` path)

Текущий выбор по одному токену считается недостаточным. Для `New`-заклинаний вводится многослойный `PrefabResolver`, который принимает признаки заклинания и возвращает готовый набор id.

Для `Legacy`-заклинаний этот раздел не применяется: визуал и звук берутся из существующего `SpellDefinition` и `SpellPrefabDatabase` как сейчас.

## 6.1 Вход резолвера

`PrefabResolverInput` собирается из runtime blueprint:
- `coreType`
- `semanticTags`
- `visualHints`
- `delivery` (вычисляется из `coreType + spawn.spawnMode`)
- `intensityBand` (вычисляется из `damage/knockback/effects`)

`intensityBand` фиксируется в 3 диапазона:
- `Low`
- `Mid`
- `High`

## 6.2 Выход резолвера (3 независимых канала)

`PrefabResolverOutput`:
- `visualPrefabId` (enum по core: `SpellProjectilePrefabId`/`SpellZonePrefabId`/`SpellBeamPrefabId`/`SpellSummonPrefabId`/`SpellSelfPrefabId`)
- `inHandPrefabId` (тот же enum-домен, если доступен)
- `soundKind` (`DamageKind`)

Каналы резолвятся раздельно. Ошибка в одном канале не должна ломать остальные.

## 6.3 Порядок резолва

1. `Exact`: `coreType + delivery + elementTag + intensityBand + styleHint`.
2. `Semantic`: `coreType + elementTag + intensityBand`.
3. `CoreDelivery`: `coreType + delivery`.
4. `CoreDefault`: дефолт для `coreType`.

Для каждого канала применяется один и тот же порядок, но с разными таблицами правил.

## 6.4 Правила деградации

- Если не найден `visualPrefabId`: взять `CoreDefault` и продолжать каст.
- Если не найден `inHandPrefabId`: вернуть `null` (допустимо текущим `SpellPrefabDatabase.Hand(...)`).
- Если не найден `soundKind`: использовать `DamageKind.Default`.
- Если конфликтуют несколько правил одного уровня: выбрать правило с большим `priority`, при равенстве - лексикографически меньший `ruleId`.

## 6.5 Интеграция с текущей системой (без ломки)

- `PrefabResolver` возвращает только существующие enum-id.
- `SpellSystem.ShowSpell(...)` и `SpellSystem.CastSpell(...)` остаются без изменения сигнатур.
- `SpellPrefabDatabase.Get(...)`, `SpellPrefabDatabase.Hand(...)`, `SpellPrefabDatabase.Sound(...)` остаются источником префабов/звука.
- В адаптере `runtime blueprint -> SpellDefinition` заполняется `prefabId` конкретного core и, при необходимости, материализуется `damageKind` через `SpellPrefabDatabase.Sound(...)`.
- `Legacy` путь не модифицируется и продолжает использовать текущую схему данных `DefaultSpells`/`SpellDefinition`.

## 7. Сетевой контракт (без версий)

## 7.1 Запрос каста
С учетом текущего `SpellCasterNet`, используется dual contract:
- `Legacy`: передается `spellName` (текущий формат).
- `New`: передается `spellKey`.
- Общее для обоих маршрутов: `alternativeSpawn`, target info, `damageMultiplier`.

## 7.2 Серверная обработка
1. Сервер определяет маршрут (`Legacy`/`New`) по входным данным каста.
2. `Legacy`: получает `SpellDefinition` через `DefaultSpells.Get(...)`/`GetSubSpell(...)` и выполняет текущий flow без изменений.
3. `New`: по `spellKey` собирает runtime blueprint.
4. `New`: выполняет `PrefabResolver`, получает `visualPrefabId/inHandPrefabId/soundKind`, адаптирует в runtime `SpellDefinition`.
5. Оба маршрута выполняют общий flow `SpawnContext` -> `SpellSystem.CastSpell`.

Для клиентов сохраняется текущий RPC-путь `OnCastClientRpc(...)`, где уже передаются `coreType` и `prefabId`.

## 8. Пример runtime blueprint JSON

```json
{
  "rawPhrase": "Адский огненный осколок",
  "spellKey": "адский огненный осколок",
  "spellName": "адский огненный осколок",
  "coreType": "Projectile",
  "spawn": {
    "spawnMode": "Direct",
    "instanceCount": 1,
    "instanceLimit": 0,
    "multiInstanceDelay": 0.0
  },
  "common": {
    "scale": 1.0,
    "lifetime": 4.0,
    "manaCost": 20.0,
    "bloodMagic": false,
    "channeling": false,
    "charging": false
  },
  "damage": {
    "mode": "Instant",
    "baseType": "Flat",
    "amount": 48.0
  },
  "effects": [
    {
      "target": "Enemies",
      "type": "DamageOverTime"
    }
  ],
  "projectile": {
    "prefabId": "Base",
    "moveType": "Linear",
    "moveSpeed": 24.0,
    "enableHoming": false,
    "enableGravity": false,
    "enableBounce": false,
    "enablePierce": false,
    "enableFork": false
  }
}
```

## 9. План внедрения (минимум)

| Этап | Результат | Критерий готовности |
|---|---|---|
| `M1` | Runtime спецификация зафиксирована | Документ принят без замечаний по версиям |
| `M2` | Runtime validator (`SPRB-*`) | Все unit-тесты валидатора проходят |
| `M3` | `New` pipeline: `phrase -> spellKey -> blueprint -> SpellDefinition` | Каст новых заклинаний работает через текущий `SpellSystem` |
| `M4` | Dual network path (`spellName` + `spellKey`) | Каст в мультиплеере без регрессий по `Legacy` |
| `M5` | Лексикон + многослойный `PrefabResolver` для `New` | Нет missing prefab для `New`, `Legacy` работает без изменений |
| `M6` | Parity gate | 0 регрессий по старым заклинаниям при включенном `New` path |

