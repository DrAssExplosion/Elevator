using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    public enum GlobalGameState
    {
        MainMenu,
        InGame,
        Loading,
    }

    public enum MainMenuState
    {
        MainMenuScreen,
        SettingsScreen,
        StartHostPopup,
        DirectConnectPopUp,
        JoinCodePopUp,
    }

    public enum PlayerState
    {
        IsPlaying,
        IsDead,
    }
   
    public class GameSettings : INotifyBindablePropertyChanged
    {
        public static GameSettings Instance { get; private set; } = null!;

        /// <summary>
        /// This INitialOnloadMethod instantiate the singletone instance 
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new GameSettings();

        const string k_PlayerNameKey = "PlayerName";
        const string k_PlayerCharacterKey = "PlayerCharacer";
        const string k_ConnectionModeKey = "ConnectionMode";
        const string k_SessionNameKey = "SessionName";
        const string k_MouseSensitivityKey = "MouseSensitivity";
        const string k_AudioVolumeKey = "AudioVolume";
        const string k_MicrophoneVolumeKey = "MicrophoneVolume";
        const string k_AudioOutputDeviceKey = "AudioOutputDevice";
        const string k_AudioInputDeviceKey = "AudioInputDevice";
        const string k_LanguageKey = "Language";

        public static readonly string[] SupportedLanguages = { "English", "Русский" };

        GameSettings()
        {
            m_PlayerName = PlayerPrefs.GetString(k_PlayerNameKey, Environment.UserName);
            m_PlayerCharacter = PlayerPrefs.GetInt(k_PlayerCharacterKey, 0);  
            m_ConnectionMode = PlayerPrefs.GetInt(k_ConnectionModeKey, 0);
            m_SessionName = PlayerPrefs.GetString(k_SessionNameKey, "default-session");
            m_MouseSensitivity = PlayerPrefs.GetFloat(k_MouseSensitivityKey, DefaultMouseSensitivity);
            m_AudioVolume = PlayerPrefs.GetFloat(k_AudioVolumeKey, 1.0f);
            m_MicrophoneVolume = PlayerPrefs.GetFloat(k_MicrophoneVolumeKey, 1.0f);
            m_AudioOutputDevice = PlayerPrefs.GetString(k_AudioOutputDeviceKey, string.Empty);
            m_AudioInputDevice = PlayerPrefs.GetString(k_AudioInputDeviceKey, string.Empty);
            m_Language = PlayerPrefs.GetString(k_LanguageKey, System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "Русский" : "English");
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        public AwaitableCompletionSource CancellableUserInputPopUp;
        
        GlobalGameState m_GameState;
        public GlobalGameState GameState
        {
            get => m_GameState;
            set
            {
                if (m_GameState == value)
                {
                    return;
                }

                m_GameState = value;

                Notify(MainMenuStylePropertyName);
                Notify(LoadingScreenStylePropertyName);
                Notify(InGameUIPropertyName);
            }
        }
        
        MainMenuState m_MainMenuState;
        public MainMenuState MainMenuState
        {
            get => m_MainMenuState;
            set
            {
                if (m_MainMenuState == value)
                {
                    return;
                }

                m_MainMenuState = value;
                Notify(MainMenuStylePropertyName);
                Notify(SettingsMenuStylePropertyName);
                Notify(JoinSessionStylePropertyName);
                Notify(StartHostStylePropertyName);
                Notify(DirectConnectStylePropertyName);
            }
        }
        
        PlayerState m_PlayerState;
        public PlayerState PlayerState
        {
            get => m_PlayerState;
            set
            {
                if (m_PlayerState == value)
                    return;

                m_PlayerState = value;
                //Notify(RespawnScreenStylePropertyName); //TODO: Define the respawn screen style
            }
        }

        public static readonly string MainMenuStylePropertyName = nameof(MainMenuStyle);
        [CreateProperty]
        DisplayStyle MainMenuStyle => m_GameState == GlobalGameState.MainMenu && 
                                      MainMenuState == MainMenuState.MainMenuScreen ? 
                                      DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string SettingsMenuStylePropertyName = nameof(SettingsMenuStyle);
        [CreateProperty]
        DisplayStyle SettingsMenuStyle => m_GameState == GlobalGameState.MainMenu &&
                                          MainMenuState == MainMenuState.SettingsScreen ?
                                          DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string JoinSessionStylePropertyName = nameof(JoinSessionStyle);
        [CreateProperty]
        DisplayStyle JoinSessionStyle => m_GameState == GlobalGameState.MainMenu && 
                                         MainMenuState == MainMenuState.JoinCodePopUp ? 
                                         DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string StartHostStylePropertyName = nameof(StartHostStyle);
        [CreateProperty]
        DisplayStyle StartHostStyle => m_GameState == GlobalGameState.MainMenu && 
                                       MainMenuState == MainMenuState.StartHostPopup ? 
                                       DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string DirectConnectStylePropertyName = nameof(DirectConnectStyle);
        [CreateProperty]
        DisplayStyle DirectConnectStyle => m_GameState == GlobalGameState.MainMenu && 
                                           MainMenuState == MainMenuState.DirectConnectPopUp ? 
                                           DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string LoadingScreenStylePropertyName = nameof(LoadingScreenStyle);
        [CreateProperty]
        DisplayStyle LoadingScreenStyle
        {
            get
            {
                if (m_GameState == GlobalGameState.Loading)
                {
                    return DisplayStyle.Flex;
                }

                LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.NotLoading,  0.0f);
                return DisplayStyle.None;
            }
        }

        public static readonly string InGameUIPropertyName = nameof(InGameUI);
        [CreateProperty]
        DisplayStyle InGameUI => m_GameState == GlobalGameState.InGame ? DisplayStyle.Flex : DisplayStyle.None;

        bool m_IsPauseMenuOpen = false;
        public bool IsPauseMenuOpen
        {
            get => m_IsPauseMenuOpen;
            set
            {
                if (m_IsPauseMenuOpen == value)
                {
                    return;
                }

                m_IsPauseMenuOpen = value;
                Utils.SetCursorVisible(value);
                Notify(PauseMenuStylePropertyName);
                Notify(MobileControlsOpacityPropertyName);
            }
        }
        public static readonly string PauseMenuStylePropertyName = nameof(PauseMenuStyle);
        [CreateProperty]
        public DisplayStyle PauseMenuStyle => IsPauseMenuOpen ? DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string MobileControlsOpacityPropertyName = nameof(MobileControlsPauseMenuOpacity);
        [CreateProperty]
        public StyleFloat MobileControlsPauseMenuOpacity => IsPauseMenuOpen ? 0.5f : 1f;

        string m_PlayerName;
        [CreateProperty]
        public string PlayerName
        {
            get => m_PlayerName;
            set
            {
                if (m_PlayerName == value)
                {
                    return;
                }

                m_PlayerName = value;
                PlayerPrefs.SetString(k_PlayerNameKey, value);
            }
        }

        private int m_PlayerCharacter = 0;
        [CreateProperty]
        public int PlayerCharacter
        {
            get => m_PlayerCharacter;
            set
            {
                if (m_PlayerCharacter == value)
                {
                    return;
                }

                m_PlayerCharacter = value;
                PlayerPrefs.SetInt(k_PlayerCharacterKey, value);
            }
        }
        
        int m_ConnectionMode = 0;
        [CreateProperty]
        public int ConnectionMode
        {
            get => m_ConnectionMode;
            set
            {
                if (m_ConnectionMode == value)
                    return;

                m_ConnectionMode = value;
                PlayerPrefs.SetInt(k_ConnectionModeKey, value);
            }
        }

        string m_SessionName = "default-session";
        [CreateProperty]
        public string SessionName
        {
            get => m_SessionName;
            set
            {
                if (m_SessionName == value)
                    return;

                m_SessionName = value;
                PlayerPrefs.SetString(k_SessionNameKey, value);
            }
        }

        /// <summary>
        /// Default mouse look sensitivity, matching the original hardcoded value.
        /// </summary>
        public const float DefaultMouseSensitivity = 3.7f;

        float m_MouseSensitivity = DefaultMouseSensitivity;
        [CreateProperty]
        public float MouseSensitivity
        {
            get => m_MouseSensitivity;
            set
            {
                if (Mathf.Approximately(m_MouseSensitivity, value))
                    return;

                m_MouseSensitivity = value;
                PlayerPrefs.SetFloat(k_MouseSensitivityKey, value);
            }
        }

        float m_AudioVolume = 1.0f;
        [CreateProperty]
        public float AudioVolume
        {
            get => m_AudioVolume;
            set
            {
                if (Mathf.Approximately(m_AudioVolume, value))
                    return;

                m_AudioVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(k_AudioVolumeKey, m_AudioVolume);
                Notify(AudioVolumeDbPropertyName);
            }
        }

        public static readonly string AudioVolumeDbPropertyName = nameof(AudioVolumeDb);
        [CreateProperty]
        public float AudioVolumeDb => Mathf.Log10(Mathf.Max(m_AudioVolume, 0.0001f)) * 20.0f;

        float m_MicrophoneVolume = 1.0f;
        [CreateProperty]
        public float MicrophoneVolume
        {
            get => m_MicrophoneVolume;
            set
            {
                if (Mathf.Approximately(m_MicrophoneVolume, value))
                    return;

                m_MicrophoneVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(k_MicrophoneVolumeKey, m_MicrophoneVolume);
            }
        }

        string m_AudioOutputDevice = string.Empty;
        [CreateProperty]
        public string AudioOutputDevice
        {
            get => m_AudioOutputDevice;
            set
            {
                if (m_AudioOutputDevice == value)
                    return;

                m_AudioOutputDevice = value ?? string.Empty;
                PlayerPrefs.SetString(k_AudioOutputDeviceKey, m_AudioOutputDevice);
            }
        }

        string m_AudioInputDevice = string.Empty;
        [CreateProperty]
        public string AudioInputDevice
        {
            get => m_AudioInputDevice;
            set
            {
                if (m_AudioInputDevice == value)
                    return;

                m_AudioInputDevice = value ?? string.Empty;
                PlayerPrefs.SetString(k_AudioInputDeviceKey, m_AudioInputDevice);
            }
        }

        string m_Language = "English";
        [CreateProperty]
        public string Language
        {
            get => m_Language;
            set
            {
                if (m_Language == value || string.IsNullOrEmpty(value))
                    return;

                m_Language = value;
                PlayerPrefs.SetString(k_LanguageKey, value);
                Localization.SetLanguage(value);
                m_LocalizationRevision++;
                Notify(LocalizedStringsPropertyName);
            }
        }

        /// <summary>
        /// Bump counter used as a binding source to make UI elements re-localize when the language changes.
        /// </summary>
        public static readonly string LocalizedStringsPropertyName = nameof(LocalizationRevision);
        int m_LocalizationRevision;
        [CreateProperty]
        public int LocalizationRevision => m_LocalizationRevision;

        public static readonly string MainMenuSceneLoadedPropertyName = nameof(MainMenuSceneLoadedStyle);
        [CreateProperty]
        DisplayStyle MainMenuSceneLoadedStyle => m_MainMenuSceneLoaded ? DisplayStyle.None : DisplayStyle.Flex;

        /// <summary>
        /// Applies the persisted settings that need to be restored at startup (audio output device, language).
        /// </summary>
        public void ApplySettings()
        {
            if (!string.IsNullOrEmpty(m_AudioOutputDevice))
                AudioOutputConfiguration.SetOutputDevice(m_AudioOutputDevice);

            Localization.SetLanguage(m_Language);
            ApplyAudioVolume();
        }

        /// <summary>
        /// Pushes the <see cref="AudioVolume"/> setting into Unity's audio output.
        /// The project's SoundSystem routes its emitters through the built-in audio listener, so scaling
        /// <c>AudioListener.volume</c> is what actually changes the perceived master volume.
        /// </summary>
        public void ApplyAudioVolume() => UnityEngine.Audio.AudioListener.volume = m_AudioVolume;

        bool m_MainMenuSceneLoaded;
        public bool MainMenuSceneLoaded
        {
            get => m_MainMenuSceneLoaded;
            set
            {
                if (m_MainMenuSceneLoaded == value)
                    return;

                m_MainMenuSceneLoaded = value;
                Notify(MainMenuSceneLoadedPropertyName);
            }
        }
    }
}
