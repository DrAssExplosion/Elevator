# Elevator

Мультиплеерный шутер, разработанный на движке **Unity 6000.6.4f1** (Unity 6) с использованием современного стека DOTS и Netcode for Entities.

## 🎮 О проекте

**Elevator** — сетевая игра с поддержкой выделенных серверов, клиентским предсказанием (client-side prediction) и авторитетной серверной моделью. Проект построен вокруг ECS-архитектуры и включает собственную прослойку **GhostBridge** для связывания GameObject-мира Unity с сетевыми сущностями Entity-мира.

## 🛠 Технологии

| Компонент | Описание |
|---|---|
| **Unity 6 (6000.6.4f1)** | Движок проекта |
| **Netcode for Entities (6.6.0)** | Сетевой стек на базе DOTS/ECS |
| **Dedicated Server (3.0.0)** | Поддержка выделенных серверов |
| **Unity Services Multiplayer (2.3.3)** | Лобби и обнаружение сессий (UGS) |
| **URP 17.6 + VFX Graph** | Рендеринг и визуальные эффекты |
| **Input System (1.20.0)** | Новая система ввода |
| **Cinemachine 6 / Physics / AI Navigation** | Камера, физика, навигация |
| **Addressables (2.11.2)** | Асинхронная загрузка ресурсов |
| **UI Toolkit + UGUI** | Интерфейс игры и меню |

## 📁 Структура проекта

```
Assets/
├── Scripts/
│   ├── Gameplay/          # Игровая логика
│   │   ├── GameManager/   # GameManager, GameFlowControl, SceneLoader, GameSettings
│   │   ├── Player/        # Движение, анимация, снаряды, рендер-проходы игрока
│   │   ├── Weapon/        # WeaponData, WeaponManager, WeaponRegistry
│   │   ├── Input/         # ECS-системы ввода и предсказания команд
│   │   ├── Leaderboard/   # Таблица лидеров
│   │   └── StateMachineBehaviors/, UI/, VisualEffects/
│   ├── Networking/        # Сетевой слой
│   │   ├── Client/        # ClientConnectionSystem, ClientGameSystem
│   │   ├── Server/        # ServerGameSystem, ServerGameData
│   │   ├── GameConnection/# GameConnection, конструкторы сетевых драйверов
│   │   └── Shared/        # RPC, точки спавна, общие компоненты
│   ├── GhostBridge/       # Мост между GameObjects и Netcode Entities
│   │   ├── Ghosts/        # GhostGameObject, системы синхронизации трансформ и RPC
│   │   ├── Player/        # Клиентское предсказание (PlayerPredictionSystem, PredictedClientInput)
│   │   └── Spawning/      # GhostSpawner, ManagerGhostsSpawner
│   ├── DedicatedServer/   # Бутстрап сервера (в т.ч. через Unity Gaming Services)
│   ├── UI/                # Игровой UI и информация о сессии
│   └── Utility/, Editor/  # Вспомогательный код и инструменты редактора
├── Scenes/                # MainMenu, GameScene, Persistents, ServerScene, субсцены
├── Art/, Audio/, Data/    # Ресурсы (модели, звук, данные оружия и т.д.)
├── Prefabs/               # Префабы
├── InputSystems/          # Управления ввода (.inputactions)
├── UI Toolkit/            # Темы, PanelSettings, стили GameUI
└── Settings/              # Конфиги URP, VFX и др.
ProjectSettings/           # Настройки проекта Unity
Packages/                  # manifest.json — зависимости проекта
```

## 🏗 Архитектура

- **Клиент–серверная модель**: сервер авторитетен, клиенты отправляют предсказанный ввод (`PredictedClientInput`), ошибки корректятся `PlayerPredictionSystem`.
- **GhostBridge**: позволяет обычным MonoBehaviour-скриптам работать поверх сетевых ghost-сущностей — спавн, синхронизация трансформ (server write / client apply), маршрутизация RPC и жизненный цикл.
- **Двойной режим запуска**: хост-игра из клиента или подключение к выделенному серверу (локально либо через UGS Multiplayer Services).
- **Сцены**: `MainMenu` → `GameScene` (+ субсцены игровых ресурсов и точек спавна), `Persistents` (незагружаемый слой), `ServerScene` (для dedicated server).

## 🚀 Запуск

1. Установите **Unity 6000.6.4f1** через Unity Hub.
2. Откройте папку проекта в Unity Hub (пакеты подтянутся автоматически из `Packages/manifest.json`).
3. Для тестирования сети используйте **Multiplayer Play Mode** — несколько игроков в редакторе без сборки.
4. Игровые сцены: `Assets/Scenes/MainMenu.unity` (главное меню) или `Assets/Scenes/GameScene.unity`.
5. Сборка dedicated-сервера: бутстрап в `Assets/Scripts/DedicatedServer/`.

## 🧪 Тестирование

В проекте подключён `com.unity.test-framework` (1.8.0) — запустите тесты через **Window → General → Test Runner**.

## 👥 Ветки и разработка

Проект ведётся через pull request'ы (пример слитой фичи: `game-menu-settings`). Основная ветка — `main`.
