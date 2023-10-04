using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystemPreviewConstruction : MonoBehaviour
{
    private const int NUMBER_OF_CONSTRUCTION_PARTS = 10;
    private const float WALL_UP_CKECK_TOLERANCE = 0.03f;

    private static GameObject[] m_constructionPreviewGameObjects = new GameObject[NUMBER_OF_CONSTRUCTION_PARTS];
    private static BuildingSystemPreviewConstruction[] m_constructionPreviewScripts = new BuildingSystemPreviewConstruction[NUMBER_OF_CONSTRUCTION_PARTS];
    private static Renderer[] m_constructionPreviewRenderer = new Renderer[NUMBER_OF_CONSTRUCTION_PARTS];
    private static readonly int LAYERMASK_TERRAIN = System.BitConverter.ToInt32(new byte[] { 0, 4, 0, 0 }, 0);
    private static readonly int LAYERMASK_NO_TERRAIN = System.BitConverter.ToInt32(new byte[] {
                                                                                                BitByteTools.getByte(true,false,false,false,false,false,false,false), // default layer
                                                                                                BitByteTools.getByte(true,false,false,true,true,true,true,true), // no bulding preview, no terrain
                                                                                                byte.MaxValue,
                                                                                                byte.MaxValue
                                                                                                }, 0); // everything but terrain

    [SerializeField] private PlayerConstruction.BuildingPartType m_constructionType = PlayerConstruction.BuildingPartType.Foundation;
    [SerializeField] private int m_previewObjIndex = -1;
    [SerializeField] private Vector3[] m_groundRaysCornerPoints;
    [SerializeField, Min(0.0001f)] private float m_rayDistance = 0.5f;
    [SerializeField] private float m_iterationHeight = 0.5f; // how much to raise every time a ray didnt hit ground
    [SerializeField] private int m_maxHeightIterations = 10;
    [SerializeField] private float m_rotationOffsetY = 0f;
    [SerializeField] private float m_heightOffset = 0f;
    [SerializeField] private float m_maxCheckExtend = 5f;
    [SerializeField] private float m_distanceToSocket = 0f;
    [SerializeField] private float m_checkStartHeight = 2f;
    [SerializeField] private Vector2 m_insertSize = Vector2.one; // if this building type is an insert (door frame): how big is it
    [SerializeField, ReadOnly] private bool m_isInCollision = true;

    [SerializeField] private bool DEBUG_CheckToGround = false;
    [SerializeField] private bool DEBUG_CheckConstant = false;
    [SerializeField] private bool DEBUG_mode = false;
    [SerializeField] private int m_associatedEntityPrefabIndex = -1;

    private Rigidbody m_rigidbody;

    private void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();

        registerConstructionPreview(gameObject, this, m_previewObjIndex);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (DEBUG_CheckToGround || DEBUG_CheckConstant)
        {
            DEBUG_CheckToGround = false;

            Vector3 unused1;
            Vector3 unused2;

            if (constructionType == PlayerConstruction.BuildingPartType.Foundation)
            {
                rayCheckToGround(transform.position, transform.rotation, out unused1, out unused2, true, LAYERMASK_TERRAIN);
            }
            colliderCheckToOtherObjectsDown(transform.position, transform.rotation);
        }
    }

    private void registerConstructionPreview(GameObject gameObject, BuildingSystemPreviewConstruction script, int index)
    {
        m_constructionPreviewGameObjects[index] = gameObject;
        m_constructionPreviewScripts[index] = script;
        m_constructionPreviewRenderer[index] = gameObject.GetComponent<Renderer>();
    }

    public static GameObject getConstructionPreviewGameObject(int index)
    {
        return m_constructionPreviewGameObjects[index];
    }

    public static BuildingSystemPreviewConstruction getConstructionPreviewScript(int index)
    {
        return m_constructionPreviewScripts[index];
    }

    public static Renderer getConstructionPreviewRenderer(int index)
    {
        return m_constructionPreviewRenderer[index];
    }

    public float getHeightOffset()
    {
        return m_heightOffset;
    }

    public float getRotationOffsetY()
    {
        return m_rotationOffsetY;
    }

    public Vector2 getInsertSize()
    {
        return m_insertSize;
    }

    public PlayerConstruction.BuildingPartType constructionType
    {
        get
        {
            return m_constructionType;
        }
    }

    public int associatedEntityPrefabIndex
    {
        get
        {
            return m_associatedEntityPrefabIndex;
        }
    }

    public float distanceToSocket
    {
        get
        {
            return m_distanceToSocket;
        }
    }

    public bool isInCollision
    {
        get
        {
            return m_isInCollision;
        }
    }

    /// <summary>
    /// for foundations
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="rotation"></param>
    /// <param name="lowestPoint"></param>
    /// <param name="highestPoint"></param>
    public void rayCheckToGroundTerrain(Vector3 origin, Quaternion rotation, out Vector3 lowestPoint, out Vector3 highestPoint)
    {
        rayCheckToGround(origin, rotation, out lowestPoint, out highestPoint, false, LAYERMASK_TERRAIN);
    }

    public bool colliderCheckToOtherObjectsDown(Vector3 origin, Quaternion rotation) // for foundation
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        transform.position = origin + Vector3.up * m_checkStartHeight;
        transform.rotation = rotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        RaycastHit[] hits;
        hits = m_rigidbody.SweepTestAll(Vector3.down, m_maxCheckExtend);

        bool placeOccupied = false;
        int occupiedIndex = -1;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.tag == "Wall")
            {
                if (Mathf.Abs(hits[i].collider.transform.position.y - origin.y) < 3) // intersection
                {
                    placeOccupied = true;
                    occupiedIndex = i;
                    break;
                }
            }
            else if (hits[i].collider.tag == "Wall Sub")
            {
                PlayerBuildingSubPart wallSub = hits[i].collider.GetComponent<PlayerBuildingSubPart>();

                if (wallSub != null)
                {
                    if (Mathf.Abs(wallSub.transform.position.y - origin.y) < 1.5f) // intersection
                    {
                        placeOccupied = true;
                        occupiedIndex = i;
                        break;
                    }
                }
            }
            else if (hits[i].collider.tag == "Floor")
            {
                if (Mathf.Abs(hits[i].collider.transform.position.y - origin.y) < 1.5f) // intersection
                {
                    placeOccupied = true;
                    occupiedIndex = i;
                    break;
                }
            }
            else
            {
                placeOccupied = true;
                occupiedIndex = i;
                break;
            }
        }

        if (placeOccupied && DEBUG_mode)
        {
            Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.yellow, 2f);
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        return placeOccupied;
        /*

        Vector3 lowestPoint;
        Vector3 highestPoint;

        rayCheckToGround(origin, rotation, out lowestPoint, out highestPoint, false, LAYERMASK_NO_TERRAIN);

        if (lowestPoint == Vector3.zero) // hit nothing
        {
            return false;
        }
        else // hit something
        {
            return true;
        }
        */
    }

    public bool colliderCheckToOtherObjectsUpDown(Vector3 origin, Quaternion rotation) // for floor
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        RaycastHit[] hits;
        bool placeOccupied;
        int occupiedIndex;

        // Up --> Down

        transform.position = origin + Vector3.up * m_checkStartHeight;
        transform.rotation = rotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        hits = m_rigidbody.SweepTestAll(Vector3.down, m_checkStartHeight);

        placeOccupied = false;
        occupiedIndex = -1;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.tag == "Wall" || hits[i].collider.tag == "Wall Sub") // ignore wall as obstacle
            {

            }
            else
            {
                placeOccupied = true;
                occupiedIndex = i;
                break;
            }
        }

        if (placeOccupied && DEBUG_mode)
        {
            Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.yellow, 2f);
        }

        // Down --> Up

        if (!placeOccupied)
        {
            transform.position = origin + Vector3.down * m_checkStartHeight;
            transform.rotation = rotation;

            m_rigidbody.position = transform.position;
            m_rigidbody.rotation = transform.rotation;

            hits = m_rigidbody.SweepTestAll(Vector3.up, m_checkStartHeight);

            occupiedIndex = -1;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.tag == "Wall" || hits[i].collider.tag == "Wall Sub") // ignore wall as obstacle
                {

                }
                else
                {
                    placeOccupied = true;
                    occupiedIndex = i;
                    break;
                }
            }

            if (placeOccupied && DEBUG_mode)
            {
                Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.yellow, 2f);
            }
        }

        // revert to initial state

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        return placeOccupied;
    }

    public bool colliderCheckToOtherObjectsForwardBackward(Vector3 origin, Quaternion rotation) // for wall
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        RaycastHit[] hits;
        bool placeOccupied;
        int occupiedIndex;

        // forward --> backward

        transform.position = origin + transform.forward * m_checkStartHeight;
        transform.rotation = rotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        hits = m_rigidbody.SweepTestAll(-transform.forward, m_checkStartHeight);

        placeOccupied = false;
        occupiedIndex = -1;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.tag == "Floor" || hits[i].collider.tag == "Foundation") // ignore floor and foundation as obstacle
            {
            }
            else
            {

                placeOccupied = true;
                occupiedIndex = i;
                break;
            }
        }

        if (placeOccupied && DEBUG_mode)
        {
            Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.yellow, 2f);
            Debug.DrawLine(hits[occupiedIndex].point, hits[occupiedIndex].transform.position, Color.yellow, 2f);
        }

        // backward --> forward

        if (!placeOccupied)
        {
            transform.position = origin - transform.forward * m_checkStartHeight;
            transform.rotation = rotation;

            m_rigidbody.position = transform.position;
            m_rigidbody.rotation = transform.rotation;

            hits = m_rigidbody.SweepTestAll(transform.forward, m_checkStartHeight);

            occupiedIndex = -1;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.tag == "Floor" || hits[i].collider.tag == "Foundation") // ignore floor and foundation as obstacle
                {

                }
                else
                {
                    placeOccupied = true;
                    occupiedIndex = i;
                    break;
                }
            }

            if (placeOccupied && DEBUG_mode)
            {
                Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.red, 2f);
                Debug.DrawLine(hits[occupiedIndex].point, hits[occupiedIndex].transform.position, Color.red, 2f);
            }
        }

        // revert to initial state

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        return placeOccupied;
    }

    public bool colliderCheckToOtherObjectsUpWall(Vector3 origin, Quaternion rotation, float distance)
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        RaycastHit[] hits;
        bool placeOccupied = false;
        int occupiedIndex;

        // Down --> Up


        transform.position = origin + Vector3.down * distance;
        transform.rotation = rotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        hits = m_rigidbody.SweepTestAll(Vector3.up, distance);

        occupiedIndex = -1;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].point.y < origin.y - distance / 2 || hits[i].point.y > origin.y + distance / 2 - WALL_UP_CKECK_TOLERANCE) // ignore hits below or above wall position
            {
                //Debug.DrawRay(hits[i].point, Vector3.up, Color.red, 2f);
            }
            else
            {
                //Debug.DrawRay(hits[i].point, Vector3.up, Color.green, 2f);

                placeOccupied = true;
                occupiedIndex = i;
                break;
            }
        }

        if (placeOccupied && DEBUG_mode)
        {
            Debug.DrawRay(hits[occupiedIndex].point, Vector3.up, Color.yellow, 2f);
            Debug.DrawLine(hits[occupiedIndex].point, hits[occupiedIndex].transform.position, Color.yellow, 2f);
        }

        // revert to initial state

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        m_rigidbody.position = transform.position;
        m_rigidbody.rotation = transform.rotation;

        return placeOccupied;
    }

    private void rayCheckToGround(Vector3 origin, Quaternion rotation, out Vector3 lowestPoint, out Vector3 highestPoint, bool debugMode, int layerMask)
    {
        if (DEBUG_mode)
        {
            debugMode = true;
        }

        lowestPoint = Vector3.zero;
        highestPoint = Vector3.zero;

        float lowestHeight = float.MaxValue;
        float highestHeight = float.MinValue;

        bool rayWithoutHit = false;

        RaycastHit[] temp_hits = new RaycastHit[1];

        int heightRaiseIteration = 0;

        while ((rayWithoutHit == true || heightRaiseIteration == 0) && heightRaiseIteration < m_maxHeightIterations)
        {
            rayWithoutHit = false;

            for (int i = 0; i < m_groundRaysCornerPoints.Length - 1; i++)
            {
                Vector3 startPos = rotation * Quaternion.Euler(0, m_rotationOffsetY, 0) * m_groundRaysCornerPoints[i] + origin + Vector3.up * m_iterationHeight * heightRaiseIteration;
                Vector3 endPos = rotation * Quaternion.Euler(0, m_rotationOffsetY, 0) * m_groundRaysCornerPoints[i + 1] + origin + Vector3.up * m_iterationHeight * heightRaiseIteration;

                Vector3 connectionDir = (endPos - startPos).normalized;

                float totalDistance = Vector3.Distance(startPos, endPos);
                float movedDistance = 0;

                while (movedDistance < totalDistance && rayWithoutHit == false)
                {
                    if (debugMode)
                    {
                        Debug.DrawRay(startPos + connectionDir * movedDistance, Vector3.down, Color.red, 2);
                    }

                    if (Physics.RaycastNonAlloc(startPos + connectionDir * movedDistance, Vector3.down, temp_hits, m_maxCheckExtend, layerMask) < 1)
                    {
                        rayWithoutHit = true;
                        break;
                    }
                    else
                    {
                        if (debugMode)
                        {
                            Debug.DrawRay(temp_hits[0].point, Vector3.up, Color.green, 2);
                        }

                        if (temp_hits[0].point.y < lowestHeight)
                        {
                            lowestHeight = temp_hits[0].point.y;
                            lowestPoint = temp_hits[0].point;
                        }

                        if (temp_hits[0].point.y > highestHeight)
                        {
                            highestHeight = temp_hits[0].point.y;
                            highestPoint = temp_hits[0].point;
                        }
                    }

                    movedDistance += m_rayDistance;
                }

                if (rayWithoutHit)
                {
                    break;
                }
            }

            heightRaiseIteration++;
        }

        if (debugMode)
        {
            Debug.DrawRay(highestPoint, Vector3.up, Color.blue, 2);
            Debug.DrawRay(lowestPoint, Vector3.up, Color.blue, 2);
            Debug.Log("heightRaiseIteration: " + heightRaiseIteration);
        }
    }
}
