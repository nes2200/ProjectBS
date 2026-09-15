using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UI_SaveMapWindow : OpenableUIBase
{
    [SerializeField] TMP_InputField inputField;
    string mapName;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        inputField.characterLimit = 20;
    }

    public override void Open()
    {
        base.Open();
        inputField.SetTextWithoutNotify("");
    }
    public override void Close()
    {
        inputField.SetTextWithoutNotify("");
        base.Close();
    }

    public void OnEndEdit(string input)
    {
        mapName = input;
        Debug.Log(mapName);
    }


    public void SaveMapDataOnServer()
    {

    }
}
