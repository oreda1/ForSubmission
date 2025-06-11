using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player_Collision : MonoBehaviour
{
    //シーン遷移用
    private SceneManagement sceneManagemet;
    private FadeManager fadeManager;


    // インスペクターからセットする変数
    [SerializeField] private Indicate_Item indicate_Item;
    [SerializeField] private Text mainText;
    [SerializeField] private List<EnemyAction> enemyActions = new List<EnemyAction>();
    [SerializeField] private float maxDistance = 5.0f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject displayButton;
    [SerializeField] private GameObject controller;

    // 内部変数
    private StoryManager storyManager;
    private Rigidbody rigid;
    private CapsuleCollider capsuleCollider;
    private Player_Camera playerCamera;
    private RawImageAppeal rawImageAppeal;

    // 状態管理用の変数
    public string itemName, doorName;
    private Transform doorTransform;
    private int clearCount;
    private bool clearFlag;
    public bool isHit;
    private float itemCount;

    // 隠れる関連（隠れているかどうかの状態は isHiding で管理）
    private Transform hidingCameraPosition;
    private Quaternion hidingCameraRotation;
    public bool isHiding = false;

    // ボタン表示用のイベント（必要に応じて利用）
    public static event System.Action OnButtonIndicate;

    // ドア回転用
    private float currentYRotation;
    
    
    private void Start()
    {
        sceneManagemet=SceneManagement.Instance;
        fadeManager = FadeManager.Instance;


        itemCount = 8; // アイテムの初期数
        // コンポーネントのキャッシュ
        rigid = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerCamera = GetComponent<Player_Camera>();
        playerTransform = transform;
        rawImageAppeal = displayButton.GetComponent<RawImageAppeal>();

        // 初期化
        clearCount = 0;
        storyManager = new StoryManager();
        clearFlag = false;
        isHit = false;

        // イベント登録（アイテム数の更新）
        if (indicate_Item != null)
        {
            indicate_Item.OnLeftoverCountChanged += HandleLeftoverCountChanged;
        }
    }

    private void HandleLeftoverCountChanged(int newCount)
    {
        Debug.Log("アイテム残数が更新されました: " + newCount);
        itemCount = newCount;
    }

    private void Update()
    {
        // 毎フレーム、視界内の隠れる場所をチェックし、最も近いものの名前を取得
        string closestRefugeName = DisplayRefugeInView();

        // 毎フレーム、視界内のアイテムチェック：最も近いアイテムの名前を取得して itemName に更新する
        itemName = DisplayItemsInView();

        // 入力処理：ドア操作と隠れる操作の管理
        HandleDoorInput();
        HandleHidingInput(closestRefugeName);
    }

    /// <summary>
    /// ドア操作の入力処理
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
    /// 隠れるアクションの入力処理
    /// </summary>
    /// <param name="closestRefugeName">視界内で最も近い隠れ場所の名前</param>
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
    /// 視界内にある「Item」タグのオブジェクトの中から最も近いものの名前を返す
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
    /// 視界内にある「Refuge」タグのオブジェクトの中から最も近いものの名前を返し、隠れるための情報を設定する
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
            // 隠れる場所が近くにある場合、ボタンを表示して演出を開始
            displayButton.GetComponent<RawImage>().enabled = true;
            displayButton.SetActive(true);
            rawImageAppeal.StartAppeal();

            // 隠れるカメラの位置と回転を更新
            hidingCameraPosition = candidateHidingPos;
            hidingCameraRotation = candidateHidingRot;

            // Outline の状態調整（隠れている場合は非表示）
            Outline outline = closestRefuge.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = !isHiding;
            }
        }
        else
        {
            // 隠れる場所が無い場合、ボタンを非表示にする
            displayButton.GetComponent<RawImage>().enabled = false;
            displayButton.SetActive(false);
        }

        return (closestRefuge != null) ? closestRefuge.name : null;
    }

    /// <summary>
    /// ドアを開閉するコルーチン
    /// </summary>
    public IEnumerator DoorMove()
    {
        // ドアを開ける処理
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
        // ドアを閉める処理
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
    /// オブジェクトとの衝突時の処理
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
            mainText.text = "あれ、、開かない....何かアイテムが足りない";

            if (clearCount == 10 || itemCount == 0)
            {
                mainText.text = "やった、、力技でこじ開けたぞ!!";
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
    /// カメラの位置と回転を指定時間で補間させるコルーチン
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
        mainText.text = "隠れている...";
    }

    private void StopHiding()
    {
        if (playerCamera == null || playerTransform == null || capsuleCollider == null)
        {
            Debug.LogError("必要なコンポーネントが初期化されていません。");
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
