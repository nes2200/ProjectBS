using System;

[System.Serializable]
public class CustomMapData
{
    public string mapName;
    public string creatorUID;
    public DateTime registerDate;
    public DateTime updateDate;
    public int formatVersion = 1;
    public SceneSaveData data;
}
