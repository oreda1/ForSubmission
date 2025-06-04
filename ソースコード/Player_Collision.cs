using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player_Collision : MonoBehaviour
{
    //�V�[���J�ڗp
    private SceneManagement sceneManagemet;
    private FadeManager fadeManager;


    // �C���X�y�N�^�[����Z�b�g����ϐ�
    [SerializeField] private Indicate_Item indicate_Item;
    [SerializeField] private Text mainText;
    [SerializeField] private List<EnemyAction> enemyActions = new List<EnemyAction>();
    [SerializeField] private float maxDistance = 5.0f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject displayButton;
    [SerializeField] private GameObject controller;

    // �����ϐ�
    private StoryManager storyManager;
    private Rigidbody rigid;
    private CapsuleCollider capsuleCollider;
    private Player_Camera playerCamera;
    private RawImageAppeal rawImageAppeal;

    // ��ԊǗ��p�̕ϐ�
    public string itemName, doorName;
    private Transform doorTransform;
    private int clearCount;
    private bool clearFlag;
    public bool isHit;
    private float itemCount;

    // �B���֘A�i�B��Ă��邩�ǂ����̏�Ԃ� isHiding �ŊǗ��j
    private Transform hidingCameraPosition;
    private Quaternion hidingCameraRotation;
    public bool isHiding = false;

    // �{�^���\���p�̃C�x���g�i�K�v�ɉ����ė��p�j
    public static event System.Action OnButtonIndicate;

    // �h�A��]�p
    private float currentYRotation;
    
    
    private void Start()
    {
        sceneManagemet=SceneManagement.Instance;
        fadeManager = FadeManager.Instance;


        itemCount = 8; // �A�C�e���̏�����
        // �R���|�[�l���g�̃L���b�V��
        rigid = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerCamera = GetComponent<Player_Camera>();
        playerTransform = transform;
        rawImageAppeal = displayButton.GetComponent<RawImageAppeal>();

        // ������
        clearCount = 0;
        storyManager = new StoryManager();
        clearFlag = false;
        isHit = false;

        // �C�x���g�o�^�i�A�C�e�����̍X�V�j
        if (indicate_Item != null)
        {
            indicate_Item.OnLeftoverCountChanged += HandleLeftoverCountChanged;
        }
    }

    private void HandleLeftoverCountChanged(int newCount)
    {
        Debug.Log("�A�C�e���c�����X�V����܂���: " + newCount);
        itemCount = newCount;
    }

    private void Update()
    {
        // ���t���[���A���E���̉B���ꏊ���`�F�b�N���A�ł��߂����̖̂��O���擾
        string closestRefugeName = DisplayRefugeInView();

        // ���t���[���A���E���̃A�C�e���`�F�b�N�F�ł��߂��A�C�e���̖��O���擾���� itemName �ɍX�V����
        itemName = DisplayItemsInView();

        // ���͏����F�h�A����ƉB��鑀��̊Ǘ�
        HandleDoorInput();
        HandleHidingInput(closestRefugeName);
    }

    /// <summary>
    /// �h�A����̓��͏���
    /// </summary>
    private void HandleDoorInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Joystick1Button0))
        {
            if (doorTransform != null)
            {
                currentYRotation = doorTransform.localEulerAngles.y;
                if (currentYRotation < 135 && currentYRotation >= 0)
                {
                    StartCoroutine(DoorMove());
                }
            }
        }
    }

    /// <summary>
    /// �B���A�N�V�����̓��͏���
    /// </summary>
    /// <param name="closestRefugeName">���E���ōł��߂��B��ꏊ�̖��O</param>
    private void HandleHidingInput(string closestRefugeName)
    {
        if (Input.GetKeyDown(KeyCode.Joystick1Button0))
        {
            if (!isHiding && !string.IsNullOrEmpty(closestRefugeName))
            {
                StartHiding();
            }
            else if (isHiding)
            {
                StopHiding();
            }
        }
    }

    /// <summary>
    /// ���E���ɂ���uItem�v�^�O�̃I�u�W�F�N�g�̒�����ł��߂����̖̂��O��Ԃ�
    /// </summary>
    public string DisplayItemsInView()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        GameObject closestItem = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject item in items)
        {
            Vector3 itemPosition = item.transform.position;
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(itemPosition);

            if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
            {
                Collider itemCollider = item.GetComponent<Collider>();
                if (itemCollider != null && GeometryUtility.TestPlanesAABB(planes, itemCollider.bounds))
                {
                    float distance = Vector3.Distance(playerTransform.position, itemPosition);
                    if (distance < closestDistance && distance <= maxDistance)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }
        }
        return (closestItem != null) ? closestItem.name : null;
    }

    /// <summary>
    /// ���E���ɂ���uRefuge�v�^�O�̃I�u�W�F�N�g�̒�����ł��߂����̖̂��O��Ԃ��A�B��邽�߂̏���ݒ肷��
    /// </summary>
    public string DisplayRefugeInView()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        GameObject[] refuges = GameObject.FindGameObjectsWithTag("Refuge");

        GameObject closestRefuge = null;
        float closestDistance = float.MaxValue;
        Transform candidateHidingPos = null;
        Quaternion candidateHidingRot = Quaternion.identity;

        foreach (GameObject refuge in refuges)
        {
            Collider refugeCollider = refuge.GetComponent<Collider>();
            if (refugeCollider == null)
            {
                continue;
            }

            Vector3 refugePosition = refuge.transform.position;
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(refugePosition);

            if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
            {
               

                if (GeometryUtility.TestPlanesAABB(planes, refugeCollider.bounds))
                {
                                        
                    float distance = Vector3.Distance(playerTransform.position, refugePosition);
                    if (distance < closestDistance && distance <= 2)
                    {
                        closestDistance = distance;
                        closestRefuge = refuge;

                        Hidding hidingSpot = refuge.GetComponent<Hidding>();
                        if (hidingSpot != null)
                        {
                            candidateHidingPos = hidingSpot.cameraPosition;
                            candidateHidingRot = hidingSpot.cameraRotation;
                        }
                    }
                    else
                    {
                        refuge.GetComponent<Outline>().enabled = false; 
                    }
                }
              
                
            }
        }

        if (closestRefuge != null)
        {
            // �B���ꏊ���߂��ɂ���ꍇ�A�{�^����\�����ĉ��o���J�n
            displayButton.GetComponent<RawImage>().enabled = true;
            displayButton.SetActive(true);
            rawImageAppeal.StartAppeal();

            // �B���J�����̈ʒu�Ɖ�]���X�V
            hidingCameraPosition = candidateHidingPos;
            hidingCameraRotation = candidateHidingRot;

            // Outline �̏�Ԓ����i�B��Ă���ꍇ�͔�\���j
            Outline outline = closestRefuge.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = !isHiding;
            }
        }
        else
        {
            // �B���ꏊ�������ꍇ�A�{�^�����\���ɂ���
            displayButton.GetComponent<RawImage>().enabled = false;
            displayButton.SetActive(false);
        }

        return (closestRefuge != null) ? closestRefuge.name : null;
    }

    /// <summary>
    /// �h�A���J����R���[�`��
    /// </summary>
    public IEnumerator DoorMove()
    {
        // �h�A���J���鏈��
        for (int i = 0; i < 90; i++)
        {
            doorTransform.Rotate(0, 3f, 0);
            currentYRotation = doorTransform.localEulerAngles.y;
            if (currentYRotation >= 135)
            {
                doorTransform.localEulerAngles = new Vector3(doorTransform.localEulerAngles.x, 135, doorTransform.localEulerAngles.z);
                yield break;
            }
            yield return new WaitForSeconds(0.02f);
        }
        yield return new WaitForSeconds(1.0f);
        // �h�A��߂鏈��
        for (int i = 0; i < 90; i++)
        {
            doorTransform.Rotate(0, -1.5f, 0);
            currentYRotation = doorTransform.localEulerAngles.y;
            if (currentYRotation <= 0)
            {
                doorTransform.localEulerAngles = new Vector3(doorTransform.localEulerAngles.x, 0, doorTransform.localEulerAngles.z);
                yield break;
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Object"))
        {
            HandleObjectCollision();
        }
        else if (other.CompareTag("Door"))
        {
            doorName = other.gameObject.name;
            doorTransform = other.gameObject.transform;
        }
    }

    /// <summary>
    /// �I�u�W�F�N�g�Ƃ̏Փˎ��̏���
    /// </summary>
    private void HandleObjectCollision()
    {
        clearCount++;

        if (clearFlag)
        {
            SceneManager.LoadScene("ClearScene", LoadSceneMode.Single);
            DontDestroyOnLoad(controller);
        }
        else
        {
            mainText.text = "����A�A�J���Ȃ�....�����A�C�e��������Ȃ�";

            if (clearCount == 10 || itemCount == 0)
            {
                mainText.text = "������A�A�͋Z�ł����J������!!";
                Invoke("Clear", 3);
                DontDestroyOnLoad(controller);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
           fadeManager.LoadSceneWithFade("GameOver");
            DontDestroyOnLoad(controller);
        }
        if (collision.collider.CompareTag("Item"))
        {
            // You might still want to use this collision event for when the player physically touches an item.
            itemName = collision.gameObject.name;
            isHit = true;
        }
        if (collision.collider.CompareTag("Door"))
        {
            doorName = collision.gameObject.name;
            doorTransform = collision.gameObject.transform;
        }
        if (collision.collider.CompareTag("floor"))
        {
            rigid.drag = 0;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Refuge"))
        {
            mainText.text = "";
        }
    }

    public void Clear()
    {
        SceneManager.LoadScene("ClearScene", LoadSceneMode.Single);
    }

    /// <summary>
    /// �J�����̈ʒu�Ɖ�]���w�莞�Ԃŕ�Ԃ�����R���[�`��
    /// </summary>
    private IEnumerator MoveCamera(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        float elapsedTime = 0;
        Vector3 startingPosition = mainCamera.transform.position;
        Quaternion startingRotation = mainCamera.transform.rotation;

        while (elapsedTime < duration)
        {
            mainCamera.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / duration);
            mainCamera.transform.rotation = Quaternion.Slerp(startingRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
    }

    private void StartHiding()
    {
        if (hidingCameraPosition == null) return;
        isHiding = true;
        playerCamera.isHiding = true;
        rigid.isKinematic = true;
        capsuleCollider.isTrigger = true;
        StartCoroutine(MoveCamera(hidingCameraPosition.position, hidingCameraRotation, 1.0f));
        capsuleCollider.enabled = false;
        displayButton.SetActive(false);
        mainText.text = "�B��Ă���...";
    }

    private void StopHiding()
    {
        if (playerCamera == null || playerTransform == null || capsuleCollider == null)
        {
            Debug.LogError("�K�v�ȃR���|�[�l���g������������Ă��܂���B");
            return;
        }
        isHiding = false;
        playerCamera.isHiding = false;
        rigid.isKinematic = false;
        capsuleCollider.isTrigger = false;
        capsuleCollider.enabled = true;
        mainText.text = "";
        StartCoroutine(MoveCamera(playerTransform.position, playerTransform.rotation, 1.0f));
    }
}
