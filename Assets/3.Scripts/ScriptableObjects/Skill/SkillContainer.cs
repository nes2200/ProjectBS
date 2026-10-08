using UnityEngine;

[CreateAssetMenu(fileName = "SkillContainer", menuName = "Scriptable Objects/SkillContainer")]
public class SkillContainer : InfoContainer
{
    [SerializeField] private float cooltime;
    public float Cooltime => cooltime;
}
