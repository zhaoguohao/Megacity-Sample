//  Add Platforms here that exclude Quit Menu option
#if !UNITY_EDITOR && (UNITY_PS4)
    #define PLATFORM_EXCLUDES_QUIT_MENU
#endif
using System.Collections.Generic;
using Unity.MegaCity.Audio;
using UnityEngine;
using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Button = UnityEngine.UIElements.Button;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Manages the UI for the main menu.
    /// This sets the audio settings for the city.
    /// Defines if the player should be manual or automatic.
    /// Allows the execution to exiting by pressing Escape
    /// Has access to the UI game settings
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private AudioMaster m_AudioMaster = null;
        [SerializeField]
        private GameObject m_Autopilot = null;
        [SerializeField]
        private HybridCameraManager m_HybridCameraManager;
        [SerializeField]
        private UIGameSettings m_GameSettings;

        private int m_CurrentMenuItem = 0;
        private int m_PrevMenuItem = 0;

        private Button m_OnRailsButton;
        private Button m_PlayerControllerButton;
        private Button m_QuitButton;
        private Button m_GameSettingsButton;
        private VisualElement m_VisualMenu;
        private VisualElement m_OverlayMenu;
        private VisualElement m_MainMenuContainer;
        private List<Button> m_Options;

        [SerializeField] public // public to suppress "always null" compiler warning
            ModeBootstrap bootstrap;

        private void Awake()
        {
            m_Options = new List<Button>();
            m_MainMenuContainer = GetComponent<UIDocument>().rootVisualElement;
            m_OnRailsButton = m_MainMenuContainer.Q<Button>("on-rails-button");
            m_PlayerControllerButton = m_MainMenuContainer.Q<Button>("player-controller-button");
            m_GameSettingsButton = m_MainMenuContainer.Q<Button>("settings-button");
            m_QuitButton = m_MainMenuContainer.Q<Button>("quit-button");
            m_VisualMenu = m_MainMenuContainer.Q<VisualElement>("visual-menu");
            m_OverlayMenu = m_MainMenuContainer.Q<VisualElement>("overlay");

            m_VisualMenu.style.display = DisplayStyle.Flex;

            m_OnRailsButton.clicked += () => { m_CurrentMenuItem = 0; SelectItem(m_CurrentMenuItem);  };
            m_PlayerControllerButton.clicked += () => { m_CurrentMenuItem = 1; SelectItem(m_CurrentMenuItem);  };
            m_GameSettingsButton.clicked += () => { m_CurrentMenuItem = 2; SelectItem(m_CurrentMenuItem);  };
            m_QuitButton.clicked += () => { m_CurrentMenuItem = 3; SelectItem(m_CurrentMenuItem);  };


            m_Options.Add(m_OnRailsButton);
            m_Options.Add(m_PlayerControllerButton);
            m_Options.Add(m_GameSettingsButton);
            m_Options.Add(m_QuitButton);
            SetMenuOptionUIElements(m_CurrentMenuItem);
        }

        private void Start()
        {
            // too strongly coupled to the ui so there we go :(
            // In Awake() the AudioManager is not present (before OnEnable()) so we would run into a null-ref exception.

            if (bootstrap.Options.SkipMenu)
            {
                OnRailsFlyoverRoutine ();
                gameObject.SetActive(false);
            }

            m_Autopilot.SetActive(false);
        }

        private void OnRailsFlyoverRoutine()
        {
            Debug.Log("Beginning on-rails flyover mode");
            m_HybridCameraManager.m_CameraTargetMode = HybridCameraManager.CameraTargetMode.DollyTrack;
            m_Autopilot.SetActive(true);
            AnimateOut();
        }

        private void PlayerControllerRoutine()
        {
            Debug.Log("Beginning player-controlled mode");
            m_HybridCameraManager.m_CameraTargetMode = HybridCameraManager.CameraTargetMode.FollowPlayer;
            m_Autopilot.SetActive(false);
            AnimateOut();
        }

        private void QuitDemo()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AnimateOut()
        {
            LoadAudioSettings();
            m_OverlayMenu.style.display = DisplayStyle.Flex;

            m_OverlayMenu.experimental.animation
                .Start(new StyleValues {opacity = 1}, 2000)
                .Ease(Easing.Linear)
                .OnCompleted(() =>{
                    m_VisualMenu.style.display = DisplayStyle.None;
                    m_VisualMenu.visible = false;
                    m_OverlayMenu.experimental.animation
                        .Start(new StyleValues {opacity = 0f}, 3000)
                        .Ease(Easing.Linear)
                        .OnCompleted(() =>{
                        m_OverlayMenu.style.display = DisplayStyle.None;
                    });
                });
        }

        private void LoadAudioSettings()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var audioSystemEntity = entityManager.CreateEntity(typeof(AudioSystemSettings));
            var systemSettings = new AudioSystemSettings
            {
                DebugMode = m_AudioMaster.showDebugLines,
                MaxDistance = m_AudioMaster.maxDistance,
                MaxSqDistance = m_AudioMaster.maxDistance * m_AudioMaster.maxDistance,
                MaxVehicles = m_AudioMaster.maxVehicles,
                ClosestEmitterPerClipCount = m_AudioMaster.closestEmitterPerClipCount,
            };
            entityManager.SetComponentData(audioSystemEntity, systemSettings);
        }

        private void Update()
        {
            if (m_VisualMenu.style.display == DisplayStyle.Flex)
            {
#if !PLATFORM_EXCLUDES_QUIT_MENU
                if (Input.GetKeyDown(KeyCode.Escape)) // Toggle Pause/Resume state
                    QuitDemo();
#endif
                //	Only restrict update interval with respect to moving, not selecting
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
                    ++m_CurrentMenuItem;
                else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A))
                    --m_CurrentMenuItem;
                else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetButtonDown("Submit"))
                    SelectItem(m_CurrentMenuItem);

                m_CurrentMenuItem = Mathf.Clamp(m_CurrentMenuItem, 0, (m_Options.Count > 0) ? (m_Options.Count - 1) : 0);

                if (m_CurrentMenuItem != m_PrevMenuItem)
                {
                    SetMenuOptionUIElements(m_CurrentMenuItem);
                    m_PrevMenuItem = m_CurrentMenuItem;
                }
            }
        }

        private void SetMenuOptionUIElements(int optionActive)
        {
            foreach (var buttonOption in m_Options)
                buttonOption.RemoveFromClassList("button-menu-active");

            m_Options[optionActive].AddToClassList("button-menu-active");
        }

        private void SelectItem(int currentOption)
        {
            switch (currentOption)
            {
                case 0: OnRailsFlyoverRoutine(); break;
                case 1: PlayerControllerRoutine(); break;
                case 2: ShowGameSettings(); break;
                case 3: QuitDemo(); break;
            }
        }

        private void ShowGameSettings()
        {
            m_GameSettings.Show(m_VisualMenu);
            m_VisualMenu.style.display= DisplayStyle.None;
        }
    }
}
