using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class CharakterControl : Player_local {

    //######################################################
    private Transform m_Cam;
    private Rigidbody m_Rigidbody;
    private float horizontal;
    private float vertical;
    private float cam_v;
    private float keyhorizontal;
    private float Gravatiy;
    private bool jump;
    private bool checkjump;
    private Vector3 cameraRotationX;
    private Vector3 cmaeraRotaionsave;
    private Vector3 p1;
    private RaycastHit hit;
    private CapsuleCollider char_colider;
    private BoxCollider char_boxcolider;
    private bool raycharmitte;
    private bool raycharlinks;
    private bool raycharrechts;
    private bool raycharvorne;
    private bool raycharhinten;
    private bool rayhitclimobjekt;
    private bool rayhitclimobjekthigh;
    private int i;
    private int raychartrue;
    private int layerMask;
    public List<bool> raycharbool = new List<bool>();
    [SerializeField] float Rotaspeed;
    [SerializeField] float Speed;
    [SerializeField] float Jumpheight;
    [SerializeField] float MoreGravatiy;
    [SerializeField] float FallGravatiy;
    [SerializeField] bool NoClipMode;
    [SerializeField] float cameraHeightOffset = 1.5f;

    protected void Awake() // für basis-Klasse Player_local
    {
        base.Awake();
    }

    //######################################################
    void Start ()
    {
        Player_local_start();
        layerMask = ~(1 << 20);
        NoClipMode = false;
        m_Cam = CameraStack.gameobject.transform;
        m_Rigidbody = GetComponent<Rigidbody>();
        char_colider = GetComponent<CapsuleCollider>();
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        cameraRotationX = Vector3.forward;
        char_boxcolider = GetComponent<BoxCollider>();
        Physics.IgnoreLayerCollision(8, 8);

    }
    //######################################################
    void Update ()
    {
        base.Update();
        raycharbool.Clear();
        raychartrue = 0;
        horizontal = Input.GetAxis("Mouse X");
        vertical = Input.GetAxis("Vertical");
        keyhorizontal = Input.GetAxis("Horizontal");
        cam_v = Input.GetAxis("Mouse Y");
        jump = CrossPlatformInputManager.GetButtonDown("Jump");
        p1 = transform.position + char_colider.center;
        //Debug.DrawRay(p1, Vector3.down *1.35F, Color.blue);
        //Debug.DrawRay(p1 + transform.right * 0.5f, Vector3.down * 1.05F, Color.red);
        //Debug.DrawRay(p1 - transform.right * 0.5f, Vector3.down * 1.05F, Color.red);
        //Debug.DrawRay(p1 + transform.forward * 0.5f, Vector3.down * 1.05F, Color.red);
        //Debug.DrawRay(p1 - transform.forward * 0.5f, Vector3.down * 1.05F, Color.red);
        //Debug.DrawRay(transform.position + new Vector3(0, 0.05f, 0), transform.TransformDirection(Vector3.forward), Color.green);
        //Debug.DrawRay(transform.position + new Vector3(0, 0.5F, 0), transform.TransformDirection(Vector3.forward), Color.green);
        Campos();
        Charmove();

        onPlayerInputMove(vertical != 0 || keyhorizontal != 0);
    }
    //######################################################
    protected void FixedUpdate()
    {
        Player_local_FixedUpdate();
        base.FixedUpdate();
    }
    //######################################################
    private void Campos()
    {
        m_Cam.position = transform.position + new Vector3(0, cameraHeightOffset, 0);

        if (!GUIManager.singleton.coursorActive)
        {
            cameraRotationX = Quaternion.Euler(cam_v * Rotaspeed * -1, 0.0f, 0.0f) * cameraRotationX;
        }

        if (cameraRotationX.z < 0.4)
        {
            cameraRotationX = cmaeraRotaionsave;
            m_Cam.rotation = transform.rotation * Quaternion.LookRotation(cmaeraRotaionsave);
        }
        else
        {
            m_Cam.rotation = transform.rotation * Quaternion.LookRotation(cameraRotationX);
            cmaeraRotaionsave = cameraRotationX;
        }
    }
    //######################################################
    private void Charmove()
    {
        if (horizontal > 0 && vertical == 0 && !jump)
        {
            m_Animator.SetBool("TurnRight", true);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("Run", false);
        }

        if (horizontal < 0 && vertical == 0 && !jump)
        {
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("TurnLeft", true);
            m_Animator.SetBool("Run", false);
        }

        if (horizontal == 0 && vertical < 0 && !jump)
        {
            m_Animator.SetBool("RunBack", true);
        }
        if (horizontal == 0 && vertical > 0 && !jump)
        {
            m_Animator.SetBool("Run", true);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("Jump", false);
        }

        if(horizontal == 0 && vertical == 0 && !jump)
        {
            m_Animator.SetBool("Run", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("Jump", false);
            m_Animator.SetBool("RunBack", false);
        }

        if (horizontal > 0 && vertical > 0 && !jump)
        {
            m_Animator.SetBool("RunTurnRight", true);
            m_Animator.SetBool("RunTurnLeft", false);
            m_Animator.SetBool("Run", true);
        }

        if (horizontal < 0 && vertical > 0 && !jump)
        {
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", true);
            m_Animator.SetBool("Run", true);
        }
        
        if (jump)
        {
            m_Animator.SetBool("Jump", true);
            m_Animator.SetBool("Run", false);
            m_Animator.SetBool("TurnLeft", false);
            m_Animator.SetBool("TurnRight", false);
            m_Animator.SetBool("RunTurnRight", false);
            m_Animator.SetBool("RunTurnLeft", false);
        }

        raycharmitte = Physics.Raycast(p1, Vector3.down, out hit, 1.35F, layerMask);
        raycharlinks = Physics.Raycast(p1 - transform.right * 0.5f, Vector3.down, out hit, 1.05F, layerMask);
        raycharrechts = Physics.Raycast(p1 + transform.right * 0.5f, Vector3.down, out hit, 1.05F, layerMask);
        raycharvorne = Physics.Raycast(p1 + transform.forward * 0.5f, Vector3.down, out hit, 1.05F, layerMask);
        raycharhinten = Physics.Raycast(p1 - transform.forward * 0.5f, Vector3.down, out hit, 1.05F, layerMask);
        rayhitclimobjekt = Physics.Raycast(transform.position + new Vector3(0, 0.05f, 0), transform.TransformDirection(Vector3.forward), out hit, 1, layerMask);
        rayhitclimobjekthigh = Physics.Raycast(transform.position + new Vector3(0, 0.5F, 0), transform.TransformDirection(Vector3.forward), out hit, 1, layerMask);

        raycharbool.Add(raycharmitte);
        raycharbool.Add(raycharlinks);
        raycharbool.Add(raycharrechts);
        raycharbool.Add(raycharvorne);
        raycharbool.Add(raycharhinten);

        for(i=0; i < raycharbool.Count; i++)
            {
              if (raycharbool[i])
              {
                raychartrue++;
              }
        }

        if (!NoClipMode)
        { 

            if (raychartrue > 1)
            {
                m_Animator.SetBool("Fall", false);
                m_Rigidbody.velocity = transform.rotation * new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed);


            if (jump)
            {
                m_Rigidbody.AddForce(new Vector3(0, Jumpheight, 0), ForceMode.Impulse);
            }
            }   
        }
        else
        {
            m_Rigidbody.useGravity = false;

            if (Input.GetKey(KeyCode.Space)) m_Rigidbody.velocity = transform.rotation * (new Vector3(0, Jumpheight, 0));
            else m_Rigidbody.velocity = new Vector3(0,0,0);

            if (Input.GetKey(KeyCode.LeftControl)) m_Rigidbody.velocity = transform.rotation * (new Vector3(0, -Jumpheight, 0));
            else m_Rigidbody.velocity = new Vector3(0, 0, 0);
        }

        if (!raycharmitte && !raycharlinks && raycharrechts && !raycharhinten && !raycharvorne && !rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed) + new Vector3(-1F, 0, 0));
        }

        if(!raycharmitte && raycharlinks && !raycharrechts && !raycharhinten && !raycharvorne && !rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed) + new Vector3(1F, 0, 0));
        }

        if (!raycharmitte && !raycharlinks && !raycharrechts && raycharhinten && !raycharvorne && !rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed) + new Vector3(0, 0, 1));
        }

        if (!raycharmitte && !raycharlinks && !raycharrechts && !raycharhinten && raycharvorne && !rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed) + new Vector3(0, 0, -1));
        }

        if (!raycharmitte && !raycharlinks && !raycharrechts && !raycharhinten && raycharvorne && rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(0, m_Rigidbody.velocity.y, -1));
        }

        if (!raycharmitte && !raycharlinks && !raycharrechts && raycharhinten && !raycharvorne && rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(0, m_Rigidbody.velocity.y, 1));
        }

        if (!raycharmitte && !raycharlinks && raycharrechts && !raycharhinten && !raycharvorne && rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(-1, m_Rigidbody.velocity.y, 0));
        }

        if (!raycharmitte && raycharlinks && !raycharrechts && !raycharhinten && !raycharvorne && rayhitclimobjekt)
        {
            m_Animator.SetBool("Fall", true);
            m_Rigidbody.velocity = transform.rotation * (new Vector3(1, m_Rigidbody.velocity.y, 0));
        }

        if (rayhitclimobjekt && !rayhitclimobjekthigh && vertical > 0)
        {
            m_Rigidbody.velocity = transform.rotation * (new Vector3(keyhorizontal * Speed, m_Rigidbody.velocity.y, vertical * Speed) + new Vector3(0, 0.05F, 0));
        }

        if (!raycharmitte && !raycharlinks && !raycharrechts && !raycharhinten && !raycharvorne)
        {
         m_Animator.SetBool("Fall", true);
        }

        if (!GUIManager.singleton.coursorActive)
        {
            transform.Rotate(0, horizontal * Rotaspeed, 0);
        }    
    }
    //######################################################
    private void Moregravity()
    {
        if(Gravatiy != 0)
        {
            Vector3 extraGravityForce = (Physics.gravity * Gravatiy);
            m_Rigidbody.AddForce(extraGravityForce, ForceMode.Impulse);
        }
    }
    //######################################################

    public float getCameraHeightOffset()
    {
        return cameraHeightOffset;
    }
}
