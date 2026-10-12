using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>설정창이 필요한 소리·화면 적용 기능만 요청하는 계약.</summary>
public interface IGameSettingsApplier
{
    IReadOnlyList<GameResolution> Resolutions { get; }
    void ApplyMasterVolume(float value);
    void ApplySfxVolume(float value);
    int InitializeResolutions();
    void ApplyResolution(int index);
}

/// <summary>UI가 Unity의 Screen API 없이 표시할 수 있는 화면 크기.</summary>
public readonly struct GameResolution
{
    public int Width { get; }
    public int Height { get; }

    public GameResolution(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

/// <summary>
/// 저장된 설정과 UI 요청을 Unity 소리·화면 API에 연결한다.
/// 새 컴포넌트가 아니며, AudioMixer 적용은 Start 이후에 호출한다.
/// </summary>
public sealed class GameSettingsApplier : IGameSettingsApplier
{
    private readonly AudioMixer mixer;
    private readonly string sfxParameter;
    private readonly List<GameResolution> resolutions = new List<GameResolution>();
    private bool warnedMixer;

    public IReadOnlyList<GameResolution> Resolutions => resolutions;

    public GameSettingsApplier(AudioMixer mixer, string sfxParameter)
    {
        this.mixer = mixer;
        this.sfxParameter = sfxParameter;
    }

    public void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    public void ApplySfxVolume(float value)
    {
        if (mixer == null) return;
        if (mixer.SetFloat(sfxParameter, GameSettings.VolumeToDecibel(Mathf.Clamp01(value)))) return;
        if (warnedMixer) return;

        warnedMixer = true;
        Debug.LogWarning($"GameSettingsApplier: '{sfxParameter}' 효과음 설정을 적용하지 못했습니다. 믹서의 노출된 파라미터 이름과 편집 상태를 확인하세요.");
    }

    /// <summary>지원 해상도를 준비하고 저장값을 적용한 뒤, UI가 선택할 번호를 반환한다.</summary>
    public int InitializeResolutions()
    {
        resolutions.Clear();
        foreach (Resolution resolution in Screen.resolutions)
        {
            var size = new GameResolution(resolution.width, resolution.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }
        if (resolutions.Count == 0)
            resolutions.Add(new GameResolution(Screen.width, Screen.height));

        bool hasSaved = GameSettings.TryGetSavedResolution(out int savedWidth, out int savedHeight);
        if (hasSaved && !Application.isEditor &&
            (savedWidth != Screen.width || savedHeight != Screen.height))
            Screen.SetResolution(savedWidth, savedHeight, Screen.fullScreenMode);

        int targetWidth = hasSaved ? savedWidth : Screen.width;
        int targetHeight = hasSaved ? savedHeight : Screen.height;
        return FindClosestResolution(targetWidth, targetHeight);
    }

    /// <summary>슬라이더를 놓을 때 호출한다. 드래그 중에는 화면 크기를 반복 변경하지 않는다.</summary>
    public void ApplyResolution(int index)
    {
        if (resolutions.Count == 0) return;
        index = Mathf.Clamp(index, 0, resolutions.Count - 1);
        GameResolution size = resolutions[index];
        Screen.SetResolution(size.Width, size.Height, Screen.fullScreenMode);
        GameSettings.SaveResolution(size.Width, size.Height);

        if (Application.isEditor)
            Debug.Log($"GameSettingsApplier: 해상도 {size.Width} x {size.Height} 설정을 저장했습니다. 실제 창·전체 화면 동작은 빌드에서도 확인하세요.");
    }

    private int FindClosestResolution(int width, int height)
    {
        int closest = 0;
        long bestDifference = long.MaxValue;
        for (int i = 0; i < resolutions.Count; i++)
        {
            long difference = System.Math.Abs((long)resolutions[i].Width - width) +
                              System.Math.Abs((long)resolutions[i].Height - height);
            if (difference >= bestDifference) continue;
            bestDifference = difference;
            closest = i;
        }
        return closest;
    }
}
