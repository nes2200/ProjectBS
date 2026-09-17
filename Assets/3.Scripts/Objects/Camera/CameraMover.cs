using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [Header("카메라 움직임 속도")]
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float rotateSpeed = 0.075f;
    [SerializeField] float zoomSpeed = 10f;

    [Header("카메라 줌 수치")]
    [SerializeField] float minFov = 20f;
    [SerializeField] float maxFov = 70f;
    [SerializeField] float lerpSpeed = 500f;
    float mainFov = 40f;


    Vector2 moveDirection;
    Vector2 rotateDirection;
    Vector2 zoomDirection;

    Camera mainCamera;

    bool _isRotating = false;
    public bool IsRotating { get { return _isRotating; } set { _isRotating = value; } }

    float xRotation, yRotation;

    public void SetCameraMover()
    {
        mainCamera = GameManager.Camera.MainCamera;

        GameManager.OnUpdateCamera -= CameraTransformUpdate;
        GameManager.OnUpdateCamera += CameraTransformUpdate;

        //처음에는 0f, 0f로 초기화 되어있기에 카메라가 확 튀어버리고 이를 방지하기 위해 초기값을 추가해준다
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        xRotation = currentRotation.x;
        yRotation = currentRotation.y;
    }
    public void RemoveCameraMover()
    {
        GameManager.OnUpdateCamera -= CameraTransformUpdate;
    }

    void CameraTransformUpdate(float deltaTime)
    {
        if (CameraInputBlocker.IsBlocked) return;

        Vector3 previousPosition = transform.position;
        Quaternion previousRotation = transform.localRotation;
        float previousFov = mainCamera.fieldOfView;

        MovementUpdate(deltaTime);
        RotateUpdate(deltaTime);
        ZoomUpdate(deltaTime);

        bool positionChanged = transform.position != previousPosition;
        bool rotationChanged = transform.localRotation != previousRotation;
        bool zoomChanged = !Mathf.Approximately(mainCamera.fieldOfView, previousFov);

        if (positionChanged || rotationChanged || zoomChanged)
        {
            GameManager.Camera.CameraChangedNotify();
        }
    }
    private void MovementUpdate(float deltaTime)
    {
        if(moveDirection == Vector2.zero) return; 

        Vector3 localDirection = new Vector3(moveDirection.x, 0f, moveDirection.y);
        transform.Translate(localDirection * moveSpeed * deltaTime);
    }
    private void RotateUpdate(float deltaTime)
    {
        if (!IsRotating) return;

        //마우스 좌우로 움직인다 -> 마우스의 x축 변화량이 들어왔다
        //그렇다면 카메라가 좌우로 회전해야 한다. 여기서 뭘 기준으로 회전한다는 것인가? 
        //y축을 중심으로 회전해야 좌우로 움직인다
        //그렇기에, 받아온 델타의 x를 y축 중심으로 움직이게 해줘야 카메라가 좌우로 움직인다
        //마우스 위아래 움직임은 y축의 변화량을 의미하며, x축을 중심으로 회전해야 카메라의 위아래 움직임을 표현 가능하다
        //빼는 이유는, x축의 rotate 값이 커지면 카메라의 시야가 아래로 내려가기 때문
        //y축 델타값이 양수라면, 그러니까 마우스가 위로 가면 x축이 감소하며 카메라를 위로 회전시켜 줘야함
        //그렇기에 x축 회전량은 y 델타의 반대로 빼줘야한다.
        yRotation += rotateDirection.x * rotateSpeed;
        xRotation -= rotateDirection.y * rotateSpeed;

        //마우스가 너무 위를 바라보거나 아래를 바라봐서 카메라 찐빠가 나지 않기 위해서
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
    private void ZoomUpdate(float deltaTime)
    {
        if (CameraInputBlocker.IsBlocked) return;
        if (zoomDirection == Vector2.zero) return;

        float targetFov = mainCamera.fieldOfView - (zoomDirection.y * zoomSpeed);
        targetFov = Mathf.Clamp(targetFov, minFov, maxFov);
        mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, targetFov, deltaTime * lerpSpeed);
    }

    public void SetMoveDireciton(Vector2 direction)
    {
        moveDirection = direction;
    }
    public void SetRotateDirection(Vector2 value)
    {
       rotateDirection = value;
    }
    public void SetZoomDirection(Vector2 value)
    {
        zoomDirection = value;
    }
    public void ResetZoom()
    {
        if (Mathf.Approximately(mainCamera.fieldOfView, mainFov))  return;
        
        mainCamera.fieldOfView = mainFov;
        GameManager.Camera.CameraChangedNotify();
    }
}
