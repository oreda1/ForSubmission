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

    // --- �R���|�[�l���g�E�Q�� ---
    [SerializeField] private Animator anim;
    public GameObject TargetObject;
    public NavMeshAgent m_navMeshAgent;

    [Header("�ړ��ݒ�")]
    public float normalSpeed = 0.1f;
    public float chaseSpeed = 0.9f;

    [Header("����ݒ�")]
    public float viewAngle = 60f;
    public float viewDistance = 15f;

    [Header("�s���G���A�ݒ�")]
    public Transform movementAreaCenter;
    public float movementAreaRadius = 50f;

    [Header("�����ݒ�")]
    [SerializeField] private AudioSource footstepAudio;
    public float footstepMaxDistance = 15f;
    public float footstepMaxVolume = 1f;

    // --- ��ԊǗ� ---
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
       
        // ���t���[���u�B��Ă��邩�v���X�V
        bool isHiding = playerCollision != null && playerCollision.isHiding;

        // ������ or �B��Ă���Ȃ�ǐՒ�~�E�A�j���[�V�������Z�b�g���ĉ������Ȃ�
        if (isFrozen || isHiding)
        {
            if (playerDetected)
              
            return;
        }

        // ����`�F�b�N
        DetectPlayer();

        // �ǐՒ��Ȃ�ړI�n���v���C���[��
        if (playerDetected)
        {
            ChangeEnemyState(EnemyState.Chase);
            m_navMeshAgent.SetDestination(TargetObject.transform.position);
            SmoothLookAt(TargetObject.transform.position);
        }

       
        // �����X�V�͂��̂܂�
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
                // �h�A�͖������ĒǐՂ��n�߂�
                if (hit.collider.CompareTag("Door") || !hit.collider.CompareTag("Obstacle"))
                {
                    if (!playerDetected)
                      
                    return;
                }
            }
            playerDetected = true;

        }

        // �����O�Ȃ�ǐՉ���
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
    /// NavMeshAgent �̑��x����u�ړ������ۂ��v�𔻒肵�A�A�j���[�V������؂�ւ���
    /// </summary>
    /// <param name="speed">�G�[�W�F���g�̌��ݑ��x�imagnitude�j</param>


    /// <summary>
    /// �G�f�B�^��Ŏ����s���G���A����������Gizmos�`��
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // ����͈͂̕\��
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // �s���G���A�̕\��
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
                ChangeEnemyState(EnemyState.Search); // 1.5�b���Search��Ԃɖ߂�
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
