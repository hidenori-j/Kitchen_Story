using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class SimpleThirdPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRig;   // yaw（左右回転）用
    public Transform cameraPivot; // pitch（上下回転）用

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f; // キャラの向き追従（大きいほどキビキビ）
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Look")]
    public float lookSensitivity = 120f; // 右スティック感度
    public float minPitch = -70f;
    public float maxPitch = 70f;

    CharacterController controller;
    PlayerInput playerInput;

    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;

    float verticalVelocity;
    float pitch; // 上下角度

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Input Actions取得（Action名は inputactions 側の名前と一致させる）
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
    }

    void Update()
    {
        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        if (cameraRig == null || cameraPivot == null) return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        // 右スティックはフレーム依存になりやすいのでdeltaTimeで補正
        float yawDelta = look.x * lookSensitivity * Time.deltaTime;
        float pitchDelta = -look.y * lookSensitivity * Time.deltaTime;

        // yaw：リグを左右回転
        cameraRig.Rotate(0f, yawDelta, 0f, Space.World);

        // pitch：ピボットを上下回転（クランプ）
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // リグはプレイヤー位置に追従（常に）
        cameraRig.position = transform.position;
    }

    void HandleMove()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();

        // カメラ基準で移動方向を作る（水平成分のみ）
        Vector3 camForward = cameraRig.forward;
        Vector3 camRight = cameraRig.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * move.y + camRight * move.x;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // 地面判定：CharacterController.isGrounded を使用
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // 地面に張り付ける小さな下向き速度

        // ジャンプ
        if (controller.isGrounded && jumpAction.WasPressedThisFrame())
        {
            // v = sqrt(h * -2g)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // 移動入力があるとき、キャラを移動方向へ向ける（自然な三人称）
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}
