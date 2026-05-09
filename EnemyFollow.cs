using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace EnemyAI
{
    public class EnemyFollow : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public Slider healthBarSlider;
        private NavMeshAgent agent;
        private Animator animator;

        [Header("Movement Settings")]
        public float walkRange = 5f;
        public float runRange = 10f;
        public float attackRange = 2f;

        [Header("Speed Settings")]
        public float walkSpeed = 3f;
        public float runSpeed = 8f;
        
        [Header("Animation Speed")]
        public float idleAnimSpeed = 1.2f;
        public float walkAnimSpeed = 1.5f;
        public float runAnimSpeed = 1.8f;
        public float attackAnimSpeed = 2.0f;
        public float deathAnimSpeed = 1.0f;

        [Header("Attack Settings")]
        public int attackDamage = 15;
        public float attackCooldown = 0.8f;
        public float attackDamageDelay = 0.3f;
        public bool randomizeAttacks = true;
        private float lastAttackTime = -999f;
        private bool hasDealtDamage = false;
        private int lastAttackIndex = -1;

        [Header("Health Settings")]
        public int maxHealth = 100;
        private int currentHealth;
        private bool hasAttacked = false;
        private string currentState = "";
        private bool isDead = false;
        
        [Header("Death Settings")]
        public bool useDeathAnimation = true;
        public string deathAnimationName = "death4";
        public float deathAnimationDuration = 3f;
        public bool keepCorpse = true;
        public float corpseCleanupDistance = 30f;
        public float corpseLifetime = 60f;
        
        [Header("Detection Settings")]
        public float detectionRange = 15f;
        public float fieldOfView = 90f;
        public bool requireLineOfSight = true;
        public LayerMask obstacleMask;
        
        [Header("Patrol Settings")]
        public bool enablePatrol = true;
        public Transform[] patrolPoints;
        public float patrolWaitTime = 2f;
        public float patrolSpeed = 2f;
        
        [Header("Audio")]
        public AudioClip[] footstepSounds;
        public float footstepInterval = 0.5f;
        public float footstepVolume = 0.3f; // Adjust footstep volume
        public float footstepMinDistance = 2f; // Start fading at this distance
        public float footstepMaxDistance = 10f; // Silent beyond this distance
        public AudioClip detectionScream;
        public AudioClip[] idleSounds;
        public float idleSoundInterval = 5f;
        public float idleSoundVolume = 0.4f; // Adjust idle sound volume
        
        private bool hasDetectedPlayer = false;
        private bool isChasing = false;
        private int currentPatrolIndex = 0;
        private float patrolWaitTimer = 0f;
        private bool isWaitingAtPoint = false;
        private float footstepTimer = 0f;
        private float idleSoundTimer = 0f;
        private bool hasPlayedDetectionSound = false;
        private AudioSource audioSource;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth;

            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.angularSpeed = 720f;
                agent.acceleration = 8f;
            }

            if (healthBarSlider != null)
            {
                healthBarSlider.maxValue = maxHealth;
                healthBarSlider.value = currentHealth;
            }
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = footstepMinDistance;
            audioSource.maxDistance = footstepMaxDistance;
            audioSource.volume = 0.6f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;
        }

        private void Update()
        {
            if (isDead) return;
            
            if (player == null || agent == null || animator == null) return;
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsDead())
            {
                agent.isStopped = true;
                ChangeAnimationState("def1");
                animator.speed = idleAnimSpeed;
                return;
            }

            float distance = Vector3.Distance(player.position, transform.position);
            
            if (!hasDetectedPlayer)
            {
                if (CanSeePlayer(distance))
                {
                    hasDetectedPlayer = true;
                    isChasing = true;
                    
                    // IMPROVED: Always play detection scream using AudioSource.PlayClipAtPoint
                    if (detectionScream != null)
                    {
                        AudioSource.PlayClipAtPoint(detectionScream, transform.position, 1.5f);
                        hasPlayedDetectionSound = true;
                        Debug.Log("DETECTION SCREAM PLAYED!");
                    }
                }
                else
                {
                    if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
                    {
                        Patrol();
                        PlayFootsteps(agent.velocity.magnitude);
                        PlayIdleSounds();
                    }
                    else
                    {
                        agent.isStopped = true;
                        ChangeAnimationState("def1");
                        animator.speed = idleAnimSpeed;
                        PlayIdleSounds();
                    }
                    return;
                }
            }
            
            if (hasDetectedPlayer)
            {
                if (distance <= attackRange)
                {
                    agent.isStopped = true;
                    
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        hasAttacked = false;
                        hasDealtDamage = false;
                        
                        TriggerRandomAttack();
                        
                        hasAttacked = true;
                        lastAttackTime = Time.time;
                        
                        StartCoroutine(DealDamageDuringAttack());
                    }
                    
                    ChangeAnimationState("def1");
                    animator.speed = idleAnimSpeed;
                }
                else
                {
                    agent.isStopped = false;
                    hasAttacked = false;

                    agent.speed = (distance > runRange) ? runSpeed :
                                  (distance > walkRange) ? walkSpeed : 0f;

                    agent.SetDestination(player.position);
                    UpdateMovementAnimation(agent.velocity.magnitude);
                    PlayFootsteps(agent.velocity.magnitude);
                }

                RotateTowardsPlayerSmooth(10f);
            }
        }

        private void UpdateMovementAnimation(float speed)
        {
            if (hasAttacked) return;

            if (speed == 0)
            {
                ChangeAnimationState("def1");
                animator.speed = idleAnimSpeed;
            }
            else if (speed <= 1f)
            {
                ChangeAnimationState("walk1");
                animator.speed = walkAnimSpeed;
            }
            else if (speed <= 2f)
            {
                ChangeAnimationState("walk2");
                animator.speed = walkAnimSpeed;
            }
            else if (speed <= 3.5f)
            {
                ChangeAnimationState("walk3");
                animator.speed = walkAnimSpeed;
            }
            else if (speed <= 5f)
            {
                ChangeAnimationState("walk4");
                animator.speed = walkAnimSpeed;
            }
            else
            {
                ChangeAnimationState("run1");
                animator.speed = runAnimSpeed;
            }
        }

        private void ChangeAnimationState(string newState)
        {
            if (currentState == newState) return;
            animator.Play(newState);
            currentState = newState;
        }

        private void RotateTowardsPlayerSmooth(float speed)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);

            if (healthBarSlider != null)
                healthBarSlider.value = currentHealth;

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            
            if (agent != null)
            {
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            this.enabled = false;
            
            EnemyDamageHandler damageHandler = GetComponent<EnemyDamageHandler>();
            if (damageHandler != null)
            {
                damageHandler.OnDeath();
            }
            
            if (useDeathAnimation && animator != null)
            {
                animator.speed = 1.0f;
                
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(param.name, false);
                    else if (param.type == AnimatorControllerParameterType.Float)
                        animator.SetFloat(param.name, 0f);
                    else if (param.type == AnimatorControllerParameterType.Int)
                        animator.SetInteger(param.name, 0);
                }
                
                animator.Play(deathAnimationName, 0, 0f);
                
                if (keepCorpse)
                {
                    StartCoroutine(BecomeCorpse());
                }
                else
                {
                    Destroy(gameObject, deathAnimationDuration);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        System.Collections.IEnumerator BecomeCorpse()
        {
            yield return new WaitForSeconds(deathAnimationDuration);
            
            if (animator != null)
            {
                animator.enabled = false;
            }
            
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            gameObject.tag = "Corpse";
            
            float timer = 0f;
            while (timer < corpseLifetime)
            {
                if (player != null)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist > corpseCleanupDistance)
                    {
                        Destroy(gameObject);
                        yield break;
                    }
                }
                timer += 1f;
                yield return new WaitForSeconds(1f);
            }
            
            Destroy(gameObject);
        }
        
        void DestroyEnemy()
        {
            Destroy(gameObject);
        }

        private void Patrol()
        {
            if (patrolPoints.Length == 0) return;
            
            if (isWaitingAtPoint)
            {
                patrolWaitTimer += Time.deltaTime;
                agent.isStopped = true;
                ChangeAnimationState("def1");
                animator.speed = idleAnimSpeed;
                
                if (patrolWaitTimer >= patrolWaitTime)
                {
                    isWaitingAtPoint = false;
                    patrolWaitTimer = 0f;
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                }
                return;
            }
            
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            
            if (targetPoint == null)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                return;
            }
            
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.SetDestination(targetPoint.position);
            
            UpdateMovementAnimation(agent.velocity.magnitude);
            
            float distanceToPoint = Vector3.Distance(transform.position, targetPoint.position);
            if (distanceToPoint < 1f)
            {
                isWaitingAtPoint = true;
            }
        }

        private bool CanSeePlayer(float distance)
        {
            if (distance > detectionRange)
                return false;
            
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            
            if (angle > fieldOfView / 2f)
                return false;
            
            if (requireLineOfSight)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange, obstacleMask))
                {
                    if (hit.transform != player)
                        return false;
                }
            }
            
            return true;
        }
        
        private void PlayFootsteps(float speed)
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            if (speed < 0.1f) return;
            
            footstepTimer += Time.deltaTime;
            
            float interval = speed > 4f ? footstepInterval * 0.7f : footstepInterval;
            
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                
                if (audioSource != null)
                {
                    AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(footstep, footstepVolume); // Use custom volume
                }
            }
        }
        
        private void PlayIdleSounds()
        {
            if (idleSounds == null || idleSounds.Length == 0) return;
            
            idleSoundTimer += Time.deltaTime;
            
            if (idleSoundTimer >= idleSoundInterval)
            {
                idleSoundTimer = 0f;
                
                if (audioSource != null && Random.value < 0.3f)
                {
                    AudioClip idleClip = idleSounds[Random.Range(0, idleSounds.Length)];
                    audioSource.pitch = Random.Range(0.85f, 1.0f);
                    audioSource.PlayOneShot(idleClip, idleSoundVolume); // Use custom volume
                }
            }
        }

        private void TriggerRandomAttack()
        {
            if (animator == null) return;
            
            animator.speed = attackAnimSpeed;
            
            if (randomizeAttacks)
            {
                string[] attackAnimations = { "attack1LSpike", "attack1RSpike", "attack2", "attack2RLSpike", "attack3" };
                
                int randomIndex;
                do
                {
                    randomIndex = Random.Range(0, attackAnimations.Length);
                } while (randomIndex == lastAttackIndex && attackAnimations.Length > 1);
                
                lastAttackIndex = randomIndex;
                string attackName = attackAnimations[randomIndex];
                
                if (HasAnimationParameter(attackName))
                {
                    animator.SetTrigger(attackName);
                }
                else
                {
                    animator.Play(attackName);
                }
            }
            else
            {
                animator.SetTrigger("attackTrigger");
            }
        }

        private System.Collections.IEnumerator DealDamageDuringAttack()
        {
            yield return new WaitForSeconds(attackDamageDelay);
            
            if (hasDealtDamage || isDead || player == null) yield break;
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsDead()) yield break;
            
            float distance = Vector3.Distance(player.position, transform.position);
            
            if (distance <= attackRange)
            {
                playerHealth.TakeDamage(attackDamage);
                hasDealtDamage = true;
            }
        }

        public void AnimationEvent_DealDamage()
        {
            if (hasDealtDamage || isDead || player == null) return;
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsDead()) return;
            
            float distance = Vector3.Distance(player.position, transform.position);
            
            if (distance <= attackRange)
            {
                playerHealth.TakeDamage(attackDamage);
                hasDealtDamage = true;
            }
        }

        private bool HasAnimationParameter(string paramName)
        {
            if (animator == null) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        public int GetCurrentHealth()
        {
            return currentHealth;
        }
        
        public bool IsDead()
        {
            return isDead;
        }
    }
}