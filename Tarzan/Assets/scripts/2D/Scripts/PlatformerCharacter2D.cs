using System;
using UnityEngine;

namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {  
        public float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        public float m_JumpForce = 400f;                  // Amount of force added when the player jumps.
        [Range(0, 1)] public float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
        public bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
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

        private Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.
        private float gravity;
        private HealthController m_HealthController;  

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up

        private bool m_Grounded;            // Whether or not the player is grounded.
        private bool m_Swiming;            // Whether or not the player is swiming.
        private bool m_Lava;            // Whether or not the player is in lava.

        private float m_TimeSwiming = 0; 
        private float m_DrownTicks = 0;
        private float m_TimeFalling = 0; 
        private float m_FallSpeed = 0;

        private void Awake()
        {
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_HealthController = GetComponent<HealthController>();

            gravity = m_Rigidbody2D.gravityScale;
        }


        private void FixedUpdate()
            {
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

                if (m_Lava)
                    m_HealthController.TakeBurnDamage(0.5f);

                HandleDrowning();
                HandleFalling();

                m_Rigidbody2D.gravityScale = m_Swiming ? gravity*0.2f : gravity;
                m_Rigidbody2D.drag = m_Swiming ? 0.5f : 0;

                m_Anim.SetBool("Ground", m_Grounded);

                // Set the vertical animation
                m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);
        }


        public void Move(float move, bool crouch, bool jump)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (m_Grounded || m_AirControl || m_Swiming)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move*m_CrouchSpeed : move);

                move = (m_Swiming ? move*0.5f : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));

                // Move on ICE
                /*
                if (Mathf.Abs(m_Rigidbody2D.velocity.x) < m_MaxSpeed)
                {
                    m_Rigidbody2D.AddForce(new Vector2(move*m_MaxSpeed, 0));
                }*/

                // Move on ground
                m_Rigidbody2D.velocity = new Vector2(move*m_MaxSpeed, m_Rigidbody2D.velocity.y);

                m_Rigidbody2D.gravityScale = m_Swiming ? gravity*0.05f : gravity;
                m_Rigidbody2D.drag = m_Swiming ? 2 : 0;

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                    // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
            }
            // If the player should jump...
            if ((m_Grounded && m_Anim.GetBool("Ground") || m_Swiming) && jump)
            {
                // Add a vertical force to the player.
                float jumpForce = m_Swiming ? m_JumpForce*0.6f : m_JumpForce;

                m_Grounded = false;
                m_Anim.SetBool("Ground", false);
                m_Rigidbody2D.AddForce(new Vector2(0f, jumpForce));
            }
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
                m_HealthController.TakeDamage(m_FallDamage*(m_FallSpeed/m_DangerousFallSpeed));
            }

            if (!m_Grounded && !m_Swiming && m_Rigidbody2D.velocity.y < 0)
            {
                m_FallSpeed = m_Rigidbody2D.velocity.y;
                m_TimeFalling += Time.fixedDeltaTime;
            }
            else
            {
                m_FallSpeed = 0;
                m_TimeFalling = 0;
            }
        }

        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }

        public float GetFaceDirection()
        {
            return m_FacingRight ? 1 : -1;
        }
    }
}
