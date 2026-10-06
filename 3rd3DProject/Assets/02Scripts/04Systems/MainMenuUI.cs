using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>메인 화면. 버튼 두 개를 Inspector에 끌어다 놓으면 코드에서 직접 연결한다.</summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameSceneName = "PlaytestScene01";   // 실제 시작할 씬 이름으로 바꿀 것

    private void Awake()
    {
        startButton.onClick.AddListener(OnStart);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnStart()
    {
        Time.timeScale = 1f;   // 결과창 등에서 멈춘 시간이 남아 있지 않도록
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}