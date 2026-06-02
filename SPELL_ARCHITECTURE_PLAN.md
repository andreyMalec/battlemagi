# План расширения архитектуры кастомных заклинаний

## Цель
Перейти от жесткой реализации в `SpellDefinition` к расширяемой data-driven архитектуре, где заклинание собирается из семантических компонентов фразы и получает детерминированный визуал через резолвер префабов.

## 1. Целевая модель заклинания

### 1.1 Компонентная схема

| Слой | Назначение | Примеры | Runtime-роль |
|---|---|---|---|
| `Core` | Что существует в мире | `Projectile`, `Beam`, `Zone`, `SelfCast`, `Summon` | Выбор ядра исполнения (`ISpellCore`) |
| `Transform` | Как распространяется | `Linear`, `Arc`, `Homing`, `Spiral`, `Bounce`, `Split`, `Wave` | Модификаторы траектории/эволюции |
| `Shape` | Геометрия воздействия | `Sphere`, `Capsule`, `Cone`, `Ring`, `Line`, `Rain` | Пространственные запросы и хит-области |
| `Payload` | Эффект попадания/тика | `Damage`, `DOT`, `CC`, `Knockback`, `Status`, `SpawnSecondary` | Применение геймплейного эффекта |
| `Modifiers` | Численные/логические усилители | `+Radius`, `-Speed`, scaling, blood-magic | Постобработка параметров |
| `Triggers` | Когда срабатывает | `OnHit`, `OnTick`, `OnEnter`, `AfterDelay` | Точки активации payload |
| `Conditions` | При каких условиях срабатывает | `HasBurn`, `HP<30%`, `Distance>15m` | Фильтр перед применением |

### 1.2 Новый data-layer

| Объект | Ключевые поля | Назначение |
|---|---|---|
| `SpellBlueprint` | `id`, `version`, `core`, `transforms[]`, `shape`, `payloads[]`, `modifiers[]`, `triggers[]`, `conditions[]`, `tags[]` | Каноническое описание спелла |
| `SpellPhraseProfile` | `locale`, `tokens`, `weights`, `implicitRules` | Связь слов и компонент |
| `SpellStyleProfile` | `element`, `carrier`, `shapeHints`, `intensity` | Вход для VFX-резолва |

## 2. Пайплайн: фраза -> компоненты

### 2.1 Этапы разбора

| Этап | Вход | Выход | Ключевая логика |
|---|---|---|---|
| `Tokenize` | Сырая фраза | Токены | Переиспользование текущего токенайзера (`SpellRecognizer`) |
| `Normalize` | Токены | Канонические термы | Нижний регистр, лемматизация, синонимы |
| `Tagging` | Канон-термы | Кандидаты компонент | Словарь: элемент/носитель/форма/модификатор/эффект |
| `ResolveConflicts` | Кандидаты | Валидный набор | Приоритеты, score, матрица совместимости |
| `ApplyImplicitRules` | Набор компонент | Расширенный blueprint | Скрытые условия и синергии по смыслу слова |
| `Materialize` | Граф | `SpellBlueprint` | Детерминированная сборка и версия |

### 2.2 Разрешение конфликтов

| Тип | Пример | Правило |
|---|---|---|
| `Core vs Core` | `шар луч` | Последний валидный core + confidence threshold |
| `Shape vs Shape` | `конус кольцо` | Primary shape + secondary как modifier |
| `Element clash` | `огненный ледяной` | Политика старта: доминантный элемент |
| `Motion clash` | `статичный самонаводящийся` | Матрица совместимости + downgrade |
| `Payload overload` | Слишком много эффектов | Лимит узлов, избыток в modifiers |

### 2.3 Implicit conditions
Семантика слова автоматически добавляет скрытые условия/синергии:
- `адский` -> усиление на цели со статусом `Burn`.
- `токсичный` -> усиление от накопленных стаков.
- `громовой` -> приоритет `Knockback` и импульсный профиль VFX.

## 3. Выбор внешнего вида и префабов (VFX Resolver)

### 3.1 Слои резолва

| Уровень | Ключи | Результат |
|---|---|---|
| `L1 Base` | `core + carrier` | Базовое семейство prefab |
| `L2 Element` | `element + damageType` | Материал, палитра, звук |
| `L3 Shape` | `shape + scaleBand` | Геометрия эмиттеров |
| `L4 Style` | `style tags` | Trail/impact/decal override |
| `L5 Fallback` | default per core | Гарантированный prefab |

### 3.2 Предлагаемые сущности
- `ISpellVfxResolver` - интерфейс резолва визуала по `SpellBlueprint`.
- `VfxRuleSet` (`ScriptableObject`) - правила и приоритеты.
- `VfxResolveContext` - контекст (`core`, `shape`, `element`, `modifiers`, LOD).
- `ResolvedVfx` - итог (`prefabId`, `inHandPrefabId`, `soundKind`, `styleParams`).

### 3.3 Интеграция
Встраиваем резолвер между сборкой blueprint и вызовом `SpellPrefabDatabase.Get(...)`, чтобы не ломать текущий путь загрузки префабов.

## 4. Миграция от `SpellDefinition` без поломки

| Этап | Что делаем | Совместимость |
|---|---|---|
| `E0 Baseline` | Фиксируем текущее поведение и метрики | 100% |
| `E1 Adapter` | `SpellBlueprintToSpellDefinitionAdapter` | 100%, shadow mode |
| `E2 Dual-path` | Старый и новый парсер за feature flag | 100% |
| `E3 Content migration` | Новые спеллы в blueprint, старые через адаптер | 95-100% |
| `E4 Native runtime` | Переход `SpellSystem` на абстракцию `ISpellSpec` | 90%+ |
| `E5 Cleanup` | Удаление legacy-полей после freeze | После стабилизации |

Ключевые точки интеграции:
- `Assets/Scripts/core/customspell/SpellDefinition.cs`
- `Assets/Scripts/_spellSystem/SpellSystem.cs`
- `Assets/Scripts/core/customspell/SpellPrefabDatabase.cs`
- `Assets/Scripts/core/spell/SpellRecognizer.cs`
- `Assets/Scripts/_spellSystem/ngo/SpellCasterNet.cs`

## 5. Сеть и производительность (Unity + Netcode)

- Передавать по сети `spellBlueprintId + seed + compact params`, а не полный граф.
- Материализация blueprint выполняется на сервере (server-authoritative).
- На клиентах - реконструкция и визуализация по детерминированному seed.
- Кэшировать результаты парсинга фраз и VFX-резолва (LRU).
- Минимизировать аллокации в hot path (реиспользуемые буферы токенов).
- Добавить метрики parser/resolver в существующую систему метрик заклинаний.

## 6. Этапы внедрения, DoD и риски

| Этап | Цель | DoD | Риск | Смягчение |
|---|---|---|---|---|
| `P1 Domain` | Утвердить схему `SpellBlueprint` | Спецификация v1 + валидатор | Переусложнение | Ограничить v1 минимальным ядром |
| `P2 Parser MVP` | Фраза -> компоненты | >=85% на контрольном словаре | Неоднозначность фраз | Score + fallback на known spell |
| `P3 VFX` | Стабильный выбор префаба | 100% fallback без missing prefab | Конфликты правил | Явные приоритеты + индексация правил |
| `P4 Adapter` | Паритет legacy спеллов | >=95% совпадения топ-20 | Дрейф параметров | Snapshot parity тесты |
| `P5 Network` | Новый протокол каста | Без регрессии sync/late join | Версионная несовместимость | Versioned payload |
| `P6 Cutover` | Blueprint-first прод | Legacy путь отключается флагом | Хвост техдолга | Жесткий freeze и cleanup milestone |

## 7. Стратегия тестирования

| Слой | Что тестируем | Формат |
|---|---|---|
| Unit Parser | Токены, нормализация, конфликты, implicit rules | Табличные cases |
| Unit Model | Валидация `SpellBlueprint` и совместимости компонентов | Schema tests |
| Unit VFX | Приоритеты правил, fallback | Golden tests |
| Integration Adapter | `Blueprint -> SpellDefinition` parity | Snapshot сравнение |
| Integration Runtime | Trigger/Condition/Payload последовательность | Симуляция сцен |
| Network | Server cast + client reconstruction | Deterministic replay |
| Perf | Время парсинга, GC, нагрузка тиков | Профилирование + метрики |

Целевые KPI:
- `Parse P95 < 2 ms`.
- `0 B` steady-state аллокаций в парсере/резолвере (целевое).
- Ошибка распознавания контрольного словаря `< 10%`.
- Паритет legacy через адаптер `>= 95%` до cutover.

## 8. Практическая дорожная карта (коротко)
1. Зафиксировать контракт `SpellBlueprint` и матрицу совместимости компонентов.
2. Реализовать parser MVP и словарь терминов из `CUSTOM_SPELL.md`.
3. Ввести `ISpellVfxResolver` + `VfxRuleSet` для выбора префабов.
4. Сделать адаптер в `SpellDefinition` и включить dual-path через feature flag.
5. Прогнать parity/regression/perf/network тесты и перейти на blueprint-first.

