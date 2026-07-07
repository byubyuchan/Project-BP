using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class AutoCameraCanvas : MonoBehaviour
{
    public static AutoCameraCanvas Instance { get; private set; }

    public Canvas myCanvas;

    void Awake()
    {
        Instance = this;

        if (myCanvas == null)
            myCanvas = GetComponent<Canvas>();
    }

    /// <summary>
    /// 로컬 플레이어가 활성화될 때 호출하여 카메라를 주입받는 메서드
    /// </summary>
    public void RegisterLocalPlayerCamera(Camera playerCamera)
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("[AutoCameraCanvas] 넘어온 카메라가 null입니다.");
            return;
        }
        myCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        myCanvas.worldCamera = playerCamera;
        myCanvas.planeDistance = 1f;

        // 어느 캔버스에 붙었는지 이름까지 명확하게 로그로 출력
        Debug.Log($"[AutoCameraCanvas] {gameObject.name}에 카메라({playerCamera.name}) 연결 성공!");
    }
}