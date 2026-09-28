using TMPro;
using UnityEngine;

public class UI_UpdateMapWindow : OpenableUIBase
{
    [SerializeField] TMP_InputField inputField;
    StageDataAuthoring authoring;
    string mapID;
    string originalMapName;
    bool isUpdating;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        inputField.characterLimit = 20;
    }

    public override void Open()
    {
        base.Open();
        inputField.SetTextWithoutNotify(originalMapName);

    }

    public void OpenForUpdate(StageDataAuthoring source, string targetMapID, string currentMapName)
    {
        authoring = source;
        mapID = targetMapID;
        originalMapName = currentMapName;

        Open();
    }

    public async void UpdateMap()
    {
        if (isUpdating) return;

        string enteredMapName = inputField.text.Trim();
        if (string.IsNullOrEmpty(enteredMapName))
        {
            UIManager.ClaimErrorMessage("맵 이름을 입력하시오");
            return;
        }
        if(!authoring || string.IsNullOrEmpty(mapID))
        {
            UIManager.ClaimErrorMessage("수정할 맵 정보가 없음");
            return;
        }

        bool isNameChanged = !string.Equals(originalMapName, enteredMapName, System.StringComparison.Ordinal);
        isUpdating = true;

        try
        {
            SceneSaveData data = SceneScanner.Capture(authoring.gameObject.scene, authoring, GetPrefabKey);

            CustomMapData updateMap = new()
            {
                mapName = enteredMapName,
                data = data
            };

            await GameManager.DB.UpdateCustomMapAsync(mapID, updateMap, isNameChanged);

            Close();
            UIManager.ClaimPopUp("맵 업데이트", isNameChanged ? "맵 이름과 데이터를 변경함" : "맵 데이터를 저장함", "확인");
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
            UIManager.ClaimErrorMessage($"업데이트 실패\n{e.Message}");
        }
        finally
        {
            isUpdating = false;
        }
    }

    private static string GetPrefabKey(GameObject obj)
    {
        return obj.TryGetComponent<PrefabIdentity>(out PrefabIdentity identity) ? identity.PrefabKey : null;
    }

}
