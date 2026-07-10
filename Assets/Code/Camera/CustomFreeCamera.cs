using UnityEngine;
using UnityEngine.InputSystem;

public class CustomFreeCamera : MonoBehaviour
{
    const float k_MouseSensitivityMultiplier = 0.01f;

    public float m_LookSpeedController = 120f;
    public float m_LookSpeedMouse = 4.0f;
    public float m_MoveSpeed = 10.0f;
    public float m_MoveSpeedIncrement = 2.5f;
    public float m_Turbo = 10.0f;

    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction speedAction;
    private InputAction yMoveAction;

    void OnEnable()
    {
        RegisterInputs();
    }

    void RegisterInputs()
    {
        lookAction = new InputAction("look", binding: "<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");

        moveAction = new InputAction("move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        speedAction = new InputAction("speed", binding: "<Gamepad>/dpad");
        speedAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/home")
            .With("Down", "<Keyboard>/end");

        yMoveAction = new InputAction("yMove");
        yMoveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/pageUp")
            .With("Down", "<Keyboard>/pageDown")
            .With("Up", "<Keyboard>/e")
            .With("Down", "<Keyboard>/q")
            .With("Up", "<Gamepad>/rightshoulder")
            .With("Down", "<Gamepad>/leftshoulder");

        moveAction.Enable();
        lookAction.Enable();
        speedAction.Enable();
        yMoveAction.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        speedAction?.Disable();
        yMoveAction?.Disable();
    }

    float inputRotateAxisX, inputRotateAxisY;
    float inputChangeSpeed;
    float inputVertical, inputHorizontal, inputYAxis;
    bool leftShiftBoost, leftShift, fire1;

    void UpdateInputs()
    {
        inputRotateAxisX = 0.0f;
        inputRotateAxisY = 0.0f;
        leftShiftBoost = false;
        fire1 = false;

        Vector2 overrideLookDelta = Vector2.zero;
        bool useTouchpad = false;

        // 기기 상관없이 Touchpad가 존재하고 드래그 중이라면 무조건 1순위로 가로챔!
        if (Touchpad.Instance != null && Touchpad.Instance.isDragging)
        {
            overrideLookDelta = Touchpad.Instance.MobileDragDelta * 15f;
            useTouchpad = true;
        }

        var lookDelta = lookAction.ReadValue<Vector2>();

        // 터치패드 조작 중이면 마우스 입력을 덮어씌움
        if (useTouchpad)
        {
            lookDelta = overrideLookDelta * 0.01f;
        }

        inputRotateAxisX = lookDelta.x * m_LookSpeedMouse * k_MouseSensitivityMultiplier;
        inputRotateAxisY = lookDelta.y * m_LookSpeedMouse * k_MouseSensitivityMultiplier;

        leftShift = Keyboard.current?.leftShiftKey?.isPressed ?? false;
        fire1 = Mouse.current?.leftButton?.isPressed == true || Gamepad.current?.xButton?.isPressed == true;

        inputChangeSpeed = speedAction.ReadValue<Vector2>().y;

        var moveDelta = moveAction.ReadValue<Vector2>();
        inputVertical = moveDelta.y;
        inputHorizontal = moveDelta.x;
        inputYAxis = yMoveAction.ReadValue<Vector2>().y;
    }

    void Update()
    {
        UpdateInputs();

        if (inputChangeSpeed != 0.0f)
        {
            m_MoveSpeed += inputChangeSpeed * m_MoveSpeedIncrement;
            if (m_MoveSpeed < m_MoveSpeedIncrement) m_MoveSpeed = m_MoveSpeedIncrement;
        }

        bool moved = inputRotateAxisX != 0.0f || inputRotateAxisY != 0.0f || inputVertical != 0.0f || inputHorizontal != 0.0f || inputYAxis != 0.0f;
        if (moved)
        {
            float rotationX = transform.localEulerAngles.x;
            float newRotationY = transform.localEulerAngles.y + inputRotateAxisX;

            float newRotationX = (rotationX - inputRotateAxisY);
            if (rotationX <= 90.0f && newRotationX >= 0.0f)
                newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
            if (rotationX >= 270.0f)
                newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);

            transform.localRotation = Quaternion.Euler(newRotationX, newRotationY, transform.localEulerAngles.z);

            float moveSpeed = Time.deltaTime * m_MoveSpeed;
            if (fire1 || leftShiftBoost && leftShift)
                moveSpeed *= m_Turbo;

            transform.position += transform.forward * (moveSpeed * inputVertical)
                                  + transform.right * (moveSpeed * inputHorizontal)
                                  + Vector3.up * (moveSpeed * inputYAxis);
        }
    }
}