using Cinemachine.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player_Camera : MonoBehaviour
{
    // 入力関連
    private InputManager inputManager;
    private Controller actions;

    // コンポーネントキャッシュ
    
    [SerializeField] private Transform camera_pos;     // カメラの位置（子オブジェクト）
    [SerializeField] private Rigidbody rb;               // プレイヤーの Rigidbody
    [SerializeField] private AudioSource source;         // オーディオソース
    [SerializeField] private AudioClip walk_audio;       // 歩行時の効果音
    [SerializeField] private Slider staminaSlider;       // スタミナ用スライダー

    // プレイヤーやカメラ関連
    private Player player;                               // プレイヤーコンポーネント
    private CameraShake shake;                           // カメラシェイク
    private Vector3 camera_vec;                          // プレイヤーとカメラの相対位置
    public bool isHiding = false;                        // 隠れている状態ならカメラ位置調整を無効化

    // 移動・回転設定
    private float sensX = 2.0f;
    private float sensY = 2.0f;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private float deadZone =0.75f;                       // 入力デッドゾーン

    [SerializeField] private float smoothTime = 0.3f;      // カメラ追従のスムーズタイム
    private Vector3 velocity = Vector3.zero;             // SmoothDamp用の速度

    // プレイヤー移動設定
    private float baseSpeed = 300.0f;      // 通常移動速度
    private float sprintSpeed = 750.0f;    // スプリント時の移動速度
    private float currentSpeed;          // 現在の移動速度
    private float movementMultiplier = 0.01f;  // 移動倍率

    // スタミナ管理
    private float maxStamina = 100f;
    private float currentStamina;
    private float staminaDecreaseRate = 10f;
    private float staminaRecoveryRate = 7f;
    private bool isMoving = false;
    private bool isSprinting = false;

    void Start()
    {
        // 入力の初期化
        inputManager = InputManager.Instance;
        actions = new Controller();
        actions.Enable();

        // Rigidbody の設定（回転固定、補間有効）
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // コンポーネントの取得
        shake = GetComponent<CameraShake>();
        player = FindObjectOfType<Player>();

        // スタミナ初期化
        currentStamina = maxStamina;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

        // カメラとプレイヤーの相対位置を計算
        camera_vec = transform.position - player.transform.position;
        currentSpeed = baseSpeed;
    }

    void FixedUpdate()
    {
        // カメラ回転とプレイヤー移動の処理
        HandleCameraRotation();
        HandlePlayerMovement();

        // 隠れていない場合、カメラ追従処理を実施
        if (!isHiding)
        {
            AdjustCameraPosition();
        }

        // スタミナの更新
        UpdateStamina();

        // Rigidbody の Y 軸位置をプレイヤーの高さに合わせる
        Vector3 rbPosition = rb.position;
        rbPosition.y = player.transform.position.y;
        rb.MovePosition(rbPosition);
    }

    /// <summary>
    /// 右スティック入力によるカメラの回転処理
    /// </summary>
    void HandleCameraRotation()
    {
        float rightStickX = ApplyDeadZone(Input.GetAxis("RightStickHorizontal"));
        float rightStickY = ApplyDeadZone(Input.GetAxis("RightStickVertical"));

        rotationX += rightStickX * sensX;
        rotationY -= rightStickY * sensY;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);
        camera_pos.localRotation = Quaternion.Euler(rotationY, rotationX, 0);
    }
    /// <summary>
    /// 左スティック or WASDキー入力によるプレイヤー移動処理
    /// </summary>
    void HandlePlayerMovement()
    {
        // 1) 入力取得
        float moveX = 0f, moveZ = 0f;

        // --- キーボード入力 (WASD / 矢印キー) ---
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;

        // --- （オプション）ゲームパッドの左スティックも混ぜる場合 ---
        moveX += ApplyDeadZone(Input.GetAxis("LeftStickHorizontal"));
        moveZ += ApplyDeadZone(Input.GetAxis("LeftStickVertical"));

        Vector3 moveInput = new Vector3(moveX, 0, moveZ);

        // 入力がまったくないなら移動せずに終了
        if (moveInput == Vector3.zero)
        {
            isMoving = false;
            StopWalkSound();
            return;
        }

        // 2) 移動方向をカメラの向きに合わせてワールド座標に変換
        Vector3 moveDirection = camera_pos.TransformDirection(moveInput.normalized);

        // 3) スプリント判定 (LeftShift 押下かつスタミナ残>0)
        if (Input.GetKey(KeyCode.LeftShift)|| Input.GetKey(KeyCode.Joystick1Button0)&& currentStamina > 0f)
        {
            currentSpeed = sprintSpeed;
            isSprinting = true;
        }
        else
        {
            currentSpeed = baseSpeed;
            isSprinting = false;
        }

        // 4) 実際の移動
        Vector3 targetPos = rb.position + moveDirection * currentSpeed * movementMultiplier * Time.deltaTime;
        rb.MovePosition(targetPos);
        isMoving = true;

        // 5) 歩行音再生
        if (!source.isPlaying)
            WalkSound();
    }


    /// <summary>
    /// スタミナの更新処理
    /// </summary>
    void UpdateStamina()
    {
        if (isSprinting)
        {
            // スプリント中はスタミナを減少
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
        }
        else if (isMoving)
        {
            // 移動中(歩行)でも少量回復
            float walkRecoveryRate = staminaRecoveryRate * 0.5f;  // 回復率を半分に調整
            currentStamina += walkRecoveryRate * Time.deltaTime;
        }
        else
        {
            // 停止中は通常回復
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }

        // スタミナ量を範囲内にクランプ＆UIスライダー更新
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        staminaSlider.value = currentStamina;
    }

    /// <summary>
    /// 歩行音の再生
    /// </summary>
    void WalkSound()
    {
        if (walk_audio != null && source != null)
        {
            source.clip = walk_audio;
            source.loop = true;
            source.Play();
        }
    }

    /// <summary>
    /// 歩行音の停止
    /// </summary>
    void StopWalkSound()
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// プレイヤーに追従するようにカメラ位置をスムーズに補間する処理
    /// </summary>
    void AdjustCameraPosition()
    {
        Vector3 desiredCameraPosition = player.transform.position + camera_vec;
        camera_pos.position = Vector3.SmoothDamp(camera_pos.position, desiredCameraPosition, ref velocity, smoothTime);
    }

    /// <summary>
    /// デッドゾーンを適用して入力値を調整する
    /// </summary>
    /// <param name="value">入力値</param>
    /// <returns>デッドゾーン適用後の値</returns>
    float ApplyDeadZone(float value)
    {
        return Mathf.Abs(value) < deadZone ? 0 : value;
    }
}
