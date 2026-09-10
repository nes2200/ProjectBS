using System.Collections.Generic;

//저장할 데이터 전체를 담은 그릇
[System.Serializable]
public class SceneSaveData
{
    public List<StageObject> objects = new();
    public List<StageUnitEntry> selectableUnits = new();
    public List<StageUnitEntry> selectableEnemies = new();
    public int[] costLimits;
}
