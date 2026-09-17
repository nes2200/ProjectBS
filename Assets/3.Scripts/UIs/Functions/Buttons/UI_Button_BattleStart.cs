using UnityEngine;

public class UI_Button_BattleStart : MonoBehaviour
{
    StageManager stage;

    private void OnEnable()
    {
        stage = GameObject.Find("StageManager")?.GetComponent<StageManager>();
    }

    public void BattleStart()
    {
        if(stage != null && stage.StartBattle())
        {
            gameObject.SetActive(false);
        }
    }
}
