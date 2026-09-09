using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : ManagerBase
{
    private string currentStageSceneName;
    private TextAsset currentStageData;
    private UIType currentScreen;
    private bool isLoading;

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {

    }

    public void LoadSceneAndSetup(string sceneName, TextAsset stageData)
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(sceneName) || !stageData)
        {
            Debug.LogError("[SceneLoadManager] 스테이지 정보가 올바르지 않습니다.");
            return;
        }

        currentStageSceneName = sceneName;
        currentStageData = stageData;
        currentScreen = UIType.Stage;

        isLoading = true;
        StartCoroutine(CoReloadSceneAndSetup(sceneName, stageData, UIType.Stage));
    }

    public void LoadSandbox(string sceneName, TextAsset sandboxData)
    {
        if (isLoading) return;

        if(string.IsNullOrEmpty(sceneName) || sandboxData is null)
        {
            Debug.LogError("[SceneLoadManager] 샌드박스 씬 이름이 비어 있습니다.");
            return;
        }

        currentStageSceneName = sceneName;
        currentStageData = sandboxData;
        currentScreen = UIType.Sandbox;

        isLoading = true;
        StartCoroutine(CoReloadSceneAndSetup(sceneName, sandboxData, UIType.Sandbox));
    }

    public void RestartCurrentStage()
    {
        if (isLoading) return;

        if (string.IsNullOrWhiteSpace(currentStageSceneName) || !currentStageData)
        {
            Debug.LogError("[SceneLoadManager] 다시 시작할 스테이지 정보가 없습니다.");
            return;
        }

        UIManager.ClaimCloseUI(UIType.Stage);

        isLoading = true;
        StartCoroutine(CoReloadSceneAndSetup(currentStageSceneName, currentStageData, currentScreen));
    }

    private IEnumerator CoReloadSceneAndSetup(string sceneName, TextAsset stageData, UIType targetScreen)
    {
        UIManager.ClaimOpenScreen(targetScreen, ScreenChangeType.SlideChanger);

        //해당 씬 로드 확인
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.isLoaded)
        {
            //기존 씬 언로드 작업
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);

            //언로드 완료까지 대기
            while(unloadOperation != null && !unloadOperation.isDone)
            {
                yield return null;
            }
        }

        //새 씬을 비동기로 불러오기
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        //로드하려는 씬은 게임 오브젝트들이 올라가있는 씬이기에 액티브 씬으로 지정해주기
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(newScene);

        //로드한 씬에서 스테이지 업데이트하기
        BattleFieldMode mode = targetScreen == UIType.Sandbox ? BattleFieldMode.Sandbox : BattleFieldMode.Stage;
        GameManager.StageLoad.LoadStage(stageData, newScene, mode);

        isLoading = false;
        yield return null;
    }
}
