using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    public float MoveSpeed = 5f;
    private Vector3 _movementVelocity;
    private PlayerInput _playerInput;
    private float _verticalVelocity;
    public float Gravity = -9.8f;
    private Animator _animator;

    public int Coin;

    //Enemy
    public bool IsPlayer = true;
    private UnityEngine.AI.NavMeshAgent _navMeshAgent;
    private Transform TargetPlayer;

    //Health
    private Health _health;
    //Damage caster
    private DamageCaster _damageCaster;
    //Player Slides
    private float attackStartTime;
    public float AttackSlideDuration = 0.4f;
    public float AttackSlideSpeed = 0.06f;

    private Vector3 impactOnCharacter;

    public bool IsInvincible;
    public float invincibleDuration = 2f;

    private float attackAnimationDuration = 2f;
    public float SlideSpeed = 9f;

    //stamina
    public Image StaminaBar;
    public float MaxStamina = 100f;
    public float CurrentStamina;
    public float StaminaRegenRate = 10f; // Tốc độ hồi stamina mỗi giây
    //public float StaminaUsageRate = 20f; // Stamina tiêu hao cho mỗi hành động
    //public float MinStaminaForAction = 10f; // Mức stamina tối thiểu để thực hiện hành động
    private bool IsRegeneratingStamina = true;
    public float AttackStaminaCost = 30f; // Chi phí stamina cho Attacking
    public float SlideStaminaCost = 20f; // Chi phí stamina cho Slide

    //State Machine
    public enum CharacterState
    {
        Normal, Attacking, Dead, BeingHit, Slide, Spawn
    }
    public CharacterState CurrentState;

    public float SpawnDuration = 2f;
    private float currentSpawnTime;

    //MAterial animation
    private MaterialPropertyBlock _materialPropertyBlock;
    private SkinnedMeshRenderer _skinnedMeshRenderer;

    public GameObject ItemToDrop;
    private void Awake()
    {
        //test stamana
        CurrentStamina = MaxStamina;
        //old
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _health = GetComponent<Health>();
        _damageCaster = GetComponentInChildren<DamageCaster>();

        _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _materialPropertyBlock = new MaterialPropertyBlock();
        _skinnedMeshRenderer.GetPropertyBlock(_materialPropertyBlock);

        if (!IsPlayer)
        {
            _navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            TargetPlayer = GameObject.FindWithTag("Player").transform;
            _navMeshAgent.speed = MoveSpeed;
            SwitchStateTo(CharacterState.Spawn);
        }
        else
        {
            _playerInput = GetComponent<PlayerInput>();
        }
    }

    private void CalculatePlayerMovement()
    {
        if(_playerInput.MouseButtonDown && _cc.isGrounded)
        {
            SwitchStateTo(CharacterState.Attacking);
            return;
        }
        else if (_playerInput.SpaceKeyDown && _cc.isGrounded)
        {
            SwitchStateTo(CharacterState.Slide);
            return;
        }

        _movementVelocity.Set(_playerInput.HorizontalInput, 0f, _playerInput.VerticalInput);
        _movementVelocity.Normalize();
        _movementVelocity = Quaternion.Euler(0, -45f, 0) * _movementVelocity;

        _animator.SetFloat("Speed", _movementVelocity.magnitude);

        _movementVelocity *= MoveSpeed * Time.deltaTime;

        if (_movementVelocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_movementVelocity);
            _animator.SetBool("AirBorn", !_cc.isGrounded);
        
    }
    private void CalculateEnemyMovement()
    {
        if (Vector3.Distance(TargetPlayer.position, transform.position) >= _navMeshAgent.stoppingDistance)
        {
            _navMeshAgent.SetDestination(TargetPlayer.position);
            _animator.SetFloat("Speed", 0.2f);
        }
        else
        {
            _navMeshAgent.SetDestination(transform.position);
            _animator.SetFloat("Speed", 0f);

            SwitchStateTo(CharacterState.Attacking);
        }
    }

private void FixedUpdate()
{
    switch(CurrentState)
    {
        case CharacterState.Normal:
            if (IsPlayer)
                CalculatePlayerMovement();
            else
                CalculateEnemyMovement();
            break;
        case CharacterState.Attacking:
            if (IsPlayer)
            {
                if (Time.time < attackStartTime + AttackSlideDuration)
                {
                    float timePassed = Time.time - attackStartTime;
                    float LerpTime = timePassed / AttackSlideDuration;
                    _movementVelocity = Vector3.Lerp(transform.forward * AttackSlideSpeed, Vector3.zero, LerpTime);
                }

                if (_playerInput.MouseButtonDown && _cc.isGrounded)
                    {
                        string currentClipName = _animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                        attackAnimationDuration = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                        if (currentClipName != "LittleAdventurerAndie_ATTACK_03" && attackAnimationDuration > 0.5f && attackAnimationDuration < 0.7f)
                        {
                            _playerInput.MouseButtonDown = false;
                            SwitchStateTo(CharacterState.Attacking);
                            //CalculatePlayerMovement();
                        }
                    }
            }
            break;
        case CharacterState.Dead:
            return;

        case CharacterState.BeingHit:
                
                // Để người chơi có thể di chuyển nhẹ
                if (IsPlayer)
            {
                Vector3 playerInputMovement = new Vector3(_playerInput.HorizontalInput, 0f, _playerInput.VerticalInput);
                playerInputMovement.Normalize();
                playerInputMovement = Quaternion.Euler(0, -45f, 0) * playerInputMovement;
                playerInputMovement *= MoveSpeed * Time.deltaTime;
                _movementVelocity += playerInputMovement;

                // Cập nhật hoạt ảnh di chuyển
                _animator.SetFloat("Speed", _movementVelocity.magnitude);
                if (_movementVelocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(_movementVelocity);
                }
            }
            // Kiểm tra thời gian để cho phép tấn công lại
            if (Time.time > attackStartTime + AttackSlideDuration)
            {
                SwitchStateTo(CharacterState.Normal); // Quay về trạng thái bình thường
            }
            break;
        case CharacterState.Slide:
             _movementVelocity = transform.forward * SlideSpeed * Time.deltaTime;
            break;

        case CharacterState.Spawn:
            currentSpawnTime -= Time.deltaTime;
            if (currentSpawnTime <= 0)
                {
                    SwitchStateTo(CharacterState.Normal);
                }
            break;
    }

        if (impactOnCharacter.magnitude > 0.2f)
        {
            _movementVelocity = impactOnCharacter * Time.deltaTime;
        }
        impactOnCharacter = Vector3.Lerp(impactOnCharacter, Vector3.zero, Time.deltaTime * 5);

        // Xử lý chuyển động
        if (IsPlayer)
    {
        if (!_cc.isGrounded)
            _verticalVelocity = Gravity;
        else
            _verticalVelocity = Gravity * 0.3f;

        _movementVelocity += _verticalVelocity * Vector3.up * Time.deltaTime;

        _cc.Move(_movementVelocity);
        _movementVelocity = Vector3.zero;
    }else
        {
            if (CurrentState != CharacterState.Normal)
            {
                _cc.Move(_movementVelocity);
                _movementVelocity = Vector3.zero;
            }
        }
}



    public void SwitchStateTo(CharacterState newState)
    {
        if (IsPlayer) 
        {
            _playerInput.ClearCache();
        }
        //Exiting state
        switch (CurrentState)
        {
            case CharacterState.Normal:
                break;
            case CharacterState.Attacking:
                if (_damageCaster != null)
                    _damageCaster.DisableDamageCaster();

                if (IsPlayer)
                    GetComponent<PlayerVFXManager>().StopBlade();
                break;
            case CharacterState.Dead:
                return;
            case CharacterState.BeingHit:
                break;
            case CharacterState.Slide:
                break;
            case CharacterState.Spawn:
                IsInvincible = false;
                break;
        }
        //Entering state
        switch (newState)
        {
            case CharacterState.Normal:
                break;
            case CharacterState.Attacking:

                /*if (!IsPlayer) 
                {
                    Quaternion newRotation = Quaternion.LookRotation(TargetPlayer.position - transform.position);
                    transform.rotation = newRotation;
                }
                _animator.SetTrigger("Attack");

                if (IsPlayer)
                {
                    attackStartTime = Time.time;
                    RotateToCursor();
                }
                break;*/
                // Kiểm tra stamina trước khi thực hiện tấn công
                if (CurrentStamina >= AttackStaminaCost)
                {
                    CurrentStamina -= AttackStaminaCost; // Trừ stamina
                    Debug.Log("Stamina reduced for Attacking: " + AttackStaminaCost);

                    if (!IsPlayer)
                    {
                        Quaternion newRotation = Quaternion.LookRotation(TargetPlayer.position - transform.position);
                        transform.rotation = newRotation;
                    }
                    _animator.SetTrigger("Attack");

                    if (IsPlayer)
                    {
                        attackStartTime = Time.time;
                        RotateToCursor();
                    }
                }
                else
                {
                    Debug.Log("Not enough stamina to attack!");
                    // Có thể không chuyển sang trạng thái Attacking nếu stamina không đủ
                    return;
                }
                break;

            case CharacterState.Dead:
                    _cc.enabled = false;
                _animator.SetTrigger("Dead");
                StartCoroutine(MaterialDissolve());

                if (!IsPlayer)
                {
                    SkinnedMeshRenderer mesh = GetComponentInChildren<SkinnedMeshRenderer>();
                    mesh.gameObject.layer = 0;
                }
                break;

            case CharacterState.BeingHit:
                _animator.SetTrigger("BeingHit");

                if (IsPlayer)
                {
                    IsInvincible = true;
                    StartCoroutine(DelayCancleInvincible());
                }
                break ;
            case CharacterState.Slide:
                /*_animator.SetTrigger("Slide");
                break;*/
                // Kiểm tra stamina trước khi thực hiện Slide
                if (CurrentStamina >= SlideStaminaCost)
                {
                    CurrentStamina -= SlideStaminaCost; // Trừ stamina
                    Debug.Log("Stamina reduced for Slide: " + SlideStaminaCost);

                    _animator.SetTrigger("Slide");
                }
                else
                {
                    Debug.Log("Not enough stamina to slide!");
                    // Có thể không chuyển sang trạng thái Slide nếu stamina không đủ
                    return;
                }
                break;

            case CharacterState.Spawn:
                IsInvincible = true;
                currentSpawnTime = SpawnDuration;
                StartCoroutine(MaterialAppear());
                break;
        }

        CurrentState = newState;

        Debug.Log("Switched to " +  CurrentState);
    }

    public void SlideAnimationEnds()
    {
        SwitchStateTo(CharacterState.Normal);
    }

    public void  AttackAnimationEnds()
    {
        SwitchStateTo(CharacterState.Normal);
    }

    public void BeingHitAnimationEnds()
    {
        SwitchStateTo(CharacterState.Normal);
    }

    public void ApplyDamage(int damage, Vector3 attackerPos = new Vector3())
    {
        if (IsInvincible)
        {
            return;
        }

        if (_health != null)
        {
            _health.ApplyDamage(damage);
        }

        if(!IsPlayer)
        {
            GetComponent<EnemyVFXManager>().PlayBeingHitVFX(attackerPos);
        }

        StartCoroutine(MaterialBlink());

        if(IsPlayer)
        {
            SwitchStateTo(CharacterState.BeingHit);
            AddImpact(attackerPos, 10f);
        }else
            {
            AddImpact(attackerPos, 2.5f);
        }
    }

    IEnumerator DelayCancleInvincible()
    {
        yield return new WaitForSeconds(invincibleDuration);
        IsInvincible = false;
    }

    private void AddImpact(Vector3 attackerPos, float force)
    {
        Vector3 impactDir = transform.position - attackerPos;
        impactDir.Normalize();
        impactDir.y = 0;
        impactOnCharacter = impactDir * force;
    }

    public void EnableDamageCaster()
    {
        _damageCaster.EnableDamageCaster();
    }

    public void DisableDamageCaster()
    {
        _damageCaster.DisableDamageCaster();
    }

    IEnumerator MaterialBlink()
    {
        _materialPropertyBlock.SetFloat("_blink", 0.4f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);

        yield return new WaitForSeconds(0.2f);

        _materialPropertyBlock.SetFloat("_blink", 0f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    IEnumerator MaterialDissolve()
    {
        yield return new WaitForSeconds(2);

        float dissolveTimeDuration = 2f;
        float currentDissolveTime = 0;
        float dissolveHeight_start = 20f;
        float dissolveHeight_target = -10f;
        float dissolveHeight;

        _materialPropertyBlock.SetFloat("_enableDissolve", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);

        while (currentDissolveTime < dissolveTimeDuration)
        {
            currentDissolveTime += Time.deltaTime;
            dissolveHeight = Mathf.Lerp(dissolveHeight_start, dissolveHeight_target, currentDissolveTime / dissolveTimeDuration);
            _materialPropertyBlock.SetFloat("_dissolve_height", dissolveHeight);
            _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);
            yield return null;
        }

        DropItem();
    }

    public void DropItem()
    {
        if (ItemToDrop != null)
        {
            Instantiate(ItemToDrop, transform.position, Quaternion.identity);
        }    
    }

    public void PickUpItem(PickUp item)
    {
        switch(item.type)
        {
            case PickUp.PickUpType.Heal:
                AddHealth(item.value);
                break;
            case PickUp.PickUpType.Coin:
                AddCoin(item.value);
                break;
        }
    }

    private void AddHealth(int health)
    {
        _health.AddHealth(health);
        GetComponent<PlayerVFXManager>().PlayHealVFX();
    }

    private void AddCoin(int  coin)
    {
        Coin += coin;
    }

    public void RatateToTarget()
    {
        if (CurrentState != CharacterState.Dead)
        {
            transform.LookAt(TargetPlayer, Vector3.up);
        }
    }

    IEnumerator MaterialAppear()
    {
        float dissolveTimeDuration = SpawnDuration;
        float currentDissolveTime = 0;
        float dissolveHeight_start = -10f;
        float dissolveHeight_target = 20f;
        float dissolveHeight;

        _materialPropertyBlock.SetFloat("_enableDissolve", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);

        while (currentDissolveTime < dissolveTimeDuration)
        {
            currentDissolveTime += Time.deltaTime;
            dissolveHeight = Mathf.Lerp(dissolveHeight_start, dissolveHeight_target, currentDissolveTime / dissolveTimeDuration);
            _materialPropertyBlock.SetFloat("_dissolve_height", dissolveHeight);
            _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);
            yield return null;
        }

        _materialPropertyBlock.SetFloat("_enableDissolve", 0f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private void OnDrawGizmos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitResult;

        if (Physics.Raycast(ray, out hitResult, 1000, 1 << LayerMask.NameToLayer("CursorTest")))
        {
            Vector3 cursorPos = hitResult.point;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cursorPos, 1);
        }
    }

    private void RotateToCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitResult;

        if (Physics.Raycast(ray, out hitResult, 1000, 1 << LayerMask.NameToLayer("CursorTest")))
        {
            Vector3 cursorPos = hitResult.point;
            transform.rotation = Quaternion.LookRotation(cursorPos - transform.position, Vector3.up);
        }
    }
    //hoiphup
    private void Update()
    {
        if (IsRegeneratingStamina && CurrentStamina < MaxStamina)
        {
            CurrentStamina += StaminaRegenRate * Time.deltaTime;
            CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina); // Đảm bảo không vượt quá giới hạn
        }

        // Khi đang thực hiện hành động, ngừng hồi stamina
        if (CurrentState == CharacterState.Attacking || CurrentState == CharacterState.Slide)
        {
            IsRegeneratingStamina = false;
        }
        else
        {
            IsRegeneratingStamina = true;
        }
    }

}
