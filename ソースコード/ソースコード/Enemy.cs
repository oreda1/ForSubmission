using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private AudioClip screamAudio;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bgmAudio;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private Text text; // UI Text for displaying messages
    private GameObject player;
    private float maxVolumeDistance = 5.0f;
    private float minVolumeDistance = 20.0f;
    private float enemyStopTime = 30f;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private Coroutine stopCoroutine;

  
    void Start()
    {
       
        player = GameObject.FindWithTag("Player");

       

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = screamAudio;
        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
        bgmAudioSource.clip = bgmAudio;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        AdjustVolume(audioSource);
        AdjustVolume(bgmAudioSource);
        DetectPlayer();
    }

    void AdjustVolume(AudioSource source)
    {
        if (player != null && source != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            float volume = Mathf.Clamp01(1.0f - (distance - maxVolumeDistance) / (minVolumeDistance - maxVolumeDistance));
            source.volume = volume;
        }
    }

    //プレイヤー発見BGM
    void DetectPlayer()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < maxVolumeDistance)
            {
                PlayBGM();
                text.text = "敵に見つかっている！";
            }
            else
            {
                StopBGM();
            }
        }
    }

    public void PlayScream()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void PlayBGM()
    {
        if (!bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }

    public void MoveEnemyToNewPosition()
    {
        // NavMesh内でランダムな位置に移動
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
    }

    public void StopEnemy(float duration)
    {
        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }
        stopCoroutine = StartCoroutine(StopEnemyCoroutine(duration));
    }

    private IEnumerator StopEnemyCoroutine(float duration)
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
        }
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(duration);

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }
    }
    private void OnParticleCollision(GameObject other)
    {
        // Debug log to verify collision detection
        Debug.Log($"Particle collided with: {other.name}");
        
        if (other.CompareTag("Bullet"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            
            enemy.StopEnemy(enemyStopTime);

        }
    }

}
