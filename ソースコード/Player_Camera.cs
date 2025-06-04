using Cinemachine.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player_Camera : MonoBehaviour
{
    // ���͊֘A
    private InputManager inputManager;
    private Controller actions;

    // �R���|�[�l���g�L���b�V��
    
    [SerializeField] private Transform camera_pos;     // �J�����̈ʒu�i�q�I�u�W�F�N�g�j
    [SerializeField] private Rigidbody rb;               // �v���C���[�� Rigidbody
    [SerializeField] private AudioSource source;         // �I�[�f�B�I�\�[�X
    [SerializeField] private AudioClip walk_audio;       // ���s���̌��ʉ�
    [SerializeField] private Slider staminaSlider;       // �X�^�~�i�p�X���C�_�[

    // �v���C���[��J�����֘A
    private Player player;                               // �v���C���[�R���|�[�l���g
    private CameraShake shake;                           // �J�����V�F�C�N
    private Vector3 camera_vec;                          // �v���C���[�ƃJ�����̑��Έʒu
    public bool isHiding = false;                        // �B��Ă����ԂȂ�J�����ʒu�����𖳌���

    // �ړ��E��]�ݒ�
    private float sensX = 2.0f;
    private float sensY = 2.0f;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private float deadZone =0.75f;                       // ���̓f�b�h�]�[��

    [SerializeField] private float smoothTime = 0.3f;      // �J�����Ǐ]�̃X���[�Y�^�C��
    private Vector3 velocity = Vector3.zero;             // SmoothDamp�p�̑��x

    // �v���C���[�ړ��ݒ�
    private float baseSpeed = 300.0f;      // �ʏ�ړ����x
    private float sprintSpeed = 750.0f;    // �X�v�����g���̈ړ����x
    private float currentSpeed;          // ���݂̈ړ����x
    private float movementMultiplier = 0.01f;  // �ړ��{��

    // �X�^�~�i�Ǘ�
    private float maxStamina = 100f;
    private float currentStamina;
    private float staminaDecreaseRate = 10f;
    private float staminaRecoveryRate = 7f;
    private bool isMoving = false;
    private bool isSprinting = false;

    void Start()
    {
        // ���͂̏�����
        inputManager = InputManager.Instance;
        actions = new Controller();
        actions.Enable();

        // Rigidbody �̐ݒ�i��]�Œ�A��ԗL���j
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // �R���|�[�l���g�̎擾
        shake = GetComponent<CameraShake>();
        player = FindObjectOfType<Player>();

        // �X�^�~�i������
        currentStamina = maxStamina;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

        // �J�����ƃv���C���[�̑��Έʒu���v�Z
        camera_vec = transform.position - player.transform.position;
        currentSpeed = baseSpeed;
    }

    void FixedUpdate()
    {
        // �J������]�ƃv���C���[�ړ��̏���
        HandleCameraRotation();
        HandlePlayerMovement();

        // �B��Ă��Ȃ��ꍇ�A�J�����Ǐ]���������{
        if (!isHiding)
        {
            AdjustCameraPosition();
        }

        // �X�^�~�i�̍X�V
        UpdateStamina();

        // Rigidbody �� Y ���ʒu���v���C���[�̍����ɍ��킹��
        Vector3 rbPosition = rb.position;
        rbPosition.y = player.transform.position.y;
        rb.MovePosition(rbPosition);
    }

    /// <summary>
    /// �E�X�e�B�b�N���͂ɂ��J�����̉�]����
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
    /// ���X�e�B�b�N or WASD�L�[���͂ɂ��v���C���[�ړ�����
    /// </summary>
    void HandlePlayerMovement()
    {
        // 1) ���͎擾
        float moveX = 0f, moveZ = 0f;

        // --- �L�[�{�[�h���� (WASD / ���L�[) ---
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;

        // --- �i�I�v�V�����j�Q�[���p�b�h�̍��X�e�B�b�N��������ꍇ ---
        moveX += ApplyDeadZone(Input.GetAxis("LeftStickHorizontal"));
        moveZ += ApplyDeadZone(Input.GetAxis("LeftStickVertical"));

        Vector3 moveInput = new Vector3(moveX, 0, moveZ);

        // ���͂��܂������Ȃ��Ȃ�ړ������ɏI��
        if (moveInput == Vector3.zero)
        {
            isMoving = false;
            StopWalkSound();
            return;
        }

        // 2) �ړ��������J�����̌����ɍ��킹�ă��[���h���W�ɕϊ�
        Vector3 moveDirection = camera_pos.TransformDirection(moveInput.normalized);

        // 3) �X�v�����g���� (LeftShift �������X�^�~�i�c>0)
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

        // 4) ���ۂ̈ړ�
        Vector3 targetPos = rb.position + moveDirection * currentSpeed * movementMultiplier * Time.deltaTime;
        rb.MovePosition(targetPos);
        isMoving = true;

        // 5) ���s���Đ�
        if (!source.isPlaying)
            WalkSound();
    }


    /// <summary>
    /// �X�^�~�i�̍X�V����
    /// </summary>
    void UpdateStamina()
    {
        if (isSprinting)
        {
            // �X�v�����g���̓X�^�~�i������
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
        }
        else if (isMoving)
        {
            // �ړ���(���s)�ł����ʉ�
            float walkRecoveryRate = staminaRecoveryRate * 0.5f;  // �񕜗��𔼕��ɒ���
            currentStamina += walkRecoveryRate * Time.deltaTime;
        }
        else
        {
            // ��~���͒ʏ��
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }

        // �X�^�~�i�ʂ�͈͓��ɃN�����v��UI�X���C�_�[�X�V
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        staminaSlider.value = currentStamina;
    }

    /// <summary>
    /// ���s���̍Đ�
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
    /// ���s���̒�~
    /// </summary>
    void StopWalkSound()
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// �v���C���[�ɒǏ]����悤�ɃJ�����ʒu���X���[�Y�ɕ�Ԃ��鏈��
    /// </summary>
    void AdjustCameraPosition()
    {
        Vector3 desiredCameraPosition = player.transform.position + camera_vec;
        camera_pos.position = Vector3.SmoothDamp(camera_pos.position, desiredCameraPosition, ref velocity, smoothTime);
    }

    /// <summary>
    /// �f�b�h�]�[����K�p���ē��͒l�𒲐�����
    /// </summary>
    /// <param name="value">���͒l</param>
    /// <returns>�f�b�h�]�[���K�p��̒l</returns>
    float ApplyDeadZone(float value)
    {
        return Mathf.Abs(value) < deadZone ? 0 : value;
    }
}
