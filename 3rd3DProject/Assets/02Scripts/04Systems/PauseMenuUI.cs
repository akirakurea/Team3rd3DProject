using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ESC를 누르면 열리는 설정(일시정지) 창. 열려 있는 동안 게임이 멈춘다. (Time.timeScale = 0)
/// - ESC를 다시 누르면 창이 닫히고 게임이 재개된다. (계속하기 버튼은 선택 사항)
/// - 슬라이더 4개: 전체 볼륨 / 효과 볼륨 / 마우스 감도 / 해상도 (연결한 것만 동작, 비워 두면 건너뜀)
/// - 다시 시작: GameManager.RestartGame() 호출
/// - 게임 종료: 빌드에서는 Application.Quit(), 에디터에서는 재생 중지
/// 값은 GameSettings를 통해 PlayerPrefs에 저장된다.
///
/// 결과 화면 / 승리 / 패배 화면에서는 ESC로 열리지 않는다. 열려 있는 중에 그런 화면으로 넘어가면 설정창은 자동으로 닫힌다.
///
/// 씬 구성: "항상 켜져 있는" Canvas 오브젝트에 이 스크립트를 붙이고, 패널(panel)은 자식으로 두어 기본 비활성으로 둔다.
///   Panel - 슬라이더들(+ 값을 보여줄 TMP 텍스트, 선택), RestartButton, QuitButton
///
/// 효과 볼륨: AudioMixer의 노출된 파라미터(기본 "SFXVolume")로 조절한다. 효과음 AudioSource의 Output을 그 믹서 그룹으로 지정해야 적용된다.
/// 해상도: 슬라이더를 놓는 순간 적용된다. (에디터의 Game 뷰 크기는 바뀌지 않고 빌드에서만 실제로 바뀐다)
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("버튼")]
    [SerializeField] private Button resumeButton;    // 선택: "계속하기" (비워 두면 ESC로만 닫힌다)
    [SerializeField] private Button restartButton;   // "다시 시작"
    [SerializeField] private Button quitButton;      // "게임 종료"

    [Header("소리")]
    [FormerlySerializedAs("volumeSlider")][SerializeField] private Slider masterSlider;   // 전체 볼륨
    [FormerlySerializedAs("volumeText")][SerializeField] private TMP_Text masterText;     // 선택: "70%"
    [SerializeField] private Slider sfxSlider;                                             // 효과 볼륨
    [SerializeField] private TMP_Text sfxText;                                             // 선택: "70%"
    [SerializeField] private AudioMixer audioMixer;                                        // 효과 볼륨용 (없으면 효과 슬라이더는 값만 저장)
    [SerializeField] private string sfxMixerParameter = "SFXVolume";                       // 믹서에서 노출(Expose)한 파라미터 이름

    [Header("마우스 감도")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;   // 선택: "1.0x"

    [Header("해상도")]
    [SerializeField] private Slider resolutionSlider;
    [SerializeField] private TMP_Text resolutionText;    // 선택: "1920 x 1080"

    private GameManager gm;
    private bool isOpen;
    private CursorLockMode prevLockState;
    private bool prevCursorVisible;
    private bool warnedMixer;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    private void Start()
    {
        panel.SetActive(false);

        SetupMasterVolume();
        SetupSfxVolume();
        SetupSensitivity();
        SetupResolution();

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);

        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("PauseMenuUI: GameManager를 찾을 수 없어 ESC로 열 수 없습니다.");
            return;
        }
        gm.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (gm != null) gm.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (!EscapePressed()) return;

        if (isOpen) Resume();
        else Open();
    }

    // ───────────── 열기 / 닫기 ─────────────

    private void Open()
    {
        if (gm == null) return;
        // 게임이 흐르는 중(진행 중 / 마지막 쥐를 잡은 직후)에만 연다
        if (gm.State != GameManager.GameState.Playing && gm.State != GameManager.GameState.RoundClear) return;

        isOpen = true;

        // 게임이 커서를 잠그고 있었다면 설정창에서 마우스를 쓸 수 있게 풀어준다
        prevLockState = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    private void Resume()
    {
        if (!isOpen) return;
        Hide(restoreCursor: true);
        Time.timeScale = 1f;
    }

    private void Restart()
    {
        Hide(restoreCursor: true);
        // timeScale은 GameManager가 재시작하면서 1로 되돌린다
        if (gm != null) gm.RestartGame();
    }

    private void Quit()
    {
        Time.timeScale = 1f;   // 멈춘 시간이 남지 않게 되돌린다
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서는 재생 중지
#else
        Application.Quit();
#endif
    }

    private void Hide(bool restoreCursor)
    {
        isOpen = false;
        panel.SetActive(false);
        PlayerPrefs.Save();

        if (restoreCursor)
        {
            Cursor.lockState = prevLockState;
            Cursor.visible = prevCursorVisible;
        }
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (!isOpen) return;

        // 설정창이 열려 있는 중에 결과/승리/패배 화면으로 넘어가면 설정창만 닫는다.
        // (시간 정지는 GameManager가 관리하고, 그 화면들은 마우스가 필요하니 커서는 그대로 둔다)
        if (state == GameManager.GameState.RoundResult ||
            state == GameManager.GameState.Win ||
            state == GameManager.GameState.Lose)
        {
            Hide(restoreCursor: false);
        }
    }

    // ───────────── 전체 볼륨 ─────────────

    private void SetupMasterVolume()
    {
        float v = GameSettings.MasterVolume;
        AudioListener.volume = v;   // 슬라이더가 없어도 저장된 값은 적용한다

        if (masterSlider == null) return;
        SetupZeroToOne(masterSlider, v);
        SetPercentText(masterText, v);
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
    }

    private void OnMasterChanged(float value)
    {
        GameSettings.MasterVolume = value;
        AudioListener.volume = value;
        SetPercentText(masterText, value);
    }

    // ───────────── 효과 볼륨 ─────────────

    private void SetupSfxVolume()
    {
        float v = GameSettings.SfxVolume;
        ApplySfx(v);   // 슬라이더가 없어도 저장된 값은 적용한다

        if (sfxSlider == null) return;
        SetupZeroToOne(sfxSlider, v);
        SetPercentText(sfxText, v);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnSfxChanged(float value)
    {
        GameSettings.SfxVolume = value;
        ApplySfx(value);
        SetPercentText(sfxText, value);
    }

    private void ApplySfx(float value)
    {
        if (audioMixer == null) return;
        if (!audioMixer.SetFloat(sfxMixerParameter, GameSettings.VolumeToDecibel(value)) && !warnedMixer)
        {
            warnedMixer = true;
            Debug.LogWarning($"PauseMenuUI: 믹서에 '{sfxMixerParameter}' 파라미터가 노출(Expose)돼 있지 않습니다.");
        }
    }

    // ───────────── 마우스 감도 ─────────────

    private void SetupSensitivity()
    {
        if (sensitivitySlider == null) return;

        sensitivitySlider.minValue = GameSettings.MinSensitivity;
        sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
        sensitivitySlider.wholeNumbers = false;
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        SetSensitivityText(GameSettings.MouseSensitivity);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;
        SetSensitivityText(value);
    }

    private void SetSensitivityText(float value)
    {
        if (sensitivityText != null) sensitivityText.text = $"{value:0.0}x";
    }

    // ───────────── 해상도 ─────────────

    private void SetupResolution()
    {
        // 모니터가 지원하는 해상도 중 가로x세로가 같은 것은 하나만 남긴다 (주사율만 다른 중복 제거)
        resolutions.Clear();
        foreach (var r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }
        if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));

        // 저장된 해상도가 있으면 시작할 때 적용 (에디터에서는 Game 뷰 크기를 바꿀 수 없어 건너뜀)
        bool hasSaved = GameSettings.TryGetSavedResolution(out int savedW, out int savedH);
        if (hasSaved && !Application.isEditor && (savedW != Screen.width || savedH != Screen.height))
            Screen.SetResolution(savedW, savedH, Screen.fullScreenMode);

        if (resolutionSlider == null) return;

        int targetW = hasSaved ? savedW : Screen.width;
        int targetH = hasSaved ? savedH : Screen.height;
        int index = FindClosestResolution(targetW, targetH);

        resolutionSlider.minValue = 0;
        resolutionSlider.maxValue = resolutions.Count - 1;
        resolutionSlider.wholeNumbers = true;
        resolutionSlider.interactable = resolutions.Count > 1;
        resolutionSlider.SetValueWithoutNotify(index);
        SetResolutionText(index);

        // 드래그하는 동안에는 글자만 바꾸고, 슬라이더를 놓는 순간 실제로 적용한다 (화면이 계속 깜빡이는 것 방지)
        resolutionSlider.onValueChanged.AddListener(OnResolutionSliderChanged);
        AddPointerUpListener(resolutionSlider.gameObject, ApplyResolution);
    }

    private int FindClosestResolution(int width, int height)
    {
        int best = 0;
        int bestDiff = int.MaxValue;
        for (int i = 0; i < resolutions.Count; i++)
        {
            int diff = Mathf.Abs(resolutions[i].x - width) + Mathf.Abs(resolutions[i].y - height);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }
        return best;
    }

    private void OnResolutionSliderChanged(float value)
    {
        SetResolutionText(Mathf.RoundToInt(value));
    }

    private void ApplyResolution()
    {
        int index = Mathf.Clamp(Mathf.RoundToInt(resolutionSlider.value), 0, resolutions.Count - 1);
        Vector2Int size = resolutions[index];

        Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
        GameSettings.SaveResolution(size.x, size.y);

        if (Application.isEditor)
            Debug.Log($"PauseMenuUI: 해상도 {size.x} x {size.y} 저장됨. 에디터에서는 Game 뷰 크기가 바뀌지 않고 빌드에서만 적용됩니다.");
    }

    private void SetResolutionText(int index)
    {
        if (resolutionText == null || index < 0 || index >= resolutions.Count) return;
        resolutionText.text = $"{resolutions[index].x} x {resolutions[index].y}";
    }

    // ───────────── 공용 도우미 ─────────────

    private static void SetupZeroToOne(Slider slider, float value)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
    }

    private static void SetPercentText(TMP_Text text, float value)
    {
        if (text != null) text.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    /// <summary>슬라이더에서 마우스를 뗀 순간 action을 호출한다. (EventTrigger를 코드로 붙임)</summary>
    private static void AddPointerUpListener(GameObject target, UnityAction action)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}