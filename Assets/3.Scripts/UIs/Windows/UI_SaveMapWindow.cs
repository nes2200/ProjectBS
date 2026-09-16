using Newtonsoft.Json;
using System.IO;
using TMPro;
using UnityEngine;

public class UI_SaveMapWindow : OpenableUIBase
{
    [SerializeField] TMP_InputField inputField;
    StageDataAuthoring authoring;
    string mapName;
    bool isSavingOnServer;
    int windowVersion;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        inputField.characterLimit = 20;
    }

    public override void Open()
    {
        base.Open();
        windowVersion++;
        inputField.SetTextWithoutNotify("");
    }
    public override void Close()
    {
        windowVersion++;
        authoring = null;
        inputField.SetTextWithoutNotify("");
        base.Close();
    }

    public void OnEndEdit(string input)
    {
        mapName = input.Trim();
        Debug.Log(mapName);
    }

    public void OpenForSave(StageDataAuthoring source)
    {
        authoring = source;
        Open();
    }

    public void SaveMapDataOnLocal()
    {
        string savingMapName = inputField.text.Trim();
        if (string.IsNullOrEmpty(savingMapName) )
        {
            UIManager.ClaimPopUp("저장 실패", "맵 이름 확인", "확인");
            return;
        }
        if (savingMapName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || savingMapName.EndsWith("."))
        {
            UIManager.ClaimPopUp("저장 실패", "사용할 수 없는 단어 있음", "확인");
            return;
        }
        if (!authoring)
        {
            UIManager.ClaimPopUp("저장 실패", "데이터 연결 없음", "확인");
            return;
        }
        try
        {
#if UNITY_EDITOR
            string directory = Path.Combine(
                Application.dataPath,
                "1.Datas", "Origin", "StageData", "Globals", "Custom");
#else
        string directory = Path.Combine(
            Application.persistentDataPath, "StageData", "Custom");
#endif

            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, savingMapName + ".json");

            // 우선 기존 맵 덮어쓰기 방지
            if (File.Exists(path))
            {
                UIManager.ClaimPopUp("저장 실패", "같은 이름의 맵이 존재함", "확인");
                return;
            }

            SceneSaveData data = SceneScanner.Capture(authoring.gameObject.scene, authoring, GetPrefabKey);
            if (!ValidateSaveData(data)) return;

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            File.WriteAllText(path, json);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Close();
            UIManager.ClaimPopUp("저장 성공", $"{savingMapName} 저장 성공", "확인");
        }
        catch (System.Exception e)
        {
            UIManager.ClaimPopUp("저장 실패", $"저장 실패\n{e.Message}", "확인");
        }
    }

    public async void SaveMapOnServer()
    {
        if (isSavingOnServer) return;

        string savingMapName = inputField.text.Trim();
        if (string.IsNullOrEmpty(savingMapName))
        {
            UIManager.ClaimPopUp("저장 실패", "맵 이름 확인", "확인");
            return;
        }
        if (!authoring)
        {
            UIManager.ClaimPopUp("저장 실패", "데이터 연결 없음", "확인");
            return;
        }

        isSavingOnServer = true;
        int savingWindowVersion = windowVersion;
        try
        {
            SceneSaveData data = SceneScanner.Capture(authoring.gameObject.scene, authoring, GetPrefabKey);
            if (!ValidateSaveData(data)) return;
            if (!GameManager.Instance || !GameManager.DB)
                throw new System.InvalidOperationException("DBManager 연결 없음");

            CustomMapData customMap = new CustomMapData
            {
                mapName = savingMapName,
                data = data
            };

            string mapId = await GameManager.DB.SaveCustomMapAsync(customMap);
            if (!this) return;

            // 저장 중 다시 연 창은 닫지 않는다.
            if (savingWindowVersion == windowVersion) Close();
            Debug.Log($"Map uploaded: customMaps/{mapId}");
            UIManager.ClaimPopUp("저장 성공", $"{savingMapName} 서버 저장 성공", "확인");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (this) UIManager.ClaimPopUp("저장 실패", $"서버 저장 실패\n{e.Message}", "확인");
        }
        finally
        {
            isSavingOnServer = false;
        }
    }

    private static string GetPrefabKey(GameObject obj)
    {
        return obj.TryGetComponent<PrefabIdentity>(out var identity)
            ? identity.PrefabKey
            : null;
    }

    private static bool ValidateSaveData(SceneSaveData data)
    {
        //코스트
        if(data.costLimits == null || data.costLimits.Length == 0)
        {
            UIManager.ClaimPopUp("저장 실패", "코스트 리밋 데이터 없음.", "확인");
            return false;
        }
        foreach(int costLimit in data.costLimits)
        {
            if(costLimit == 0)
            {
                UIManager.ClaimPopUp("저장 실패", "코스트 리밋에 0이 포함되어 있습니다.", "확인");
                return false;
            }
        }

        //유닛 엔트리
        if (data.selectableUnits == null || data.selectableUnits.Count == 0)
        {
            UIManager.ClaimPopUp("저장 실패", "유닛 엔트리를 하나 이상 선택해 주세요.", "확인");
            return false;
        }

        //적 소환
        bool hasSpawnedEnemy = data.objects != null && data.objects.Exists(stageObject => stageObject.parentName == "TeamB");
        if (!hasSpawnedEnemy)
        {
            UIManager.ClaimPopUp("저장 실패", "적을 하나 이상 소환하세요.", "확인");
            return false;
        }

        return true;
    }
}
