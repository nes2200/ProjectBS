using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StageLoadManager : ManagerBase
{
    //내 유닛 로드시 StageScreen에서 버튼 생성하도록 명령 내리기
    public event Action<StageManager> OnStageLoaded;

    private readonly List<GameObject> selectableUnits = new();
    private readonly List<GameObject> selectableEnemies = new();
    public IReadOnlyList<GameObject> SelectableUnits => selectableUnits;
    public IReadOnlyList<GameObject> SelectableEnemies => selectableEnemies;

    public event Action OnSelectableCharactersLoaded;

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        yield return null;
    }

    protected override void OnDisconnected()
    {
        selectableUnits.Clear();
        selectableEnemies.Clear();
        OnSelectableCharactersLoaded = null;
    }

    public void LoadStage(TextAsset stageData, Scene stageScene, BattleFieldMode mode = BattleFieldMode.Stage)
    {
        ExecuteLoad(stageData, stageScene, mode);
        //카메라 위치 초기화
        GameManager.Camera.SetCameraDefaultPosition();
    }

    private void ExecuteLoad(TextAsset stageData, Scene stageScene, BattleFieldMode mode)
    {
        //씬 유효성 검사
        if(!stageScene.IsValid() || !stageScene.isLoaded)
        {
            Debug.LogError("[StageLoadManager] 유효한 스테이지 씬이 아닙니다.");
            return;
        }

        //스테이지 데이터 자체가 없다면
        if (!stageData)
        {
            Debug.LogError("[StageLoadManager] StageData가 null입니다.");
            return;
        }

        //제이슨 파일 읽고 역직렬화
        string jsonContent = stageData.text;

        //스테이지 데이터를 읽었는데 비었다면
        if (string.IsNullOrEmpty(jsonContent))
        {
            Debug.LogError($"[StageLoadManager] '{stageData.name}'의 내용이 비어 있습니다.");
            return;
        }

        SceneSaveData loadData;

        //JSON 형식 검사
        try
        {
            loadData = JsonConvert.DeserializeObject<SceneSaveData>(jsonContent);
        }
        catch(JsonException e)
        {
            Debug.LogError($"[StageLoadManager] '{stageData.name}'의 JSON 형식이 올바르지 않습니다.\n" + e.Message);
            return;
        }

        //JSON 형식은 맞는데 loadData가 이상하다면
        if(loadData == null)
        {
            Debug.LogError($"[StageLoadManager] '{stageData.name}'을 로드하지 못했습니다.");
            return;
        }

        //loadData는 null이 아닌데 속이 비어있다면
        if(loadData.objects == null)
        {
            Debug.LogError($"[StageLoadManager] '{stageData.name}'에 probs 데이터가 없습니다.");
            return;
        }

        LoadSelectableUnits(loadData.selectableUnits, selectableUnits);
        LoadSelectableUnits(loadData.selectableEnemies, selectableEnemies); 
        OnSelectableCharactersLoaded?.Invoke();

        //씬에 있는 주요 부모/관리자를 미리 검색하여 등록
        StageManager stageManager = FindStageManager(stageScene);
        if(!stageManager)
        {
            Debug.LogError("[StageLoadManager] StageManager를 찾지 못했습니다.");
            return;
        }
        stageManager.SetFieldMode(mode);
        stageManager.InitializeSelectableUnits(null);
        stageManager.InitializeCostLimits(loadData.costLimits);
        if (!stageManager.Floor || !stageManager.Probs || !stageManager.TeamA || !stageManager.TeamB)
        {
            Debug.LogError("[StageLoadManager] StageManager의 컨테이너 참조가 설정되지 않았습니다.");
            return;
        }
        Transform terrain = stageManager.Floor.Find("Terrain");
        if (!terrain)
        {
            Debug.LogError("[StageLoadManager] Floor 아래에서 Terrain을 찾지 못했습니다.");
            return;
        }
        Dictionary<string, Transform> parentContainer = new Dictionary<string, Transform> 
        {
            { "Floor", stageManager.Floor},
            { "Terrain", terrain },
            { "Probs", stageManager.Probs},
            { "TeamA", stageManager.TeamA},
            { "TeamB", stageManager.TeamB}
        };
        
        List<NavMeshAgent> disabledNavAgents = new();
        List<GameObject> spawnedObjects = new();
        //기존 자식 및 배치 오브젝트 청소
        //Probs, TeamB 아래 있는 기존 자식들을 일괄 제거
        foreach (var container in parentContainer)
        {
            //Terrain이나 Floor를 지우면 안되니까 자식만 특정하기
            if (container.Key == "Probs" || container.Key == "TeamB")
            {
                Transform parent = container.Value;
                for(int i = parent.childCount - 1; i >= 0; i--)
                {
                    ObjectManager.DestroyObject((parent.GetChild(i).gameObject));
                }
            }
            else if (container.Key == "Terrain")
            {
                Transform crossLine = container.Value.Find("CrossLine");
                if (crossLine)
                {
                    ObjectManager.DestroyObject(crossLine.gameObject);
                }
            }
        }

        //데이터를 기반으로 에셋 폴더 내 프리팹을 검색하여 스폰 및 위치 복구
        foreach (StageObject data in loadData.objects)
        {
            if(data.name == "Terrain" && data.parentName == "Floor")
            {
                Transform existingTerrain = parentContainer["Terrain"];
                
                existingTerrain.localPosition = data.position;
                existingTerrain.localScale = data.scale;
                existingTerrain.localRotation = data.rotation;
                
                continue;
            }

            GameObject spawnObject = ObjectManager.CreateObjectWithoutRegistration(data.prefabName);
            if(!spawnObject)
            {
                Debug.LogWarning($"[StageLoadManager] '{data.prefabName}' 오브젝트를 생성하지 못했습니다.");
                continue;
            }
           
            spawnedObjects.Add(spawnObject);
            spawnObject.name = data.name;

            //내브메쉬 베이크시 오브젝트 위치가 찐빠나는 경우 대비
            NavMeshAgent agent = spawnObject.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
                disabledNavAgents.Add(agent);
            }

            //부모 관계 복구  
            if (data.parentName != "None" && parentContainer.TryGetValue(data.parentName, out Transform parentTransform))
            {
                spawnObject.transform.SetParent(parentTransform, false);
            }
            

            // 트랜스폼 복구 (Local 값 적용)
            spawnObject.transform.localPosition = data.position;
            spawnObject.transform.localScale = data.scale;
            spawnObject.transform.localRotation = data.rotation;

            if (spawnObject.TryGetComponent(out TeamLine teamLine))
            {
                stageManager.SetTeamLine(teamLine);
            }

            ObjectManager.RegistrationObject(spawnObject);
            
            if(spawnObject.TryGetComponent<CharacterBase>(out CharacterBase character))
            {
                TeamID team = data.parentName switch
                {
                    "TeamA" => TeamID.TeamA,
                    "TeamB" => TeamID.TeamB,
                    _ => TeamID.None
                };
                stageManager.CharacterRegistry.Register(character, team);
            }
            
        }

        //Probs 폴더에서 자식들 세팅하기
        if (parentContainer.ContainsKey("Probs"))
        {
            foreach (GameObject spawnedObject in spawnedObjects)
            {
                if (spawnedObject.transform.parent != parentContainer["Probs"].transform)
                    continue;

                //자신과 자식들의 모든 상태 변경 
                SetChildsStaticAndLayer(spawnedObject, true, 9);

                NavMeshObstacle navObs = spawnedObject.GetComponent<NavMeshObstacle>();
                if (navObs == null)
                {
                    navObs = spawnedObject.AddComponent<NavMeshObstacle>();
                }
                if (navObs)
                {
                    navObs.carving = true;
                }
            }
        }

        // 2. Terrain의 NavMesh 베이크(Bake) 하기
        if (parentContainer.ContainsKey("Terrain"))
        {
            NavMeshSurface navSurface = parentContainer["Terrain"].GetComponent<NavMeshSurface>();
            if (navSurface != null)
            {
                // 실제 NavMesh 빌드(Bake) 실행!
                navSurface.BuildNavMesh();
            }
            else
            {
                Debug.LogWarning("[SceneScanner] Terrain 오브젝트에서 NavMeshSurface 컴포넌트를 찾을 수 없습니다.");
            }
        }

        //disable 해놨던 유닛들의 navmeshAgent 켜주기
        foreach(NavMeshAgent agent in disabledNavAgents)
        {
            Vector3 targetPosition = agent.transform.position;
            agent.enabled = true;
        }

        //유닛들의 내부 참조 해주기
        if(!parentContainer.TryGetValue("TeamA", out Transform teamATransform))
        {
            Debug.LogError("[StageLoadManager] TeamA 오브젝트를 찾지 못했습니다.");
            return;
        }
        if (!parentContainer.TryGetValue("TeamB", out Transform teamBTransform))
        {
            Debug.LogError("[StageLoadManager] TeamB 오브젝트를 찾지 못했습니다.");
            return;
        }
        foreach (Transform unit in teamBTransform)
        {
            TargetingModule targetModule = unit.GetComponent<TargetingModule>();
            if (targetModule)
            {
                targetModule.SetHostileGroupParents(teamATransform);
            }
        }
        OnStageLoaded?.Invoke(stageManager);
    }

    private GameObject FindObjectInScene(Scene scene, string objectName)
    {
        foreach(GameObject rootObject in scene.GetRootGameObjects())
        {
            //비활성화된 자식도 훑어보게 true 넣기
            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true); 
            foreach(Transform target in transforms)
            {
                if(target.name == objectName)
                {
                    return target.gameObject;
                }
            }
        }
        return null;
    }

    private StageManager FindStageManager(Scene scene)
    {
        foreach(GameObject rootObject in scene.GetRootGameObjects())
        {
            StageManager stageManager = rootObject.GetComponentInChildren<StageManager>(true);
            if (stageManager) return stageManager;
        }
        return null;
    }

    private void SetChildsStaticAndLayer(GameObject obj, bool isStatic, int layer)
    {
        if (obj == null) return;

        obj.isStatic = isStatic;
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetChildsStaticAndLayer(child.gameObject, isStatic, layer);
        }
    }

    private void LoadSelectableUnits(IReadOnlyList<StageUnitEntry> entries, List<GameObject> result)
    {
        result.Clear();

        if (entries == null) return;

        HashSet<string> loadNames = new();

        foreach(StageUnitEntry entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.unitPrefabName)) continue;

            if (!loadNames.Add(entry.unitPrefabName)) continue;

            if(DataManager.TryLoadDataFile(entry.unitPrefabName, out GameObject prefab))
            {
                result.Add(prefab);
            }
        }
    }
    
}
