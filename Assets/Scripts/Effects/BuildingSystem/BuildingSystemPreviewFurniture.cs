using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
public class BuildingSystemPreviewFurniture : MonoBehaviour
{
    [SerializeField] private float SURFACE_COLLISION_TOLERANCE = 0.18f; // make this const later

    private static Dictionary<int, BuildingSystemPreviewFurniture> m_furniturePreviewID_m_furniturePreviewScript = new Dictionary<int, BuildingSystemPreviewFurniture>();

    [Header("Configurations")]
    [SerializeField] private int m_furnitureItemID = -1; // UID for every furniture type. same as item id
    [SerializeField] private Vector2 m_sweepTestSize = Vector3.one;
    [SerializeField] private Vector3 m_offset = Vector3.zero;
    [SerializeField] protected int m_entityIDToSpawn = -1;
    [SerializeField] private float m_placeDistance = 3f;
    [SerializeField] private int m_placeSoundIndex = 21;
    [Header("Debug")]
    [SerializeField] private bool DEBUG_showSweepSize = false;
    [SerializeField] private bool DEBUG_modeCollision = false;
    [SerializeField] private bool DEBUG_modeSweepTest = false;

    private Rigidbody m_ridigbody;
    private Collider m_collider;

    public int furnitureItemID { get { return m_furnitureItemID; } }
    public Vector3 offset { get { return m_offset; } }
    public float placeDistance { get { return m_placeDistance; } }

    protected void Awake()
    {
        m_ridigbody = GetComponent<Rigidbody>();
        m_collider = GetComponent<Collider>();
        m_furniturePreviewID_m_furniturePreviewScript.Add(m_furnitureItemID, this);
    }

    protected void Start()
    {
        gameObject.SetActive(false);
    }

    protected void Update()
    {
        if (DEBUG_showSweepSize)
        {
            Debug.DrawRay(transform.position, Vector3.up, Color.red);
            Debug.DrawLine(transform.position + transform.right * m_sweepTestSize.x, transform.position - transform.right * m_sweepTestSize.x, Color.red);
            Debug.DrawRay(transform.position + transform.right * m_sweepTestSize.x, transform.up, Color.red);
            Debug.DrawRay(transform.position - transform.right * m_sweepTestSize.x, transform.up, Color.red);
            Debug.DrawLine(transform.position + transform.up * m_sweepTestSize.y, transform.position - transform.up * m_sweepTestSize.y, Color.blue);
            Debug.DrawRay(transform.position + transform.up * m_sweepTestSize.y, transform.right, Color.red);
            Debug.DrawRay(transform.position - transform.up * m_sweepTestSize.y, transform.right, Color.red);
        }
    }

    /// <summary>
    /// returns true if is colliding with another gameobject
    /// </summary>
    /// <returns></returns>
    public bool checkInCollision()
    {
        if(!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        m_ridigbody.rotation = transform.rotation;

        RaycastHit[][] hits = new RaycastHit[6][];

        hits[0] = sweepTestAll(transform.position + transform.forward * m_sweepTestSize.x, -transform.forward, m_sweepTestSize.x);
        hits[1] = sweepTestAll(transform.position - transform.forward * m_sweepTestSize.x, transform.forward, m_sweepTestSize.x);
        hits[2] = sweepTestAll(transform.position + transform.right * m_sweepTestSize.x, -transform.right, m_sweepTestSize.x);
        hits[3] = sweepTestAll(transform.position - transform.right * m_sweepTestSize.x, transform.right, m_sweepTestSize.x);
        hits[4] = sweepTestAll(transform.position + transform.up * m_sweepTestSize.y, -transform.up, m_sweepTestSize.y);
        hits[5] = sweepTestAll(transform.position - transform.up * m_sweepTestSize.y, transform.up, m_sweepTestSize.y);

        for (int i = 0; i < hits.Length; i++)
        {
            for (int j = 0; j < hits[i].Length; j++)
            {
                if (i < 4) // for wall checks
                {
                    // collisions directly on surface will not get identified as within collider.

                    if (isInsideCollider(hits[i][j].point, SURFACE_COLLISION_TOLERANCE))
                    {
                        if (DEBUG_modeCollision)
                        {
                            Debug.DrawRay(hits[i][j].point, Vector3.up, Color.yellow);
                        }

                        return true;
                    }
                }
                else
                {
                    if (isInsideCollider(hits[i][j].point))
                    {
                        if (DEBUG_modeCollision)
                        {
                            Debug.DrawRay(hits[i][j].point, Vector3.up, Color.yellow);
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }


    /// <summary>
    /// returns true if there is a connected object below
    /// </summary>
    /// <param name="connectedObjectHit"></param>
    /// <returns></returns>
    public bool getConnectedObjectBelow(out RaycastHit connectedObjectHit)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        RaycastHit[] belowHits = sweepTestAll(transform.position, -transform.up, m_sweepTestSize.y);

        float bestDistance = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < belowHits.Length; i++)
        {
            float distanceToSurface = getDistanceToColliderSurface(belowHits[i].point);

            if (distanceToSurface < bestDistance)
            {
                bestDistance = distanceToSurface;
                bestIndex = i;
            }
        }

        if (bestDistance < SURFACE_COLLISION_TOLERANCE)
        {
            connectedObjectHit = belowHits[bestIndex];
            return true;
        }
        else
        {
            connectedObjectHit = new RaycastHit();
            return false;
        }
    }

    public virtual void server_spawnAssociatedEntity(Player_base builder)
    {
        EntityManager.singleton.spawnEntity(m_entityIDToSpawn, transform.position, transform.rotation);
        NetworkingManager.singleton.server_sendWorldSoundToAllInRange(m_placeSoundIndex, transform.position);
    }

    private RaycastHit[] sweepTestAll(Vector3 position, Vector3 direction, float distance)
    {
        Vector3 startPosition = transform.position;
        m_ridigbody.position = position;
        transform.position = position;

        RaycastHit[] returnValue;

        returnValue = m_ridigbody.SweepTestAll(direction, distance);

        m_ridigbody.position = startPosition;
        transform.position = startPosition;

        if (DEBUG_modeSweepTest)
        {
            for (int i = 0; i < returnValue.Length; i++)
            {
                Debug.DrawRay(returnValue[i].point, Vector3.up, Color.blue);
            }
        }
        return returnValue;
    }

    private bool isInsideCollider(Vector3 point, float tolerance = 0f)
    {
        RaycastHit hitInfo;

        m_ridigbody.position = transform.position;

        //Vector3 center = collider.bounds.center;
        Vector3 center = transform.position;

        // Cast a ray from point to center
        Vector3 direction = center - point;
        Ray ray = new Ray(point, direction);

        if (!m_collider.Raycast(ray, out hitInfo, direction.magnitude))
        {
            return true;
        }
        else
        {
            if (Vector3.Distance(point, hitInfo.point) < tolerance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private float getDistanceToColliderSurface(Vector3 point)
    {
        RaycastHit hitInfo;

        m_ridigbody.position = transform.position;

        //Vector3 center = collider.bounds.center;
        Vector3 center = transform.position;

        // Cast a ray from point to center
        Vector3 direction = center - point;
        Ray ray = new Ray(point, direction);

        if (!m_collider.Raycast(ray, out hitInfo, direction.magnitude))
        {
            return 0;
        }
        else
        {
            return Vector3.Distance(point, hitInfo.point);
        }
    }

    public static BuildingSystemPreviewFurniture getScriptForID(int furniturePreviewID)
    {
        if (m_furniturePreviewID_m_furniturePreviewScript.ContainsKey(furniturePreviewID))
        {
            return m_furniturePreviewID_m_furniturePreviewScript[furniturePreviewID];
        }
        else
        {
            return null;
        }
    }
}
