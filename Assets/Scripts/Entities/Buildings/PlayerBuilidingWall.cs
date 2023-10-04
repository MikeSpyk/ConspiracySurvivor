using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerBuilidingWall : PlayerConstruction
{
    private static readonly BuildingSocket[] m_buildingSockets = new BuildingSocket[]
    {
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, 1.5f, 0f),
            m_socketRotation = Quaternion.Euler(0,0,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, -1.5f, 0f),
            m_socketRotation = Quaternion.Euler(0,0,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { false, false, false }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(1.5f, 0f, 0f),
            m_socketRotation = Quaternion.Euler(0,0,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall},
            m_buildingPartSocketVisible = new bool[] { false }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(-1.5f, 0f, 0f),
            m_socketRotation = Quaternion.Euler(0,0,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall},
            m_buildingPartSocketVisible = new bool[] { false }
        }
    };

    private static Queue<PlayerBuildingSubPart> m_cachedColliderObjects = new Queue<PlayerBuildingSubPart>();

    [SerializeField] private GameObject m_subColliderPrefab;
    [SerializeField] private GameObject m_doorwayPrefab;
    [SerializeField] private bool m_hasAttachment = false;
    [SerializeField] private BuildingPartType m_attachmentType = BuildingPartType.Default;

    private bool m_additionSocketOccuipied = false;
    private BuildingPartType m_additionalSocketType = BuildingPartType.Default;
    private Vector3 m_additionSocketPosition;
    private Quaternion m_additionSocketRotation;
    private Entity_base m_additionSocketEntity = null;


    private Vector2 m_attachmentPosition;
    private List<PlayerBuildingSubPart> m_connectedSubColliders = new List<PlayerBuildingSubPart>();
    private GameObject m_attachmentObject = null;
    private Renderer m_renderer;
    private Collider m_collider;

    public bool hasAttachment { get { return m_hasAttachment; } }
    public bool additionSocketOccuipied { get { return m_additionSocketOccuipied; } }
    public BuildingPartType additionalSocketType { get { return m_additionalSocketType; } }
    public Vector3 additionSocketPosition { get { return m_additionSocketPosition; } }
    public Quaternion additionSocketRotation { get { return m_additionSocketRotation; } }

    protected void Awake()
    {
        base.Awake();

        m_renderer = GetComponent<Renderer>();
        m_collider = GetComponent<Collider>();
    }

    public void setAttachment(BuildingPartType attachmentType, Vector2 position)
    {
        if ((GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient) && additionSocketOccuipied)
        {
            return;
        }

        // hide wall
        m_renderer.enabled = false;
        m_collider.enabled = false;

        Vector2 size;

        if (m_attachmentObject != null)
        {
            Destroy(m_attachmentObject);
            m_attachmentObject = null;
        }

        m_attachmentPosition = position;

        switch (attachmentType)
        {
            case BuildingPartType.DoorFrame:
                {
                    size = new Vector2(1.2f, 2f);
                    Debug.Log("TODO: check if out of bounds !");

                    m_attachmentObject = Instantiate(m_doorwayPrefab) as GameObject;

                    m_attachmentObject.transform.position = transform.position + transform.right * position.x + transform.up * position.y;
                    m_attachmentObject.transform.SetParent(transform);
                    m_attachmentObject.transform.localRotation = Quaternion.Euler(0, 90, 0);

                    setAdditionSocket(BuildingPartType.Door, m_attachmentObject.transform.position, m_attachmentObject.transform.rotation);

                    position.y += 1f;

                    break;
                }
            default:
                {
                    throw new System.NotImplementedException();
                }
        }

        m_hasAttachment = true;
        m_attachmentType = attachmentType;

        createCollidersAroundInsert(position, size);

        if(GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_sendAttachment();
        }
    }

    public void occupieAdditionSocket(BuildingPartType type)
    {
        if (!m_additionSocketOccuipied)
        {
            switch (type)
            {
                case BuildingPartType.Door:
                    {
                        BuildingSystemPreviewConstruction preview = BuildingSystemPreviewConstruction.getConstructionPreviewScript(8);
                        GameObject doorObj = EntityManager.singleton.spawnEntity(preview.associatedEntityPrefabIndex, m_additionSocketPosition, m_additionSocketRotation);

                        PlayerBuildingDoor doorScript = doorObj.GetComponent<PlayerBuildingDoor>();
                        doorScript.m_associatedBuildingID = m_associatedBuilding.buildingUID;
                        doorScript.m_associatedWallEntityID = entityUID;

                        m_additionSocketEntity = doorScript;

                        break;
                    }
                default:
                    {
                        Debug.LogWarning("PlayerBuilidingWall: occupieAdditionSocket: unknown BuildingPartType: " + type);
                        break;
                    }
            }

            m_additionSocketOccuipied = true;
        }
    }

    private void setAdditionSocket(BuildingPartType type, Vector3 position, Quaternion rotation)
    {
        m_additionalSocketType = type;
        m_additionSocketPosition = position;
        m_additionSocketRotation = rotation;
    }

    private void createCollidersAroundInsert(Vector2 insertPos, Vector2 insertSize)
    {
        removeCurrentColliders();

        /*
        Debug.Log("insertPos Start: " + insertPos.ToString() + "; insertSize: " + insertSize.ToString());
        Vector3 insertOrigin = transform.position + transform.right * insertPos.x + transform.up * insertPos.y;
        Debug.DrawRay(insertOrigin, transform.forward, Color.red, 2f);
        Debug.DrawRay(insertOrigin, transform.right * insertSize.x / 2, Color.red, 2f);
        Debug.DrawRay(insertOrigin, transform.up * insertSize.y / 2, Color.red, 2f);
        Debug.DrawRay(insertOrigin, -transform.right * insertSize.x / 2, Color.red, 2f);
        Debug.DrawRay(insertOrigin, -transform.up * insertSize.y / 2, Color.red, 2f);
        */

        // make sure insert within bounds

        float attachmentUpperEdgeY = insertPos.y + insertSize.y / 2;
        float attachmentLowerEdgeY = insertPos.y - insertSize.y / 2;
        float attachmentRightEdgeX = insertPos.x + insertSize.x / 2;
        float attachmentLeftEdgeX = insertPos.x - insertSize.x / 2;

        if (attachmentUpperEdgeY > EDGE_LENGTH / 2) // too high
        {
            insertPos.y = EDGE_LENGTH / 2 - insertSize.y / 2;
            attachmentUpperEdgeY = insertPos.y + insertSize.y / 2;
            attachmentLowerEdgeY = insertPos.y - insertSize.y / 2;
        }
        else if (attachmentLowerEdgeY < -EDGE_LENGTH / 2) // too low
        {
            insertPos.y = -EDGE_LENGTH / 2 + insertSize.y / 2;
            attachmentUpperEdgeY = insertPos.y + insertSize.y / 2;
            attachmentLowerEdgeY = insertPos.y - insertSize.y / 2;
        }

        if (attachmentRightEdgeX > EDGE_LENGTH / 2)
        {
            insertPos.x = EDGE_LENGTH / 2 - insertSize.x / 2;
            attachmentRightEdgeX = insertPos.x + insertSize.x / 2;
            attachmentLeftEdgeX = insertPos.x - insertSize.x / 2;
        }
        else if (attachmentLeftEdgeX < -EDGE_LENGTH / 2)
        {
            insertPos.x = -EDGE_LENGTH / 2 + insertSize.x / 2;
            attachmentRightEdgeX = insertPos.x + insertSize.x / 2;
            attachmentLeftEdgeX = insertPos.x - insertSize.x / 2;
        }

        // calculate edges and spaces

        float leftEdgePosX = -EDGE_LENGTH / 2;
        float rightEdgePosX = +EDGE_LENGTH / 2;
        float upperEdgePosY = +EDGE_LENGTH / 2;
        float lowerEdgePosY = -EDGE_LENGTH / 2;

        float leftSpace = insertPos.x - insertSize.x / 2 - leftEdgePosX;  // free space to the left of the attachment
        float rightSpace = rightEdgePosX - insertSize.x / 2 - insertPos.x;
        float upperSpace = upperEdgePosY - insertPos.y - insertSize.y / 2;
        float lowerSpace = insertPos.y - insertSize.y / 2 - lowerEdgePosY;

        bool upCollider = upperSpace > 0;
        bool downCollider = lowerSpace > 0;
        bool rightCollider = rightSpace > 0;
        bool leftCollider = leftSpace > 0;

        // create colliders

        if (upCollider)
        {
            PlayerBuildingSubPart script = getBoxColliderScript();
            m_connectedSubColliders.Add(script);
            GameObject upperColliderObj = script.gameObject;
            script.m_parent = this;

            upperColliderObj.transform.SetParent(transform);
            upperColliderObj.transform.position = transform.position + transform.up * (attachmentUpperEdgeY + upperSpace / 2);              // new Vector3(transform.position.x, attachmentUpperEdgeY + upperSpace / 2, transform.position.z);
            upperColliderObj.transform.rotation = transform.rotation;
            upperColliderObj.transform.localScale = new Vector3(EDGE_LENGTH, upperSpace, upperColliderObj.transform.localScale.z);
            //Debug.DrawRay(upperColliderObj.transform.position, Vector3.forward, Color.red, 2f);
        }

        if (downCollider)
        {
            PlayerBuildingSubPart script = getBoxColliderScript();
            m_connectedSubColliders.Add(script);
            GameObject lowerColliderObj = script.gameObject;
            script.m_parent = this;

            lowerColliderObj.transform.SetParent(transform);
            lowerColliderObj.transform.position = transform.position - transform.up * (-attachmentLowerEdgeY + lowerSpace / 2);      //new Vector3(transform.position.x, attachmentLowerEdgeY - lowerSpace / 2, transform.position.z);
            lowerColliderObj.transform.rotation = transform.rotation;
            lowerColliderObj.transform.localScale = new Vector3(EDGE_LENGTH, lowerSpace, lowerColliderObj.transform.localScale.z);
            //Debug.DrawRay(lowerColliderObj.transform.position, Vector3.forward, Color.red, 2f);
        }

        if (leftCollider)
        {
            PlayerBuildingSubPart script = getBoxColliderScript();
            m_connectedSubColliders.Add(script);
            GameObject leftColliderObj = script.gameObject;
            script.m_parent = this;

            leftColliderObj.transform.localScale = new Vector3(leftSpace, insertSize.y, leftColliderObj.transform.localScale.z);
            leftColliderObj.transform.RotateAround(transform.position, Vector3.up, transform.rotation.eulerAngles.y);
            leftColliderObj.transform.position = transform.position + transform.right * (attachmentLeftEdgeX - leftSpace / 2) + transform.up * insertPos.y;
            leftColliderObj.transform.SetParent(transform);

            //Debug.DrawRay(leftColliderObj.transform.position, Vector3.forward, Color.red, 2f);
        }

        if (rightCollider)
        {
            PlayerBuildingSubPart script = getBoxColliderScript();
            m_connectedSubColliders.Add(script);
            GameObject rightColliderObj = script.gameObject;
            script.m_parent = this;

            rightColliderObj.transform.localScale = new Vector3(rightSpace, insertSize.y, rightColliderObj.transform.localScale.z);
            rightColliderObj.transform.RotateAround(transform.position, Vector3.up, transform.rotation.eulerAngles.y);
            rightColliderObj.transform.position = transform.position + transform.right * (attachmentRightEdgeX + rightSpace / 2) + transform.up * insertPos.y;
            rightColliderObj.transform.SetParent(transform);

            //Debug.DrawRay(rightColliderObj.transform.position, Vector3.forward, Color.red, 2f);
        }
        //Debug.Log("insertPos End: " + insertPos.ToString() + "; insertSize: " + insertSize.ToString());
        //Debug.DrawRay(new Vector3(leftEdgePosX, transform.position.y, transform.position.z), transform.right * leftSpace, Color.blue, 2f);
        //Debug.DrawRay(new Vector3(rightEdgePosX, transform.position.y, transform.position.z), -transform.right * rightSpace, Color.blue, 2f);
        //Debug.DrawRay(new Vector3(transform.position.x, upperEdgePosY, transform.position.z), -transform.up * upperSpace, Color.blue, 2f);
        //Debug.DrawRay(new Vector3(transform.position.x, lowerEdgePosY, transform.position.z), transform.up * lowerSpace, Color.blue, 2f);
    }

    private void removeCurrentColliders()
    {
        for (int i = 0; i < m_connectedSubColliders.Count; i++)
        {
            recycleBoxColliderByScript(m_connectedSubColliders[i]);
        }

        m_connectedSubColliders.Clear();
    }

    private PlayerBuildingSubPart getBoxColliderScript()
    {
        PlayerBuildingSubPart returnValue;

        if (m_cachedColliderObjects.Count > 0)
        {
            returnValue = m_cachedColliderObjects.Dequeue();
            returnValue.gameObject.SetActive(true);
            returnValue.transform.rotation = Quaternion.identity;
        }
        else
        {
            returnValue = Instantiate(m_subColliderPrefab).GetComponent<PlayerBuildingSubPart>();
            returnValue.gameObject.name = "Sub Building Part";
        }

        return returnValue;
    }

    private static void recycleBoxColliderByScript(PlayerBuildingSubPart colliderObj)
    {
        colliderObj.m_parent = null;
        colliderObj.transform.SetParent(null);
        colliderObj.gameObject.SetActive(false);
        m_cachedColliderObjects.Enqueue(colliderObj);
    }

    protected override BuildingSocket[] getBuildingSocketTemplate()
    {
        return m_buildingSockets;
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_BuildingPartWall dataEntity = m_dataEntity as DataEntity_BuildingPartWall;

        dataEntity.m_attachmentType = m_attachmentType;
        dataEntity.m_hasAttachment = m_hasAttachment;
        dataEntity.m_attachmentPosition = m_attachmentPosition;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_BuildingPartWall dataEntity = m_dataEntity as DataEntity_BuildingPartWall;

        m_attachmentType = dataEntity.m_attachmentType;
        m_hasAttachment = dataEntity.m_hasAttachment;
        m_attachmentPosition = dataEntity.m_attachmentPosition;

        if (m_hasAttachment)
        {
            setAttachment(m_attachmentType, m_attachmentPosition);
        }
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_BuildingPartWall();
    }

    public override NetworkMessage server_fillUncullingMessage(NetworkMessage message)
    {
        base.server_fillUncullingMessage(message);

        if (m_hasAttachment)
        {
            message.addIntegerValues(1);
            message.addIntegerValues((int)m_attachmentType);
            message.addFloatValues(m_attachmentPosition.x);
            message.addFloatValues(m_attachmentPosition.y);
        }
        else
        {
            message.addIntegerValues(0);
        }

        return message;
    }

    public override void client_receiveUncullMessage(NetworkMessage message)
    {
        base.client_receiveUncullMessage(message);

        if (message.integerValuesCount > 2)
        {
            if (message.getIntValue(2) == 1)
            {
                if (message.integerValuesCount > 3)
                {
                    if (Enum.IsDefined(typeof(BuildingPartType), message.getIntValue(3)))
                    {
                        if (message.floatValuesCount == 8)
                        {
                            setAttachment((BuildingPartType)message.getIntValue(3), new Vector2(message.getFloatValue(6), message.getFloatValue(7)));
                        }
                    }
                }
            }
        }
    }

    private void server_sendAttachment()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.SetBuildingAttachment);
        message.addIntegerValues((int)m_attachmentType);
        message.addFloatValues(m_attachmentPosition.x, m_attachmentPosition.y);

        server_sendTCPMessageToAllClients(message);
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        if (base.client_receivedCustomNetworkMessage(message))
        {
            return true; // a base class already handles this message
        }
        else
        {
            switch (message.getIntValue(1))
            {
                case (int)CustomMessageContext1.SetBuildingAttachment:
                    {
                        if(GameManager_Custom.singleton.isServerAndClient)
                        {
                            return true;
                        }

                        if (message.checkInputCorrectness(3, 2, 0))
                        {
                            if (Enum.IsDefined(typeof(BuildingPartType), message.getIntValue(2)))
                            {
                                setAttachment((BuildingPartType)message.getIntValue(2), new Vector2(message.getFloatValue(0), message.getFloatValue(1)));
                            }
                            else
                            {
                                Debug.LogWarning("PlayerBuilidingWall: client_receivedCustomNetworkMessage: SetBuildingAttachment: enum out of range");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("PlayerBuilidingWall: client_receivedCustomNetworkMessage: SetBuildingAttachment: message has wrong data count");
                        }
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
    }

}
