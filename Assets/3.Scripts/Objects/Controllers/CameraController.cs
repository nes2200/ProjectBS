using System;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    CameraManager _manager;
    public CameraManager Manager => _manager;

    CameraMover _cameraMover;
    public CameraMover CameraMover => _cameraMover;

    public void SetCameraController(CameraManager cameraManager)
    {
        _manager = cameraManager;

        _cameraMover = gameObject.TryAddComponent<CameraMover>();
        CameraMover.SetCameraMover();

        InputManager.OnCameraMove -= CameraMove;
        InputManager.OnCameraMove += CameraMove;

        InputManager.OnMouseRightButton -= CameraRotatingCheck; 
        InputManager.OnMouseRightButton += CameraRotatingCheck;

        InputManager.OnCameraRotate -= CameraRotate;
        InputManager.OnCameraRotate += CameraRotate;

        InputManager.OnMouseWheelScroll -= CameraZoom;
        InputManager.OnMouseWheelScroll += CameraZoom;

        InputManager.OnMouseWheelButton -= CameraZoomReset;
        InputManager.OnMouseWheelButton += CameraZoomReset;
    }

    public void UnsetCameraController()
    {
        _manager = null;

        if(_cameraMover) _cameraMover.RemoveCameraMover();

        InputManager.OnCameraMove -= CameraMove;
        InputManager.OnMouseRightButton -= CameraRotatingCheck;
        InputManager.OnCameraRotate -= CameraRotate;
        InputManager.OnMouseWheelScroll -= CameraZoom;
        InputManager.OnMouseWheelButton -= CameraZoomReset;
    }

    private void CameraMove(Vector2 direction)
    {
        CameraMover.SetMoveDireciton(direction);
    }
    private void CameraRotate(Vector2 value)
    {
        CameraMover.SetRotateDirection(value);
    }
    private void CameraRotatingCheck(bool value, Vector2 screenPosition, Vector3 worldPosition)
    {
        CameraMover.IsRotating = value;
    }
    private void CameraZoom(Vector2 value)
    {
        CameraMover.SetZoomDirection(value);
    }
    private void CameraZoomReset(bool value, Vector2 _, Vector3 __)
    {
        CameraMover.ResetZoom();
    }
}

//스테이지 스크린에 들어가면 카메라 입력을 받아야 한다
//여기서는 카메라가 어떻게 움직일지 함수를 만들고, 해당 기능들을 InputManager의 이벤트에 등록한다
//이벤트를 언제 넣나? 스테이지 스크린이 켜질때
//이벤트를 언제 빼나? 스테이지 스크린이 꺼질때

//이벤트를 넣는데, 누가 넣나?
//스테이지 스크린이 켜질때는 언제인가?
//UIManager에서 스테이지 스크린이 켜질 때, OpenUI()로 스테이지 스크린의 OpenableUIBase의 Open()을 실행한다.
//StageScreen 스크립트에서 Open()을 오버라이드하여 켜질 때 InputManager의 이벤트에 카메라 이동 함수를 등록하게 한다

//그러면 스테이지 스크린이 카메라 컨트롤러를 알고 있어야 한다는 건가?
//스테이지가 켜지는 것은 프레임 단위로 발생하는 것이 아닌, 게임 중간중간 몇번 일어나지 않는 일임
// => 켤 때 마다 스테이지 스크린이 카메라를 찾고, 카메라 컨트롤러에서 이벤트 등록하는 함수를 호출하는 것은 큰 낭비가 아닐것 같음

//카메라 컨트롤러가 가져야 하는 것들
//이벤트 등록용 -> 입력을 받으면 실행되어 카메라 이동 관련 기능을 실행함.
//이벤트 전체 모음 등록용 -> 스테이지 스크린이 켜질 때 이걸 찾아서 실행할 것임
//이벤트 전체 모음 등록용 -> 스테이지 스크린이 꺼질 때 이걸 찾아서 실행할 것임

//카메라가 실질적으로 이동하는 기능은 카메라 매니저가 가지고 있는다
//다른 카메라들도 이동이 필요할 수 있으니까

//GameManager의 이벤트에 OnUpdateCamera를 새로 만든다
//스테이지 스크린이 켜지면. InputManager의 이벤트에 CameraController가 가진 카메라 이동 함수를 추가한다

//다른 controller처럼 모듈이나 베이스가 필요한가?
//카메라는 확장성 없이 오직 카메라만 움직일 수 있다. 굳이 필요하지 않은 것 같음
//그럼 모든 기능을 카메라 컨트롤러에 다 집어넣는건가?