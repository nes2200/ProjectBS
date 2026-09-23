using TMPro;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using System;

public class UI_LogInWindow : OpenableUIBase
{
    [Header("Input Field")]
    [SerializeField] TMP_InputField idField;
    [SerializeField] TMP_InputField passwordField;

    bool isLoggingIn;
    bool isPasswordVisible;

    public override void Open()
    {
        base.Open();
        idField.text = "";
        passwordField.text = "";

        SetPasswordVisible(false);
    }
    public override void Close()
    {
        base.Close();
    }

    public async void Login()
    {
        if (isLoggingIn) return;

        if(string.IsNullOrWhiteSpace(idField.text) || string.IsNullOrWhiteSpace(passwordField.text))
        {
            UIManager.ClaimErrorMessage("아이디/비밀번호를 입력해 주세요");
            return;
        }

        isLoggingIn = true;

        try
        {
            FirebaseUser loginUser = await GameManager.DB.LoginAsync(idField.text, passwordField.text);
            Debug.Log($"로그인 성공 : {loginUser.UserId}");
            UIManager.ClaimPopUp("로그인", $"{idField.text} 계정으로 로그인했습니다", "확인");
            Close();
        }
        catch(FirebaseException exception)
        {
            AuthError error = (AuthError)exception.ErrorCode;

            switch (error) 
            {
                case AuthError.InvalidCredential:
                case AuthError.UserNotFound:
                case AuthError.WrongPassword:
                    UIManager.ClaimErrorMessage("아이디/비밀번호가 올바르지 않습니다");
                    break;
                case AuthError.TooManyRequests:
                    UIManager.ClaimErrorMessage("로그인 시도가 너무 많습니다. 잠시 후 다시 시도해 주세요");
                    break;
                default:
                    Debug.LogError(exception);
                    UIManager.ClaimErrorMessage("로그인 중 오류가 발생했습니다");
                    break;
            }
        }
        catch(Exception exception)
        {
            Debug.LogError(exception);
            UIManager.ClaimErrorMessage(exception.Message);
        }
        finally
        {
            isLoggingIn = false;
        }
    }

    public void TogglePasswordVisibility()
    {
        SetPasswordVisible(!isPasswordVisible);
    }

    private void SetPasswordVisible(bool visible)
    {
        isPasswordVisible = visible;

        passwordField.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

        passwordField.ForceLabelUpdate();
    }
}
