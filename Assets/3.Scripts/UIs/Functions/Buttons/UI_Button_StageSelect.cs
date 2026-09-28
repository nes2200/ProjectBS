using System.IO;
using TMPro;
using UnityEngine;

public class UI_Button_StageSelect : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI stageText;

    [Range(1, 5)]
    [SerializeField]int stage;
    [SerializeField] TextAsset stageDataJson;

    string customFilePath;
    string serverMapID;

    bool loadAsSandbox;

    public void SetFromLocalFile(string path, bool asSandbox = false)
    {
        customFilePath = path;
        loadAsSandbox = asSandbox;
        serverMapID = null;
        stageDataJson = null;
        stageText.text = Path.GetFileNameWithoutExtension(path);
    }
    public void SetFromServerFile(string mapID, string mapName, bool asSandbox = false)
    {
        customFilePath = null;
        serverMapID = mapID;
        loadAsSandbox = asSandbox;
        stageDataJson = null;
        stageText.text = mapName;
    }

    public void SetStageText(int chapter)
    {
        stageText.text = $"{chapter}-{stage}";
    }

    public async void ChangeSceneToStage()
    {
        try
        {
            //경로가 있고, 데이터가 없다 -> 커스텀 맵이구나
            if (!string.IsNullOrEmpty(customFilePath) && !stageDataJson)
            {
                stageDataJson = new TextAsset(File.ReadAllText(customFilePath));
                stageDataJson.name = Path.GetFileNameWithoutExtension(customFilePath);
            }
            //서버맵 아이디가 있고 데이터가 없다 -> 서버 맵이구나
            else if(!string.IsNullOrEmpty(serverMapID) && !stageDataJson)
            {
                CustomMapData map = await GameManager.DB.ReadDataAsync<CustomMapData>("customMaps", serverMapID);
                
                if (!this) return;

                if (map == null || map.data == null)
                {
                    Debug.LogError("서버에 맵 데이터가 없음");
                    return;
                }
                stageDataJson = new TextAsset(Newtonsoft.Json.JsonConvert.SerializeObject(map.data));
                stageDataJson.name = map.mapName;
            }

            if (loadAsSandbox)
            {
                GameManager.SceneLoad.LoadSandbox("StageScene", stageDataJson, serverMapID, stageDataJson.name);
            }
            else
            {
                bool isCustom = !string.IsNullOrEmpty(customFilePath) || !string.IsNullOrEmpty(serverMapID);
                GameManager.SceneLoad.LoadSceneAndSetup("StageScene", stageDataJson, isCustom);

            }
        }
        catch(System.Exception e)
        {
            Debug.LogError($"스테이지 파일 로드 실패: {e.Message}");
        }
    }

}
