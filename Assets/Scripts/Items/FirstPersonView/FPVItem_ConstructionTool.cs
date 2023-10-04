using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_ConstructionTool : FPVItem_Base
{
    private static readonly int RAY_LAYERMASK_FOUNDATION = System.BitConverter.ToInt32(new byte[] { 0, 4, 64, 0 }, 0);
    private static readonly int RAY_LAYERMASK_WALL_FLOOR = System.BitConverter.ToInt32(new byte[] { 0, 0, 64, 0 }, 0);

    [SerializeField] private float m_maxPlaceDistance = 10;
    [SerializeField] private float m_maxHeighDelta = 2f;
    [SerializeField] private float m_doorwayMinDistanceToEdge = 0.1f;
    [SerializeField, ReadOnly] private bool m_constructMode = false;

    private GameObject m_playerCamera;
    private bool m_placeable = false;

    private GameObject m_currentPreviewObject = null;
    private BuildingSystemPreviewConstruction m_currentPreviewScript = null;
    private Renderer m_currentPreviewRenderer = null;
    private PlayerConstruction m_lastHitBuildingPart = null;
    private float m_resetBuildAnimationFrame = 0;

    private void Start()
    {
        base.Start();

        m_playerCamera = CameraStack.gameobject;

        GUIManager.singleton.getBuildingSelectionCircularMenu().buttonClickedHeadlineEvent += onBuildingSelectionChanged;
    }

    private void Update()
    {
        base.Update();

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            if (Time.frameCount == m_resetBuildAnimationFrame)
            {
                FirstPersonViewManager.singleton.firstPersonArmsAnimator.SetBool("ItemConstructionTool_build", false);
            }

            if (Input.GetKeyUp(m_keySource.getSecondaryActionKey()))
            {
                GUIManager.singleton.setBuildingSelectionCircularMenuActivity(false);
            }

            m_placeable = false;
            m_lastHitBuildingPart = null;

            if (m_currentPreviewObject != null && m_constructMode)
            {
                Vector3 position;
                Quaternion rotation;

                client_getPreviewPosition(out position, out rotation);

                m_currentPreviewObject.transform.position = position;
                m_currentPreviewObject.transform.rotation = rotation;

                if (client_checkPreviewPositionValid(position, rotation))
                {
                    m_placeable = true;
                }
                else
                {
                    m_placeable = false;
                }
            }
        }
    }

    private void onBuildingSelectionChanged(object sender, StringEventArgs e)
    {
        if (m_currentPreviewObject != null && m_currentPreviewObject.activeSelf)
        {
            m_currentPreviewObject.SetActive(false);
            m_currentPreviewObject.transform.position = Vector3.zero;
        }

        m_constructMode = false;
        int constructionIndex = -1;

        switch (e.m_string)
        {
            case "Triangle Foundation":
                {
                    constructionIndex = 0;
                    break;
                }
            case "Rectangle Foundation":
                {
                    constructionIndex = 1;
                    break;
                }
            case "Pentagon Foundation":
                {
                    constructionIndex = 2;
                    break;
                }
            case "Wall":
                {
                    constructionIndex = 3;
                    break;
                }
            case "Triangle Floor":
                {
                    constructionIndex = 4;
                    break;
                }
            case "Rectangle Floor":
                {
                    constructionIndex = 5;
                    break;
                }
            case "Pentagon Floor":
                {
                    constructionIndex = 6;
                    break;
                }
            case "Door Frame":
                {
                    constructionIndex = 7;
                    break;
                }
            case "Door":
                {
                    constructionIndex = 8;
                    break;
                }
            default:
                {
                    Debug.LogWarning("FPVItem_ConstructionTool: onBuildingSelectionChanged: unknown building type: \"" + e.m_string + "\"");
                    return;
                }
        }

        m_currentPreviewObject = BuildingSystemPreviewConstruction.getConstructionPreviewGameObject(constructionIndex);
        m_currentPreviewScript = BuildingSystemPreviewConstruction.getConstructionPreviewScript(constructionIndex);
        m_currentPreviewRenderer = BuildingSystemPreviewConstruction.getConstructionPreviewRenderer(constructionIndex);

        m_constructMode = true;

        m_currentPreviewObject.SetActive(true);
    }

    private void client_getPreviewPosition(out Vector3 outPosition, out Quaternion outRotation)
    {
        Player_local localPlayer = EntityManager.singleton.getLocalPlayer();

        if (m_currentPreviewObject != null)
        {
            if (localPlayer != null)
            {
                if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Foundation)
                {
                    RaycastHit rayHit;

                    if (Physics.Raycast(m_playerCamera.transform.position, m_playerCamera.transform.forward, out rayHit, m_maxPlaceDistance, RAY_LAYERMASK_FOUNDATION)) // if hit terrain/another building
                    {
                        m_lastHitBuildingPart = rayHit.collider.gameObject.GetComponent<PlayerConstruction>();

                        if (m_lastHitBuildingPart == null)
                        {
                            PlayerBuildingSubPart subPart = rayHit.collider.gameObject.GetComponent<PlayerBuildingSubPart>();

                            if (subPart != null)
                            {
                                m_lastHitBuildingPart = subPart.m_parent;
                            }
                        }

                        if (m_lastHitBuildingPart == null) // terrain
                        {
                            Vector3 lowestPoint;
                            Vector3 highestPoint;

                            m_currentPreviewScript.rayCheckToGroundTerrain(rayHit.point, localPlayer.transform.rotation, out lowestPoint, out highestPoint);

                            outPosition = new Vector3(rayHit.point.x, highestPoint.y, rayHit.point.z) + (Vector3.up * m_currentPreviewScript.getHeightOffset());
                            outRotation = localPlayer.transform.rotation;
                        }
                        else
                        {
                            BuildingSocket[] socketTemplate;
                            Vector3[] socketPositions;
                            Quaternion[] socketsRotations;
                            List<int>[] connectedBuildingParts;

                            m_lastHitBuildingPart.getBuildingSockets(out socketTemplate, out socketPositions, out socketsRotations, out connectedBuildingParts);

                            // find closest fitting socket

                            Vector3 closetsPositions = Vector3.zero;
                            float closestDistance = float.MaxValue;
                            Quaternion closestRotation = Quaternion.identity;

                            for (int i = 0; i < socketPositions.Length; i++)
                            {
                                if (socketTemplate[i].isBuildingPartAllowedAndVisible(PlayerConstruction.BuildingPartType.Foundation))
                                {
                                    bool isOccupied = false;

                                    for (int j = 0; j < connectedBuildingParts[i].Count; j++)
                                    {
                                        PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(connectedBuildingParts[i][j]) as PlayerConstruction;

                                        if (connectedBuildingPart != null && connectedBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Foundation)
                                        {
                                            isOccupied = true;
                                            break;
                                        }
                                    }

                                    if (!isOccupied)
                                    {
                                        if (Vector3.Distance(rayHit.point, socketPositions[i]) < closestDistance)
                                        {
                                            closestDistance = Vector3.Distance(rayHit.point, socketPositions[i]);
                                            closetsPositions = socketPositions[i];
                                            closestRotation = socketsRotations[i];
                                        }
                                    }
                                }
                            }

                            if (closetsPositions == Vector3.zero) // no free socket available
                            {
                                outPosition = rayHit.point;
                                outRotation = localPlayer.transform.rotation;
                            }
                            else
                            {
                                outPosition = closetsPositions + (Vector3.up * m_currentPreviewScript.getHeightOffset()) + (closestRotation * Vector3.forward * m_currentPreviewScript.distanceToSocket);
                                outRotation = closestRotation;
                            }
                        }
                    }
                    else
                    {
                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                        outRotation = localPlayer.transform.rotation;
                    }
                }
                else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Wall)
                {
                    RaycastHit rayHit;

                    if (Physics.Raycast(m_playerCamera.transform.position, m_playerCamera.transform.forward, out rayHit, m_maxPlaceDistance, RAY_LAYERMASK_WALL_FLOOR))
                    {
                        m_lastHitBuildingPart = rayHit.collider.gameObject.GetComponent<PlayerConstruction>();

                        if (m_lastHitBuildingPart == null)
                        {
                            PlayerBuildingSubPart subPart = rayHit.collider.gameObject.GetComponent<PlayerBuildingSubPart>();

                            if (subPart != null)
                            {
                                m_lastHitBuildingPart = subPart.m_parent;
                            }
                        }

                        if (m_lastHitBuildingPart == null)
                        {
                            outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                            outRotation = localPlayer.transform.rotation;
                        }
                        else
                        {
                            BuildingSocket[] socketTemplate;
                            Vector3[] socketPositions;
                            Quaternion[] socketsRotations;
                            List<int>[] connectedBuildingParts;

                            m_lastHitBuildingPart.getBuildingSockets(out socketTemplate, out socketPositions, out socketsRotations, out connectedBuildingParts);

                            // find closest fitting socket

                            Vector3 closetsPositions = Vector3.zero;
                            float closestDistance = float.MaxValue;
                            Quaternion closestRotation = Quaternion.identity;

                            if (m_lastHitBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Floor)
                            {
                                for (int i = 0; i < socketPositions.Length; i++)
                                {
                                    if (socketTemplate[i].isBuildingPartAllowedAndVisible(PlayerConstruction.BuildingPartType.Wall))
                                    {
                                        bool aboveOccuipied = false;

                                        for (int j = 0; j < connectedBuildingParts[i].Count; j++)
                                        {
                                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(connectedBuildingParts[i][j]) as PlayerConstruction;

                                            if (connectedBuildingPart != null && connectedBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
                                            {
                                                if (connectedBuildingPart.transform.position.y > m_lastHitBuildingPart.transform.position.y)
                                                {
                                                    aboveOccuipied = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (!aboveOccuipied)
                                        {
                                            if (Vector3.Distance(rayHit.point, socketPositions[i]) < closestDistance)
                                            {
                                                closestDistance = Vector3.Distance(rayHit.point, socketPositions[i]);
                                                closetsPositions = socketPositions[i];
                                                closestRotation = socketsRotations[i];
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < socketPositions.Length; i++)
                                {
                                    if (socketTemplate[i].isBuildingPartAllowedAndVisible(PlayerConstruction.BuildingPartType.Wall))
                                    {
                                        bool isOccupied = false;

                                        for (int j = 0; j < connectedBuildingParts[i].Count; j++)
                                        {
                                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(connectedBuildingParts[i][j]) as PlayerConstruction;

                                            if (connectedBuildingPart != null && connectedBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
                                            {
                                                isOccupied = true;
                                                break;
                                            }
                                        }

                                        if (!isOccupied)
                                        {
                                            if (Vector3.Distance(rayHit.point, socketPositions[i]) < closestDistance)
                                            {
                                                closestDistance = Vector3.Distance(rayHit.point, socketPositions[i]);
                                                closetsPositions = socketPositions[i];
                                                closestRotation = socketsRotations[i];
                                            }
                                        }
                                    }
                                }
                            }

                            if (closetsPositions == Vector3.zero) // no free socket available
                            {
                                outPosition = rayHit.point;
                                outRotation = localPlayer.transform.rotation;
                            }
                            else
                            {
                                outPosition = closetsPositions + (Vector3.up * m_currentPreviewScript.getHeightOffset()) + (closestRotation * Vector3.forward * m_currentPreviewScript.distanceToSocket);
                                outRotation = closestRotation;
                            }
                        }
                    }
                    else
                    {
                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                        outRotation = localPlayer.transform.rotation;
                    }
                }
                else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Floor)
                {
                    RaycastHit rayHit;

                    if (Physics.Raycast(m_playerCamera.transform.position, m_playerCamera.transform.forward, out rayHit, m_maxPlaceDistance, RAY_LAYERMASK_WALL_FLOOR))
                    {
                        m_lastHitBuildingPart = rayHit.collider.gameObject.GetComponent<PlayerConstruction>();

                        if (m_lastHitBuildingPart == null)
                        {
                            PlayerBuildingSubPart subPart = rayHit.collider.gameObject.GetComponent<PlayerBuildingSubPart>();

                            if (subPart != null)
                            {
                                m_lastHitBuildingPart = subPart.m_parent;
                            }
                        }

                        if (m_lastHitBuildingPart == null)
                        {
                            outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                            outRotation = localPlayer.transform.rotation;
                        }
                        else
                        {
                            BuildingSocket[] socketTemplate;
                            Vector3[] socketPositions;
                            Quaternion[] socketsRotations;
                            List<int>[] connectedBuildingParts;

                            m_lastHitBuildingPart.getBuildingSockets(out socketTemplate, out socketPositions, out socketsRotations, out connectedBuildingParts);

                            // find closest fitting socket

                            Vector3 closetsPositions = Vector3.zero;
                            float closestDistance = float.MaxValue;
                            Quaternion closestRotation = Quaternion.identity;

                            for (int i = 0; i < socketPositions.Length; i++)
                            {
                                if (socketTemplate[i].isBuildingPartAllowedAndVisible(PlayerConstruction.BuildingPartType.Floor))
                                {
                                    if (m_lastHitBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall) // special treatment for wall because it has 2 possible floor position for 1 socket
                                    {
                                        Vector3[] floorDirections = new Vector3[2];
                                        floorDirections[0] = socketsRotations[i] * Vector3.forward;
                                        floorDirections[1] = socketsRotations[i] * Quaternion.Euler(0, 180, 0) * Vector3.forward;
                                        bool[] directionValid = new bool[2] { true, true };

                                        for (int j = 0; j < connectedBuildingParts[i].Count; j++)
                                        {
                                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(connectedBuildingParts[i][j]) as PlayerConstruction;

                                            if (connectedBuildingPart != null && connectedBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Floor)
                                            {
                                                Vector3 connectionVec = connectedBuildingPart.transform.position - socketPositions[i];

                                                for (int k = 0; k < 2; k++)
                                                {
                                                    if (Vector3.Angle(connectionVec, floorDirections[k]) < 0.01f)
                                                    {
                                                        directionValid[k] = false;
                                                    }
                                                }
                                            }
                                        }

                                        for (int j = 0; j < 2; j++)
                                        {
                                            if (directionValid[j])
                                            {
                                                Vector3 socketPositionsOffset = socketPositions[i] + floorDirections[j] * 0.01f;

                                                if (Vector3.Distance(rayHit.point, socketPositionsOffset) < closestDistance)
                                                {
                                                    closestDistance = Vector3.Distance(rayHit.point, socketPositionsOffset);
                                                    closetsPositions = socketPositions[i];
                                                    if (j == 0)
                                                    {
                                                        closestRotation = socketsRotations[i];
                                                    }
                                                    else
                                                    {
                                                        closestRotation = socketsRotations[i] * Quaternion.Euler(0, 180, 0);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        bool isOccupied = false;

                                        for (int j = 0; j < connectedBuildingParts[i].Count; j++)
                                        {
                                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(connectedBuildingParts[i][j]) as PlayerConstruction;

                                            if (connectedBuildingPart != null && connectedBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Floor)
                                            {
                                                isOccupied = true;
                                                break;
                                            }
                                        }

                                        if (!isOccupied)
                                        {
                                            Vector3 socketPositionOffset = socketPositions[i] + socketsRotations[i] * Vector3.forward * 0.01f; // add a little bit in the given direction so, that 2 sockets at the same postition can get choosen by looking at them from different point of views

                                            if (Vector3.Distance(rayHit.point, socketPositionOffset) < closestDistance)
                                            {
                                                closestDistance = Vector3.Distance(rayHit.point, socketPositionOffset);
                                                closetsPositions = socketPositions[i];
                                                closestRotation = socketsRotations[i];
                                            }
                                        }
                                    }
                                }
                            }

                            if (closetsPositions == Vector3.zero) // no free socket available
                            {
                                outPosition = rayHit.point;
                                outRotation = localPlayer.transform.rotation;
                            }
                            else
                            {
                                outPosition = closetsPositions + (Vector3.up * m_currentPreviewScript.getHeightOffset()) + (closestRotation * Vector3.forward * m_currentPreviewScript.distanceToSocket);
                                outRotation = closestRotation;
                            }
                        }
                    }
                    else
                    {
                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                        outRotation = localPlayer.transform.rotation;
                    }
                }
                else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.DoorFrame)
                {
                    RaycastHit rayHit;

                    if (Physics.Raycast(m_playerCamera.transform.position, m_playerCamera.transform.forward, out rayHit, m_maxPlaceDistance, RAY_LAYERMASK_WALL_FLOOR))
                    {
                        m_lastHitBuildingPart = rayHit.collider.gameObject.GetComponent<PlayerConstruction>();

                        if (m_lastHitBuildingPart == null)
                        {
                            PlayerBuildingSubPart subPart = rayHit.collider.gameObject.GetComponent<PlayerBuildingSubPart>();

                            if (subPart != null)
                            {
                                m_lastHitBuildingPart = subPart.m_parent;
                            }
                        }

                        if (m_lastHitBuildingPart == null)
                        {
                            outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                            outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                        }
                        else
                        {
                            if (m_lastHitBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
                            {
                                outPosition = new Vector3(rayHit.point.x, m_lastHitBuildingPart.transform.position.y + m_currentPreviewScript.getHeightOffset(), rayHit.point.z);
                                outRotation = m_lastHitBuildingPart.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                            }
                            else
                            {
                                outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                                outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                            }
                        }
                    }
                    else
                    {
                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                        outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                    }
                }
                else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Door)
                {
                    RaycastHit rayHit;

                    if (Physics.Raycast(m_playerCamera.transform.position, m_playerCamera.transform.forward, out rayHit, m_maxPlaceDistance, RAY_LAYERMASK_WALL_FLOOR))
                    {
                        m_lastHitBuildingPart = rayHit.collider.gameObject.GetComponent<PlayerConstruction>();

                        if (m_lastHitBuildingPart == null)
                        {
                            PlayerBuildingSubPart subPart = rayHit.collider.gameObject.GetComponent<PlayerBuildingSubPart>();

                            if (subPart != null)
                            {
                                m_lastHitBuildingPart = subPart.m_parent;
                            }
                        }

                        if (m_lastHitBuildingPart == null)
                        {
                            outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                            outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                        }
                        else
                        {
                            if (m_lastHitBuildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
                            {
                                PlayerBuilidingWall wall = m_lastHitBuildingPart as PlayerBuilidingWall;

                                if (wall == null)
                                {
                                    outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                                    outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                                }
                                else
                                {
                                    if (wall.additionSocketOccuipied)
                                    {
                                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                                        outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                                    }
                                    else
                                    {
                                        if (wall.additionalSocketType == PlayerConstruction.BuildingPartType.Door)
                                        {
                                            outPosition = wall.additionSocketPosition;
                                            outRotation = wall.additionSocketRotation;
                                        }
                                        else
                                        {
                                            outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                                            outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                                outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                            }
                        }
                    }
                    else
                    {
                        outPosition = m_playerCamera.transform.position + m_playerCamera.transform.forward * m_maxPlaceDistance + Vector3.up * m_currentPreviewScript.getHeightOffset();
                        outRotation = localPlayer.transform.rotation * Quaternion.Euler(0, m_currentPreviewScript.getRotationOffsetY(), 0);
                    }
                }
                else
                {
                    outPosition = Vector3.zero;
                    outRotation = Quaternion.identity;
                }
            }
            else
            {
                outPosition = Vector3.zero;
                outRotation = Quaternion.identity;
            }
        }
        else
        {
            outPosition = Vector3.zero;
            outRotation = Quaternion.identity;
        }
    }

    private bool client_checkPreviewPositionValid(Vector3 position, Quaternion rotation)
    {
        if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Foundation)
        {
            Vector3 lowestPoint;
            Vector3 highestPoint;

            m_currentPreviewScript.rayCheckToGroundTerrain(position, rotation, out lowestPoint, out highestPoint);

            if (Mathf.Abs(highestPoint.y - lowestPoint.y) > m_maxHeighDelta) // too steep
            {
                return false;
            }
            else if (Mathf.Abs(position.y - highestPoint.y) > m_maxHeighDelta || Mathf.Abs(position.y - lowestPoint.y) > m_maxHeighDelta) // desired position too far from ground
            {
                return false;
            }
            else if (m_currentPreviewScript.colliderCheckToOtherObjectsDown(position, rotation)) // object in the way
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Wall)
        {
            if (m_lastHitBuildingPart == null)
            {
                return false;
            }
            else
            {
                if (m_currentPreviewScript.colliderCheckToOtherObjectsForwardBackward(position, rotation)) // object in the way
                {
                    return false;
                }
                else
                {
                    if (m_currentPreviewScript.colliderCheckToOtherObjectsUpWall(position, rotation, PlayerConstruction.EDGE_LENGTH))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Floor)
        {
            if (m_lastHitBuildingPart == null)
            {
                return false;
            }
            else
            {
                if (m_currentPreviewScript.colliderCheckToOtherObjectsUpDown(position, rotation)) // object in the way
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.DoorFrame)
        {
            if (m_lastHitBuildingPart == null)
            {
                return false;
            }
            else
            {
                PlayerBuilidingWall wall = m_lastHitBuildingPart as PlayerBuilidingWall;

                if (wall == null)
                {
                    return false;
                }
                else
                {
                    float maxDelta = PlayerConstruction.EDGE_LENGTH / 2 - m_currentPreviewScript.getInsertSize().x / 2 - m_doorwayMinDistanceToEdge;
                    float deltaX = Vector2.Distance(new Vector2(wall.transform.position.x, wall.transform.position.z), new Vector2(m_currentPreviewScript.transform.position.x, m_currentPreviewScript.transform.position.z));

                    if (deltaX < maxDelta && m_currentPreviewScript.transform.position.y == (wall.transform.position.y + m_currentPreviewScript.getHeightOffset()))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Door)
        {
            if (m_lastHitBuildingPart == null)
            {
                return false;
            }
            else
            {
                PlayerBuilidingWall wall = m_lastHitBuildingPart as PlayerBuilidingWall;

                if (wall == null)
                {
                    return false;
                }
                else
                {
                    if (wall.additionalSocketType == PlayerConstruction.BuildingPartType.Door && !wall.additionSocketOccuipied)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

    protected override void onItemUsagePrimary()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            FirstPersonViewManager.singleton.firstPersonArmsAnimator.SetBool("ItemConstructionTool_build", true);
        }
        m_resetBuildAnimationFrame = Time.frameCount + 2;

        if (m_placeable && m_constructMode)
        {
            if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.DoorFrame)
            {
                Vector3 connectionVec = m_currentPreviewScript.transform.position - m_lastHitBuildingPart.transform.position;

                float angleRight = Vector3.Angle(connectionVec.normalized, m_lastHitBuildingPart.transform.right);
                float angleUp = Vector3.Angle(connectionVec.normalized, m_lastHitBuildingPart.transform.up);

                //Debug.DrawRay(m_lastHitBuildingPart.transform.position, m_lastHitBuildingPart.transform.right, Color.red, 2f);
                //Debug.DrawRay(m_lastHitBuildingPart.transform.position, m_lastHitBuildingPart.transform.up, Color.blue, 2f);
                //Debug.DrawRay(m_currentPreviewScript.transform.position, connectionVec, Color.green, 2f);

                Vector2 insertPosition = new Vector2(Mathf.Cos(angleRight * Mathf.Deg2Rad) * connectionVec.magnitude, Mathf.Cos(angleUp * Mathf.Deg2Rad) * connectionVec.magnitude);

                //Debug.DrawRay(m_lastHitBuildingPart.transform.position, m_lastHitBuildingPart.transform.right * insertPosition.x + m_lastHitBuildingPart.transform.up * insertPosition.y, Color.cyan, 2f);
                //Debug.Log("insertPosition [tool]: " + insertPosition.ToString());

                //PlayerBuildingManager.singleton.server_onPlayerAddAttachmentRequest(m_lastHitBuildingPart.entityUID, m_currentPreviewScript.constructionType, insertPosition);

                NetworkMessage message = getCustomMessageBase();
                message.addIntegerValues((int)CustomMessageContext1.AddBuildingAttachment);

                message.addIntegerValues((int)m_currentPreviewScript.constructionType);
                message.addIntegerValues(m_lastHitBuildingPart.entityUID);

                message.addFloatValues(insertPosition.x);
                message.addFloatValues(insertPosition.y);

                client_sendCustomUDPMessage(message);
            }
            else if (m_currentPreviewScript.constructionType == PlayerConstruction.BuildingPartType.Door)
            {
                PlayerBuilidingWall wall = m_lastHitBuildingPart as PlayerBuilidingWall;

                if (wall != null)
                {
                    NetworkMessage message = getCustomMessageBase();
                    message.addIntegerValues((int)CustomMessageContext1.setAdditionSocket);
                    message.addIntegerValues(wall.entityUID);
                    message.addIntegerValues((int)m_currentPreviewScript.constructionType);

                    client_sendCustomUDPMessage(message);
                }
            }
            else
            {
                NetworkMessage message = getCustomMessageBase();
                message.addIntegerValues((int)CustomMessageContext1.CreateBuildingPart);

                message.addIntegerValues(m_currentPreviewScript.associatedEntityPrefabIndex);

                message.addFloatValues(m_currentPreviewObject.transform.position.x);
                message.addFloatValues(m_currentPreviewObject.transform.position.y);
                message.addFloatValues(m_currentPreviewObject.transform.position.z);

                message.addFloatValues(m_currentPreviewObject.transform.rotation.eulerAngles.x);
                message.addFloatValues(m_currentPreviewObject.transform.rotation.eulerAngles.y);
                message.addFloatValues(m_currentPreviewObject.transform.rotation.eulerAngles.z);

                client_sendCustomUDPMessage(message);
            }
        }
    }

    protected override void onItemUsageSecondary()
    {
        GUIManager.singleton.setBuildingSelectionCircularMenuActivity(true);
    }

    protected override void onItemActivated()
    {
        base.onItemActivated();

        if (m_currentPreviewObject != null)
        {
            m_currentPreviewObject.SetActive(true);
        }

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            FirstPersonViewManager.singleton.firstPersonArmsAnimator.SetBool("ItemConstructionTool", true);
        }
    }

    protected override void onItemDeactivated()
    {
        GUIManager.singleton.setBuildingSelectionCircularMenuActivity(false);

        if (m_currentPreviewObject != null && m_currentPreviewObject.activeSelf)
        {
            m_currentPreviewObject.SetActive(false);
            m_currentPreviewObject.transform.position = Vector3.zero;
        }
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext1.CreateBuildingPart:
                {
                    Debug.LogWarning("TODO: message.checkInputCorrectness() ! check free space ! check player has building privileg !");

                    PlayerBuildingManager.singleton.server_onPlayerBuildRequest(message.getIntValue(2),
                                                                                new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2)),
                                                                                Quaternion.Euler(message.getFloatValue(3), message.getFloatValue(4), message.getFloatValue(5)),
                                                                                m_carrierPlayer.m_gameID);

                    return true;
                }
            case (int)CustomMessageContext1.AddBuildingAttachment:
                {
                    Debug.LogWarning("TODO: message.checkInputCorrectness() ! check free space ! check player has building privileg !");

                    PlayerBuildingManager.singleton.server_onPlayerAddAttachmentRequest(message.getIntValue(3), (PlayerConstruction.BuildingPartType)message.getIntValue(2), new Vector2(message.getFloatValue(0), message.getFloatValue(1)));

                    return true;
                }
            case (int)CustomMessageContext1.setAdditionSocket:
                {
                    Debug.LogWarning("TODO: message.checkInputCorrectness() ! check free space ! check player has building privileg !");

                    if (System.Enum.IsDefined(typeof(PlayerConstruction.BuildingPartType), message.getIntValue(3)))
                    {
                        PlayerBuildingManager.singleton.server_onPlayerSetAdditionAttachment(message.getIntValue(2), NetworkingManager.singleton.server_getPlayerGameIDForIPEndpoint(message.iPEndPoint), (PlayerConstruction.BuildingPartType)message.getIntValue(3));
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        Debug.Log("client_receivedCustomNetworkMessage: " + message.getStringValue(0));

        return false;
    }

}
