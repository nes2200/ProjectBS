using TMPro;
using UnityEngine;

public class UI_LogInWindow : OpenableUIBase
{
    [Header("Input Field")]
    [SerializeField] TMP_InputField idField;
    [SerializeField] TMP_InputField passwordField;


    public override void Open()
    {
        base.Open();
        idField.text = "";
        passwordField.text = "";

    }
    public override void Close()
    {
        base.Close();
    }
}
