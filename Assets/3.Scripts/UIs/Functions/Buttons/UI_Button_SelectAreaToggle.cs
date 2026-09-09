using UnityEngine;

public class UI_Button_SelectAreaToggle : MonoBehaviour
{
    [SerializeField] GameObject unitSelectArea;
    [SerializeField] GameObject enemySelectArea;

    public void SwapToUnitSelectArea()
    {
        unitSelectArea.SetActive(true);
        enemySelectArea.SetActive(false);
    }
    public void SwapToWeaponSelectArea()
    {
        unitSelectArea.SetActive(false);
        enemySelectArea.SetActive(true);
    }

}
