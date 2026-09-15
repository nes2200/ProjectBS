#if UNITY_EDITOR
using Newtonsoft.Json; 
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class SceneScannerWindow : EditorWindow
{
    private string inputFileName = "FileName_Default";
    private string calculatedSubPath = "1.Datas/Origin/StageData/Globals";

    //유니티 상단 바에 메뉴 추가
    [MenuItem("Tools/Scene Scanner")]
    public static void ShowWindow()
    {
        GetWindow<SceneScannerWindow>("Scene Scanner");
    }

    //현재 탭을 나타낼 정보들
    private int selectedTab = 0;
    private string[] tabNames = { "Save Stage (Scan)", "Load Stage", "Clean Scene" };

    //그리기
    private void OnGUI()
    {
        //헤더
        GUILayout.Label("Scene Object Scanner & Loader", EditorStyles.boldLabel);
        
        GUILayout.Space(10); //여백

        //상단 탭 구분
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));

        GUILayout.Space(15);

        //공통 입력 정보
        GUILayout.Label("File Name Here (No Extensions)");
        inputFileName = GUILayout.TextField(inputFileName);

        GUILayout.Space(10);

        //유니티 내장 폴더 선택 필드
        GUILayout.Label("Target Save Folder");

        //유니티 os 내장 폴더 탐색기를 띄울 버튼
        if(GUILayout.Button("Choose Folder..", GUILayout.Width(120)))
        {
            //프로젝트의 Assets 폴더 기준 절대 경로 추출
            string defaultPath = Path.Combine(Application.dataPath, calculatedSubPath);
            if (!Directory.Exists(defaultPath)) defaultPath = Application.dataPath;

            //폴더 선택창 팝업
            string selectedPath = EditorUtility.OpenFolderPanel("대상 폴더 선택", defaultPath, "");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                //선택한 경로가 현재 프로젝트 안의 Asset 폴더인지 검증
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    if(selectedPath == Application.dataPath)
                    {
                        calculatedSubPath = "";
                    }
                    else
                    {
                        calculatedSubPath = selectedPath.Substring(Application.dataPath.Length + 1);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("경고", "현재 프로젝트의 Asset 폴더 내부에서만 선택 가능", "확인");
                }
            }
        }

        //계산되어 적용될 경로를 화면에 가이드라인으로 표시
        EditorGUILayout.HelpBox($"작업 위치 : Asset/{calculatedSubPath}", MessageType.Info);

        GUILayout.Space(20);

        //선택된 탭에 따라 버튼 화면을 분기처리 및 안전 팝업
        if (selectedTab == 0)
        {
            //save 화면
            GUI.backgroundColor = new Color(0.7f, 0.8f, 1f);
            //실행 버튼
            if (GUILayout.Button("Scan Scene and Save JSON", GUILayout.Height(40)))
            {
                //빈칸 있는지 방어 함수 추가
                if (string.IsNullOrEmpty(inputFileName))
                {
                    EditorUtility.DisplayDialog("경고", "파일 이름을 입력해 주세요", "확인");
                    return;
                }

                //덮어쓰기 방지 경고창
                string fullPath = Path.Combine(Application.dataPath, calculatedSubPath, inputFileName + ".json");
                if (File.Exists(fullPath))
                {
                    bool proceed = EditorUtility.DisplayDialog("덮어쓰기 경고",
                        $"이미 '{inputFileName}.json'이 존재합니다.\n정말로 덮어 쓰시겠습니까?", "예, 덮어 씁니다", "아니오");

                    if (!proceed) return; //취소하면 끝
                }

                ExecuteScan(inputFileName, calculatedSubPath);
            }
        }
        else if(selectedTab == 1)//로드 화면
        {
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // 연녹
            if (GUILayout.Button("Load JSON to Scene", GUILayout.Height(40)))
            {
                if (string.IsNullOrEmpty(inputFileName))
                {
                    EditorUtility.DisplayDialog("경고", "로드할 파일 이름을 입력해 주세요.", "확인");
                    return;
                }

                bool proceed = EditorUtility.DisplayDialog("씬 초기화 및 로드 경고",
                    $"정말로 기존 배치를 지우고 '{inputFileName}.json' 데이터를 새로 로드하시겠습니까?",
                    "예, 새로 로드합니다", "아니오");
                if (!proceed) return;
                ExecuteLoad(inputFileName, calculatedSubPath);
            }
        }
        else if(selectedTab == 2)
        {
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            EditorGUILayout.HelpBox("현재 씬의 Probs와 TeamB 아래에 배치된 모든 자식을 제거합니다.", MessageType.Warning);

            if (GUILayout.Button("Clean Stage Objects", GUILayout.Height(40)))
            {
                bool proceed = EditorUtility.DisplayDialog("씬 청소",
                    "Probs와 TeamB의 모든 자식을 제거하시겠습니까?\nCtrl+Z로 복구할 수 있습니다.", "제거", "취소");
                if (!proceed) return;

                ExecuteClean();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void ExecuteScan(string fileName, string subPath)
    {
        StageDataAuthoring authoring = FindStageDataAuthoring();

        if (!authoring)
        {
            EditorUtility.DisplayDialog("저장 실패", "StageDataAuthoring을 찾을 수 없음.", "확인");
            return;
        }

        SceneSaveData saveData = SceneScanner.Capture(EditorSceneManager.GetActiveScene(), authoring, GetEditorPrefabName);

        string jsonResult = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        string directoryPath = Path.Combine(Application.dataPath, subPath);

        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        if (!fileName.EndsWith(".json")) fileName += ".json";

        string finalPath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(finalPath, jsonResult);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("저장 완료", $"성공적으로 스테이지 파일 생성 \n경로 : Asset/{subPath}/{fileName}", "확인");
    }

    private void ExecuteLoad(string fileName, string subPath)
    {
        if (!fileName.EndsWith(".json")) fileName += ".json";
        string targetPath = Path.Combine(Application.dataPath, subPath, fileName);

        //제이슨 파일 읽고 역직렬화
        if (!File.Exists(targetPath))
        {
            EditorUtility.DisplayDialog( "로드 실패", $"파일을 찾지 못했습니다.\n{targetPath}", "확인");
            return;
        }
        SceneSaveData loadData;
        try
        {
            string jsonContent = File.ReadAllText(targetPath);
            loadData = JsonConvert.DeserializeObject<SceneSaveData>(jsonContent);
        }
        catch (JsonException exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("로드 실패", "JSON 형식이 올바르지 않습니다.", "확인");
            return;
        }
        catch (IOException exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog( "로드 실패", "파일을 읽지 못했습니다.", "확인");
            return;
        }

        if (loadData is null || loadData.objects is null)
        {
            EditorUtility.DisplayDialog("에러", "로드 데이터가 올바르지 않거나 비어있음", "확인");
            return;
        }

        StageDataAuthoring authoring = FindStageDataAuthoring();
        if (!authoring)
        {
            EditorUtility.DisplayDialog("로드 실패", "StageDataAuthoring을 찾지 못했습니다.", "확인");
            return;
        }

        List<GameObject> selectableUnitPrefabs = new();
        List<GameObject> selectableEnemyPrefabs = new();
        LoadUnits(loadData.selectableUnits, selectableUnitPrefabs);
        LoadUnits(loadData.selectableEnemies, selectableEnemyPrefabs);
        

        //씬에 있는 주요 부모/관리자를 미리 검색하여 등록   
        string[] requiredNames ={ "Probs", "TeamA", "TeamB", "Terrain", "Floor"};
        Dictionary<string, GameObject> parentContainer = new();
        foreach (string objectName in requiredNames)
        {
            GameObject found = GameObject.Find(objectName);
            if (!found)
            {
                EditorUtility.DisplayDialog("로드 실패", $"현재 씬에서 '{objectName}'을 찾지 못했습니다.", "확인");
                return;
            }
            parentContainer.Add(objectName, found);
        }

        //Undo(되돌리기) 등록을 위한 그룹 ID 생성
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Load Stage Object");

        //기존 자식 및 배치 오브젝트 청소
        //Probs, TeamB 아래 있는 기존 자식들을 일괄 제거
        if (parentContainer.TryGetValue("Probs", out GameObject probs))
        {
            ClearContainerChildren(probs);
        }
        if (parentContainer.TryGetValue("TeamB", out GameObject TeamB))
        {
            ClearContainerChildren(TeamB);
        }

        //Terrain이나 Floor를 지우면 안되니까 자식만 특정하기
        if (parentContainer.TryGetValue("Terrain", out GameObject terrain))
        {
            Transform teamLine = terrain.transform.Find("TeamLine");

            if (teamLine)
            {
                Undo.DestroyObjectImmediate(teamLine.gameObject);
            }
        }
        

        //데이터를 기반으로 에셋 폴더 내 프리팹을 검색하여 스폰 및 위치 복구
        foreach (StageObject data in loadData.objects)
        {
            if (data.name == "Terrain")
            {
                Undo.RecordObject(terrain.transform, "Restore Terrain Transform");

                terrain.transform.localPosition = data.position;
                terrain.transform.localScale = data.scale;
                terrain.transform.localRotation = data.rotation;

                continue;
            }

            GameObject spawnObject = null;

            //프로젝트 내 프리팹 검색
            GameObject prefabAsset = FindPrefabAsset(data.prefabName);

            //프리팹을 찾았다면 Prefab 연결을 유지하며 스폰
            if (prefabAsset)
            {
                spawnObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                Undo.RegisterCreatedObjectUndo(spawnObject, "Spawn Stage Object");
            }
            else
            {
                //프리팹을 못찾은 경우 기본 빈 오브젝트로 임시 생성하여 유실을 방지
                spawnObject = new GameObject(data.name);
                Undo.RegisterCreatedObjectUndo(spawnObject, "Spawn Fallback Object");
                UnityEngine.Debug.LogWarning($"[SceneScanner] '{data.prefabName}' 프리팹을 찾을 수 없어 기본 오브젝트로 대체됩니다;");
            }

            if (spawnObject)
            {
                spawnObject.name = data.name;

                //부모 관계 복구
                if (data.parentName != "None" && parentContainer.TryGetValue(   data.parentName, out GameObject parent))
                {
                    spawnObject.transform.SetParent(parent.transform, false);
                }

                // 트랜스폼 복구 (Local 값 적용)
                spawnObject.transform.localPosition = data.position;
                spawnObject.transform.localScale = data.scale;
                spawnObject.transform.localRotation = data.rotation;
            }
        }

        //Probs 폴더에서 자식들 세팅하기
        if (parentContainer.ContainsKey("Probs"))
        {
            foreach(Transform child in parentContainer["Probs"].transform)
            {
                GameObject probObj = child.gameObject;
              
                //자신과 자식들의 모든 상태 변경 
                SetChildsStaticAndLayer(probObj, true, 9);

                NavMeshObstacle navObs = probObj.GetComponent<NavMeshObstacle>();
                if (navObs == null)
                {
                    // Undo 지원용 컴포넌트 추가 기능 사용
                    navObs = Undo.AddComponent<NavMeshObstacle>(probObj);
                }
                if (navObs)
                {
                    Undo.RecordObject(navObs, "Enable Obstacle Carving");
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
                // 베이크 시 가끔 씬 데이터 변경 감지가 안 될 수 있으므로 상태 등록
                Undo.RegisterCompleteObjectUndo(navSurface, "Bake Stage NavMesh");

                // 실제 NavMesh 빌드(Bake) 실행!
                navSurface.BuildNavMesh();

                // 에디터 씬 뷰 갱신 유도
                SceneView.RepaintAll();
            }
            else
            {
                Debug.LogWarning("[SceneScanner] Terrain 오브젝트에서 NavMeshSurface 컴포넌트를 찾을 수 없습니다.");
            }
        }

        //유닛들의 내부 참조 해주기
        Transform teamA = parentContainer["TeamA"].transform;
        Transform teamB = parentContainer["TeamB"].transform;
        foreach (Transform unit in teamB)
        {
            TargetingModule targetModule = unit.GetComponent<TargetingModule>();
            if (targetModule)
            {
                targetModule.SetHostileGroupParents(teamA);
            }
        }
        Undo.RecordObject(authoring, "Load Stage Data");

        authoring.SetCostLimits(loadData.costLimits);
        authoring.SetSelectableUnits(selectableUnitPrefabs);
        authoring.SetSelectableEnemies(selectableEnemyPrefabs);
        EditorUtility.SetDirty(authoring);
        if (PrefabUtility.IsPartOfPrefabInstance(authoring))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
        }

        // 실행 기록을 하나의 Undo 그룹으로 병합
        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.DisplayDialog("로드 완료", $"'{fileName}' 데이터를 기반으로 배치를 정상적으로 복구했습니다.", "확인");
    }

    void ExecuteClean()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clean Stage Objects");

        int removedCount = 0;

        //Probs와 TeamB 비워주기
        removedCount += ClearContainerChildren(GameObject.Find("Probs"));
        removedCount += ClearContainerChildren(GameObject.Find("TeamB"));
        removedCount += ClearContainerChildren(GameObject.Find("Terrain"));

        //사용할 유닛 데이터 비워주기
        StageDataAuthoring authoring = FindStageDataAuthoring();
        bool unitsCleared = authoring != null;
        if(authoring)
        {
            Undo.RecordObject(authoring, "Clear Selectable Entries");
            authoring.ClearData();
            EditorUtility.SetDirty(authoring);
            if (PrefabUtility.IsPartOfPrefabInstance(authoring))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();

        string selectableUnitMessage = unitsCleared ? "Selectable Units Entry를 초기화했습니다." 
            : "StageDataAuthoring을 찾지 못해 Selectable Units Entry는 초기화하지 못했습니다.";

        EditorUtility.DisplayDialog("씬 청소 완료",
            $"Probs와 TeamB에서 총 {removedCount}개의 오브젝트를 제거했습니다.\n" + selectableUnitMessage, "확인");
    }

    private StageDataAuthoring FindStageDataAuthoring()
    {
        StageManager stageManager = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);

        if (!stageManager) return null;

        return stageManager.GetComponentInChildren<StageDataAuthoring>(true);
    }

    private GameObject FindPrefabAsset(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;

        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefabAsset && prefabAsset.name == prefabName)
            {
                return prefabAsset;
            }
        }
        return null;
    }

    private void SaveUnits(SceneSaveData saveData, IReadOnlyList<GameObject> entry, TeamID team)
    {
        HashSet<string> savedNames = new();
        foreach (GameObject unitPrefab in entry)
        {
            if (!unitPrefab)
            {
                Debug.LogWarning("[SceneScanner] Selectable Units Entry에 비어 있는 항목이 있습니다.");
                continue;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(unitPrefab))
            {
                Debug.LogWarning($"[SceneScanner] '{unitPrefab.name}'은 프리팹 에셋이 아니므로 저장하지 않습니다.");
                continue;
            }

            string prefabName = unitPrefab.name;

            if (!savedNames.Add(prefabName))
            {
                Debug.LogWarning($"[SceneScanner] 중복 유닛 '{prefabName}'은 한 번만 저장합니다.");
                continue;
            }

            if(team == TeamID.TeamA)
            {
                saveData.selectableUnits.Add(new StageUnitEntry
                {
                    unitPrefabName = prefabName
                });
            }
            else if(team == TeamID.TeamB)
            {
                saveData.selectableEnemies.Add(new StageUnitEntry
                {
                    unitPrefabName = prefabName
                });
            }
            
        }
    }
    private void LoadUnits(IEnumerable<StageUnitEntry> entries, List<GameObject> selectablePrefabs)
    {
        if (entries is null) return;

        HashSet<string> loadedUnitNames = new();
        foreach (StageUnitEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.unitPrefabName))
            {
                Debug.LogWarning("[SceneScanner] selectableUnits에 잘못된 항목이 있습니다.");
                continue;
            }

            if (!loadedUnitNames.Add(entry.unitPrefabName))
            {
                Debug.LogWarning($"중복 유닛 '{entry.unitPrefabName}'은 제외합니다.");
                continue;
            }

            GameObject unitPrefab = FindPrefabAsset(entry.unitPrefabName);

            if (!unitPrefab)
            {
                Debug.LogWarning($"[SceneScanner] 선택 가능 유닛 프리팹 '{entry.unitPrefabName}'을 찾지 못했습니다.");
                continue;
            }

            selectablePrefabs.Add(unitPrefab);
        }
    }


    private bool ApplySelectableUnits(StageDataAuthoring authoring, IEnumerable<GameObject> unitPrefabs, string undoName)
    {
        if (!authoring) return false;

        Undo.RecordObject(authoring, undoName);
        authoring.SetSelectableUnits(unitPrefabs);
        EditorUtility.SetDirty(authoring);
        if (PrefabUtility.IsPartOfPrefabInstance(authoring))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
        }
        return true;
    }

    private void SetChildsStaticAndLayer(GameObject obj, bool isStatic, int layer)
    {
        if (obj == null) return;

        // Undo 등록을 위해 상태 기록
        Undo.RegisterCompleteObjectUndo(obj, "Set Static and Layer");

        obj.isStatic = isStatic;
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetChildsStaticAndLayer(child.gameObject, isStatic, layer);
        }
    }

    private int ClearContainerChildren(GameObject container)
    {
        if (!container) return 0;

        int removedCount = 0;

        for (int i = container.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.transform.GetChild(i).gameObject;

            Undo.DestroyObjectImmediate(child);
            removedCount++;
        }
        return removedCount;
    }

    private static string GetEditorPrefabName(GameObject obj)
    {
        GameObject originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);

        return originalPrefab ? originalPrefab.name : obj.name;
    }
}
#endif