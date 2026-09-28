using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class CameraManager : ManagerBase
{
    public Camera MainCamera { get; private set; }

    CameraController _controller;
    public CameraController Controller => _controller;

    public static event System.Action OnCameraViewChanged;
    public void CameraChangedNotify() { OnCameraViewChanged?.Invoke();}


    //카메라 원래 위치 저장용 구조체
    struct MainCameraTransform
    {
        public Vector3 position, rotation, scale;
    }
    MainCameraTransform mainCameraDefaultPosition = new MainCameraTransform
    {
        position = new Vector3(0f, 10f, -20f),
        scale = new Vector3(1f, 1f, 1f),
        rotation = new Vector3(30f, 0f, 0f)
    };

    protected override IEnumerator Onconnected(GameManager newManager)
    {
        SetMainCamera(Camera.main); 
        yield return null;
    }

    protected override void OnDisconnected()
    {
        RemoveCameraController();
    }

    public void SetMainCamera(Camera wantCamera)
    {
        MainCamera = wantCamera;
    }

    public void GetRaycastResult(Vector2 screenPosition, List<RaycastResult> outResult)
    {
        EventSystem currentEvent = EventSystem.current;

        if (!currentEvent) return;

        PointerEventData eventData = new(currentEvent);
        eventData.position = screenPosition;
        currentEvent.RaycastAll(eventData, outResult);
    }

    //카메라에 카메라 컨트롤러 넣기
    //카메라에 넣고 다니는게 아닌, 카메라에 넣고 뺴고 하는 이유 => 스테이지 아니더라도 카메라가 필요한 곳은 많다
    //혹시나 모를 상황을 방지하기 위해 enable disable 하는 것보다 넣고 빼는게 맞다고 생각함
    public void AddCameraController()
    {
        if (!MainCamera) return;

        _controller = MainCamera.TryAddComponent<CameraController>();
        Controller.SetCameraController(this);
    }
    public void RemoveCameraController()
    {
        if (!_controller) return;

        Controller.UnsetCameraController();
    }

    public void SetCameraDefaultPosition()
    {
        if (!MainCamera) return;

        Transform mainCamTransform = MainCamera.transform;
        mainCamTransform.position = mainCameraDefaultPosition.position;
        mainCamTransform.eulerAngles = mainCameraDefaultPosition.rotation;
        mainCamTransform.localScale = mainCameraDefaultPosition.scale;

    }
}

