using UnityEngine;
using Unity.Netcode;

public class AnimalBehavior : NetworkBehaviour
{
    [Header("إعدادات الحيوان")]
    public string animalName; // بقرة، خروف، دجاجة
    public AudioClip animalSound;
    public float wanderRadius = 5f;
    public float moveSpeed = 2f;
    public float stopDuration = 3f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isMoving = false;
    private float stopTimer;
    private Animator animator;

    private void Start()
    {
        startPos = transform.position;
        animator = GetComponent<Animator>();
        PickNewTarget();
    }

    private void Update()
    {
        if (!IsServer) return; // الخادم فقط يتحكم بحركة الحيوانات

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            transform.LookAt(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                isMoving = false;
                stopTimer = stopDuration;
                UpdateAnimation(false);
            }
        }
        else
        {
            stopTimer -= Time.deltaTime;
            if (stopTimer <= 0)
            {
                PickNewTarget();
                UpdateAnimation(true);
            }
        }
    }

    private void PickNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPos = startPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        isMoving = true;
    }

    private void UpdateAnimation(bool moving)
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", moving);
        }
    }

    // عند الضغط على الحيوان
    public void OnMouseDown()
    {
        // تشغيل الصوت محلياً للكل
        PlaySoundServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundServerRpc()
    {
        PlaySoundClientRpc();
    }

    [ClientRpc]
    private void PlaySoundClientRpc()
    {
        if (animalSound != null)
        {
            AudioSource.PlayClipAtPoint(animalSound, transform.position);
            
            // نطق اسم الحيوان أيضاً لتعليم الطفل
            Debug.Log($"🔊 صوت {animalName}"); 
        }
    }
}
