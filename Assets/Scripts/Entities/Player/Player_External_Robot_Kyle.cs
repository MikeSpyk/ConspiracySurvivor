using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_External_Robot_Kyle : Player_external
{
    [SerializeField] private int m_verticalBufferLength = 3;
    [SerializeField] private float m_animationVelocityThreshold = 0.001f;

    private float vertical;
    private float deltaAngleY;
    private int layerMask;
    private RaycastHit hit;
    private Vector3 p1;
    private CapsuleCollider char_colider;
    private bool jump;
    private bool receivedmove;
    private bool receivedrotation;
    float[] m_verticalBuffer;

    private void Awake()
    {
        base.Awake();

        m_verticalBuffer = new float[m_verticalBufferLength];

        m_rigidbody = GetComponent<Rigidbody>();
        char_colider = GetComponent<CapsuleCollider>();
        m_Animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    protected void Start()
    {
        base.Start();

        layerMask = ~(1 << 20);
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();

        CharAnimation();
    }

    protected void LateUpdate()
    {
        base.LateUpdate();

        receivedmove = false;
        receivedrotation = false;
    }

    public override void onReceivedPlayerPosition(Vector3 newPosition)
    {
        receivedmove = true;
        Vector3 deltaVec = newPosition - transform.position;
        Vector2 deltaVecXZ = new Vector2(deltaVec.x, deltaVec.z);
        Vector2 forwardXZ = new Vector2(transform.forward.x, transform.forward.z);
        float angleDeltaForward = Vector2.SignedAngle(forwardXZ, deltaVecXZ);

        float horizontal = Mathf.Sin(Mathf.Deg2Rad * angleDeltaForward) * deltaVecXZ.magnitude; // right/left
        m_rigidbody.velocity = deltaVec / Mathf.Max(0, 01f, (Time.time - m_lastTimeReceivedPosition));
        //Debug.Log("deltaVec.magnitude: " + deltaVec.magnitude +", m_rigidbody.velocity.magnitude: " + m_rigidbody.velocity.magnitude);

        m_lastTimeReceivedPosition = Time.time;

        //Debug.DrawRay(transform.position, Vector3.up, Color.green);
        //Debug.DrawRay(transform.position + m_rigidbody.velocity, Vector3.up, Color.red);

        //Debug.Log(horizontal);
        //Debug.DrawRay(transform.position, transform.right * horizontal, Color.red);
        //Debug.DrawRay(transform.position, transform.forward * vertical, Color.green);

        transform.position = newPosition; // set new position
    }

    public override void onReceivedPlayerRotation(float angleY, float angleZ)
    {
        receivedrotation = true;
        deltaAngleY = angleY - transform.rotation.eulerAngles.y; // right/left
        float deltaAngleZ = angleZ - transform.rotation.eulerAngles.z; // up/down
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, angleY, angleZ); // set new rotation
    }

    private void CharAnimation()
    {
        p1 = transform.position + char_colider.center;

        Vector2 forwardVelocity = VectorTools.projectFoward(new Vector2(transform.forward.x, transform.forward.z), new Vector2(m_rigidbody.velocity.x, m_rigidbody.velocity.z)); // @dennis: hiermit kannst du animationen vorwärts/Rückwärts und rechts/links bestimmen 

        for (int i = m_verticalBuffer.Length - 1; i > 0; i--)
        {
            m_verticalBuffer[i] = m_verticalBuffer[i - 1];
        }
        m_verticalBuffer[0] = forwardVelocity.y;

        vertical = 0;
        for (int i = 0; i < m_verticalBuffer.Length; i++)
        {
            vertical += m_verticalBuffer[i];
        }
        vertical /= m_verticalBuffer.Length;

        if (Mathf.Abs(vertical) < m_animationVelocityThreshold)
        {
            vertical = 0;
        }

        jump = Physics.Raycast(p1, Vector3.down, out hit, 1.2F, layerMask);

        if (deltaAngleY > 0 && vertical == 0 && jump)
        {
            m_Animator.SetBool("TurnRight", true);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("Run", false);
        }

        if (deltaAngleY < 0 && vertical == 0 && jump)
        {
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("TurnLeft", true);
            m_Animator.SetBool("Run", false);
        }

        if (deltaAngleY == 0 && vertical < 0 && jump)
        {
            m_Animator.SetBool("RunBack", true);
        }
        if (deltaAngleY == 0 && vertical > 0 && jump)
        {
            m_Animator.SetBool("Run", true);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("Jump", false);
        }

        if (deltaAngleY == 0 && vertical == 0 && jump)
        {
            m_Animator.SetBool("Run", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("Jump", false);
            m_Animator.SetBool("RunBack", false);
        }

        if (deltaAngleY > 0 && vertical > 0 && jump)
        {
            m_Animator.SetBool("RunTurnRight", true);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("Run", true);
        }

        if (deltaAngleY < 0 && vertical > 0 && jump)
        {
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", true);
            m_Animator.SetBool("Run", true);
        }

        if (!jump)
        {
            m_Animator.SetBool("Jump", true);
            m_Animator.SetBool("Run", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", false);
        }

        if (receivedmove)
        {
            vertical = 0;
        }

        if (receivedrotation)
        {
            deltaAngleY = 0;
        }
    }
}
