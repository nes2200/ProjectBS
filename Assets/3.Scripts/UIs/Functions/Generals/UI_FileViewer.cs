using System;
using System.IO;
using TMPro;
using UnityEngine;

public class UI_FileViewer : MonoBehaviour
{
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Transform contentRoot;
    [SerializeField] GameObject innerTextObj;
    [SerializeField] TextMeshProUGUI innerText;

    public void Clear()
    {
        //화면을 다시 열때 이전 버튼 제거
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentRoot.GetChild(i).gameObject;

            if (child == innerTextObj)
                continue;

            Destroy(child);
        }

        innerTextObj.SetActive(false);
    }

    public void ReadLocalFiles()
    {
#if UNITY_EDITOR
        string directory = Path.Combine(Application.dataPath, "1.Datas", "Origin", "StageData", "Globals", "Custom");
#else
        string directory = Path.Combine(Application.persistentDataPath, "StageData", "Custom");
#endif
        if (!Directory.Exists(directory)) 
        {
            ChangeInnerText("로컬 파일 읽을 수 없음");
            return;
        }
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
        GameObject obj = Instantiate(buttonPrefab, contentRoot, false);
        RectTransform trans = obj.GetComponent<RectTransform>();
        trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90f);
        return obj;
    }

    public void ChangeInnerText(string text)
    {
        innerTextObj.SetActive(true);
        innerText.text = text;
    }
}
