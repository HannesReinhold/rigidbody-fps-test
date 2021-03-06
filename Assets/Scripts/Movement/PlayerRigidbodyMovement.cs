using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRigidbodyMovement : MonoBehaviour
{
    [Header("Camera Control")]
    public float MouseSensitivity = 100f;

    private float RotationX = 0f;
    private float RotationY = 0f;

    [Header("Movement Speed")]
    public float WalkSpeed = 12f;
    public float SprintSpeed = 18f;
    public float CrouchingSpeed = 5f;
    public float AirMultiplier = 0.5f;

    private float CurrentMaxSpeed = 0f;
    private float CurrentSpeed = 0f;

    [Header("Height")]
    public float StandingHeight = 1f;
    public float CrouchingHeight = 0.5f;

    private Vector3 OriginalScale;

    [Header("Jumping")]
    public float JumpHeight = 3f;
    public int MaxNumJumps = 1;
    public bool AutoJump = false;
    private Vector3 Normal;
    private int CurrentNumJumps;

    [Header("Ground Check")]
    public LayerMask GroundMask;
    public float MaxSlopeAngle = 45f;

    [Header("References")]
    public Rigidbody rigidbody;
    public Transform PlayerCamera;
    public Transform PlayerBody;
    public Transform Orientation;


    private bool IsMoving;
    private bool IsGrounded;
    private bool IsSprinting;
    private bool IsCrouching;
    private bool CanJump;
    private bool IsJumping;

    private float InputX = 0f;
    private float InputZ = 0f;



    private void Start()
    {
        CurrentMaxSpeed = WalkSpeed;
        OriginalScale = transform.localScale;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        UpdateControls();
        UpdateMaxSpeed();
        Look();

    }


    void FixedUpdate()
    {
        UpdateMovement();
    }


    void UpdateControls()
    {
        InputX = Input.GetAxisRaw("Horizontal");
        InputZ = Input.GetAxisRaw("Vertical");

        IsSprinting = Input.GetKey(KeyCode.LeftShift) && InputZ == 1 && !IsCrouching;
        IsCrouching = Input.GetKey(KeyCode.LeftControl);
        IsMoving = InputX != 0 || InputZ != 0;

        if (Input.GetKeyDown(KeyCode.Space)) CanJump = true;
        if (Input.GetKeyUp(KeyCode.Space)) CanJump = false;
    }


    void UpdateMaxSpeed()
    {
        if (IsSprinting)
        {
            IsCrouching = false;
            CurrentMaxSpeed = SprintSpeed;

        }
        else if (IsCrouching)
        {
            IsSprinting = false;
            CurrentMaxSpeed = CrouchingSpeed;

            SetPlayerHeightTo(CrouchingHeight);

        }
        else
        {
            CurrentMaxSpeed = WalkSpeed;

            SetPlayerHeightTo(StandingHeight);

        }
    }


    void UpdateMovement()
    {
        Vector3 move = Orientation.right * InputX + Orientation.forward * InputZ;
        move.Normalize();

        if (IsGrounded)
        {
            CurrentNumJumps = 0;
            if (IsMoving)
            {
                rigidbody.AddForce(move * 500 * Time.deltaTime * CurrentMaxSpeed);
                rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, CurrentMaxSpeed);
            }
            else
            {
                rigidbody.AddForce(-rigidbody.velocity * 500 * Time.deltaTime);
            }
        }
        else
        {
            float speed = 500 * CurrentMaxSpeed * AirMultiplier * Time.deltaTime;

            Vector2 v = new Vector2(rigidbody.velocity.x * move.x, rigidbody.velocity.z * move.z);
            if (Mathf.Sqrt(v.x*v.x + v.y*v.y) >= CurrentMaxSpeed) speed = 0;
            Debug.Log(Mathf.Sqrt(rigidbody.velocity.x * rigidbody.velocity.x + rigidbody.velocity.z * rigidbody.velocity.z));
            rigidbody.AddForce(move * speed);
        }

        if (CanJump && !IsJumping)
        {
            if (IsGrounded) Jump();
            else if (CurrentNumJumps < MaxNumJumps - 1) Jump();
        }

    }


    void Jump()
    {
            Vector3 velocity = rigidbody.velocity;
            velocity.y = 0;
            rigidbody.velocity = velocity;

            IsGrounded = false;
            IsJumping = true;
            if (!AutoJump) CanJump = false;

            rigidbody.AddForce(Normal * JumpHeight * 1.5f, ForceMode.Impulse);
            rigidbody.AddForce(Vector3.up * JumpHeight * 0.5f, ForceMode.Impulse);

            CurrentNumJumps++;

            Invoke("ResetJump", 0.4f);
    }

    void ResetJump()
    {
        IsJumping = false;
    }


    void SetPlayerHeightTo(float height)
    {
        Vector3 scale = OriginalScale;
        scale.y = height;
        transform.localScale = scale;
    }


    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.fixedDeltaTime;

        RotationX -= mouseY;
        RotationX = Mathf.Clamp(RotationX, -90f, 90f);

        RotationY += mouseX;

        PlayerCamera.rotation = Quaternion.Euler(RotationX, RotationY, 0f);

        Orientation.localRotation = Quaternion.Euler(0, RotationY, 0f);
    }


    void OnCollisionStay(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if (GroundMask != (GroundMask | (1 << layer))) return;

        for(int i=0; i<collision.contactCount; i++)
        {
            Vector3 normal = collision.contacts[i].normal;

            if (CheckIfWalkable(normal))
            {
                IsGrounded = true;
                Normal = normal;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        int layer = collision.gameObject.layer;
        if (GroundMask == (GroundMask | (1 << layer))) IsGrounded = false ;
        Normal = Vector3.up;
    }

    bool CheckIfWalkable(Vector3 n)
    {
        float angle = Vector3.Angle(Vector3.up, n);
        return angle <= MaxSlopeAngle;
    }

}
