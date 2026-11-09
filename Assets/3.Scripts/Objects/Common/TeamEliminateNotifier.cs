using UnityEngine;
using System.Collections.Generic;


public class TeamEliminateNotifier : MonoBehaviour
{
    [Header("Stage Manager")]
    [SerializeField] StageManager stageManager;
    public StageManager Stage => stageManager;
    [Header("Player Check")]
    [SerializeField] bool isPlayer;

    List<CharacterBase> teamCharacters;


    private void OnEnable()
    {
        StageManager.OnStageStateChange -= CacheChildrensCharacter;
        StageManager.OnStageStateChange += CacheChildrensCharacter;

        teamCharacters = new List<CharacterBase>();
    }
    private void OnDisable()
    {
        StageManager.OnStageStateChange -= CacheChildrensCharacter;
    }

    //자식들 캐싱하기
    public void CacheChildrensCharacter(StageState oldState, StageState newState)
    {
        if(newState == StageState.Battle)
        {
            teamCharacters.Clear();
            foreach (Transform child in transform)
            {
                CharacterBase character = child.GetComponent<CharacterBase>();
                if (character)
                {
                    teamCharacters.Add(character);
                }
            }
        }
    }

    //내 팀 유닛들이 전부 죽었는지 체크
    public void TeamEliminateCheck()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out CharacterBase character) &&
                character.IsAlive && character.gameObject.activeInHierarchy)
                return;
        }

        TeamEliminated();
    }

    private void TeamEliminated()
    {
        stageManager.EndBattle(isPlayer);
    }
}
