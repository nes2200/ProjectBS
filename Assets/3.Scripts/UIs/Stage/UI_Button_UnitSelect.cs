using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Button_UnitSelect : UIBase
{
    //버튼을 누르면 유닛 프리팹과 비용이 임시로 저장됨
    //바닥을 클릭하면 프리팹 복사본을 생성
    //추가될 비용이 최대 코스트 제한을 넘으면 생성 안됨

    [Header("유닛 프리팹")]
    [SerializeField] GameObject unitPrefab;
    
    UnitStatus status;
    TeamID placementTeam;

    [Header("오브젝트 구성 요소")]
    [SerializeField] TextMeshProUGUI unitNameText;
    [SerializeField] TextMeshProUGUI unitCostText;
    [SerializeField] Image unitImage;

    public void Initialize(GameObject newUnitPrefab, TeamID newTeam)
    {
        if(!newUnitPrefab)
        {
            Debug.LogError("[UI_Button_UnitSelect] 유효한 유닛 정의가 필요합니다.");
            return;
        }

        unitPrefab = newUnitPrefab;
        CharacterBase character = unitPrefab.GetComponent<CharacterBase>();
        if (!character)
        {
            Debug.LogError("[UI_Button_UnitSelect] 선택된 프리팹은 캐릭터가 아닙니다.");
            return;
        }
        status = character.Status;
        if (!status)
        {
            Debug.LogError("[UI_Button_UnitSelect] 선택된 프리팹에 정보가 없습니다.");
            return;
        }
        placementTeam = newTeam;

        unitNameText.text = status.unitName;
        unitCostText.text = status.cost.ToString();
    }

    public void OnClickUnitSelect()
    {
        if (!unitPrefab || !status) return;

        //샌드박스는 등록
        UI_SandboxScreen sandboxScreen = UIManager.ClaimGetUI(UIType.Sandbox) as UI_SandboxScreen;
        if(placementTeam == TeamID.TeamA && sandboxScreen && sandboxScreen.isActiveAndEnabled)
        {
            sandboxScreen.RegisterSelectableUnit(unitPrefab);
            return;
        }

        //일반은 유닛 선택
        InputManager.InvokeUnitSelect(unitPrefab, placementTeam);

    }
}
