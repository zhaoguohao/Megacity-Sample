using Unity.Entities;
using Unity.Mathematics;
using Unity.MegaCity.Streaming;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Unity.MegaCity.UI
{
    /// <summary>
    ///     Reads the progress value from GameLoadInfo singleton and update the loading progress bar accordingly
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private EntityManager m_EntityManager;
        private SceneSystem m_SceneSystem;

        private VisualElement m_MainMenu;
        private VisualElement m_LoadingScreen;
        private ProgressBar m_ProgressBar;

// Only enable the loading screen in player builds
#if !UNITY_EDITOR
    private void Start()
    {
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        SetUpUI();
    }

    private void SetUpUI()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_MainMenu = root.Q<VisualElement>("visual-menu");
        m_LoadingScreen = root.Q<VisualElement>("loading-screen");
        m_ProgressBar = root.Q<ProgressBar>("progressbar");

        m_MainMenu.style.display = DisplayStyle.None;
        m_LoadingScreen.style.display = DisplayStyle.Flex;
    }

    private void FixedUpdate()
    {
        var gameLoadInfo = m_EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GameLoadInfo>())
            .GetSingleton<GameLoadInfo>();

        m_ProgressBar.value = math.lerp(m_ProgressBar.value, gameLoadInfo.GetProgress(), Time.deltaTime);

        if (gameLoadInfo.IsLoaded)
        {
            m_MainMenu.style.display = DisplayStyle.Flex;
            m_LoadingScreen.style.display = DisplayStyle.None;
        }
    }
#endif
    }
}
