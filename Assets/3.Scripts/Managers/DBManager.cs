using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DBManager : ManagerBase
{
    FirebaseAuth authentication;
    private FirebaseUser user;
    private DatabaseReference rootDB;

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        //              의존성 검사         비동기 
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(InitializeFirebase);
        yield return null;   
    }

    protected override void OnDisconnected()
    {
       
    }

    void InitializeFirebase(Task<DependencyStatus> task)
    {
        if(task is null)
        {
            Debug.Log($"Firebase Initialize Failed. No {task}");
        }

        if(task.Result == DependencyStatus.Available)
        {
            //인증용 인스턴스 가져오기
            authentication = FirebaseAuth.DefaultInstance;
            //인증을 하기 위해 필요한 "유저" 가져오기
            user = authentication.CurrentUser;
            //데이터 베이스에 가려면 데이터 베이스가 어디에 있는지 찾아갈 수 있어야 한다
            //데이터 베이스 참조(Reference)
            rootDB = FirebaseDatabase.DefaultInstance.RootReference;

            GuestLogin();
         
            Debug.Log("Firebase Initialize");
        }
        else
        {
            Debug.LogError($"Fail to Initialize Firebase : {task.Exception}");
        }
    }

    public async void GuestLogin()
    {
        //인증기가 없다면?
        if (authentication is null) return;
        //이미 로그인 되어있는지 체크하기
        if(user is not null)
        {
            Debug.Log($"Already signed in : {user.UserId}");
            return;
        }
        //익명으로 로그인하기
        await authentication.SignInAnonymouslyAsync().ContinueWithOnMainThread(OnLoginResult);
    }

    private void OnLoginResult(Task<AuthResult> task)
    {
        if(task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError($"Failed to Sign in : {task.Exception}");
            return;
        }

        user = task.Result.User;
        Debug.Log($"Sign in Succeed : {user.UserId}");
    }

    public DatabaseReference GetFinalDirectory(DatabaseReference root, params string[] directory)
    {
        if (directory is null || directory.Length == 0) return root;

        DatabaseReference currentReference = root;
        foreach (string currentChild in directory)
        {
            currentReference = currentReference.Child(currentChild);
        }
        return currentReference;
    }

    public async Task<string> SaveCustomMapAsync(CustomMapData customMap)
    {
        if (rootDB is null || authentication is null)
            throw new InvalidOperationException("Firebase 초기화가 완료되지 않음");

        FirebaseUser creator = authentication.CurrentUser;
        if (creator is null)
            throw new InvalidOperationException("Firebase 로그인이 완료되지 않음");
        if (customMap is null || customMap.data is null)
            throw new ArgumentException("저장할 맵 데이터 없음", nameof(customMap));
        if (string.IsNullOrWhiteSpace(customMap.mapName))
            throw new ArgumentException("맵 이름 없음", nameof(customMap));

        customMap.creatorUID = creator.UserId;
        string json = JsonUtility.ToJson(customMap);
        DatabaseReference mapReference = GetFinalDirectory(rootDB, "customMaps").Push();

        // 서버 쓰기가 완료되어야 호출한 UI에 성공을 알린다.
        await mapReference.SetRawJsonValueAsync(json);
        return mapReference.Key;
    }
    public void WriteData(object wantData, params string[] directory)
    {
        if (rootDB is null || wantData is null) return;

        //NoSQL은 JSON으로 저장한다
        string jsonData = JsonUtility.ToJson(wantData);
        //뿌리에서부터 시작
        GetFinalDirectory(rootDB, directory).SetRawJsonValueAsync(jsonData).ContinueWithOnMainThread(OnTaskResult);
    }
    public void WriteData(Dictionary<string, object> changes, params string[] directory)
    {
        if (rootDB is null || changes is null) return;

        //폴더를 따라 내려가는 것
        //제일 처음에 만든 reference가 바로 root폴더 => c드라이브다
        GetFinalDirectory(rootDB, directory).UpdateChildrenAsync(changes).ContinueWithOnMainThread(OnTaskResult);
    }

    public void ReadData(Action<Task<DataSnapshot>> OnReadData, params string[] directory)
    {
        GetFinalDirectory(rootDB, directory).GetValueAsync().ContinueWithOnMainThread(OnReadData);
    }

    //데이터 읽기의 경우 기다려야 하는 사안이 많음
    //로그인시 로그인 정보 읽기가 끝나야 들여보내줌
    //아이템 구매시 내 인벤토리 정보를 읽어보고 구매 가능하면 그때 아이템 구매를 허가해줌
    //이렇듯 데이터 읽기가 끝나야 다음 일을 해야 하는 경우가 많음
    public IEnumerator ReadDataCoroutine(Action<Task<DataSnapshot>> OnReadData, params string[] directory)
    {
        Task<DataSnapshot> readTask = GetFinalDirectory(rootDB, directory).GetValueAsync();
        yield return readTask.WaitForTask();
        OnReadData?.Invoke(readTask);
    }

    //기다릴 수 있는 형태의 함수 => 코루틴, 비동기
    //                        Ienumerator async
    public async Task<T> ReadDataAsync<T>(params string[] directory)
    {
        //다른 비동기 함수가 진행되는 동안 기다린다고 알려주는 구문
        DataSnapshot currentTask = await GetFinalDirectory(rootDB, directory).GetValueAsync();

        if (currentTask is null) return default;
        if (!currentTask.Exists) return default;

        //1. 복합타입
        //구조화된 존재를 JSON으로 저장하고 있었다
        try
        {
            if (currentTask.HasChildren)
            {
                return JsonUtility.FromJson<T>(currentTask.GetRawJsonValue());
            }
            //2. 단일타입
            return (T)System.Convert.ChangeType(currentTask.Value, typeof(T));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    private void OnTaskResult(Task task)
    {
        if(task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError(task.Exception);
        }
    }
}
