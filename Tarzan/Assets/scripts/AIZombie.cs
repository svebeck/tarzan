using UnityEngine;
using System.Collections;

public class AIZombie : MonoBehaviour {

    public GameObject target;
    public float visionRange = 180f;
    public float hearingRange = 200f;
    public float wanderRange = 900f;
    public float closeRange = 10f;
    public float damage = 1f;
    public float attackReloadTime = 1f;
    public float attackMoveSpeed = 2f;
    public float idleMoveSpeed = 2f;
    public float jumpForce = 200f;
    public int randomDirectionTime = 5;
    public float m_DrownTime = 10f;              // Whether or not a player can steer while jumping;
    public float m_DrownTimeIntervall = 2f;
    public float m_FallDamage = 1f;
    public float m_FallTimeLimit = 2f;
    public float m_DangerousFallSpeed = -10f;
    public LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character
    public LayerMask m_WhatIsWater;                  // A mask determining what is ground to the character
    public LayerMask m_WhatIsLava;                  // A mask determining what is ground to the character

    public GameObject splashLavaEffect;
    public GameObject splashWaterEffect;

    DigController digController;
    Rigidbody2D rigidbody2D;
    Vector2 velocity;
    private HealthController m_HealthController;  

    private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private Transform m_CeilingCheck;   // A position marking where to check for ceilings
    const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up

    float randomDirectionTimer = 0;

    float oldDiffX = 0;
    Vector3 diff;
    private float gravity;

    private bool m_Grounded;            // Whether or not the player is grounded.
    private bool m_Swiming;            // Whether or not the player is swiming.
    private bool m_Lava;            // Whether or not the player is in lava.

    private float m_TimeSwiming = 0; 
    private float m_DrownTicks = 0;
    private float m_TimeFalling = 0; 
    private float m_FallSpeed = 0;

    private float attackTime;

    GameObject meshGameObject;

    void Start()
    {
        digController = DigController.instance;
        m_GroundCheck = transform.Find("GroundCheck");
        m_CeilingCheck = transform.Find("CeilingCheck");
        rigidbody2D = GetComponent<Rigidbody2D> ();
        m_HealthController = GetComponent<HealthController>();

        gravity = rigidbody2D.gravityScale;

        meshGameObject = GetComponentInChildren<MeshRenderer>().gameObject;
    }

    void Update () 
    {
        float diffSQR = diff.sqrMagnitude;

        if ( diffSQR > wanderRange)
        {
            this.gameObject.SetActive(false);
            return;
        }

        if (m_Grounded && target)
        {

            if (diffSQR < visionRange)
            {
                velocity = new Vector2(diff.x, 0).normalized * attackMoveSpeed;

                if (diff.x*diff.x < closeRange*closeRange)
                {
                    //target is above
                    if (diff.y > closeRange)
                    {
                        digController.Dig(transform.position, Vector3.up, this.gameObject);
                        rigidbody2D.AddForce(new Vector2(0f, jumpForce));
                        return;
                    }

                    //target is below
                    if (diff.y < -closeRange)
                    {
                        digController.Dig(transform.position, Vector3.down, this.gameObject);
                        return;
                    }
                }
                else if (diff.y*diff.y < closeRange*closeRange)
                {
                    //target is to right
                    if (diff.x > closeRange)
                    {
                        digController.Dig(transform.position, Vector3.right, this.gameObject);
                    }

                    //target is to left
                    if (diff.x < -closeRange)
                    {
                        digController.Dig(transform.position, Vector3.left, this.gameObject);
                    }
                }
            }
            else if (diffSQR < hearingRange)
            {
                velocity = new Vector2(0f, 0f);
            }
            else if (diffSQR < wanderRange)
            {
                if (randomDirectionTimer >= randomDirectionTime)
                {
                    randomDirectionTimer = 0;

                    float direction = idleMoveSpeed - idleMoveSpeed*Random.value*2;
                    float sign = Mathf.Sign(direction);
                    velocity = new Vector2(direction, 0) * idleMoveSpeed;
                }
                randomDirectionTimer += Time.deltaTime;
            }
            else
            {
                velocity = new Vector2(0f, 0f);
            }

            oldDiffX = diff.x;

            rigidbody2D.velocity = velocity;
        }
    }

    void FixedUpdate() 
    {
        if (target != null)
            diff = target.transform.position - transform.position;
        
        if (rigidbody2D.isKinematic)
            return;
        

        m_Grounded = false;
        m_Swiming = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.

        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
                m_Grounded = true;
        }

        m_Lava = false;

        colliders = Physics2D.OverlapCircleAll(m_CeilingCheck.position, 0.2f, m_WhatIsWater);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Swiming = true;

                if (colliders[i].gameObject.layer == LayerMask.NameToLayer("Lava"))
                    m_Lava = true;
            }
        }

        TryDoDamage();

        if (m_Lava)
            m_HealthController.TakeBurnDamage(0.5f);

        HandleDrowning();
        HandleFalling();

        rigidbody2D.gravityScale = m_Swiming ? gravity*0.2f : gravity;
        rigidbody2D.drag = m_Swiming ? 0.5f : 0;
    }

    void TryDoDamage()
    {
        if (target == null)
            return;

        if (attackTime > attackReloadTime)
        {
            bool isZombieEating = false;

            if (diff.sqrMagnitude < 3)
                isZombieEating = true;

            if (isZombieEating)
                target.GetComponent<HealthController>().TakeDamage(damage);

            attackTime = 0;
        }
        attackTime += Time.fixedDeltaTime;
    }

    void HandleDrowning()
    {
        if (m_Swiming)
        {

            if (m_TimeSwiming == 0) //we just entered the water
            {
                if (m_Lava)
                    Instantiate(splashLavaEffect, this.transform.position, Quaternion.identity);
                else
                    Instantiate(splashWaterEffect, this.transform.position, Quaternion.identity);
            }
            m_TimeSwiming += Time.fixedDeltaTime;
        }
        else
        {
            m_TimeSwiming = 0;
            m_DrownTicks = 0;
            return;
        }

        if (m_TimeSwiming > m_DrownTime)
        {
            if (m_TimeSwiming > m_DrownTime + m_DrownTimeIntervall*m_DrownTicks)
            {
                m_HealthController.TakeDrownDamage(0.5f);
                m_DrownTicks++;
            }
        }
    }

    void HandleFalling()
    {
        if (m_Grounded && m_TimeFalling > m_FallTimeLimit && m_FallSpeed < m_DangerousFallSpeed)
        {
            m_HealthController.TakeDamage(m_FallDamage*(m_TimeFalling/m_FallTimeLimit));
        }

        if (!m_Grounded && !m_Swiming && rigidbody2D.velocity.y < 0)
        {
            m_FallSpeed = rigidbody2D.velocity.y;
            m_TimeFalling += Time.fixedDeltaTime;
        }
        else
        {
            m_FallSpeed = 0;
            m_TimeFalling = 0;
        }
    }
}
