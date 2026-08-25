using DreamOfMilitary.Routine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamOfMilitary.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioController : MonoBehaviour
    {
        private const string MusicVolumePreferenceKey = "Audio.MusicVolume";
        private const string EffectsVolumePreferenceKey = "Audio.EffectsVolume";

        public static GameAudioController Instance { get; private set; }

        [Header("배경음악")]
        [SerializeField] private AudioClip titleMusic;
        [SerializeField] private AudioClip lobbyMusic;
        [SerializeField] private AudioClip minigameMusic;
        [SerializeField] private AudioClip intermissionMusic;
        [SerializeField] private AudioClip successMusic;
        [SerializeField] private AudioClip failureMusic;
        [SerializeField] private AudioClip endingMusic;

        [Header("당직 졸기 / 엔딩 컷신 - 저벅저벅")]
        [SerializeField] private AudioClip footstepEffect;

        [Header("내무반 정리 - 옷·락커")]
        [SerializeField] private AudioClip clothEffect;
        [SerializeField] private AudioClip lockerEffect;

        [Header("MOPP 방호복 - 철컥")]
        [SerializeField] private AudioClip metalEffect;

        [Header("훈련 사격 - 총·타겟")]
        [SerializeField] private AudioClip gunshotEffect;
        [SerializeField, Range(0f, 1f)] private float gunshotVolumeScale = 0.35f;
        [SerializeField] private AudioClip targetHitEffect;

        [Header("TCCC - 출혈 발견·지혈대 부착·성공 비명")]
        [SerializeField] private AudioClip tcccBleedRevealEffect;
        [SerializeField] private AudioClip tcccTourniquetEffect;
        [SerializeField] private AudioClip tcccSuccessScreamEffect;

        [Header("체력측정: 팔굽혀펴기 - 숨소리")]
        [SerializeField] private AudioClip breathEffect;

        [Header("당직 졸기 - 수면·놀람")]
        [SerializeField] private AudioClip sleepEffect;
        [SerializeField] private AudioClip surpriseEffect;

        [Header("노래 게임 -  버튼 클릭")]
        [SerializeField] private AudioClip buttonEffect;

        [Header("나사 게임 -  드라이버 소리")]
        [SerializeField] private AudioClip driverEffect;

        [Header("제식 게임 -  걸음 걸이 소리")]
        [SerializeField] private AudioClip frontLeftEffect;
        [SerializeField] private AudioClip frontRightEffect;
        [SerializeField] private AudioClip playerLeftEffect;
        [SerializeField] private AudioClip playerRightEffect;

        [Header("인사 게임 -  성공 소리")]
        [SerializeField] private AudioClip successEffect;

        [Header("인원 수 계산 게임 -  계산기 클릭 소리")]
        [SerializeField] private AudioClip clickEffect;

        [Header("일과 결과 / 로비 버튼")]
        [SerializeField] private AudioClip textRevealEffect;
        [SerializeField] private AudioClip continueEffect;

        [Header("씬 이름")]
        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private string minigameSceneName = "Minigame";
        [SerializeField] private string endingSceneName = "Ending";

        private AudioSource _musicSource;
        private AudioSource _effectsSource;
        private RoutineRunner _routineRunner;
        private float _musicVolume;
        private float _effectsVolume;

        public float MusicVolume => _musicVolume;
        public float EffectsVolume => _effectsVolume;

        private void Awake()
        {
            InitializeTcccEffectDefaults();

            if (Instance != null && Instance != this)
            {
                CopyEffectConfigurationTo(Instance);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = CreateAudioSource("Background Music", true);
            _effectsSource = CreateAudioSource("Sound Effects", false);
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePreferenceKey, 1f));
            _effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumePreferenceKey, 1f));
            ApplyVolumes();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshForScene(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeRoutineRunner();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BindVolumeSliders(Slider musicSlider, Slider effectsSlider)
        {
            BindSlider(musicSlider, _musicVolume, SetMusicVolume);
            BindSlider(effectsSlider, _effectsVolume, SetEffectsVolume);
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            _musicSource.volume = _musicVolume;
            PlayerPrefs.SetFloat(MusicVolumePreferenceKey, _musicVolume);
            PlayerPrefs.Save();
        }

        public void SetEffectsVolume(float value)
        {
            _effectsVolume = Mathf.Clamp01(value);
            _effectsSource.volume = _effectsVolume;
            PlayerPrefs.SetFloat(EffectsVolumePreferenceKey, _effectsVolume);
            PlayerPrefs.Save();
        }

        public void PlayEffect(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null)
            {
                _effectsSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            }
        }

        public void PlayFootstep() => PlayEffect(footstepEffect);
        public void PlayCloth() => PlayEffect(clothEffect);
        public void PlayLocker() => PlayEffect(lockerEffect);
        public void PlayMetal() => PlayEffect(metalEffect);
        public void PlayGunshot() => PlayEffect(gunshotEffect, gunshotVolumeScale);
        public void PlayTargetHit() => PlayEffect(targetHitEffect);
        public void PlayTcccBleedReveal() => PlayEffect(tcccBleedRevealEffect);
        public void PlayTcccTourniquet() => PlayEffect(tcccTourniquetEffect);
        public void PlayTcccSuccessScream() => PlayEffect(tcccSuccessScreamEffect);
        public void PlayBreath() => PlayEffect(breathEffect);
        public void PlaySleep() => PlayEffect(sleepEffect, 0.55f);
        public void PlaySurprise() => PlayEffect(surpriseEffect);
        public void PlayTextReveal() => PlayEffect(textRevealEffect, 0.7f);
        public void PlayContinue() => PlayEffect(continueEffect);
        public void PlayUiClick() => PlayEffect(textRevealEffect);
        public void PlayScream() => PlayEffect(surpriseEffect);
        public void Playbutton() => PlayEffect(buttonEffect);
        public void Playdriver() => PlayEffect(driverEffect);
        public void PlayfrontLeft() => PlayEffect(frontLeftEffect);
        public void PlayfrontRight() => PlayEffect(frontRightEffect);
        public void PlayplayerLeft() => PlayEffect(playerLeftEffect);
        public void PlayplayerRight() => PlayEffect(playerRightEffect);
        public void Playsuccess() => PlayEffect(successEffect);
        public void PlayclickEffect() => PlayEffect(clickEffect);
      
        private AudioSource CreateAudioSource(string sourceName, bool loop)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);
            var source = sourceObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void InitializeTcccEffectDefaults()
        {
            // 기존 프리팹을 열었을 때에도 TCCC 전용 슬롯이 비어 재생되지 않도록
            // 최초 실행만 기존 효과음으로 채운다. 인스펙터에서 각 슬롯을 별도로 지정하면 덮어쓰지 않는다.
            if (tcccBleedRevealEffect == null)
            {
                tcccBleedRevealEffect = targetHitEffect;
            }

            if (tcccTourniquetEffect == null)
            {
                tcccTourniquetEffect = metalEffect;
            }

            if (tcccSuccessScreamEffect == null)
            {
                tcccSuccessScreamEffect = surpriseEffect;
            }
        }

        private void CopyEffectConfigurationTo(GameAudioController target)
        {
            CopyIfMissing(ref target.footstepEffect, footstepEffect);
            CopyIfMissing(ref target.clothEffect, clothEffect);
            CopyIfMissing(ref target.lockerEffect, lockerEffect);
            CopyIfMissing(ref target.metalEffect, metalEffect);
            CopyIfMissing(ref target.gunshotEffect, gunshotEffect);
            CopyIfMissing(ref target.targetHitEffect, targetHitEffect);
            CopyIfMissing(ref target.tcccBleedRevealEffect, tcccBleedRevealEffect);
            CopyIfMissing(ref target.tcccTourniquetEffect, tcccTourniquetEffect);
            CopyIfMissing(ref target.tcccSuccessScreamEffect, tcccSuccessScreamEffect);
            CopyIfMissing(ref target.breathEffect, breathEffect);
            CopyIfMissing(ref target.sleepEffect, sleepEffect);
            CopyIfMissing(ref target.surpriseEffect, surpriseEffect);
            CopyIfMissing(ref target.textRevealEffect, textRevealEffect);
            CopyIfMissing(ref target.continueEffect, continueEffect);
        }

        private static void CopyIfMissing(ref AudioClip destination, AudioClip source)
        {
            if (destination == null && source != null)
            {
                destination = source;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            RefreshForScene(scene);
        }

        private void RefreshForScene(Scene scene)
        {
            UnsubscribeRoutineRunner();

            if (scene.name == titleSceneName)
            {
                PlayMusic(titleMusic, true);
                return;
            }

            if (scene.name == lobbySceneName)
            {
                PlayMusic(lobbyMusic, true);
                return;
            }

            if (scene.name == endingSceneName)
            {
                PlayMusic(endingMusic, true);
                return;
            }

            if (!IsMinigameScene(scene.name))
            {
                StopMusic();
                return;
            }

            PlayMusic(intermissionMusic, true);
            _routineRunner = FindFirstObjectByType<RoutineRunner>();

            if (_routineRunner != null)
            {
                _routineRunner.StateChanged += OnRoutineStateChanged;
                _routineRunner.CommandShown += OnCommandShown;
                _routineRunner.FeedbackShown += OnFeedbackShown;
            }
            else
            {
                Debug.LogWarning("미니게임 씬에서 RoutineRunner를 찾지 못했습니다.", this);
            }
        }

        private void OnRoutineStateChanged(RoutineRunState state)
        {
            switch (state)
            {
                case RoutineRunState.ShowingProgress:
                case RoutineRunState.Completed:
                    PlayMusic(intermissionMusic, true);
                    break;
            }
        }

        private void OnCommandShown(string command, int current, int total)
        {
            // RoutineHUDView assigns this command to ToDo_Text from the same event.
            PlayMusic(minigameMusic, true);
        }

        private void OnFeedbackShown(MinigameJudgement judgement, int score)
        {
            PlayMusic(judgement == MinigameJudgement.Success ? successMusic : failureMusic, true);
        }

        private bool IsMinigameScene(string sceneName)
        {
            return sceneName == minigameSceneName || sceneName == "SampleMiniGameScene";
        }

        private void PlayMusic(AudioClip clip, bool restart)
        {
            if (clip == null)
            {
                StopMusic();
                return;
            }

            if (!restart && _musicSource.isPlaying && _musicSource.clip == clip)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        private void StopMusic()
        {
            _musicSource.Stop();
            _musicSource.clip = null;
        }

        private void ApplyVolumes()
        {
            _musicSource.volume = _musicVolume;
            _effectsSource.volume = _effectsVolume;
        }

        private void UnsubscribeRoutineRunner()
        {
            if (_routineRunner != null)
            {
                _routineRunner.StateChanged -= OnRoutineStateChanged;
                _routineRunner.CommandShown -= OnCommandShown;
                _routineRunner.FeedbackShown -= OnFeedbackShown;
                _routineRunner = null;
            }
        }

        private static void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveListener(callback);
            slider.onValueChanged.AddListener(callback);
        }
    }
}
