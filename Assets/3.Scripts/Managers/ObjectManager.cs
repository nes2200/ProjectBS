using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectManager : ManagerBase
{
    //이제 새로운 global 파일을 추가할 때 글자 추가만 하면 됨
    //이것들은 바꿀 필요가 없다 => 변수가 아니라 상수인 셈 => 나중에 바뀌면 안됨
    //일반적인 상수는 constant variable이 맞다
    //"읽기 전용"으로 바꿔야 한다
    readonly string[] globalPoolSettings = 
    { 
        "GlobalCharacterPool", 
        "GlobalControllerPool", 
        "GlobalEffectPool", 
        "GlobalObjectPool", 
        "GlobalUIPool" 
    };

    //[SerializeField] PoolSetting[] testSetting;

    //PoolRequest는 얼마나 자주 추가될까? => 로딩할 떄 즈음?
    //로딩되는 회수보다 대상에 개수가 부족하면 새로 추가하거나 하는 일
    List<PoolRequest> loadedPoolRequests = new();

    //해당하는 이름의 대상으로 불러주기 위해서
    //[이름 - 게임오브젝트] 자료구조
    static Dictionary<string, ObjectPoolModule> poolDictionary = new(StringComparer.OrdinalIgnoreCase);

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        RegistrationInHierarchy();
        RegistrationPool(globalPoolSettings);
        InitializePool();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        yield return null;
    }

    protected override void OnDisconnected()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static GameObject CreateObject(string wantName, Transform parent = null)
    {
        GameObject result = null; ;

        //이 이름으로 된 풀이 등록되어 있다면
        if (poolDictionary.TryGetValue(wantName, out ObjectPoolModule pool))
        {
            result = pool.CreateObject(parent);
        }
        else
        {
            //풀에 없는 야생의 오브젝트 만들기 => 데이터 있는지 확인해보기
            if(DataManager.TryLoadDataFile<GameObject>(wantName, out GameObject prefab)) 
            {
                if (prefab) result = Instantiate(prefab, parent);
            }
        }

        if (!result) UIManager.ClaimErrorMessage(SystemMessage.ObjectNameNotFound(wantName));

        RegistrationObject(result);
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        GameObject result = Instantiate(prefab, parent);
        RegistrationObject(result);
        return result;
    }
    public static GameObject CreateObjectWithoutRegistration(string wantName, Transform parent = null)
    {
        GameObject result = null; ;

        //이 이름으로 된 풀이 등록되어 있다면
        if (poolDictionary.TryGetValue(wantName, out ObjectPoolModule pool))
        {
            result = pool.CreateObject(parent);
        }
        else
        {
            //풀에 없는 야생의 오브젝트 만들기 => 데이터 있는지 확인해보기
            if (DataManager.TryLoadDataFile<GameObject>(wantName, out GameObject prefab))
            {
                if (prefab) result = Instantiate(prefab, parent);
            }
        }

        if (!result) UIManager.ClaimErrorMessage(SystemMessage.ObjectNameNotFound(wantName));

        return result;
    }
    public static GameObject CreateObjectWithoutRegistration(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        GameObject result = Instantiate(prefab, parent);
        return result;
    }

    public static GameObject CreateObject(string wantName, Vector3 position)
    {
        GameObject result = CreateObject(wantName);
        if (result) result.transform.position = position;
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Vector3 position)
    {
        GameObject result = CreateObject(prefab);
        if (result) result.transform.position = position;
        return result;
    }
    public static GameObject CreateObject(string wantName, Vector3 position, Transform parent = null)
    {
        GameObject result = CreateObject(wantName, parent);
        if (result) result.transform.position = position;
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Vector3 position, Transform parent = null)
    {
        GameObject result = CreateObject(prefab, parent);
        if (result) result.transform.position = position;
        return result;
    }

    public static GameObject CreateObject(string wantName, Vector3 position, Quaternion rotation)
    {
        GameObject result = CreateObject(wantName);
        if (result)
        {
            result.transform.position = position;
            result.transform.rotation = rotation;
        }
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject result = CreateObject(prefab);
        if (result)
        {
            result.transform.position = position;
            result.transform.rotation = rotation;
        }
        return result;
    }
    public static GameObject CreateObject(string wantName, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject result = CreateObject(wantName);
        if (result)
        {
            result.transform.position = position;
            result.transform.rotation = rotation;
            result.transform.localScale = scale;
        }
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject result = CreateObject(prefab);
        if (result)
        {
            result.transform.position = position;
            result.transform.rotation = rotation;
            result.transform.localScale = scale;
        }
        return result;
    }
    public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Space space = Space.Self)
    {
        GameObject result = CreateObject(wantName, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    break;
            }
        }
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Space space = Space.Self)
    {
        GameObject result = CreateObject(prefab, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    break;
            }
        }
        return result;
    }
    public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.Self)
    {
        GameObject result = CreateObject(wantName, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    result.transform.rotation = rotation;
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    result.transform.localRotation = rotation;
                    break;
            }
        }
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.Self)
    {
        GameObject result = CreateObject(prefab, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    result.transform.rotation = rotation;
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    result.transform.localRotation = rotation;
                    break;
            }
        }
        return result;
    }
    public static GameObject CreateObject(string wantName, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Space space = Space.Self)
    {
        GameObject result = CreateObject(wantName, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    result.transform.rotation = rotation;
                    result.transform.localScale = scale;
                    //"진짜 크기"는 "부모 크기"와 비교해서 값을 구한다
                    //로컬 => 월드    로컬 * (월드 / 로컬) = 월드
                    //월드 => 로컬    월드 * (로컬 / 월드) = 로컬
                    //float scaledScaleX = scale.x * (result.transform.localScale.x / result.transform.lossyScale.x);
                    //float scaledScaleY = scale.y * (result.transform.localScale.y / result.transform.lossyScale.y);
                    //float scaledScaleZ = scale.z * (result.transform.localScale.z / result.transform.lossyScale.z);
                    //result.transform.localScale = new Vector3(scaledScaleX, scaledScaleY, scaledScaleZ);
                    
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    result.transform.localRotation = rotation;
                    result.transform.localScale = scale;
                    break;
            }
        }
        return result;
    }
    public static GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Space space = Space.Self)
    {
        GameObject result = CreateObject(prefab, parent);
        if (result)
        {
            switch (space)
            {
                case Space.World:
                    result.transform.position = position; //절대값을 기준으로
                    result.transform.rotation = rotation;
                    result.transform.localScale = scale;
                    //"진짜 크기"는 "부모 크기"와 비교해서 값을 구한다
                    //로컬 => 월드    로컬 * (월드 / 로컬) = 월드
                    //월드 => 로컬    월드 * (로컬 / 월드) = 로컬
                    //float scaledScaleX = scale.x * (result.transform.localScale.x / result.transform.lossyScale.x);
                    //float scaledScaleY = scale.y * (result.transform.localScale.y / result.transform.lossyScale.y);
                    //float scaledScaleZ = scale.z * (result.transform.localScale.z / result.transform.lossyScale.z);
                    //result.transform.localScale = new Vector3(scaledScaleX, scaledScaleY, scaledScaleZ);
                    
                    break;
                case Space.Self:
                    result.transform.localPosition = position; //부모를 기준으로
                    result.transform.localRotation = rotation;
                    result.transform.localScale = scale;
                    break;
            }
        }
        return result;
    }

    //변수로 받은 오브젝트 등록하기
    public static void RegistrationObject(GameObject target)
    {
        if (target)
        {
            // 오브젝트에 'IFunctionable'을 가진 모든 '컴포넌트'를 다 가져와야한다.
            foreach (var current in target.GetComponentsInChildren<IFunctionable>())
            {
                current.RegistrationFunctions();
            }
        }
    }

    public static void DestroyObject(GameObject target)
    {
        if (!target) return;
        UnRegistrationObject(target);
        if(target.TryGetComponent(out PooledObject pool))
        {
            pool.OnEnqueue();
        }
        else
        {
            Destroy(target);
        }

    }
    public static void UnRegistrationObject(GameObject target)
    {
        if (target)
        {
            // 오브젝트에 'IFunctionable'을 가진 모든 '컴포넌트'를 다 가져와야한다.
            foreach (var current in target.GetComponentsInChildren<IFunctionable>())
            {
                current.UnregistrationFunctions();
            }
        }
    }

    public void RegistrationInHierarchy()
    {
        foreach(MonoBehaviour current in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if(current is IFunctionable currentFunctionable)
            {
                currentFunctionable.RegistrationFunctions();
            }
        }
    }

    public void RegistrationPool(string poolName)
    {
        PoolRequest currentRequest = DataManager.LoadDataFile<PoolRequest>(poolName);
        if (currentRequest == null) return;
        if (currentRequest.settings == null) return;
        loadedPoolRequests.Add(currentRequest); 
        //애들마다 하나씩
        //         학생         다음학생    in     3학년4반
        foreach(PoolSetting currentSetting in currentRequest.settings)
        {
            string currentName = currentSetting.poolName;
            GameObject currentPrefab = currentSetting.target;
            //다음학생이 오늘 안왔대요! => 다음 학생을 불러야 한다
            if (currentPrefab == null) continue;
            //이름에 문제가 있을 수 있음
            if (poolDictionary.ContainsKey(currentName)) continue;
            //여기까지 왔으면 추가하기
            poolDictionary.Add(currentName, new(currentSetting));
        }
    }

    //"매개 변수" = parameter
    //여러개 받고싶으면 prarmeters => params
    public void RegistrationPool(params string[] poolNames)
    {
        foreach(string poolName in poolNames)
        {
            //가변인자는 "우선순위"가 낮다
            //가변인자다 보니 갯수가 "고정인자"를 가진 함수와 같아질 수 있음
            //"고정인자"를 가진 함수를 먼저 인식해서 실행한다
            RegistrationPool(poolName);
        }
    }

    public void InitializePool()
    {
        foreach(ObjectPoolModule currentPool in poolDictionary.Values)
        {
            currentPool?.Initialize();
        }
    }

    public void PreparePool(string poolName, uint count)
    {
        if (string.IsNullOrEmpty(poolName)) return;
        if (poolDictionary.TryGetValue(poolName, out ObjectPoolModule pool)) pool.PrepareAtLeast(count);
        
    }

    //씬 로드시 해당 씬에 있던 오브젝트들을 다시 한번 등록하기 위한 기능
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(mode == LoadSceneMode.Additive)
        {
            RegistrationInScene(scene);
        }
    }

    public void RegistrationInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach(GameObject root in rootObjects)
        {
            foreach (var current in root.GetComponentsInChildren<IFunctionable>())
            {
                current.RegistrationFunctions();
            }
        }
    }
}
