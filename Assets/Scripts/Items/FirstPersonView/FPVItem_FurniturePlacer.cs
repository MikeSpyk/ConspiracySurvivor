using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_FurniturePlacer : FPVItem_Base
{
    private static readonly int LAYERMASK_NORMAL = System.BitConverter.ToInt32(new byte[] {
                                                                                                BitByteTools.getByte(true,false,false,false,false,false,false,false), // default layer
                                                                                                BitByteTools.getByte(true,false,true,true,true,true,true,true), // no bulding preview
                                                                                                BitByteTools.getByte(true,true,true,true,false,true,true,true), // no player
                                                                                                byte.MaxValue
                                                                                                }, 0); // everything but terrain

    [SerializeField] private bool DEBUG_mode = false;
    [SerializeField] private bool DEBUG_noTransformUpdate = false;
    [SerializeField] private bool DEBUG_clientNoOccupiedCheck = false;

    private BuildingSystemPreviewFurniture m_currentFurniturePreview = null;
    private bool m_positionValid = false; // can object get placed or is its space occupied

    protected void Update()
    {
        base.Update();

        if (GameManager_Custom.singleton.isClient || (GameManager_Custom.singleton.isServerAndClient && m_carrierPlayer.m_gameID == -1)) // local player update
        {
            Player_local localPlayer = EntityManager.singleton.getLocalPlayer();

            if (localPlayer != null)
            {
                RaycastHit hit;

                int previewIndex = localPlayer.getCurrentHotBarItem().itemTemplateIndex;

                BuildingSystemPreviewFurniture preview = BuildingSystemPreviewFurniture.getScriptForID(previewIndex); // if this fails you may have forgotten to add the corresponding gameobject in the scene or the index

                preview.gameObject.SetActive(true);

                if (m_currentFurniturePreview != null && m_currentFurniturePreview != preview)
                {
                    m_currentFurniturePreview.transform.position = Vector3.zero;
                    m_currentFurniturePreview.gameObject.SetActive(false);
                }
                m_currentFurniturePreview = preview;

                if (Physics.Raycast(CameraStack.position, CameraStack.m_singleton.transform.forward, out hit, m_currentFurniturePreview.placeDistance, LAYERMASK_NORMAL))
                {
                    if (DEBUG_mode)
                    {
                        Debug.DrawRay(hit.point, Vector3.up, Color.red, 3f);
                    }

                    if (!DEBUG_noTransformUpdate)
                    {
                        preview.transform.position = hit.point + localPlayer.transform.rotation * preview.offset;
                        preview.transform.rotation = localPlayer.transform.rotation;
                    }

                    if (DEBUG_clientNoOccupiedCheck)
                    {
                        m_positionValid = true;
                    }
                    else if (preview.checkInCollision())
                    {
                        m_positionValid = false;
                    }
                    else
                    {
                        m_positionValid = true;
                    }
                }
                else
                {
                    // in air

                    if (!DEBUG_noTransformUpdate)
                    {
                        preview.transform.position = CameraStack.position + CameraStack.m_singleton.transform.forward * m_currentFurniturePreview.placeDistance + localPlayer.transform.rotation * preview.offset;
                        preview.transform.rotation = localPlayer.transform.rotation;
                    }

                    if (DEBUG_clientNoOccupiedCheck)
                    {
                        m_positionValid = true;
                    }
                    else
                    {
                        m_positionValid = false;
                    }
                }
            }
            else
            {
                if (m_currentFurniturePreview != null)
                {
                    m_currentFurniturePreview.transform.position = Vector3.zero;
                    m_currentFurniturePreview.gameObject.SetActive(false);
                }

                m_currentFurniturePreview = null;
            }
        }

        if (DEBUG_mode)
        {
            if (m_positionValid)
            {
                Debug.Log("FPVItem_FurniturePlacer: Update: m_positionValid = true");
            }
            else
            {
                Debug.Log("FPVItem_FurniturePlacer: Update: m_positionValid = false");
            }
        }
    }

    protected void OnDestroy()
    {
        if (m_currentFurniturePreview != null)
        {
            m_currentFurniturePreview.transform.position = Vector3.zero;
            m_currentFurniturePreview.gameObject.SetActive(false);
            m_currentFurniturePreview = null;
        }

        base.OnDestroy();
    }

    protected override void onItemDeactivated()
    {
        base.onItemDeactivated();

        if (m_currentFurniturePreview != null)
        {
            m_currentFurniturePreview.transform.position = Vector3.zero;
            m_currentFurniturePreview.gameObject.SetActive(false);
        }

        m_currentFurniturePreview = null;
    }

    protected override void onItemUsagePrimary()
    {
        if (m_positionValid)
        {
            client_sendBuildRequest();
        }
    }

    private void client_sendBuildRequest()
    {
        if (m_currentFurniturePreview != null && m_positionValid)
        {
            NetworkMessage message = getCustomMessageBase();

            message.addIntegerValues((int)CustomMessageContext1.CreateFurniture);

            message.addIntegerValues(m_currentFurniturePreview.furnitureItemID);

            message.addFloatValues(m_currentFurniturePreview.transform.position.x);
            message.addFloatValues(m_currentFurniturePreview.transform.position.y);
            message.addFloatValues(m_currentFurniturePreview.transform.position.z);

            message.addFloatValues(m_currentFurniturePreview.transform.rotation.eulerAngles.x);
            message.addFloatValues(m_currentFurniturePreview.transform.rotation.eulerAngles.y);
            message.addFloatValues(m_currentFurniturePreview.transform.rotation.eulerAngles.z);

            client_sendCustomUDPMessage(message);
        }
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext1.CreateFurniture:
                {
                    if (m_carrierPlayer != null && m_carrierPlayer.getCurrentHotBarItem() != null && m_carrierPlayer.getCurrentHotBarItem().itemTemplateIndex == message.getIntValue(2))
                    {
                        BuildingSystemPreviewFurniture associatedPreview = BuildingSystemPreviewFurniture.getScriptForID(message.getIntValue(2));

                        if (associatedPreview != null)
                        {
                            Vector3 desiredPosition = new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2));
                            Player_base associatedPlayer = EntityManager.singleton.getActivePlayer(NetworkingManager.singleton.server_getPlayerGameIDForIPEndpoint(message.iPEndPoint));

                            if (associatedPlayer != null && Vector3.Distance(associatedPlayer.transform.position + GameManager_Custom.singleton.getPlayerLocalCameraOffset(), desiredPosition) < associatedPreview.placeDistance)
                            {
                                Quaternion desiredRotation = Quaternion.Euler(message.getFloatValue(3), message.getFloatValue(4), message.getFloatValue(5));

                                associatedPreview.transform.position = desiredPosition;
                                associatedPreview.transform.rotation = desiredRotation;

                                bool serverAndClient_previewActive = false; // checkInCollision and getConnectedObjectBelow will enable object if disabled

                                if (GameManager_Custom.singleton.isServerAndClient)
                                {
                                    serverAndClient_previewActive = associatedPreview.gameObject.activeSelf;
                                }

                                if (PlayerBuildingManager.singleton.checkPlayerBuildingAllowed(associatedPlayer))
                                {
                                    if (!associatedPreview.checkInCollision())
                                    {
                                        RaycastHit belowHit;

                                        if (associatedPreview.getConnectedObjectBelow(out belowHit))
                                        {
                                            associatedPreview.server_spawnAssociatedEntity(associatedPlayer);

                                            m_carrierPlayer.server_removeItem(GUIRaycastIdentifier.Type.PlayerHotbar, hotbarIndex);
                                        }
                                    }
                                    if (GameManager_Custom.singleton.isServerAndClient)
                                    {
                                        if (associatedPreview.gameObject.activeSelf != serverAndClient_previewActive)
                                        {
                                            associatedPreview.gameObject.SetActive(serverAndClient_previewActive);
                                        }
                                    }
                                }
                            }
                        }
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
