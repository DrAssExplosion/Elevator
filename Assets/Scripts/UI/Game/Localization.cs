using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Lightweight localization helper used by the UI screens.
    /// Strings are stored per-language in a simple table; missing entries fall back to English.
    /// </summary>
    public static class Localization
    {
        const string k_English = "English";
        const string k_Russian = "Русский";

        static readonly Dictionary<string, Dictionary<string, string>> k_Strings = new()
        {
            [k_English] = new Dictionary<string, string>(),
            [k_Russian] = new Dictionary<string, string>
            {
                // Main menu
                ["Player Name"] = "Имя игрока",
                ["Choose Character"] = "Выбор персонажа",
                ["Rifle"] = "Винтовка",
                ["Shotgun"] = "Дробовик",
                ["Connection Mode"] = "Режим подключения",
                ["Relay"] = "Ретранслятор",
                ["Direct"] = "Прямое подключение",
                ["Session Name"] = "Название сессии",
                ["Create or Join Session"] = "Создать или войти в сессию",
                ["Start Host"] = "Запустить хост",
                ["Connect to Server"] = "Подключиться к серверу",
                ["Matchmake"] = "Быстрый поиск",
                ["Quit"] = "Выход",
                ["Settings"] = "Настройки",

                // Pause menu
                ["Resume"] = "Продолжить",
                ["Main Menu"] = "Главное меню",

                // Settings tabs
                ["Controls"] = "Управление",
                ["Gameplay"] = "Геймплей",
                ["Language"] = "Язык",

                // Controls settings
                ["Mouse Sensitivity"] = "Чувствительность мыши",
                ["Key Bindings"] = "Назначения клавиш",
                ["Press a key"] = "Нажмите клавишу",
                ["Esc to cancel"] = "Esc для отмены",

                ["Move"] = "Движение",
                ["Look"] = "Обзор",
                ["Attack"] = "Атака",
                ["Interact"] = "Взаимодействие",
                ["Crouch"] = "Присесть",
                ["Jump"] = "Прыжок",
                ["Previous"] = "Предыдущее оружие",
                ["Next"] = "Следующее оружие",
                ["Sprint"] = "Бег",

                // Gameplay settings
                ["Audio Input Source"] = "Аудио входной источник",
                ["Microphone Volume"] = "Громкость микрофона",
                ["Audio Output Source"] = "Аудио выходной источник",
                ["Audio Volume"] = "Громкость аудио",
                ["Default"] = "По умолчанию",
            }
        };

        public static event Action LanguageChanged;

        public static string CurrentLanguage { get; private set; } = k_English;

        public static void SetLanguage(string language)
        {
            if (string.IsNullOrEmpty(language) || !k_Strings.ContainsKey(language))
                return;

            if (CurrentLanguage == language)
                return;

            CurrentLanguage = language;
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// Translates <paramref name="key"/> into the current language. Falls back to the key itself
        /// (which is the English source string) when no translation is registered.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;

            if (k_Strings.TryGetValue(CurrentLanguage, out var table) &&
                table.TryGetValue(key, out var localized))
                return localized;

            return key;
        }

        /// <summary>
        /// Convenience overload that also assigns the translated text to a visual element.
        /// </summary>
        public static void ApplyLabel(Label label, string key)
        {
            if (label != null)
                label.text = Get(key);
        }

        public static IEnumerable<string> Languages => k_Strings.Keys;
    }
}
