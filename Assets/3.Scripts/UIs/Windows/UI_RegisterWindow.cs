using TMPro;
using UnityEngine;

public class UI_RegisterWindow : OpenableUIBase
{
    [Header("Input Field")]
    [SerializeField] TMP_InputField idField;
    [SerializeField] TMP_InputField passwordField;
    [SerializeField] TMP_InputField pwConfirmField;

    [Header("Check Text")]
    [SerializeField] TextMeshProUGUI idCheckText;
    [SerializeField] TextMeshProUGUI passwordCheckText;
    [SerializeField] TextMeshProUGUI pwConfirmCheckText;

    bool idValid;
    public bool IDValid => idValid;
    bool passwordValid;
    public bool PasswordValid => passwordValid;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        idField.onValueChanged.AddListener(CheckID);
        passwordField.onValueChanged.AddListener(CheckPassword);
        pwConfirmField.onValueChanged.AddListener(CheckPWCorrect);
    }
    public override void Unregistration(UIManager manager)
    {
        idField.onValueChanged.RemoveListener(CheckID);
        passwordField.onValueChanged.RemoveListener(CheckPassword);
        pwConfirmField.onValueChanged.RemoveListener(CheckPWCorrect);
        base.Unregistration(manager);
    }

    public override void Open()
    {
        base.Open();
        idField.text = "";
        passwordField.text = "";
        pwConfirmField.text = "";

        idCheckText.text = "";
        passwordCheckText.text = "";
        pwConfirmCheckText.text = "";
    }
    public override void Close()
    {
        idField.text = "";
        passwordField.text = "";
        pwConfirmField.text = "";

        idCheckText.text = "";
        passwordCheckText.text = "";
        pwConfirmCheckText.text = "";
        base.Close();
    }

    private void CheckID(string value)
    {
        if (!IsLongEnough(value, 2))
        {
            idCheckText.text = "아이디는 2자 이상이어야 합니다";
            idCheckText.color = Color.red;
            idValid = false;
            return;
        }
        if (!IsValidLetters(value))
        {
            idCheckText.text = "아이디는 공백없이 영어와 숫자만 가능합니다";
            idCheckText.color = Color.red;
            idValid = false;
            return;
        }

        idCheckText.text = "사용 가능";
        idCheckText.color = Color.green;
        idValid = true;
    }

    private void CheckPassword(string value)
    {
        if (!IsLongEnough(value, 2))
        {
            passwordCheckText.text = "비밀번호는 2자 이상이어야 합니다";
            passwordCheckText.color = Color.red;
            passwordValid = false;

            return;
        }
        if (!IsValidLetters(value))
        {
            passwordCheckText.text = "비밀번호는 공백없이 영어와 숫자만 가능합니다";
            passwordCheckText.color = Color.red;
            passwordValid = false;

            return;
        }

        passwordCheckText.text = "사용 가능";
        passwordCheckText.color = Color.green;

        CheckPWCorrect(value);
    }

    private void CheckPWCorrect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            pwConfirmCheckText.text = "내용을 입력해 주세요";
            pwConfirmCheckText.color = Color.red;
            passwordValid = false;
            return;
        }
        if(passwordField.text != pwConfirmField.text)
        {
            pwConfirmCheckText.text = "비밀번호와 비밀번호 확인이 다릅니다";
            pwConfirmCheckText.color = Color.red;
            passwordValid = false;
            return; 
        }
        pwConfirmCheckText.text = "확인 완료";
        pwConfirmCheckText.color = Color.green;

        passwordValid = true;
    }

    private bool IsLongEnough(string value, int length)
    {
        return value.Length >= length;
    }
    private bool IsValidLetters(string value)
    {
        foreach(char c in value)
        {
            bool isEnglish = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            if (!isEnglish && !char.IsDigit(c)) return false;
            if (char.IsWhiteSpace(c)) return false;
        }
        return true;
    }
    
    public void Register()
    {
        if(!idValid || !passwordValid)
        {
            UIManager.ClaimPopUp("가입 불가", "아이디/비밀번호를 다시 확인해 주세요", "확인");
            return;
        }
        UIManager.ClaimPopUp("가입", "가입 완료", "뻥임");
    }
}
