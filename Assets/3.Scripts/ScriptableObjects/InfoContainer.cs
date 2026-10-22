using UnityEngine;

[CreateAssetMenu(fileName = "InfoContainer", menuName = "Scriptable Objects/InfoContainer")]
public class InfoContainer : ScriptableObject
{
    public Sprite icon;
    public string displayName;
    public string explain;
    public int id;
}
