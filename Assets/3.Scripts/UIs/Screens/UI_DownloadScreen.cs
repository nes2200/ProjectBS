using UnityEngine;

public class UI_DownloadScreen : UI_ScreenBase
{
    [SerializeField] UI_FileViewer fileViewer;
    
    public override void Open()
    {
        base.Open();

        GameManager.DB.ReadData(task =>
        {
            if (!this || !gameObject.activeInHierarchy) return;
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError(task.Exception);
                return;
            }

            fileViewer.Clear();
            foreach (var child in task.Result.Children)
            {
                string mapID = child.Key;
                string mapName = child.Child("mapName").Value?.ToString();

                fileViewer.AddButton().GetComponent<UI_Button_StageSelect>().SetFromServerFile(mapID, mapName);
            }
        }, "customMaps");
    }


}

