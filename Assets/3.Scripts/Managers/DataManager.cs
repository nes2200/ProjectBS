using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataManager : ManagerBase
{
    static Dictionary<System.Type, Dictionary<string, Object>> dataDictionary = new();

    event System.Action DisconnectEvent;

    public bool IsLoadingFinished { get; private set; } = false;

    public override int LoadCount
    {
        get
        {
            var task = Addressables.LoadResourceLocationsAsync("Global"); //열기
            var result = task.WaitForCompletion();
            int count = result.Count;
            task.Release(); //파일 열어놓은거 닫아주기
            return count;
        }
    }

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        IsLoadingFinished = false;

        // 로딩 진해율 => 최대 몇개를 로딩해야 하는지, 몇개까지 했는지
        //                현재 / 최대
        UIBase loading = UIManager.ClaimGetUI(UIType.Loading);
        IProgress<int> progressUI = loading as IProgress<int>;
        IStatus<string> statusUI = loading as IStatus<string>;

        int loaded = 0; 
        int total = LoadCount;
        string loadString = "Load Data";

        //람다. Lambda. => 이름없는 함수. anonymous function
        //함수 안에서 만들어지는 함수 => 변수로 저장할 수 있다
        System.Action ProgressOnLoad = () => 
        {
            loaded++;
            progressUI?.AddCurrent(1);
            statusUI?.SetCurrentStatus($"{loadString} {loaded}/{total}");
        };

        //새로운 타입의 무언가를 추가할 떄 마다 여기다 넣기
        loadString = "Load Game Objects";
        yield return LoadAllFromAssetBundle<GameObject>("Global", ProgressOnLoad).WaitForTask();
        loadString = "Load Stage Datas";
        yield return LoadAllFromAssetBundle<TextAsset>("Global", ProgressOnLoad).WaitForTask();
        loadString = "Load Pool Requests";
        yield return LoadAllFromAssetBundle<PoolRequest>("Global", ProgressOnLoad).WaitForTask();
        loadString = "Load Items";
        yield return LoadAllFromAssetBundle<ItemContainer>("Global", ProgressOnLoad).WaitForTask();
        loadString = "Load Unit Status";
        yield return LoadAllFromAssetBundle<UnitStatus>("Global", ProgressOnLoad).WaitForTask();

        //그냥 함수를 실행하는 것이 아닌, 이 작업 시작을 지시해야 한다.
        //LoadFileFromAssetBundle<GameObject>("Origin/Prefabs/Square.prefab");

        //if (TryGetFildFromResources("Prefabs/Square", out Sprite trash)) Debug.Log(trash);

        // Interface : 어떤 기능이 있을거란 약속.

        IsLoadingFinished = true;
        yield return null;
    }

    protected override void OnDisconnected()
    {
        DisconnectEvent?.Invoke();
        DisconnectEvent = null;
    }

    //Resources => 유니티에서 Resources라는 폴더를 만들면 사용 가능
    bool TryGetFildFromResources<T>(string path, out T result) where T : Object
    {
        result = Resources.Load<T>(path);
        return result != null;
    }

    //async => 비동기함수 => 다른 함수와 같이 돌아갈 수 있는 함수
    //Coroutine은 멀티 스레드가 아니다.
    //혼자지만 둘이서 하는 것 처럼 보인다 => 효율은 떨어짐. 결국 한 사람이니까
    //Coroutine은 데드락에 걸릴 일이 없다
    //기다려야 하는 일은 없다.
    //관리가 잘 된 멀티스레드 > 코루틴

    //저장을 할 때 가장 중요한 것 -> 어떻게 꺼낼 것인가
    public static void SaveDataFile<T>(T target) where T : Object
    {
        if (target == null) return;
        Dictionary<string, Object> innerDic;

        if(!dataDictionary.TryGetValue(typeof(T), out innerDic))
        {
            innerDic = new();
            dataDictionary.Add(typeof(T), innerDic);
        }
        //innerDic.TryAdd(target.name.ToLower(), target);
        innerDic.TryAdd(target.name, target);
    }

    protected static T GetDataFromDictionary<T>(string fileName) where T : Object
    {
        //1.글자가 없을 때 fileName is null     nullstring
        //2.글자가 없을 때 fileName.length == 0 emptystring
        if (string.IsNullOrEmpty(fileName)) return null;

        //fileName = fileName.ToLower();
        if (dataDictionary.TryGetValue(typeof(T), out Dictionary<string, Object> innerDic))
        {
            if (innerDic.TryGetValue(fileName, out Object result))
            {
                return result as T;
            }
        }
        return null;
    }   
    public static T LoadDataFile<T>(string fileName) where T : Object
    {
        T result = GetDataFromDictionary<T>(fileName);
        if(!result) UIManager.ClaimErrorMessage(SystemMessage.FileNameNotFound(fileName));
        return result;
    }
    public static bool TryLoadDataFile<T>(string fileName, out T result) where T : Object
    {
        result = GetDataFromDictionary<T>(fileName);
        return result;
    }


    //Action -> 반환값이 없는 함수.
    //Action<int>        => void Function(int a)
    //Action<int, float> => void Function(int a, float b)
    //
    //Func<float>               => float Fuction()
    //Func<float, int>          => int Fuction(float a)
    //Func<float, string int>   => int Fuction(float a, string b)
    public async Task LoadAllFromAssetBundle<T>(string label, System.Action actionForEachLoad) where T : Object
    {
        var finder = Addressables.LoadAssetsAsync<T>(label, (T loaded) => 
        {
            SaveDataFile(loaded); //로드 됬으니까 저장
            actionForEachLoad?.Invoke(); //할 일 있으면 실행
        });
        Task result = finder.Task;
        await result;
        //만약 데이터 매니저가 끝난다면 이걸 릴리즈 해달라
        DisconnectEvent += () => finder.Release();
    }

    async void LoadFileFromAssetBundle<T>(string address)  where T : Object
    {
        // A-, An-  
        // "~이 아닌", "반대되는" 이라는 접두사
        // 동기화하지 않는다 => 비동기
        // 프로세스가 동기화하지 않는다 => 하나의 프로세스로 돌리는 것이 아니다 => 멀티스레드
        // 스레드를 여럿 사용하기 위해서는 우선순위를 설정해야 함
        // 특정 기능을 쓰려고 하는데, 다른 곳에서 이 기능을 쓰고있다? => 데드락
        var finder = Addressables.LoadAssetAsync<GameObject>(address);
        await finder.Task;
        SaveDataFile(finder.Result);
        finder.Release();
    }
}
