using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;
using System;

public class UI_LoadScreen : UI_ScreenBase
{
    [SerializeField] UI_FileViewer userMaps;
    [SerializeField] UI_FileViewer localMaps;

    void BackToTitle(bool value) 
    {
        UIManager.ClaimOpenScreen(UIType.Title, ScreenChangeType.ScreenChanger);
    }

    public override async void Open()
    {
        base.Open();

        InputManager.OnCancel -= BackToTitle;
        InputManager.OnCancel += BackToTitle;

        // 로컬 맵
        localMaps.Clear();
        localMaps.ReadLocalFiles();

        //내가 만든 맵
        userMaps.Clear();
        if (!GameManager.DB.IsLoggedIn)
        {
            userMaps.ChangeInnerText("로그인이 필요합니다");
            return;
        }

        try
        {
            string uid = GameManager.DB.CurrentUserID;
            DataSnapshot mapIds = await GameManager.DB.ReadSnapshotAsync("userDB", uid, "maps");

            if (!this || !gameObject.activeInHierarchy) return;
            if (!mapIds.Exists || !mapIds.HasChildren)
            {
                userMaps.ChangeInnerText("서버에 등록한 맵이 없습니다");
                return;
            }

            foreach(DataSnapshot child in mapIds.Children)
            {
                string mapId = child.Key;
                string mapName = child.Value?.ToString();

                userMaps.AddButton().GetComponent<UI_Button_StageSelect>().SetFromServerFile(mapId, mapName, true);
            }
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            if (this) userMaps.ChangeInnerText("내가 만든 맵을 불러오지 못헀습니다");
        }
    }
    public override void Close()
    {
        InputManager.OnCancel -= BackToTitle;
        base.Close();
    }
}
