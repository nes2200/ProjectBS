using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectPoolModule
{
    //오브젝트 하나를 관리하는 하청 모듈
    PoolSetting _setting;
    public PoolSetting Setting => _setting;

    Transform rootTransform;

    //"대기열" => 선입 선출. 먼저 온 애가 먼저 나간다.
    Queue<GameObject> prepareQueue = new();

    //생성할 때 정보를 넣어주기
    //생성자! 반환값 => 본인, 이름 => 본인
    public ObjectPoolModule(PoolSetting newSetting)
    {
        _setting = newSetting;
    }

    public void Initialize()
    {
        //초기 요구량이 0일 경우 생성 안함
        if (Setting.countInitial == 0) return;

        EnsureInitialized();
        PrepareObjects(Setting.countInitial);
    }
    //나중에 풀을 만들고 싶을때 쓰기 위해 분리
    private void EnsureInitialized()
    {
        if (rootTransform) return;

        // 이전 스테이지가 언로드되면 큐에는 파괴된 참조가 남을 수 있음
        prepareQueue.Clear();

        rootTransform = new GameObject(Setting.poolName).transform;

        //풀링하려고 하는 원본 프리팹에 "PooledObject"라고 하는게 안들어있으면 추가해 줄 필요가 있다
        Setting.target?.TryAddComponent<PooledObject>();
    }

    GameObject PrepareObject()
    {
        if (!Setting.target) return null;
        GameObject result = CreateFromPrefab();
        EnqueueObject(result);
        return result;
    }

    //uint => 마이너스가 존재하면 안됨
    //unsigned => 부호가 없다
    void PrepareObjects(uint count)
    {
        if (!Setting.target) return;
        for (uint i = 0; i < count; i++)
        {
            GameObject result = CreateFromPrefab();
            EnqueueObject(result);
        }
    }
    void PrepareObjects(uint count, out GameObject activeObject)
    {
        if (!Setting.target)
        {
            activeObject = null;
            return;
        }

        activeObject = CreateFromPrefab();

        for (uint i = 1; i < count; i++)
        {
            GameObject result = CreateFromPrefab();
            EnqueueObject(result);
        }
    }


    public void PrepareAtLeast(uint count)
    {
        EnsureInitialized();

        uint currentCount = (uint)prepareQueue.Count;
        if (currentCount >= count) return;

        PrepareObjects(count - currentCount);
    }

    public GameObject CreateFromPrefab()
    {
        GameObject result = ObjectManager.CreateObject(Setting.target, rootTransform);
        
        if (result)
        {
            result.name = Setting.poolName;
            if(result.TryGetComponent(out PooledObject pool))
            {
                pool.OnEnqueueEvent -= DestroyObject;
                pool.OnEnqueueEvent += DestroyObject;
            }
        }
        return result;
    }

    //오브젝트 생성 부탁
    public GameObject CreateObject(Transform parent = null)
    {
        //대기자중에 꺼내보기
        GameObject result;
        //대기열에 아무도 없을때
        if(!prepareQueue.TryDequeue(out result))
        {
            //새로 대기자를 뽑아오기
            PrepareObjects(Setting.countAdditional, out result);
        }

        if (result) //만들어 졌는지 체크
        {
            if (result.TryGetComponent(out PooledObject pool))
            {
                pool.OnDequeue();
            }

            result.SetActive(true);

            Transform currentTransform = result.transform;
            Transform originTransform = Setting.target.transform;

            currentTransform.SetParent(parent);
            //위치, 크기, 회전을 "부모를 기준으로" 초기화 해줘야함
            //2가지 상황 (일반적인 상황, UI인 상황)
            //원본과 자식 모두 렉트일때만
            if(currentTransform is RectTransform asRectTransform 
                && originTransform is RectTransform originRectTransform)
            {
                //1.앵커를 복사해오기
                asRectTransform.anchorMin = originRectTransform.anchorMin;
                asRectTransform.anchorMax = originRectTransform.anchorMax;
                //2.피벗 복사해오기
                asRectTransform.pivot = originRectTransform.pivot;

                //화면을 강제로 갱신
                if (parent) 
                { 
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parent.transform as RectTransform);
                }

                //이 친구가 stretch인 것을 확인할 수 있는 방법
                bool stretchX = asRectTransform.anchorMin.x != originRectTransform.anchorMin.x;
                bool stretchY = asRectTransform.anchorMin.y != originRectTransform.anchorMin.y;
                if(stretchX || stretchY)
                {
                    asRectTransform.offsetMin = originRectTransform.offsetMin;
                    asRectTransform.offsetMax = originRectTransform.offsetMax;
                }
                else
                {
                    //3.앵커를 기준으로 만든 "위치값"을 가져와야 함
                    asRectTransform.anchoredPosition = originRectTransform.anchoredPosition;
                    //4.stretch 상태가 아니라면 UI의 "사이즈 값"을 가져온다
                    asRectTransform.sizeDelta = originRectTransform.sizeDelta;
                }
            }
            else
            {
                currentTransform.localPosition = originTransform.localPosition;
            }
            currentTransform.localRotation = originTransform.localRotation;
            currentTransform.localScale = originTransform.localScale;

        }

        return result;
    }

    //오브젝트 제거 부탁
    public void DestroyObject(GameObject target)
    {
        EnqueueObject(target);
        if (target)
        {
            target.transform.SetParent(rootTransform);
        }
    }

    public void EnqueueObject(GameObject target)
    {
        if(!target) return;

        target.SetActive(false);
        //부모 - 자식 관계 만들기
        //폴더처럼 쓸 수 있는 것을 만들어보자
        prepareQueue.Enqueue(target);
    }
}
