using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAction : MonoBehaviour
{
   private Enemy enemy;
    private enum EnemyState
    {
        Search,
        Chase,
        IsMoving
    }
    EnemyState enemyState = EnemyState.Search;

    // --- コンポーネント・参照 ---
    [SerializeField] private Animator anim;
    public GameObject TargetObject;
    public NavMeshAgent m_navMeshAgent;

    [Header("移動設定")]
    public float normalSpeed = 0.1f;
    public float chaseSpeed = 0.9f;

    [Header("視野設定")]
    public float viewAngle = 60f;
    public float viewDistance = 15f;

    [Header("行動エリア設定")]
    public Transform movementAreaCenter;
    public float movementAreaRadius = 50f;

    [Header("足音設定")]
    [SerializeField] private AudioSource footstepAudio;
    public float footstepMaxDistance = 15f;
    public float footstepMaxVolume = 1f;

    // --- 状態管理 ---
    private bool playerDetected = false;
    private bool isFrozen = false;
    private Player_Collision playerCollision;

    void Start()
    {
        
        StartEnemy_Init();
        enemy =new Enemy();
        anim = GetComponent<Animator>();
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_navMeshAgent.speed = normalSpeed;
        m_navMeshAgent.updateRotation = false;
        m_navMeshAgent.angularSpeed = 120f;
     
       
        
        

        if (TargetObject == null)
            TargetObject = GameObject.FindWithTag("Player");

        playerCollision = TargetObject.GetComponent<Player_Collision>();

        


        StartCoroutine(Patrol());
     
    }

    void Update()
    {
       
        // 毎フレーム「隠れているか」を更新
        bool isHiding = playerCollision != null && playerCollision.isHiding;

        // 凍結中 or 隠れているなら追跡停止・アニメーションリセットして何もしない
        if (isFrozen || isHiding)
        {
            if (playerDetected)
              
            return;
        }

        // 視野チェック
        DetectPlayer();

        // 追跡中なら目的地をプレイヤーに
        if (playerDetected)
        {
            ChangeEnemyState(EnemyState.Chase);
            m_navMeshAgent.SetDestination(TargetObject.transform.position);
            SmoothLookAt(TargetObject.transform.position);
        }

       
        // 足音更新はそのまま
        UpdateFootstepSound();
    }

    void DetectPlayer()
    {
        if (TargetObject == null) return;

        Vector3 dir = (TargetObject.transform.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, TargetObject.transform.position);
        float ang = Vector3.Angle(transform.forward, dir);

        if (dist < viewDistance && ang < viewAngle * 0.5f)
        {

           
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, dist))
            {
                // ドアは無視して追跡を始める
                if (hit.collider.CompareTag("Door") || !hit.collider.CompareTag("Obstacle"))
                {
                    if (!playerDetected)
                      
                    return;
                }
            }
            playerDetected = true;

        }

        // 条件外なら追跡解除
        if (playerDetected)
        {
            
            MoveToRandomPositionInArea();
        }
    }

 

    IEnumerator Patrol()
    {
        while (true)
        {
            if (!playerDetected)
                MoveToRandomPositionInArea();
          
            yield return new WaitForSeconds(Random.Range(5f, 10f));
        }
    }

    void MoveToRandomPositionInArea()
    {
        if (GetRandomPointInArea(out Vector3 pt))
            m_navMeshAgent.SetDestination(pt);
    }

    bool GetRandomPointInArea(out Vector3 point)
    {
        Vector3 randDir = Random.insideUnitSphere * movementAreaRadius + movementAreaCenter.position;
        if (NavMesh.SamplePosition(randDir, out NavMeshHit hit, movementAreaRadius, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    void SmoothLookAt(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        Quaternion rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
    }

    void UpdateFootstepSound()
    {
        if (footstepAudio == null || TargetObject == null) return;
        float dist = Vector3.Distance(transform.position, TargetObject.transform.position);
        float vol = Mathf.Clamp01(1f - (dist / footstepMaxDistance)) * footstepMaxVolume;
        footstepAudio.volume = Mathf.Lerp(footstepAudio.volume, vol, Time.deltaTime * 5f);
    }

    /// <summary>
    /// NavMeshAgent の速度から「移動中か否か」を判定し、アニメーションを切り替える
    /// </summary>
    /// <param name="speed">エージェントの現在速度（magnitude）</param>


    /// <summary>
    /// エディタ上で視野や行動エリアを可視化するGizmos描画
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 視野範囲の表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // 行動エリアの表示
        if (movementAreaCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(movementAreaCenter.position, movementAreaRadius);
        }
    }

    private void ChangeEnemyState(EnemyState state)
    {
        if (enemyState ==state) return;
        enemyState = state;
        anim.SetBool("IsMoving", false);
        anim.SetBool("Chase", false);
        anim.SetBool("Search", false);
        switch (state)
        {
            case EnemyState.Search:
                anim.SetBool("Search", true);
                m_navMeshAgent.speed = normalSpeed;   
                ChangeEnemyState(EnemyState.Search); // 1.5秒後にSearch状態に戻す
                MoveToRandomPositionInArea();
                break;
            case EnemyState.Chase:
                anim.SetBool("Chase", true);
                m_navMeshAgent.speed = chaseSpeed;
                break;
            case EnemyState.IsMoving:
                anim.SetBool("IsMoving", true);
               enemy.StopEnemy(1.5f);


                break;
            default:
                break;
        }
        


    }
    void StartEnemy_Init()
    {
        anim.SetBool("IsMoving", false);
        anim.SetBool("Chase", false);
        anim.SetBool("Search", true);
    }

}
