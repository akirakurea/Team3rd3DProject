using UnityEngine;

/// <summary>
/// 설정창에서 바꾼 값(전체 볼륨, 효과 볼륨, 마우스 감도, 해상도)을 저장하고 어디서든 읽을 수 있게 하는 정적 클래스.
/// 값은 PlayerPrefs에 저장되어 다음 실행 때도 유지된다.
///
/// 마우스 감도는 카메라/고양이 컨트롤러가 직접 읽어서 시점 회전 입력에 곱해야 적용된다.
///   예) Vector2 look = lookInput * GameSettings.MouseSensitivity;
/// </summary>
public static class GameSettings
{
    private const string MasterKey = "MasterVolume";   // 이전 버전(PauseMenuUI)과 같은 키라서 저장된 값이 유지된다
    private const string SfxKey = "SfxVolume";
    private const string SensitivityKey = "MouseSensitivity";
    private const string ResWidthKey = "ResolutionWidth";
    private const string ResHeightKey = "ResolutionHeight";

    public const float DefaultVolume = 0.5f;        // 볼륨 슬라이더 기본 위치: 가운데
    public const float DefaultSensitivity = 1f;     // 감도 배율 기본값
    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 1.9f;       // 최소와 최대의 가운데가 1.0이 되도록 (기본값 1이 슬라이더 가운데에 옴)

    private static bool loaded;
    private static float master = DefaultVolume;
    private static float sfx = DefaultVolume;
    private static float sensitivity = DefaultSensitivity;

    // Enter Play Mode Options에서 Domain Reload를 꺼둔 경우 static이 남아있는 것을 방지
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultVolume));
        sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultVolume));
        sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity), MinSensitivity, MaxSensitivity);
        loaded = true;
    }

    /// <summary>전체 볼륨 (0~1)</summary>
    public static float MasterVolume
    {
        get { EnsureLoaded(); return master; }
        set
        {
            EnsureLoaded();
            master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, master);
        }
    }

    /// <summary>효과음 볼륨 (0~1)</summary>
    public static float SfxVolume
    {
        get { EnsureLoaded(); return sfx; }
        set
        {
            EnsureLoaded();
            sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, sfx);
        }
    }

    /// <summary>마우스 감도 배율 (기본 1). 시점 회전 입력에 곱해서 쓴다.</summary>
    public static float MouseSensitivity
    {
        get { EnsureLoaded(); return sensitivity; }
        set
        {
            EnsureLoaded();
            sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
        }
    }

    public static bool TryGetSavedResolution(out int width, out int height)
    {
        width = PlayerPrefs.GetInt(ResWidthKey, 0);
        height = PlayerPrefs.GetInt(ResHeightKey, 0);
        return width > 0 && height > 0;
    }

    public static void SaveResolution(int width, int height)
    {
        PlayerPrefs.SetInt(ResWidthKey, width);
        PlayerPrefs.SetInt(ResHeightKey, height);
    }

    /// <summary>0~1 볼륨을 AudioMixer용 데시벨(-80~0)로 바꾼다. 0이면 -80dB(무음).</summary>
    public static float VolumeToDecibel(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
    }
}