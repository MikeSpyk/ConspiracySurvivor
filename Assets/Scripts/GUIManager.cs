using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Text;
using System.Xml;
using System.IO;

public class GUIManager : MonoBehaviour
{
    public static GUIManager singleton = null;

    [Header("GUI Element References")]
    [SerializeField] private GameObject m_GUI_Canvas;
    [SerializeField] private GameObject m_GameObject_EventSystem;
    [SerializeField] private GameObject m_GUI_Panel_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Main_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Server_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Credits_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Options_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Create_Server_MainMenu_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Loading_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Ingame_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Ingame_BuildingSelectionRef;
    [SerializeField] private GameObject m_GUI_Panel_Inventory_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Inventory_PlayerStorage_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Inventory_Hotbar_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Inventory_ItemDescription_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Inventory_Container_Ref;
    [SerializeField] private GameObject m_GUI_Panel_DeadScreen_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Asleep_Ref;
    [SerializeField] private GameObject m_GUI_Panel_Disconnected_Ref;
    [SerializeField] private GameObject m_GUI_Text_IP_Adress_direct_Connect;
    [SerializeField] private GameObject m_GUI_Text_Port_direct_Connect;
    [SerializeField] private GameObject m_GUI_Text_Port_client_direct_Connect;
    [SerializeField] private GameObject m_GUI_Text_Port_Create_Server;
    [SerializeField] private GameObject m_GUI_Text_Seed_Create_Server;
    [SerializeField] private GameObject m_GUI_Text_Size_Create_Server;
    [SerializeField] private GameObject m_GUI_Text_Stats_Ingame;
    [SerializeField] private GameObject m_GUI_Text_Inventory_ItemDescription;
    [SerializeField] private GameObject m_GUI_Text_debug_info;
    [SerializeField] private GameObject m_GUI_Text_Progress_Loading;
    [SerializeField] private GameObject m_GUI_Text_Disconnected_info;
    [SerializeField] private GameObject m_GUI_Text_ViewDistanceValue_Options;
    [SerializeField] private GameObject m_GUI_Inputfield_Seed_Create_Server;
    [SerializeField] private GameObject m_GUI_Dropdown_Vsync_Ref;
    [SerializeField] private GameObject m_GUI_Dropdown_Debug_Messages_Ref;
    [SerializeField] private GameObject m_GUI_Dropdown_Shadow_Resolution_Ref;
    [SerializeField] private GameObject m_GUI_Dropdown_Tree_distance_Ref;
    [SerializeField] private GameObject m_GUI_Dropdown_Water_Quality_Ref;
    [SerializeField] private GameObject m_GUI_Slider_Master_Volume_Ref;
    [SerializeField] private GameObject m_GUI_Slider_ViewDistance_Ref;
    [SerializeField] private GameObject m_GUI_Image_playerHealthBarRef;
    [SerializeField] private Image m_GUI_Image_playerWaterBar;
    [SerializeField] private Image m_GUI_Image_playerFoodBar;
    [SerializeField] private Text m_GUI_Text_playerWater;
    [SerializeField] private Text m_GUI_Text_playerFood;
    [SerializeField] private Text m_GUI_Text_playerHunger;
    [SerializeField] private Text m_GUI_Text_playerThirst;
    [SerializeField] private GameObject m_GUI_Image_playerInventoryDragDropRef;
    [SerializeField] private Image m_GUI_Image_Ingame_PlayerHurtOverlay;
    [SerializeField] private GameObject m_GUI_Ingame_HitmarkerRef;
    [SerializeField] private GameObject m_GUI_Text_playerHealthRef;
    [SerializeField] private GameObject m_GUI_Text_Interactable;
    [SerializeField] private GameObject m_GUI_toggle_ServerAndClient;
    [SerializeField] private Dropdown m_GUI_Dropdown_multiple_Cameras;
    [SerializeField] private Image m_GUI_Image_Crosshair;
    [SerializeField] private CircularMenu m_GUI_BuildingCircularMenu;
    [SerializeField] private Text m_GUI_Text_CreditsNames_Credits;
    [SerializeField] private Text m_GUI_Text_IngameName;
    [SerializeField] private Text m_GUI_Text_Username;
    [SerializeField] private Text m_GUI_Text_password;
    [SerializeField] private Button m_GUI_Button_SpawnSavedLocation;
    [SerializeField] private Text m_GUI_Text_SpawnCooldown;
    [SerializeField] private Toggle m_GUI_toogle_loadSaveFile;
    [SerializeField] private Text m_GUI_Text_TimeUntilAllEnd;
    [SerializeField] private GameObject m_GUI_Panel_Catastrophes;
    [SerializeField] private GameObject m_GUI_ScrollContent_CatEffects;
    [SerializeField] private GameObject m_GUI_ScrollContent_ActiveEvents;
    [SerializeField] private GameObject m_GUI_ScrollContent_Indications;
    [Header("Settings")]
    [SerializeField] private TextAsset m_creditsXMLFile;
    [SerializeField] private Sprite[] m_ItemIcons;
    [SerializeField] private bool m_showDebugMessages = false;
    [SerializeField] private int maxDisplayMessages = 32;
    [SerializeField] private float m_hitmarkerTime = 1;
    [SerializeField] private Color m_itemSlotDefaultColor = Color.white;
    [SerializeField] private Color m_itemSlotSelectedColor = Color.black;
    [SerializeField] private Color m_itemSlotHotbarActiveColor = Color.grey;
    [SerializeField] private float m_hurtOverlayFadeTime = 2f;
    [SerializeField] private float m_hurtOverlayMaxAlpha = 60f;

    private Image m_hitmarkerImage;
    private Image m_playerHealthBarImage;
    private Text m_playerHealthText;
    private Text m_playerInteractableText;
    private Text m_stats_text;
    private LootContainerGame m_playerOpenContainerGame = null;
    private LootContainer m_playerInventoryContainer;
    private LootContainer m_playerHotbarContainer;
    private LootContainer m_playerOpenContainer;
    private StorableItem[] m_playerInventoryItems;
    private StorableItem[] m_playerHotbarItems;
    private StorableItem[] m_playerContainerItems;
    private int m_inventoryExternalContainerSize = 30;
    private Image[] m_playerInventoryItemsIcons;
    private Image[] m_playerInventorySlotImages;
    private Text[] m_playerInventoryItemsStackText;
    private Image[] m_playerHotbarItemsIcons;
    private Image[] m_playerHotbarSlotImages;
    private Text[] m_playerHotbarItemsStackText;
    private Image[] m_playerContainerItemsIcons;
    private Image[] m_playerContainerSlotImages;
    private Text[] m_playerContainerItemsStackText;
    private Image m_playerInventoryDragDropImage;
    private Vector3 m_playerInventoryStartClickPos;
    private bool m_playerInventoryDragDropActive = false;
    private Image m_playerInventoryLastClickedItemSlot = null;

    private int m_playerInventoryPickedUpItemIndex = -1;
    private GUIRaycastIdentifier.Type m_playerInventoryPickedUpItemType = GUIRaycastIdentifier.Type.Default;

    private GraphicRaycaster m_Raycaster;
    private EventSystem m_EventSystem;

    private bool m_inventoryClickedLastFrame = false;
    private bool m_isInventory = false;
    private bool m_isIngame = false; // as opposed to is in menu
    private bool m_coursorActive = true;
    public bool coursorActive
    {
        get
        {
            return m_coursorActive;
        }
    }
    private bool m_showStatsIngame = true;
    private float m_lastFrameStartTime = 0;
    private float m_lastSecoundStartTime = 0;
    private int m_lastSecoundFrameCounter = 0;
    private float m_lastSecoundMaxFrameTime = 0;
    private float m_lastSecoundAverageFrameTime = 0;
    private float m_lastTimeShowHitmarker = 0;

    private long m_memoryInUseGCMb = 0;
    private StringBuilder m_statsStrBuilder = new StringBuilder();
    private int m_stats_packageCounter = 0;
    private int m_stats_receivedBytes = 0;
    private int m_selectedHotbarIndex = -1;

    private float m_hurtOverlayFadeStart = 0;
    private bool m_hurtOverlayActive = false;

    void Awake()
    {
        singleton = this;

        m_Raycaster = m_GUI_Canvas.GetComponent<GraphicRaycaster>();
        m_EventSystem = m_GameObject_EventSystem.GetComponent<EventSystem>();
        m_stats_text = m_GUI_Text_Stats_Ingame.GetComponent<Text>();
    }

    // Use this for initialization
    void Start()
    {
        initializePlayerInventoryGUI();

        m_GUI_Button_SpawnSavedLocation.gameObject.SetActive(false);

        m_hitmarkerImage = m_GUI_Ingame_HitmarkerRef.GetComponent<Image>();
        m_playerHealthBarImage = m_GUI_Image_playerHealthBarRef.GetComponent<Image>();
        m_playerHealthText = m_GUI_Text_playerHealthRef.GetComponent<Text>();
        m_playerInteractableText = m_GUI_Text_Interactable.GetComponent<Text>();

        m_playerInventoryDragDropImage = m_GUI_Image_playerInventoryDragDropRef.GetComponent<Image>();

        m_playerInventoryDragDropImage.color = new Color(1, 1, 1, 0);

        m_GUI_Panel_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(true);

        m_GUI_Dropdown_Vsync_Ref.GetComponent<Dropdown>().value = QualitySettings.vSyncCount;

        m_GUI_Slider_Master_Volume_Ref.GetComponent<Slider>().onValueChanged.AddListener(delegate
        {
            On_master_volume_Slider_changed(m_GUI_Slider_Master_Volume_Ref.GetComponent<Slider>().value);
        });

        m_GUI_Slider_ViewDistance_Ref.GetComponent<Slider>().onValueChanged.AddListener(delegate
        {
            On_ViewDistance_Slider_changed(m_GUI_Slider_ViewDistance_Ref.GetComponent<Slider>().value);
        });

        m_GUI_Dropdown_Water_Quality_Ref.GetComponent<Dropdown>().onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_Water_Quality(m_GUI_Dropdown_Water_Quality_Ref.GetComponent<Dropdown>().value);
        });

        m_GUI_Dropdown_Tree_distance_Ref.GetComponent<Dropdown>().onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_Tree_distance(m_GUI_Dropdown_Tree_distance_Ref.GetComponent<Dropdown>().value);
        });

        m_GUI_Dropdown_Shadow_Resolution_Ref.GetComponent<Dropdown>().onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_Shadow_Resolution(m_GUI_Dropdown_Shadow_Resolution_Ref.GetComponent<Dropdown>().value);
        });

        m_GUI_Dropdown_Vsync_Ref.GetComponent<Dropdown>().onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_Vsync(m_GUI_Dropdown_Vsync_Ref.GetComponent<Dropdown>().value);
        });

        m_GUI_Dropdown_multiple_Cameras.onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_multiple_cameras(m_GUI_Dropdown_multiple_Cameras.value);
        });

        if (m_showDebugMessages)
        {
            m_GUI_Dropdown_Debug_Messages_Ref.GetComponent<Dropdown>().value = 1;
        }
        else
        {
            m_GUI_Dropdown_Debug_Messages_Ref.GetComponent<Dropdown>().value = 0;
        }
        m_GUI_Dropdown_Debug_Messages_Ref.GetComponent<Dropdown>().onValueChanged.AddListener(delegate
        {
            On_GUI_Dropdown_changed_Debug_Messages(m_GUI_Dropdown_Debug_Messages_Ref.GetComponent<Dropdown>().value);
        });

        setInteractableText("");
        loadCreditsFromXML(m_creditsXMLFile.text);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_showStatsIngame)
        {
            ingameStatsManaging();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager_Custom.singleton.isGameInitialized)
            {
                switchGUI_MainMenu_Ingame();
            }
        }

        if (m_isInventory)
        {
            m_GUI_Image_playerInventoryDragDropRef.transform.position = Input.mousePosition;
        }

        if (m_isInventory && Input.GetKey(KeyCode.Mouse0))
        {
            GUIRaycastIdentifier identifier = getCurrentGUIIdentifierMouse();

            if (identifier != null)
            {
                //Debug.Log("Hit " + identifier.name);

                if (m_inventoryClickedLastFrame)
                {
                    onInventoryGUIContinueClick(identifier);
                }
                else
                {
                    onInventoryGUIStartClick(identifier);
                }
            }

            m_inventoryClickedLastFrame = true;
        }
        else if (m_inventoryClickedLastFrame)
        {
            m_inventoryClickedLastFrame = false;
            onInventoryGUIClickEnded();
        }

        if (m_isInventory && Input.GetKeyUp(KeyCode.Mouse1))
        {
            GUIRaycastIdentifier identifier = getCurrentGUIIdentifierMouse();

            if (identifier != null)
            {
                onInventoryGUIStartRightClick(identifier);
            }
        }

        hitmarkerManagement();
        spawnpointUpdate();

        // sometimes lockstate is lost for some reason (editor and build)
        if (!m_coursorActive)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if(m_hurtOverlayActive)
        {
            Color currentColor = m_GUI_Image_Ingame_PlayerHurtOverlay.color;

            float alpha = (1f- (Time.time - m_hurtOverlayFadeStart) / m_hurtOverlayFadeTime) * m_hurtOverlayMaxAlpha;

            m_GUI_Image_Ingame_PlayerHurtOverlay.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);

            if(Time.time > m_hurtOverlayFadeStart + m_hurtOverlayFadeTime)
            {
                m_hurtOverlayActive = false;
                m_GUI_Image_Ingame_PlayerHurtOverlay.gameObject.SetActive(false);
            }
        }
    }

    private void spawnpointUpdate()
    {
        if (m_GUI_Button_SpawnSavedLocation.gameObject.activeSelf)
        {
            ClientUserData userData = NetworkingManager.singleton.client_getLocalClientUserData();

            float cooldown = userData.client_getSpawnpointCooldownIndex(0);

            if (cooldown < 0)
            {
                m_GUI_Text_SpawnCooldown.text = "";
                m_GUI_Button_SpawnSavedLocation.interactable = true;
            }
            else
            {
                m_GUI_Text_SpawnCooldown.text = ((int)cooldown).ToString();
                m_GUI_Button_SpawnSavedLocation.interactable = false;
            }
        }
        else
        {
            m_GUI_Text_SpawnCooldown.text = "";
        }
    }

    private void loadCreditsFromXML(string xmlFile)
    {
        string outputText = "";

        XmlDocument inputFile = null;
        XmlNamespaceManager namespaceManager = null;

        try
        {
            if (xmlFile == null || xmlFile == "")
            {
                Debug.LogWarning("GUIManager: could not load credits-XML-File: File is null or empty");
                return;
            }

            inputFile = new XmlDocument();
            inputFile.LoadXml(xmlFile);
            namespaceManager = new XmlNamespaceManager(inputFile.NameTable);
            namespaceManager.AddNamespace("ehd", "urn:ehd/001");

            try
            {
                XmlNodeList categoriesNodes = inputFile.SelectNodes("//ehd:Category", namespaceManager);

                foreach (XmlNode categoryNode in categoriesNodes)
                {
                    if (categoryNode.Attributes["Text"] != null)
                    {
                        if (categoryNode.Attributes["Text"].Value.Equals("Developers"))
                        {
                            outputText += "Developers\n";

                            XmlNodeList devsNodes = categoryNode.SelectNodes("//ehd:Developer", namespaceManager);

                            foreach (XmlNode dev in devsNodes)
                            {
                                outputText += dev.Attributes["Name"].Value + ": " + dev.Attributes["Roles"].Value + "\n";
                            }
                        }

                        if (categoryNode.Attributes["Text"].Value.Equals("3rd-Party"))
                        {
                            outputText += "\n3rd-Party Assets\n";

                            XmlNodeList assetNodes = categoryNode.SelectNodes("//ehd:Asset", namespaceManager);

                            foreach (XmlNode asset in assetNodes)
                            {
                                if (!string.IsNullOrEmpty(asset.Attributes["AssetName"].Value))
                                {
                                    outputText += asset.Attributes["AssetName"].Value + " by " + asset.Attributes["Creator"].Value + "\n";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("GUIManager: error while loading credits: " + ex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GUIManager: could not load credits-XML-File: " + ex);
            return;
        }

        m_GUI_Text_CreditsNames_Credits.text = outputText;
    }

    private GUIRaycastIdentifier getCurrentGUIIdentifierMouse()
    {
        //Set up the new Pointer Event
        PointerEventData m_PointerEventData = new PointerEventData(m_EventSystem);
        //Set the Pointer Event Position to that of the mouse position
        m_PointerEventData.position = Input.mousePosition;

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();

        //Raycast using the Graphics Raycaster and mouse click position
        m_Raycaster.Raycast(m_PointerEventData, results);

        //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
        foreach (RaycastResult result in results)
        {
            GUIRaycastIdentifier identifier = result.gameObject.GetComponent<GUIRaycastIdentifier>();

            if (identifier != null)
            {
                return identifier;
            }
        }

        return null;
    }

    private void onInventoryGUIStartClick(GUIRaycastIdentifier identifier)
    {
        m_playerInventoryPickedUpItemIndex = identifier.getIndex();
        m_playerInventoryPickedUpItemType = identifier.getType();

        m_playerInventoryStartClickPos = Input.mousePosition;

        // drag and drop icon

    }

    private void onInventoryGUIStartRightClick(GUIRaycastIdentifier identifier)
    {
        m_playerInventoryPickedUpItemIndex = identifier.getIndex();
        m_playerInventoryPickedUpItemType = identifier.getType();

        m_playerInventoryStartClickPos = Input.mousePosition;

        if (GameManager_Custom.singleton.isGameInitialized)
        {
            NetworkingManager.singleton.client_sendItemSplitRequest(GUIRaycastIdentifier.Type.PlayerInventory, m_playerInventoryPickedUpItemIndex, -1);
        }
    }

    private void onInventoryGUIContinueClick(GUIRaycastIdentifier identifier)
    {
        if (!m_playerInventoryDragDropActive)
        {
            if (m_playerInventoryStartClickPos != Input.mousePosition)
            {
                showInventoryDragDropIcon();
            }
        }
    }

    private void onInventoryGUIClickEnded()
    {
        hideInventoryDragDropIcon();
        GUIRaycastIdentifier CurrentIdentifier = getCurrentGUIIdentifierMouse();

        if (CurrentIdentifier != null && CurrentIdentifier.getType() == m_playerInventoryPickedUpItemType && CurrentIdentifier.getIndex() == m_playerInventoryPickedUpItemIndex) // clicked item
        {
            if (m_playerInventoryLastClickedItemSlot != null) // before setting
            {
                if (checkIfGUIItemIsActiveItemHotbar(m_playerInventoryLastClickedItemSlot))
                {
                    m_playerInventoryLastClickedItemSlot.color = m_itemSlotHotbarActiveColor;
                }
                else
                {
                    m_playerInventoryLastClickedItemSlot.color = m_itemSlotDefaultColor;
                }
            }
            m_playerInventoryLastClickedItemSlot = null;

            if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerHotbar)
            {
                if (m_playerHotbarItems[m_playerInventoryPickedUpItemIndex] != null)
                {
                    m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = m_playerHotbarItems[m_playerInventoryPickedUpItemIndex].displayName + ": \n" + m_playerHotbarItems[m_playerInventoryPickedUpItemIndex].description;
                }
                else
                {
                    m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = "";
                }
                m_playerInventoryLastClickedItemSlot = m_playerHotbarSlotImages[m_playerInventoryPickedUpItemIndex];
            }
            else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerInventory)
            {
                if (m_playerInventoryItems[m_playerInventoryPickedUpItemIndex] != null)
                {
                    m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = m_playerInventoryItems[m_playerInventoryPickedUpItemIndex].displayName + ": \n" + m_playerInventoryItems[m_playerInventoryPickedUpItemIndex].description;
                }
                else
                {
                    m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = "";
                }
                m_playerInventoryLastClickedItemSlot = m_playerInventorySlotImages[m_playerInventoryPickedUpItemIndex];
            }
            else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerLootContainer)
            {
                if (m_playerContainerItems != null) // open container and items might be lost
                {
                    if (m_playerContainerItems[m_playerInventoryPickedUpItemIndex] != null)
                    {
                        m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = m_playerContainerItems[m_playerInventoryPickedUpItemIndex].displayName + ": \n" + m_playerContainerItems[m_playerInventoryPickedUpItemIndex].description;
                    }
                    else
                    {
                        m_GUI_Text_Inventory_ItemDescription.GetComponent<Text>().text = "";
                    }
                    m_playerInventoryLastClickedItemSlot = m_playerContainerSlotImages[m_playerInventoryPickedUpItemIndex];
                }
            }

            if (m_playerInventoryLastClickedItemSlot != null) // after setting
            {
                m_playerInventoryLastClickedItemSlot.color = m_itemSlotSelectedColor;
            }
        }
        else // moved Item
        {
            GUIRaycastIdentifier releasedSlotIdentifier = getCurrentGUIIdentifierMouse();

            if (releasedSlotIdentifier == null)
            {
                if (GameManager_Custom.singleton.isGameInitialized)
                {
                    // drop item
                    if (m_playerOpenContainerGame == null)
                    {
                        NetworkingManager.singleton.client_sendEntityDropRequest(m_playerInventoryPickedUpItemType, m_playerInventoryPickedUpItemIndex, -1);
                    }
                    else
                    {
                        NetworkingManager.singleton.client_sendEntityDropRequest(m_playerInventoryPickedUpItemType, m_playerInventoryPickedUpItemIndex, m_playerOpenContainerGame.getEntityUID());
                    }
                }
            }
            else
            {
                if (GameManager_Custom.singleton.isGameInitialized)
                {
                    if (releasedSlotIdentifier.getType() == GUIRaycastIdentifier.Type.PlayerLootContainer && releasedSlotIdentifier.getIndex() > m_inventoryExternalContainerSize - 1)
                    {
                        return; // dont send move item request if the item-slot isnt even visible
                    }

                    GUIRaycastIdentifier.Type releasedSlotType = releasedSlotIdentifier.getType();

                    if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerHotbar) // picked up type = PlayerHotbar
                    {
                        if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerHotbar)  // drop down type = PlayerHotbar
                        {
                            //m_playerHotbarContainer.switchItem(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex());
                            NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerHotbar, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerHotbar, releasedSlotIdentifier.getIndex(), -1);
                        }
                        else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerInventory)
                        {
                            //m_playerHotbarContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerInventoryContainer);
                            NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerHotbar, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerInventory, releasedSlotIdentifier.getIndex(), -1);
                        }
                        else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerLootContainer)
                        {
                            //m_playerHotbarContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerOpenContainer);
                            if (m_playerOpenContainerGame != null)
                            {
                                NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerHotbar, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerLootContainer, releasedSlotIdentifier.getIndex(), m_playerOpenContainerGame.getEntityUID());
                            }
                        }
                    }
                    else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerInventory) // picked up type = PlayerInventory
                    {
                        if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerHotbar)
                        {
                            //m_playerInventoryContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerHotbarContainer);
                            NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerInventory, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerHotbar, releasedSlotIdentifier.getIndex(), -1);
                        }
                        else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerInventory)
                        {
                            //m_playerInventoryContainer.switchItem(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex());
                            NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerInventory, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerInventory, releasedSlotIdentifier.getIndex(), -1);
                        }
                        else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerLootContainer)
                        {
                            //m_playerInventoryContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerOpenContainer);
                            if (m_playerOpenContainerGame != null)
                            {
                                NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerInventory, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerLootContainer, releasedSlotIdentifier.getIndex(), m_playerOpenContainerGame.getEntityUID());
                            }
                        }
                    }
                    else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerLootContainer) // picked up type = PlayerInventory
                    {
                        if (m_playerOpenContainer != null && m_playerOpenContainerGame != null) // open container might be lost
                        {
                            if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerHotbar)
                            {
                                //m_playerOpenContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerHotbarContainer);
                                NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerLootContainer, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerHotbar, releasedSlotIdentifier.getIndex(), m_playerOpenContainerGame.getEntityUID());
                            }
                            else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerInventory)
                            {
                                //m_playerOpenContainer.switchItemContainer(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex(), m_playerInventoryContainer);
                                NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerLootContainer, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerInventory, releasedSlotIdentifier.getIndex(), m_playerOpenContainerGame.getEntityUID());
                            }
                            else if (releasedSlotType == GUIRaycastIdentifier.Type.PlayerLootContainer)
                            {
                                //m_playerOpenContainer.switchItem(m_playerInventoryPickedUpItemIndex, releasedSlotIdentifier.getIndex());
                                NetworkingManager.singleton.client_sendItemSwitchRequest(GUIRaycastIdentifier.Type.PlayerLootContainer, m_playerInventoryPickedUpItemIndex, GUIRaycastIdentifier.Type.PlayerLootContainer, releasedSlotIdentifier.getIndex(), m_playerOpenContainerGame.getEntityUID());
                            }
                        }
                    }
                }
            }
        }
    }

    private void showInventoryDragDropIcon()
    {
        if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerHotbar)
        {
            if (m_playerHotbarItems[m_playerInventoryPickedUpItemIndex] != null)
            {
                m_playerInventoryDragDropImage.sprite = m_ItemIcons[m_playerHotbarItems[m_playerInventoryPickedUpItemIndex].GUIIconIndex];
                m_playerInventoryDragDropImage.color = new Color(1, 1, 1, 1);
                m_playerInventoryDragDropActive = true;
            }
        }
        else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerInventory)
        {
            if (m_playerInventoryItems[m_playerInventoryPickedUpItemIndex] != null)
            {
                m_playerInventoryDragDropImage.sprite = m_ItemIcons[m_playerInventoryItems[m_playerInventoryPickedUpItemIndex].GUIIconIndex];
                m_playerInventoryDragDropImage.color = new Color(1, 1, 1, 1);
                m_playerInventoryDragDropActive = true;
            }
        }
        else if (m_playerInventoryPickedUpItemType == GUIRaycastIdentifier.Type.PlayerLootContainer)
        {
            if (m_playerContainerItems != null)
            {
                if (m_playerContainerItems[m_playerInventoryPickedUpItemIndex] != null)
                {
                    m_playerInventoryDragDropImage.sprite = m_ItemIcons[m_playerContainerItems[m_playerInventoryPickedUpItemIndex].GUIIconIndex];
                    m_playerInventoryDragDropImage.color = new Color(1, 1, 1, 1);
                    m_playerInventoryDragDropActive = true;
                }
            }
        }
    }

    private void hideInventoryDragDropIcon()
    {
        m_playerInventoryDragDropImage.color = new Color(1, 1, 1, 0); // hide drag and drop item
        m_playerInventoryDragDropActive = false;
    }

    private void initializePlayerInventoryGUI()
    {
        m_playerInventoryItemsIcons = new Image[m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.childCount];
        m_playerInventorySlotImages = new Image[m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.childCount];
        m_playerInventoryItemsStackText = new Text[m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.childCount];

        m_playerHotbarItemsIcons = new Image[m_GUI_Panel_Inventory_Hotbar_Ref.transform.childCount];
        m_playerHotbarSlotImages = new Image[m_GUI_Panel_Inventory_Hotbar_Ref.transform.childCount];
        m_playerHotbarItemsStackText = new Text[m_GUI_Panel_Inventory_Hotbar_Ref.transform.childCount];

        m_playerContainerItemsIcons = new Image[m_GUI_Panel_Inventory_Container_Ref.transform.childCount];
        m_playerContainerSlotImages = new Image[m_GUI_Panel_Inventory_Container_Ref.transform.childCount];
        m_playerContainerItemsStackText = new Text[m_GUI_Panel_Inventory_Container_Ref.transform.childCount];

        for (int i = 0; i < m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.childCount; i++)
        {
            m_playerInventoryItemsIcons[i] = m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            m_playerInventorySlotImages[i] = m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.GetChild(i).GetComponent<Image>();
            m_playerInventoryItemsStackText[i] = m_GUI_Panel_Inventory_PlayerStorage_Ref.transform.GetChild(i).GetChild(1).GetComponent<Text>();
        }

        for (int i = 0; i < m_GUI_Panel_Inventory_Hotbar_Ref.transform.childCount; i++)
        {
            m_playerHotbarItemsIcons[i] = m_GUI_Panel_Inventory_Hotbar_Ref.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            m_playerHotbarItemsIcons[i].color = Color.clear;
            m_playerHotbarSlotImages[i] = m_GUI_Panel_Inventory_Hotbar_Ref.transform.GetChild(i).GetComponent<Image>();
            m_playerHotbarItemsStackText[i] = m_GUI_Panel_Inventory_Hotbar_Ref.transform.GetChild(i).GetChild(1).GetComponent<Text>();
            m_playerHotbarItemsStackText[i].text = "";
        }

        for (int i = 0; i < m_GUI_Panel_Inventory_Container_Ref.transform.childCount; i++)
        {
            m_playerContainerItemsIcons[i] = m_GUI_Panel_Inventory_Container_Ref.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            m_playerContainerSlotImages[i] = m_GUI_Panel_Inventory_Container_Ref.transform.GetChild(i).GetComponent<Image>();
            m_playerContainerItemsStackText[i] = m_GUI_Panel_Inventory_Container_Ref.transform.GetChild(i).GetChild(1).GetComponent<Text>();
        }
    }

    private void onPlayerInventoryItemChangedEvent(object obj, IntegerEventArgs args_index)
    {
        if (m_playerInventoryItems[args_index.integer] == null)
        {
            m_playerInventoryItemsIcons[args_index.integer].color = new Color(1, 1, 1, 0); // invisible
            m_playerInventoryItemsStackText[args_index.integer].text = "";
        }
        else
        {
            int iconIndex = m_playerInventoryItems[args_index.integer].GUIIconIndex;

            if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
            {
                Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                iconIndex = 0;
            }

            m_playerInventoryItemsIcons[args_index.integer].sprite = m_ItemIcons[iconIndex];
            m_playerInventoryItemsIcons[args_index.integer].color = new Color(1, 1, 1, 1); // visible

            if (m_playerInventoryItems[args_index.integer].m_stackSize == 1)
            {
                m_playerInventoryItemsStackText[args_index.integer].text = "";
            }
            else
            {
                m_playerInventoryItemsStackText[args_index.integer].text = "" + m_playerInventoryItems[args_index.integer].m_stackSize;
            }
        }
    }

    private void onPlayerHotbarItemChangedEvent(object obj, IntegerEventArgs args_index)
    {
        if (m_playerHotbarItems[args_index.integer] == null)
        {
            m_playerHotbarItemsIcons[args_index.integer].color = new Color(1, 1, 1, 0); // invisible
            m_playerHotbarItemsStackText[args_index.integer].text = "";
        }
        else
        {
            int iconIndex = m_playerHotbarItems[args_index.integer].GUIIconIndex;

            if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
            {
                Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                iconIndex = 0;
            }

            m_playerHotbarItemsIcons[args_index.integer].sprite = m_ItemIcons[iconIndex];
            m_playerHotbarItemsIcons[args_index.integer].color = new Color(1, 1, 1, 1); // visible

            if (m_playerHotbarItems[args_index.integer].m_stackSize == 1)
            {
                m_playerHotbarItemsStackText[args_index.integer].text = "";
            }
            else
            {
                m_playerHotbarItemsStackText[args_index.integer].text = "" + m_playerHotbarItems[args_index.integer].m_stackSize;
            }
        }
    }

    private void onPlayerContainerItemChangedEvent(object obj, IntegerEventArgs args_index)
    {
        if (m_playerContainerItems[args_index.integer] == null)
        {
            m_playerContainerItemsIcons[args_index.integer].color = new Color(1, 1, 1, 0); // invisible
            m_playerContainerItemsStackText[args_index.integer].text = "";
        }
        else
        {
            int iconIndex = m_playerContainerItems[args_index.integer].GUIIconIndex;

            if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
            {
                Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                iconIndex = 0;
            }

            m_playerContainerItemsIcons[args_index.integer].sprite = m_ItemIcons[iconIndex];
            m_playerContainerItemsIcons[args_index.integer].color = new Color(1, 1, 1, 1); // visible

            if (m_playerContainerItems[args_index.integer].m_stackSize == 1)
            {
                m_playerContainerItemsStackText[args_index.integer].text = "";
            }
            else
            {
                m_playerContainerItemsStackText[args_index.integer].text = "" + m_playerContainerItems[args_index.integer].m_stackSize;
            }
        }
    }

    public void addStatsPackageCounter(int add)
    {
        m_stats_packageCounter += add;
    }

    public void addStatsReceivedBytes(int add)
    {
        m_stats_receivedBytes += add;
    }

    public void reloadPlayerInventoryItems()
    {
        Player_local currentPlayerScript = EntityManager.singleton.getLocalPlayer();
        if (currentPlayerScript != null)
        {
            LootContainer playerInventory = currentPlayerScript.getInventory();
            LootContainer playerHotbar = currentPlayerScript.getHotbar();
            LootContainer openContainer = currentPlayerScript.getOpenLootContainerExternal();
            LootContainerGame openContainerGame = currentPlayerScript.getOpenLootContainerGameExternal();
            setPlayerInventoryContainers(playerInventory, playerHotbar, openContainer, openContainerGame);
        }
    }

    private void setPlayerInventoryContainers(LootContainer playerInventoryContainer, LootContainer playerHotbarContainer, LootContainer playerOpenContainer, LootContainerGame lootContainerGame)
    {
        if (m_playerInventoryContainer != null)
        {
            m_playerInventoryContainer.ItemChangedEvent -= onPlayerInventoryItemChangedEvent; // unsubscibe from old container
        }

        if (m_playerHotbarContainer != null)
        {
            m_playerHotbarContainer.ItemChangedEvent -= onPlayerHotbarItemChangedEvent; // unsubscibe from old container
        }

        if (m_playerOpenContainer != null)
        {
            m_playerOpenContainer.ItemChangedEvent -= onPlayerContainerItemChangedEvent; // unsubscibe from old container
        }

        if (playerInventoryContainer == null)
        {
            Debug.LogError("GUIManager: setPlayerInventoryItems: playerInventoryContainer is null");
        }

        if (playerHotbarContainer == null)
        {
            Debug.LogError("GUIManager: setPlayerInventoryItems: playerHotbarContainer is null");
        }

        m_playerOpenContainerGame = lootContainerGame;

        m_playerInventoryContainer = playerInventoryContainer;
        m_playerInventoryItems = m_playerInventoryContainer.getAllItems();
        playerInventoryContainer.ItemChangedEvent += onPlayerInventoryItemChangedEvent;

        m_playerHotbarContainer = playerHotbarContainer;
        m_playerHotbarItems = m_playerHotbarContainer.getAllItems();
        m_playerHotbarContainer.ItemChangedEvent += onPlayerHotbarItemChangedEvent;

        m_playerOpenContainer = playerOpenContainer;
        if (m_playerOpenContainer == null)
        {
            m_playerContainerItems = null;
        }
        else
        {
            m_playerContainerItems = m_playerOpenContainer.getAllItems();
            m_playerOpenContainer.ItemChangedEvent += onPlayerContainerItemChangedEvent;

            if (m_playerContainerItems.Length > 30)
            {
                Debug.LogError("GUIManager: setPlayerInventoryItems: too many player inventory open-container items committed. Allowed max 30. committed: " + m_playerContainerItems.Length);
                return;
            }
        }

        if (m_playerInventoryItems.Length != 24)
        {
            Debug.LogError("GUIManager: setPlayerInventoryItems: wrong player inventory item count committed");
            return;
        }

        if (m_playerHotbarItems.Length != 6)
        {
            Debug.LogError("GUIManager: setPlayerInventoryItems: wrong player hotbar item count committed");
            return;
        }

        reloadPlayerInventoryGUI();
    }

    private void reloadPlayerInventoryGUI()
    {
        if (m_playerInventoryLastClickedItemSlot != null) // before setting
        {
            if (checkIfGUIItemIsActiveItemHotbar(m_playerInventoryLastClickedItemSlot))
            {
                m_playerInventoryLastClickedItemSlot.color = m_itemSlotHotbarActiveColor;
            }
            else
            {
                m_playerInventoryLastClickedItemSlot.color = m_itemSlotDefaultColor;
            }
        }
        m_playerInventoryLastClickedItemSlot = null;

        if (m_playerInventoryItemsIcons != null && m_playerInventoryItems != null)
        {
            for (int i = 0; i < m_playerInventoryItems.Length; i++)
            {
                if (m_playerInventoryItems[i] == null)
                {
                    m_playerInventoryItemsIcons[i].color = new Color(1, 1, 1, 0); // invisible
                    m_playerInventoryItemsStackText[i].text = "";
                }
                else
                {
                    int iconIndex = m_playerInventoryItems[i].GUIIconIndex;

                    if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
                    {
                        Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                        iconIndex = 0;
                    }

                    m_playerInventoryItemsIcons[i].sprite = m_ItemIcons[iconIndex];
                    m_playerInventoryItemsIcons[i].color = new Color(1, 1, 1, 1); // visible

                    if (m_playerInventoryItems[i].m_stackSize == 1)
                    {
                        m_playerInventoryItemsStackText[i].text = "";
                    }
                    else
                    {
                        m_playerInventoryItemsStackText[i].text = "" + m_playerInventoryItems[i].m_stackSize;
                    }
                }
            }
        }

        if (m_playerHotbarItemsIcons != null && m_playerHotbarItems != null)
        {
            for (int i = 0; i < m_playerHotbarItems.Length; i++)
            {
                if (m_playerHotbarItems[i] == null)
                {
                    m_playerHotbarItemsIcons[i].color = new Color(1, 1, 1, 0); // invisible
                    m_playerHotbarItemsStackText[i].text = "";
                }
                else
                {
                    int iconIndex = m_playerHotbarItems[i].GUIIconIndex;

                    if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
                    {
                        Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                        iconIndex = 0;
                    }

                    m_playerHotbarItemsIcons[i].sprite = m_ItemIcons[iconIndex];
                    m_playerHotbarItemsIcons[i].color = new Color(1, 1, 1, 1); // visible

                    if (m_playerHotbarItems[i].m_stackSize == 1)
                    {
                        m_playerHotbarItemsStackText[i].text = "";
                    }
                    else
                    {
                        m_playerHotbarItemsStackText[i].text = "" + m_playerHotbarItems[i].m_stackSize;
                    }
                }
            }
        }

        if (m_playerContainerItemsIcons != null && m_playerContainerItems != null)
        {
            for (int i = 0; i < m_inventoryExternalContainerSize; i++)
            {
                m_playerContainerSlotImages[i].color = m_itemSlotDefaultColor; // unhide needed container

                if (m_playerContainerItems[i] == null)
                {
                    m_playerContainerItemsIcons[i].color = new Color(1, 1, 1, 0); // invisible
                    m_playerContainerItemsStackText[i].text = "";
                }
                else
                {
                    int iconIndex = m_playerContainerItems[i].GUIIconIndex;

                    if (iconIndex < 0 || iconIndex > m_ItemIcons.Length - 1)
                    {
                        Debug.LogWarning("GUIManager: Items icon-index if out of range: " + iconIndex);
                        iconIndex = 0;
                    }

                    m_playerContainerItemsIcons[i].sprite = m_ItemIcons[iconIndex];
                    m_playerContainerItemsIcons[i].color = new Color(1, 1, 1, 1); // visible

                    if (m_playerContainerItems[i].m_stackSize == 1)
                    {
                        m_playerContainerItemsStackText[i].text = "";
                    }
                    else
                    {
                        m_playerContainerItemsStackText[i].text = "" + m_playerContainerItems[i].m_stackSize;
                    }
                }
            }
        }
        else
        {
            // hide the hole container by hiding all children
            for (int i = 0; i < m_playerContainerItemsIcons.Length; i++)
            {
                m_playerContainerItemsIcons[i].color = new Color(1, 1, 1, 0);
                m_playerContainerItemsStackText[i].text = "";
                m_playerContainerSlotImages[i].color = new Color(1, 1, 1, 0);
            }
        }
    }

    /// <summary>
    /// sets GUI menu [Options] values to what is saved in clientSettings
    /// </summary>
    public void updateOptionsFromClientSettings()
    {
        m_GUI_Text_ViewDistanceValue_Options.GetComponent<Text>().text = "" + ClientSettingsManager.singleton.viewDistance;
        m_GUI_Slider_ViewDistance_Ref.GetComponent<Slider>().value = ClientSettingsManager.singleton.viewDistance;
    }

    private void hitmarkerManagement()
    {
        if (m_GUI_Ingame_HitmarkerRef.activeSelf)
        {
            if (Time.time > m_lastTimeShowHitmarker + m_hitmarkerTime)
            {
                m_GUI_Ingame_HitmarkerRef.SetActive(false);
            }
        }
    }

    private void ingameStatsManaging()
    {
        if (Time.realtimeSinceStartup > m_lastSecoundStartTime + 1)
        {
            m_lastSecoundAverageFrameTime = 1f / m_lastSecoundFrameCounter;

            m_memoryInUseGCMb = (System.GC.GetTotalMemory(false) / 1024) / 1024;

            m_statsStrBuilder.Remove(0, m_statsStrBuilder.Length); // clear;

            m_statsStrBuilder.Append(m_lastSecoundFrameCounter);
            m_statsStrBuilder.Append(" fps \n");
            m_statsStrBuilder.Append((1f / m_lastSecoundMaxFrameTime));
            m_statsStrBuilder.Append(" fps \n");
            m_statsStrBuilder.Append(m_lastSecoundMaxFrameTime);
            m_statsStrBuilder.Append(" s \n");
            m_statsStrBuilder.Append(m_lastSecoundAverageFrameTime);
            m_statsStrBuilder.Append(" s \n");
            m_statsStrBuilder.Append(m_memoryInUseGCMb);
            m_statsStrBuilder.Append(" mb GC \n");
            m_statsStrBuilder.Append(m_stats_packageCounter);
            m_statsStrBuilder.Append(" Net Packages/s \n");
            m_statsStrBuilder.Append(m_stats_receivedBytes);
            m_statsStrBuilder.Append(" Net Bytes/s \n");

            m_stats_packageCounter = 0;
            m_stats_receivedBytes = 0;

            m_stats_text.text = m_statsStrBuilder.ToString();

            m_lastSecoundStartTime = Time.realtimeSinceStartup;
            m_lastSecoundFrameCounter = 0;
            m_lastSecoundMaxFrameTime = 0;
        }

        m_lastSecoundFrameCounter++;
        m_lastSecoundMaxFrameTime = Mathf.Max(m_lastSecoundMaxFrameTime, Time.realtimeSinceStartup - m_lastFrameStartTime);

        m_lastFrameStartTime = Time.realtimeSinceStartup;
    }

    #region GUI inputs

    private void On_GUI_Dropdown_changed_Debug_Messages(int newIndex)
    {
        if (newIndex == 0)
        {
            m_showDebugMessages = false;
            m_GUI_Text_debug_info.SetActive(false);
        }
        else if (newIndex == 1)
        {
            m_showDebugMessages = true;
            m_GUI_Text_debug_info.SetActive(true);
        }
        else
        {
            displayMessage("illegal show-Debug-Messages index: " + newIndex + ". skipping...");
        }
    }

    private void On_master_volume_Slider_changed(float newValue)
    {
        AudioListener.volume = newValue;
    }
    private void On_ViewDistance_Slider_changed(float newValue)
    {
        m_GUI_Text_ViewDistanceValue_Options.GetComponent<Text>().text = "" + newValue;
        ClientSettingsManager.singleton.viewDistance = newValue;
    }

    private void On_GUI_Dropdown_changed_Water_Quality(int newIndex)
    {
        OceanWater.singleton.setWaterMaterial(newIndex);
    }

    private void On_GUI_Dropdown_changed_Tree_distance(int newIndex)
    {
        if (newIndex == 0) // very low
        {
            WorldManager.singleton.set3DTreeRenderDistance(0);
        }
        else if (newIndex == 1) // low
        {
            WorldManager.singleton.set3DTreeRenderDistance(1);
        }
        else if (newIndex == 2) // medium
        {
            WorldManager.singleton.set3DTreeRenderDistance(2);
        }
        else if (newIndex == 3) // high
        {
            WorldManager.singleton.set3DTreeRenderDistance(3);
        }
        else if (newIndex == 4) // very high
        {
            WorldManager.singleton.set3DTreeRenderDistance(4);
        }
        else
        {
            displayMessage("illegal index: " + newIndex + ". skipping...");
        }
    }

    private void On_GUI_Dropdown_changed_Shadow_Resolution(int newIndex)
    {
        if (newIndex == 0)
        {
            QualitySettings.shadowResolution = ShadowResolution.Low;
        }
        else if (newIndex == 1)
        {
            QualitySettings.shadowResolution = ShadowResolution.Medium;
        }
        else if (newIndex == 2)
        {
            QualitySettings.shadowResolution = ShadowResolution.High;
        }
        else if (newIndex == 3)
        {
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        }
        else
        {
            displayMessage("illegal index: " + newIndex + ". skipping...");
        }
    }

    private void On_GUI_Dropdown_changed_Vsync(int newIndex)
    {
        if (newIndex > -1 && newIndex < 5)
        {
            QualitySettings.vSyncCount = newIndex;

            if(newIndex == 0)
            {
                //Application.targetFrameRate = 1000;
            }
            else
            {
                //Application.targetFrameRate = 60;
            }
        }
        else
        {
            displayMessage("illegal Vsync count: " + newIndex + ". skipping...");
        }
    }

    private void On_GUI_Dropdown_changed_multiple_cameras(int newIndex)
    {
        if (newIndex > -1 && newIndex < 2)
        {
            if (newIndex == 0)
            {
                ClientSettingsManager.singleton.multipleCameraMode = true;
            }
            else
            {
                ClientSettingsManager.singleton.multipleCameraMode = false;
            }
        }
        else
        {
            displayMessage("illegal Vsync count: " + newIndex + ". skipping...");
        }
    }

    public void On_GUI_Button_Respawn()
    {
        GameManager_Custom.singleton.client_spawnRequestRandomLocation();
    }

    public void On_GUI_Button_WakeUp()
    {
        GameManager_Custom.singleton.client_wakeUpRequest();
    }

    public void On_GUI_Button_Respawn_SavedLocation()
    {
        //TODO: multiple spawnpoints in GUI: replace 0
        GameManager_Custom.singleton.client_spawnRequestSavedLocation(0);
    }

    public void On_GUI_Button_Disconnected_OK()
    {
        setGUIMainMenu();
    }

    public void On_GUI_Button_Create_Server()
    {
        int parseResultPort;
        int parseResultSize;
        float parseResultSeed;

        int.TryParse(m_GUI_Text_Port_Create_Server.GetComponent<Text>().text, out parseResultPort);
        int.TryParse(m_GUI_Text_Size_Create_Server.GetComponent<Text>().text, out parseResultSize);
        float.TryParse(m_GUI_Text_Seed_Create_Server.GetComponent<Text>().text, out parseResultSeed);

        int resultInt = parseResultSize;
        float resultfloat = parseResultSize;

        for (int i = 0; i < 8; i++)
        {
            resultInt /= 2;
            resultfloat /= 2;

            if ((float)resultInt != resultfloat || resultInt == 0)
            {
                displayMessage("Map-Size \"" + resultInt + "\" is not divisible by 2 8 Times. ");
                return;
            }
        }

        if (parseResultSeed < -100000 || parseResultSeed > 100000)
        {
            displayMessage("Seed out of bounds !");
            return;
        }

        displayMessage("starting server on port: " + parseResultPort + ".");
        displayMessage("Building Map with Size: " + parseResultSize + ", Seed: " + parseResultSeed);

        if (m_GUI_toggle_ServerAndClient.GetComponent<Toggle>().isOn)
        {
            displayMessage("Starting server and client");
            GameManager_Custom.singleton.startAsServerWithLocalClient(parseResultSeed, parseResultSize, parseResultPort, m_GUI_toogle_loadSaveFile.isOn);
        }
        else // server only
        {
            displayMessage("Starting server only");
            GameManager_Custom.singleton.startAsServer(parseResultSeed, parseResultSize, parseResultPort, m_GUI_toogle_loadSaveFile.isOn);
        }

    }

    public void On_GUI_Button_Connect_direct_Server_Menu()
    {
        StartCoroutine(ConnectToServer());
    }

    private IEnumerator ConnectToServer()
    {
        setGUILoadingActive();
        setGUILoadingProgressText("Establishing Connection");
        yield return null;

        int serverPortParseResult;
        if (!int.TryParse(m_GUI_Text_Port_direct_Connect.GetComponent<Text>().text, out serverPortParseResult))
        {
            setGUIDisconnectedInfo("Server Port wrong format !");
            setGUIDisconnectedActive();
            yield break;
        }

        int clientPortParseResult;
        if (!int.TryParse(m_GUI_Text_Port_client_direct_Connect.GetComponent<Text>().text, out clientPortParseResult))
        {
            setGUIDisconnectedInfo("Client Port wrong format !");
            setGUIDisconnectedActive();
            yield break;
        }

        System.Net.IPAddress serverIPParseResult;
        if (!System.Net.IPAddress.TryParse(m_GUI_Text_IP_Adress_direct_Connect.GetComponent<Text>().text, out serverIPParseResult))
        {
            setGUIDisconnectedInfo("Server IP Address wrong format !");
            setGUIDisconnectedActive();
            yield break;
        }

        string username = m_GUI_Text_Username.text;
        if (username == string.Empty)
        {
            setGUIDisconnectedInfo("Username cannot be empty !");
            setGUIDisconnectedActive();
            yield break;
        }

        string userPassword = m_GUI_Text_password.text;
        if (userPassword.Length < 8)
        {
            setGUIDisconnectedInfo("password must have at least 8 characters !");
            setGUIDisconnectedActive();
            yield break;
        }

        System.Security.Cryptography.HashAlgorithm hash = System.Security.Cryptography.SHA256.Create();
        string passwordHash = Encoding.Unicode.GetString(hash.ComputeHash(Encoding.Unicode.GetBytes(userPassword)));
        hash.Dispose();

        string ingameName = m_GUI_Text_IngameName.text;
        if (ingameName == string.Empty)
        {
            setGUIDisconnectedInfo("Ingame Name cannot be empty !");
            setGUIDisconnectedActive();
            yield break;
        }

        GameManager_Custom.singleton.startAsClient(serverIPParseResult, serverPortParseResult, clientPortParseResult, username, passwordHash, ingameName);
    }

    public void On_GUI_Button_Random_Seed_Create_Server_Menu()
    {
        m_GUI_Inputfield_Seed_Create_Server.GetComponent<InputField>().text = "" + RandomValuesSeed.getRandomValueSeed(Time.realtimeSinceStartup, Time.realtimeSinceStartup, -100000, 100000);
    }

    public void On_GUI_Button_Back_Server_Menu()
    {
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Server_MainMenu_Ref.SetActive(false);
    }

    public void On_GUI_Button_Back_Create_Server_Menu()
    {
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Create_Server_MainMenu_Ref.SetActive(false);
    }

    public void On_GUI_Button_Back_Options_Menu()
    {
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Options_MainMenu_Ref.SetActive(false);
    }

    public void On_GUI_Button_Find_Server()
    {
        m_GUI_Panel_Server_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(false);
    }

    public void On_GUI_Button_Create_Server_Main_Menu()
    {
        m_GUI_Panel_Create_Server_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(false);
    }

    public void On_GUI_Button_Options()
    {
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(false);
        m_GUI_Panel_Options_MainMenu_Ref.SetActive(true);
    }

    public void onGUIButton_Credits_MainMenu()
    {
        m_GUI_Panel_Credits_MainMenu_Ref.SetActive(true);
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(false);
    }

    public void onGUIButton_Back_Credits()
    {
        m_GUI_Panel_Credits_MainMenu_Ref.SetActive(false);
        m_GUI_Panel_Main_MainMenu_Ref.SetActive(true);
    }

    public void On_GUI_Button_Quit()
    {
        Application.Quit();
    }

    #endregion

    public void setTimeUntilEnd(TimeSpan time )
    {
        m_GUI_Text_TimeUntilAllEnd.text = time.ToString();
    }

    public void setSpawnSavedLocationVisibility(bool visibly)
    {
        m_GUI_Button_SpawnSavedLocation.gameObject.SetActive(visibly);
    }

    private bool checkIfGUIItemIsActiveItemHotbar(Image GUISlot)
    {
        for (int i = 0; i < m_playerHotbarSlotImages.Length; i++)
        {
            if (GUISlot == m_playerHotbarSlotImages[i] && i == m_selectedHotbarIndex)
            {
                return true;
            }
        }

        return false;
    }

    private void setAllGUIPanelsInactive()
    {
        m_GUI_Panel_MainMenu_Ref.SetActive(false);
        m_GUI_Panel_Ingame_Ref.SetActive(false);
        m_GUI_Panel_Loading_Ref.SetActive(false);
        m_GUI_Panel_DeadScreen_Ref.SetActive(false);
        m_GUI_Panel_Asleep_Ref.SetActive(false);
        m_GUI_Panel_Disconnected_Ref.SetActive(false);
    }

    public void setHealth(float newHealth)
    {
        if (newHealth < 0)
        {
            Debug.LogWarning("GUIManager: new health out of bounds: " + newHealth);
            newHealth = 0;
        }
        else if (newHealth > 100)
        {
            Debug.LogWarning("GUIManager: new health out of bounds: " + newHealth);
            newHealth = 100;
        }

        m_playerHealthText.text = "" + (int)newHealth;
        m_playerHealthBarImage.fillAmount = newHealth / 100f;
    }

    public void setFood(float food)
    {
        m_GUI_Text_playerFood.text = "" + Math.Round(food);
        m_GUI_Image_playerFoodBar.fillAmount = food / 100f;
    }

    public void setHunger(float hunger)
    {
        m_GUI_Text_playerHunger.text = "- " + Math.Round(hunger, 3);
    }

    public void setWater(float water)
    {
        m_GUI_Text_playerWater.text = "" + Math.Round(water);
        m_GUI_Image_playerWaterBar.fillAmount = water / 100f;
    }

    public void setThirst(float thirst)
    {
        m_GUI_Text_playerThirst.text = "- " + Math.Round(thirst, 3);
    }

    public void setGUILoadingProgressText(string newText)
    {
        m_GUI_Text_Progress_Loading.GetComponent<Text>().text = newText;
    }

    public void setGUILoadingActive()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_Loading_Ref.SetActive(true);
    }

    public void onPlayerSpawn()
    {
        setGUIIngameActive();
        reloadPlayerInventoryItems();
    }

    public void setGUIIngameActive()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_Ingame_Ref.SetActive(true);
        setCursorActive(false);
        m_isIngame = true;
    }

    public void setGUIMainMenu()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_MainMenu_Ref.SetActive(true);
    }

    public void setGUIDeadScreenActive()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_DeadScreen_Ref.SetActive(true);
        setCursorActive(true);
    }

    public void setGUIAsleepScreenActive()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_Asleep_Ref.SetActive(true);
        setCursorActive(true);
    }

    public void setGUIDisconnectedActive()
    {
        setAllGUIPanelsInactive();
        m_GUI_Panel_Disconnected_Ref.SetActive(true);
    }

    public void setGUIDisconnectedInfo(string newText)
    {
        m_GUI_Text_Disconnected_info.GetComponent<Text>().text = newText;
    }

    public void setGUI_Inventory(bool newState)
    {
        if (newState == true)
        {
            reloadPlayerInventoryItems();

            m_GUI_Panel_Inventory_Ref.SetActive(true);
            setCursorActive(true);
            m_isInventory = true;
        }
        else
        {
            m_GUI_Panel_Inventory_Ref.SetActive(false);
            setCursorActive(false);
            m_isInventory = false;
        }
    }

    public void setInventoryExternalContainerSize(int size)
    {
        m_inventoryExternalContainerSize = size;
    }

    public void switchGUI_Inventory()
    {
        if (m_isIngame)
        {
            if (m_GUI_Panel_Inventory_Ref.activeSelf)
            {
                setGUI_Inventory(false);
            }
            else
            {
                setGUI_Inventory(true);
            }
        }
    }

    public void switchGUI_MainMenu_Ingame()
    {
        if (!m_GUI_Panel_MainMenu_Ref.activeSelf && !m_GUI_Panel_Ingame_Ref.activeSelf)
        {
            Debug.LogError("GUI_Panel_MainMenu_Ref and GUI_Panel_Ingame_Ref arent active");
            return;
        }

        if (m_GUI_Panel_MainMenu_Ref.activeSelf)
        {
            m_GUI_Panel_MainMenu_Ref.SetActive(false);
            m_GUI_Panel_Ingame_Ref.SetActive(true);

            setCursorActive(false);
            m_isIngame = true;
        }
        else
        {
            m_GUI_Panel_MainMenu_Ref.SetActive(true);
            m_GUI_Panel_Ingame_Ref.SetActive(false);

            setCursorActive(true);
            m_isIngame = false;
        }
    }

    public void setCursorActive(bool newState)
    {
        if (newState)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            m_coursorActive = true;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            m_coursorActive = false;
        }
    }

    public void showHitmarker()
    {
        m_lastTimeShowHitmarker = Time.time;
        if (!m_GUI_Ingame_HitmarkerRef.activeSelf)
        {
            m_GUI_Ingame_HitmarkerRef.SetActive(true);
        }
    }

    public void setInteractableText(string text)
    {
        m_playerInteractableText.text = text;
    }

    public void setHotbarSelectedIndex(int index)
    {
        m_selectedHotbarIndex = index;

        for (int i = 0; i < m_playerHotbarSlotImages.Length; i++)
        {
            m_playerHotbarSlotImages[i].color = m_itemSlotDefaultColor;
        }

        if (index > -1 && index < m_playerHotbarSlotImages.Length)
        {
            m_playerHotbarSlotImages[index].color = m_itemSlotHotbarActiveColor;
        }
    }

    public CircularMenu getBuildingSelectionCircularMenu()
    {
        return m_GUI_BuildingCircularMenu;
    }

    public void setBuildingSelectionCircularMenuActivity(bool active)
    {
        m_GUI_Panel_Ingame_BuildingSelectionRef.SetActive(active);

        if (active)
        {
            setCursorActive(true);
        }
        else
        {
            setCursorActive(false);
        }
    }

    public void setCrosshairVisibility(bool visible)
    {
        m_GUI_Image_Crosshair.gameObject.SetActive(visible);
    }

    public void tryStartHurtOverlay()
    {
        if (!m_hurtOverlayActive)
        {
            m_hurtOverlayActive = true;
            m_hurtOverlayFadeStart = Time.time;
            m_GUI_Image_Ingame_PlayerHurtOverlay.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Shows an info Dialog on Debug and Release Build
    /// </summary>
    /// <param name="inMessage">In message.</param>
    public void displayMessage(string inMessage)
    {
        displayMessage(inMessage, false);
    }

    private List<string> m_displayMessages = new List<string>();
    /// <summary>
    /// Shows an info Dialog on players screen depending if Debug or Release Build
    /// </summary>
    /// <param name="inMessage">In message.</param>
    /// <param name="onlyDebug">If set to <c>true</c> only debug.</param>
    public void displayMessage(string inMessage, bool onlyDebug)
    {
        if (onlyDebug && Debug.isDebugBuild || !onlyDebug)
        {
            if (m_displayMessages.Count > maxDisplayMessages)
            {
                m_displayMessages.RemoveAt(0);
            }
            m_displayMessages.Add(inMessage);

            m_GUI_Text_debug_info.GetComponent<Text>().text = "";
            foreach (string str in m_displayMessages)
            {
                m_GUI_Text_debug_info.GetComponent<Text>().text += str + "\n";
            }
        }

    }

}
