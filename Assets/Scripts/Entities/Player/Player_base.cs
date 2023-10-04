using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Player_base : Entity_damageable
{
    protected const int m_inventorySize = 24; // 6 * 4 = 24
    protected const int m_hotbarSize = 6;

    [Header("Player_base")]
    [SerializeField] public int m_gameID = -1;
    [SerializeField] private float m_distanceFootsteps = 0.1f;
    [SerializeField] private float m_fellThroughGroundHeight = -1;
    [SerializeField] private float m_waterDamage = 20f; // water hurt
    [SerializeField] private float m_heightHurt = 21.8f; // water hurt
    [SerializeField] private float m_heightKill = 20.45f; // water hurt
    [SerializeField] private float m_waterDamageFadeSpeed = 0.2f; // water hurt
    [SerializeField] private bool DEBUG_printInventory = false;
    [SerializeField] protected float m_food = 50f;
    [SerializeField] protected float m_water = 50f;
    [SerializeField] private float m_maxFood = 100f;
    [SerializeField] private float m_maxWater = 100f;
    [SerializeField] private float m_waterFoodSendInterval = 0.2f;
    [SerializeField, ReadOnly] protected float m_hunger = 0f;
    [SerializeField, ReadOnly] protected float m_thirst = 0f;
    [SerializeField, ReadOnly] protected int m_selectedHotbarIndex = -1;
    [SerializeField] private bool DEBUG_showAssignedGrid = false;
    private float m_health_lastSend = 100;
    private Vector3 m_lastPosFootstep = Vector3.zero;
    private Sound m_waterHurtSound = null;
    private bool m_wasInWaterLastFrame = false;
    private Dictionary<HungerEffects, float> m_hungerEffect_penalty = new Dictionary<HungerEffects, float>();
    private Dictionary<ThirstEffects, float> m_thirstEffect_penalty = new Dictionary<ThirstEffects, float>();
    private float m_lastTimeSendFoodWater = 0f;

    protected bool m_isGrounded = false;
    protected bool m_isStatic = false;
    protected LootContainer m_inventoryContainer = null;
    protected LootContainer m_hotbarContainer = null;
    protected LootContainerGame m_openContainer = null;
    protected Animator m_Animator;

    public int selectedHotbarIndex { get { return m_selectedHotbarIndex; } }
    public int inventorySize { get { return m_inventorySize; } }
    public int hotbarSize { get { return m_hotbarSize; } }

    protected void Start()
    {
        Entity_damageable_Start();

        m_Animator = GetComponent<Animator>();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            applyHungerEffect(HungerEffects.Base);
            applyThristEffect(ThirstEffects.Base);
        }
    }

    protected void Update()
    {
        if(DEBUG_showAssignedGrid)
        {
            DEBUG_showAssignedGrid = false;
            EntityManager.singleton.getGridFieldForUID(entity_gridFieldID).DEBUG_drawRectangle();
        }

        if (transform.position.y < m_heightHurt)
        {
            if (!m_wasInWaterLastFrame)
            {
                if (m_waterHurtSound == null)
                {
                    m_waterHurtSound = SoundManager.singleton.playSoundAt(26, transform.position, Sound.SoundPlaystyle.loop);
                    m_waterHurtSound.m_volumeFadeSpeed = m_waterDamageFadeSpeed;
                    m_waterHurtSound.transform.SetParent(transform);
                    m_waterHurtSound.setVolume(0);
                }

                m_waterHurtSound.fadeVolumeTo(m_waterHurtSound.defaultVolumne);
            }
        }
        else
        {
            if (m_wasInWaterLastFrame)
            {
                m_waterHurtSound.fadeVolumeTo(0f);
            }
        }

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (transform.position.y < m_heightKill)
            {
                onDamaged(10000f);
            }
            else if (transform.position.y < m_heightHurt)
            {
                onDamaged(m_waterDamage * Time.deltaTime);
            }

            if(!m_isStatic)
            {
                m_food -= m_hunger * Time.deltaTime;
                m_water -= m_thirst * Time.deltaTime;

                if(m_food < 0)
                {
                    m_food = 0;
                    onDamaged(0.7f * Time.deltaTime);
                    // TODO: real penalty
                }

                if (m_water < 0)
                {
                    m_water = 0;
                    onDamaged(0.7f * Time.deltaTime);
                    // TODO: real penalty
                }
            }

            server_foodWaterSending();
        }

        if (!m_isStatic)
        {
            if (Physics.Raycast(transform.position + Vector3.up * getAimOffsetY(), Vector3.down, getAimOffsetY() + 0.1f, ~(1 << 20)))
            {
                m_isGrounded = true;
            }
            else
            {
                m_isGrounded = false;
            }

            if (GameManager_Custom.singleton.isServer)
            {
                if (m_health_lastSend != m_health)
                {
                    Player_local localPlayer = this as Player_local;

                    if (localPlayer == null)
                    {
                        server_sendHealthUpdate();
                    }
                }
            }

            if (m_isGrounded && Vector3.Distance(transform.position, m_lastPosFootstep) > m_distanceFootsteps)
            {
                m_lastPosFootstep = transform.position;
                if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                {
                    NetworkingManager.singleton.server_sendWorldSoundToAllInRange((int)(UnityEngine.Random.value * 9 + 1), transform.position);
                }
            }
        }

        if (DEBUG_printInventory)
        {
            DEBUG_printInventory = false;

            string text = "";

            StorableItem[] items = m_hotbarContainer.getAllItems();

            text += "hotbar : ";

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    text += "NULL, ";
                }
                else
                {
                    text += items[i].itemTemplateIndex + ", ";
                }
            }

            items = m_inventoryContainer.getAllItems();

            text += "; inventory : ";

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    text += "NULL, ";
                }
                else
                {
                    text += items[i].itemTemplateIndex + ", ";
                }
            }

            Debug.Log("Player_base: Update: print inventory: " + text);
        }
    }

    protected void FixedUpdate()
    {
        updateWorldGridAssignment();
    }

    protected void LateUpdate()
    {
        if (transform.position.y < m_heightHurt)
        {
            m_wasInWaterLastFrame = true;
        }
        else
        {
            m_wasInWaterLastFrame = false;
        }
    }

    protected override void initializeChildrenHitboxes(Transform parent)
    {
        EntityHitbox hitbox = parent.GetComponent<EntityHitbox>();

        if (hitbox != null)
        {
            hitbox.setParentEntity(this);
            hitbox.m_gameID = m_gameID;
        }

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            initializeChildrenHitboxes(parent.transform.GetChild(i));
        }
    }

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_sendHealthUpdate();
        }
    }

    protected override void onHeal(float heal)
    {
        float temphealth;
        temphealth = heal + m_health;

        if (temphealth >= 100)
        {
            m_health = 100;
        }
        else
        {
            m_health = temphealth;
        }

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_sendHealthUpdate();
        }
    }

    public void server_sendHealthUpdate()
    {
        m_health_lastSend = m_health;

        if (m_health < 0)
        {
            GameManager_Custom.singleton.server_onPlayerNoHealth(m_gameID);
        }
        else
        {
            NetworkingManager.singleton.server_UpdateExternalClientHealth(m_gameID, m_health);
        }
    }

    private void server_foodWaterSending()
    {
        if (Time.realtimeSinceStartup > m_lastTimeSendFoodWater + m_waterFoodSendInterval)
        {
            m_lastTimeSendFoodWater = Time.realtimeSinceStartup;

            NetworkMessage message = getCustomMessageBase();
            message.addIntegerValues((int)CustomMessageContext1.FoodWaterHungerThirst);
            message.addFloatValues(m_food, m_water, m_hunger, m_thirst);

            server_sendCustomTCPMessage(message, m_gameID); // gets received by a player local
        }
    }

    public void setOpenContainer(LootContainerGame container)
    {
        m_openContainer = container;
    }

    public void setHealth(float newHealth)
    {
        m_health = newHealth;
    }

    public void setGameID(int newGameID)
    {
        m_gameID = newGameID;
    }

    public override void onChildHitboxHit(Vector3 position, Vector3 force, float damage, EntityHitbox.HitboxBodyPart bodyPart)
    {
        if (bodyPart == EntityHitbox.HitboxBodyPart.Head)
            onDamaged(damage * 2);

        if (bodyPart == EntityHitbox.HitboxBodyPart.Body)
            onDamaged(damage);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftUpperArm)
            onDamaged(damage / 1.5F);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightUpperArm)
            onDamaged(damage / 1.5F);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftForArm)
            onDamaged(damage / 2);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightForArm)
            onDamaged(damage / 2);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftHand)
            onDamaged(damage / 3);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightHand)
            onDamaged(damage / 3);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftThigh)
            onDamaged(damage / 1.5F);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightThigh)
            onDamaged(damage / 1.5F);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftKnee)
            onDamaged(damage / 2);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightKnee)
            onDamaged(damage / 2);

        if (bodyPart == EntityHitbox.HitboxBodyPart.LeftAnkle)
            onDamaged(damage / 3);

        if (bodyPart == EntityHitbox.HitboxBodyPart.RightAnkle)
            onDamaged(damage / 3);
    }

    protected void fellThroughGroundCheck()
    {
        if (transform.position.y < m_fellThroughGroundHeight)
        {
            Debug.LogWarning("Player_local: Player fell through ground. Teleporting Up");

            RaycastHit hit;
            Physics.Raycast(new Vector3(transform.position.x, 100000, transform.position.z), Vector3.down, out hit);

            transform.position = hit.point + new Vector3(0, 5, 0);
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    protected void applyHungerEffect(HungerEffects effect)
    {
        if (!m_hungerEffect_penalty.ContainsKey(effect))
        {
            m_hungerEffect_penalty.Add(effect, HungerThristValues.getHungerValue(effect));
            m_hunger += HungerThristValues.getHungerValue(effect);
        }
    }

    protected void applyThristEffect(ThirstEffects effect)
    {
        if (!m_thirstEffect_penalty.ContainsKey(effect))
        {
            m_thirstEffect_penalty.Add(effect, HungerThristValues.getThirstValue(effect));
            m_thirst += HungerThristValues.getThirstValue(effect);
        }
    }

    protected void removeHungerEffect(HungerEffects effect)
    {
        if (m_hungerEffect_penalty.ContainsKey(effect))
        {
            m_hunger -= m_hungerEffect_penalty[effect];
            m_hungerEffect_penalty.Remove(effect);
        }
    }

    protected void removeThirstEffect(ThirstEffects effect)
    {
        if (m_thirstEffect_penalty.ContainsKey(effect))
        {
            m_thirst -= m_thirstEffect_penalty[effect];
            m_thirstEffect_penalty.Remove(effect);
        }
    }

    public void addFood(float food)
    {
        m_food += food;

        if(m_food > m_maxFood)
        {
            m_food = m_maxFood;
        }
    }

    public void addWater(float water)
    {
        m_water += water;

        if (m_water > m_maxWater)
        {
            m_water = m_maxWater;
        }
    }

    #region Player And Server or server Only (no client)

    public bool tryAddItem(StorableItem item)
    {
        return m_inventoryContainer.tryAddItem(item);
    }

    public StorableItem getCurrentHotBarItem()
    {
        return m_hotbarContainer.getItem(selectedHotbarIndex);
    }

    public int getCurrentHotBarItemIndex()
    {
        return selectedHotbarIndex;
    }

    public void tryDeleteItem(int amount, int index)
    {
        m_hotbarContainer.tryDeleteItem(amount, index);
    }

    public void server_dropAllItems()
    {
        RaycastHit hit;

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(transform.position, Vector3.up, out hit, 2f))
        {
            float distance = Vector3.Distance(transform.position, hit.point);
            Vector3 dir = (hit.point - spawnPosition).normalized;
            spawnPosition = transform.position + dir * distance / 2;
        }

        StorableItem[] items;

        items = m_inventoryContainer.getAllItems();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                items[i].m_owningContainer = null;
                EntityManager.singleton.server_spawnDroppedItemWorld(items[i], spawnPosition);
            }
        }
        m_inventoryContainer.destroyAllItems();

        items = m_hotbarContainer.getAllItems();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                items[i].m_owningContainer = null;
                EntityManager.singleton.server_spawnDroppedItemWorld(items[i], spawnPosition);
            }
        }
        m_hotbarContainer.destroyAllItems();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="position"></param>
    /// <param name="containerUID">only used if type is PlayerLootContainer </param>
    /// <returns></returns>
    public StorableItem server_removeItem(GUIRaycastIdentifier.Type type, int position, int containerUID = -1)
    {
        switch (type)
        {
            case GUIRaycastIdentifier.Type.PlayerHotbar:
                {
                    return m_hotbarContainer.removeItem(position);
                }
            case GUIRaycastIdentifier.Type.PlayerInventory:
                {
                    return m_inventoryContainer.removeItem(position);
                }
            case GUIRaycastIdentifier.Type.PlayerLootContainer:
                {
                    if (m_openContainer == null || m_openContainer.getEntityUID() != containerUID)
                    {
                        NetworkingManager.singleton.server_sendContainerCloseCommand(m_gameID, containerUID);
                        return null;
                    }
                    else
                    {
                        return m_openContainer.removeItem(position);
                    }
                }
            default:
                {
                    return null;
                }
        }
    }

    public void server_SplitIventoryItems(int containerUID, GUIRaycastIdentifier.Type sourceType, int sourceIndex)
    {
        switch (sourceType)
        {
            case GUIRaycastIdentifier.Type.PlayerInventory:
                {
                    m_inventoryContainer.SplitItem(sourceIndex);
                    break;
                }
        }
    }

    public void server_switchIventoryItems(int containerUID, GUIRaycastIdentifier.Type sourceType, int sourceIndex, GUIRaycastIdentifier.Type targetType, int targetIndex)
    {
        switch (sourceType)
        {
            case GUIRaycastIdentifier.Type.PlayerHotbar:
                {
                    switch (targetType)
                    {
                        case GUIRaycastIdentifier.Type.PlayerHotbar:
                            {
                                m_hotbarContainer.switchItem(sourceIndex, targetIndex);
                                break;
                            }
                        case GUIRaycastIdentifier.Type.PlayerInventory:
                            {
                                m_hotbarContainer.switchItemContainer(sourceIndex, targetIndex, m_inventoryContainer);
                                break;
                            }
                        case GUIRaycastIdentifier.Type.PlayerLootContainer:
                            {
                                if (m_openContainer == null || m_openContainer.getEntityUID() != containerUID)
                                {
                                    NetworkingManager.singleton.server_sendContainerCloseCommand(m_gameID, containerUID);
                                }
                                else
                                {
                                    m_openContainer.switchItemContainer(targetIndex, sourceIndex, m_hotbarContainer);
                                }
                                break;
                            }
                        default:
                            {
                                Debug.LogError("Player_external: server_switchIventoryItems: unknown inventory-target-type: " + targetType);
                                break;
                            }
                    }
                    break;
                }
            case GUIRaycastIdentifier.Type.PlayerInventory:
                {
                    switch (targetType)
                    {
                        case GUIRaycastIdentifier.Type.PlayerHotbar:
                            {
                                m_inventoryContainer.switchItemContainer(sourceIndex, targetIndex, m_hotbarContainer);
                                break;
                            }
                        case GUIRaycastIdentifier.Type.PlayerInventory:
                            {
                                m_inventoryContainer.switchItem(sourceIndex, targetIndex);
                                break;
                            }
                        case GUIRaycastIdentifier.Type.PlayerLootContainer:
                            {
                                if (m_openContainer == null || m_openContainer.getEntityUID() != containerUID)
                                {
                                    NetworkingManager.singleton.server_sendContainerCloseCommand(m_gameID, containerUID);
                                }
                                else
                                {
                                    m_openContainer.switchItemContainer(targetIndex, sourceIndex, m_inventoryContainer);
                                }
                                break;
                            }
                        default:
                            {
                                Debug.LogError("Player_external: server_switchIventoryItems: unknown inventory-target-type: " + targetType);
                                break;
                            }
                    }
                    break;
                }
            case GUIRaycastIdentifier.Type.PlayerLootContainer:
                {
                    if (m_openContainer == null || m_openContainer.getEntityUID() != containerUID)
                    {
                        NetworkingManager.singleton.server_sendContainerCloseCommand(m_gameID, containerUID);
                    }
                    else
                    {
                        switch (targetType)
                        {
                            case GUIRaycastIdentifier.Type.PlayerHotbar:
                                {
                                    m_openContainer.switchItemContainer(sourceIndex, targetIndex, m_hotbarContainer);
                                    break;
                                }
                            case GUIRaycastIdentifier.Type.PlayerInventory:
                                {
                                    m_openContainer.switchItemContainer(sourceIndex, targetIndex, m_inventoryContainer);
                                    break;
                                }
                            case GUIRaycastIdentifier.Type.PlayerLootContainer:
                                {
                                    m_openContainer.switchItem(sourceIndex, targetIndex);
                                    break;
                                }
                            default:
                                {
                                    Debug.LogError("Player_external: server_switchIventoryItems: unknown inventory-target-type: " + targetType);
                                    break;
                                }
                        }
                    }
                    break;
                }
            default:
                {
                    Debug.LogError("Player_external: server_switchIventoryItems: unknown inventory-source-type: " + sourceType);
                    break;
                }
        }
    }

    protected virtual void onInventoryItemChanged_server(object obj, IntegerEventArgs args)
    {
        NetworkingManager.singleton.server_sendInventoryItemUpdate(m_gameID, GUIRaycastIdentifier.Type.PlayerInventory, args.integer, -1, m_inventoryContainer.getItem(args.integer));
    }

    protected virtual void onHotbarItemChanged_server(object obj, IntegerEventArgs args)
    {
        NetworkingManager.singleton.server_sendInventoryItemUpdate(m_gameID, GUIRaycastIdentifier.Type.PlayerHotbar, args.integer, -1, m_hotbarContainer.getItem(args.integer));
    }

    #endregion

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        return base.client_receivedCustomNetworkMessage(message);
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        if (base.server_receivedCustomNetworkMessage(message))
        {
            return true; // a base class already handles this message
        }
        else
        {
            switch (message.getIntValue(1))
            {
                case (int)CustomMessageContext1.PlayerMovingState: // potential thread due to everyone could send this message not only the client that owns player-object
                    {
                        if (message.checkInputCorrectness(3, 0, 0))
                        {
                            if(message.getIntValue(2) == 0)
                            {
                                removeHungerEffect(HungerEffects.Moving);
                                removeThirstEffect(ThirstEffects.Moving);
                            }
                            else
                            {
                                applyHungerEffect(HungerEffects.Moving);
                                applyThristEffect(ThirstEffects.Moving);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Player_base: server_receivedCustomNetworkMessage: message values count unexpected: " + message.ToString());
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

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_Player dataEntity = m_dataEntity as DataEntity_Player;

        dataEntity.m_playerGameID = m_gameID;
        dataEntity.m_playerInventory = m_inventoryContainer;
        dataEntity.m_playerHotbar = m_hotbarContainer;
        dataEntity.m_entityPrefabID = 17; // player sleeper
        dataEntity.m_rotation = dataEntity.m_rotation * Quaternion.Euler(-180, -90, 90); // just rotate gameobject until a animation is available: WARNING: This will be broken on multiple successively save file loads
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_Player dataEntity = m_dataEntity as DataEntity_Player;

        m_gameID = dataEntity.m_playerGameID;
        m_inventoryContainer = dataEntity.m_playerInventory;
        m_hotbarContainer = dataEntity.m_playerHotbar;
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_Player();
    }
}
