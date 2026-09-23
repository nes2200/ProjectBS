using UnityEngine;

public class UI_Button_LogOut : MonoBehaviour
{
    public void Logout()
    {
        GameManager.DB.Logout();
        UIManager.ClaimPopUp("로그아웃", "로그아웃 했습니다", "확인");
    }
}
