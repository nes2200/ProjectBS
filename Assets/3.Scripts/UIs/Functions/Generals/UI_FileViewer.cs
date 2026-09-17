using System;
using System.IO;
using UnityEngine;

public class UI_FileViewer : MonoBehaviour
{
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Transform contentRoot;
    

    public void Clear()
    {
        //화면을 다시 열때 이전 버튼 제거
        foreach (Transform child in contentRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void ReadLocalFiles()
    {
#if UNITY_EDITOR
        string directory = Path.Combine(Application.dataPath, "1.Datas", "Origin", "StageData", "Globals", "Custom");
#else
        string directory = Path.Combine(Application.persistentDataPath, "StageData", "Custom");
#endif
        if (!Directory.Exists(directory)) return;
        BuildButtons(directory);
    }

    public void BuildButtons(string directory)
    {
        try
        {
            string[] paths = Directory.GetFiles(directory, "*.json");
            Array.Sort(paths);

            foreach(string path in paths)
            {
                AddButton().GetComponent<UI_Button_StageSelect>().SetFromLocalFile(path);
            }
        }
        catch(Exception e)
        {
            Debug.LogError($"파일 목록 로드 실패 : {e.Message}");
        }
    }
    public GameObject AddButton()
    {
        return Instantiate(buttonPrefab, contentRoot, false);
    }
}
