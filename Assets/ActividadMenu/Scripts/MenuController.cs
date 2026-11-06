// Actividad Menú II — Agustín López Parra y Carlos Manuel Palma Muñoz.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YaCasi.ActividadMenu
{
    public sealed class MenuController : MonoBehaviour
    {
        [Header("Paneles")]
        public GameObject mainPanel, settingsPanel, scoresPanel, creditsPanel;
        [Header("Navegación")]
        public Button playButton, settingsButton, scoresButton, creditsButton, quitButton;
        public Button[] backButtons;
        [Header("Ajustes")]
        public TMP_InputField playerNameField;
        public TMP_Dropdown qualityDropdown;
        public Slider brightnessSlider, volumeSlider;
        public Toggle fullscreenToggle;
        public Image brightnessOverlay;
        [Tooltip("Índice real en Project Settings > Quality para Baja, Media, Alta y Ultra.")]
        public int[] qualityIndices = { 0, 2, 4, 5 };
        [Header("Escena y audio")]
        public string gameSceneName = "PruebaMenu";
        public AudioSource uiAudio;
        public AudioClip clickClip;
        const string Prefix = "YaCasi.ActividadMenu.";
        int originalQuality;
        float originalVolume;

        void Awake()
        {
            originalQuality = QualitySettings.GetQualityLevel();
            originalVolume = AudioListener.volume;
            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(() => Show(settingsPanel));
            scoresButton.onClick.AddListener(() => Show(scoresPanel));
            creditsButton.onClick.AddListener(() => Show(creditsPanel));
            quitButton.onClick.AddListener(Quit);
            foreach (var button in backButtons) button.onClick.AddListener(() => Show(mainPanel));
            playerNameField.SetTextWithoutNotify(PlayerPrefs.GetString(Prefix + "Name", ""));
            playerNameField.onEndEdit.AddListener(v => { PlayerPrefs.SetString(Prefix + "Name", v.Trim()); PlayerPrefs.Save(); });
            int quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", 2), 0, 3);
            qualityDropdown.SetValueWithoutNotify(quality);
            qualityDropdown.onValueChanged.AddListener(SetQuality);
            ApplyQuality(quality);
            float brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Brightness", .8f));
            brightnessSlider.SetValueWithoutNotify(brightness);
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            ApplyBrightness(brightness);
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", .6f));
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            SetVolume(volume);
        }
        void Start() { Show(mainPanel, false); }
        public void Show(GameObject panel) { Show(panel, true); }
        void Show(GameObject panel, bool click)
        {
            if (click) Click();
            mainPanel.SetActive(panel == mainPanel);
            settingsPanel.SetActive(panel == settingsPanel);
            scoresPanel.SetActive(panel == scoresPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                var first = panel.GetComponentInChildren<Selectable>();
                if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }
        public void SetQuality(int option)
        {
            ApplyQuality(option);
            PlayerPrefs.SetInt(Prefix + "Quality", option);
        }
        void ApplyQuality(int option)
        {
            if (qualityIndices == null || qualityIndices.Length == 0 || QualitySettings.names.Length == 0) return;
            int mapped = qualityIndices[Mathf.Clamp(option, 0, qualityIndices.Length - 1)];
            QualitySettings.SetQualityLevel(Mathf.Clamp(mapped, 0, QualitySettings.names.Length - 1), true);
        }
        public void SetBrightness(float value)
        {
            ApplyBrightness(value);
            PlayerPrefs.SetFloat(Prefix + "Brightness", value);
        }
        void ApplyBrightness(float value)
        {
            // Atenuación local del Canvas: no cambia luces, materiales ni volumen del juego.
            if (brightnessOverlay != null) brightnessOverlay.color = new Color(0f, 0f, 0f, (1f - Mathf.Clamp01(value)) * .65f);
        }
        public void SetVolume(float value)
        {
            // Solo modifica los AudioSource de esta actividad, no el volumen global del nivel 1.
            foreach (var source in GetComponentsInChildren<AudioSource>(true)) source.volume = Mathf.Clamp01(value);
            if (uiAudio != null) uiAudio.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + "Volume", value);
        }
        public void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt(Prefix + "Fullscreen", value ? 1 : 0);
        }
        public void Play()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            { Debug.LogError("La escena de prueba no está incluida en la compilación: " + gameSceneName, this); return; }
            Click();
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        void Click() { if (uiAudio != null && clickClip != null) uiAudio.PlayOneShot(clickClip); }
        public void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        {
            // Calidad es global en Unity: devolver el valor previo al abandonar la actividad.
            QualitySettings.SetQualityLevel(originalQuality, true);
            AudioListener.volume = originalVolume;
            PlayerPrefs.Save();
        }
    }
}
