using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class ItemManager : MonoBehaviour
{
    public static ItemManager singleton;

    [SerializeField] private TextAsset m_storableItemsXMLFile;

    private Dictionary<int, StorableItemTemplate> m_itemID_itemStorableTemplate = new Dictionary<int, StorableItemTemplate>();
    private Dictionary<string, int> m_ItemStorableName_index = new Dictionary<string, int>();
    private RandomItemDispenser[] m_lootContainersItemsDispenser;

    private void Awake()
    {
        singleton = this;
        initialize();
    }

    public void initialize()
    {
        loadStorableItemsXMLAsset(m_storableItemsXMLFile.text, ClientSettingsManager.singleton.GetLanguage());
    }

    public int getGUIIconIndex(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_GUIIconIndex;
    }

    public int getWorldPrefabIndex(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_worldModelIndex;
    }

    public bool getItemStackable(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_stackable;
    }

    public int getMaxStackSize(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_maxStackSize;
    }

    public string getDisplayName(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_displayName;
    }

    public StorableItemTemplate.ItemType getItemType(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_itemType;
    }

    public string getDescription(int itemID)
    {
        return m_itemID_itemStorableTemplate[itemID].m_description;
    }

    public StorableItem createNewStorableItem(string StorableItemName)
    {
        if (m_ItemStorableName_index.ContainsKey(StorableItemName))
        {
            return new StorableItem(m_ItemStorableName_index[StorableItemName]);
        }
        else
        {
            Debug.LogWarning("ItemManager: createNewStorableItem: StorableItemName \"" + StorableItemName + "is unknown.");
            return null;
        }
    }
    public StorableItem createNewStorableItem(string StorableItemName, int stackSize)
    {
        if (m_ItemStorableName_index.ContainsKey(StorableItemName))
        {
            return new StorableItem(m_ItemStorableName_index[StorableItemName], stackSize);
        }
        else
        {
            Debug.LogWarning("ItemManager: createNewStorableItem: StorableItemName \"" + StorableItemName + "is unknown.");
            return null;
        }
    }
    public StorableItem createNewStorableItem(int itemID)
    {
        if (m_itemID_itemStorableTemplate.ContainsKey(itemID))
        {
            return new StorableItem(itemID);
        }
        else if (itemID == -1)
        {
            return null;
        }
        else
        {
            Debug.LogWarning("ItemManager: createNewStorableItem: index is out of range: " + itemID);
            return null;
        }
    }
    public StorableItem createNewStorableItem(int itemID, int stacksize)
    {
        if (m_itemID_itemStorableTemplate.ContainsKey(itemID))
        {
            return new StorableItem(itemID, stacksize);
        }
        else if (itemID == -1)
        {
            return null;
        }
        else
        {
            Debug.LogWarning("ItemManager: createNewStorableItem: index is out of range: " + itemID);
            return null;
        }
    }

    private void loadStorableItemsXMLAsset(string inputContent, SystemLanguage language)
    {
        XmlDocument inputFile = null;
        XmlNamespaceManager namespaceManager = null;

        try
        {
            if (inputContent == null || inputContent == "")
            {
                Debug.LogError("ItemManager: could not load storableItems-XML-File: File is null or empty");
                return;
            }

            inputFile = new XmlDocument();
            inputFile.LoadXml(inputContent);
            namespaceManager = new XmlNamespaceManager(inputFile.NameTable);
            namespaceManager.AddNamespace("ehd", "urn:ehd/001");
        }
        catch (Exception ex)
        {
            Debug.LogError("ItemManager: could not load storableItems-XML-File: " + ex);
            return;
        }

        try
        {
            XmlNodeList itemsNodes = inputFile.SelectNodes("//ehd:StorableItem", namespaceManager);
            XmlNode myLanguageNode = null;
            XmlNode englishLanguageNode = null;

            //Debug.Log("ItemManager: language = \"" + language.ToString() + "\"");

            Dictionary<int, List<int>> lootconatiner_itemsID = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> lootconatiner_itemsRarity = new Dictionary<int, List<int>>();

            for (int i = 0; i < itemsNodes.Count; i++)
            {
                try
                {
                    myLanguageNode = null;
                    englishLanguageNode = null;

                    //find language
                    for (int j = 0; j < itemsNodes[i].ChildNodes.Count; j++)
                    {
                        if (itemsNodes[i].ChildNodes[j].Name == "Language")
                        {
                            if (itemsNodes[i].ChildNodes[j].Attributes["name"].Value.ToUpper() == SystemLanguage.English.ToString().ToUpper())
                            {
                                englishLanguageNode = itemsNodes[i].ChildNodes[j];
                            }
                            if (itemsNodes[i].ChildNodes[j].Attributes["name"].Value.ToUpper() == language.ToString().ToUpper())
                            {
                                myLanguageNode = itemsNodes[i].ChildNodes[j];
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ItemManager: StorableItem child-node is not language. Item: \"" + itemsNodes[i].Attributes["StorableItemName"].Value + "\"");
                        }
                    }

                    // load item

                    if (myLanguageNode == null)
                    {
                        Debug.LogWarning("ItemManager: language \"" + language.ToString() + "\" could not be found. Loading english !");

                        if (englishLanguageNode == null)
                        {
                            Debug.LogError("ItemManager: Error: language \"" + SystemLanguage.English.ToString() + "\" could not be found.");
                            continue;
                        }
                    }

                    string temp_name = itemsNodes[i].Attributes["StorableItemName"].Value;
                    StorableItemTemplate.ItemType temp_itemType = (StorableItemTemplate.ItemType)int.Parse(itemsNodes[i].Attributes["ItemType"].Value);
                    int temp_GUIIconIndex = int.Parse(itemsNodes[i].Attributes["GUIIconIndex"].Value);
                    int temp_maxStackSize = int.Parse(itemsNodes[i].Attributes["StackSize"].Value);
                    int temp_ID = int.Parse(itemsNodes[i].Attributes["ID"].Value);
                    int temp_worldModelID = int.Parse(itemsNodes[i].Attributes["WorldModel"].Value);
                    int temp_lootRarity = int.Parse(itemsNodes[i].Attributes["LootRarity"].Value);

                    string lootContainers = itemsNodes[i].Attributes["LootContainers"].Value;
                    string[] lootContainersSplit = lootContainers.Split(',');

                    for (int j = 0; j < lootContainersSplit.Length; j++)
                    {
                        int lootcontainer = int.Parse(lootContainersSplit[j]);

                        if (!lootconatiner_itemsID.ContainsKey(lootcontainer))
                        {
                            lootconatiner_itemsID.Add(lootcontainer, new List<int>());
                            lootconatiner_itemsRarity.Add(lootcontainer, new List<int>());
                        }

                        lootconatiner_itemsID[lootcontainer].Add(temp_ID);
                        lootconatiner_itemsRarity[lootcontainer].Add(temp_lootRarity);
                    }

                    string temp_description = myLanguageNode.SelectSingleNode("ehd:Description", namespaceManager).Attributes["Text"].Value;
                    string temp_displayName = myLanguageNode.SelectSingleNode("ehd:DisplayName", namespaceManager).Attributes["Text"].Value;

                    bool temp_stackable = temp_maxStackSize > 1;

                    StorableItemTemplate temp_itemTemplate = new StorableItemTemplate(temp_name, temp_displayName, temp_itemType, temp_GUIIconIndex, temp_stackable, temp_maxStackSize, temp_ID, temp_description, temp_worldModelID);
                    m_itemID_itemStorableTemplate.Add(temp_ID, temp_itemTemplate);
                    m_ItemStorableName_index.Add(temp_name, temp_ID);
                }
                catch (Exception ex2)
                {
                    Debug.LogError("ItemManager: failed to load a StorableItem: " + ex2);
                }
            }

            List<RandomItemDispenser> lootDispenser = new List<RandomItemDispenser>();

            foreach (KeyValuePair<int, List<int>> pair in lootconatiner_itemsID)
            {
                RandomItemDispenser dispenser = new RandomItemDispenser(pair.Value.ToArray(), lootconatiner_itemsRarity[pair.Key].ToArray());
                lootDispenser.Add(dispenser);
            }

            m_lootContainersItemsDispenser = lootDispenser.ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogError("ItemManager: error reading storableItems-XML-File: " + ex);
        }
    }

    public StorableItem getRandomItemLootContainer(int lootcontainerIndex)
    {
        return createNewStorableItem(m_lootContainersItemsDispenser[lootcontainerIndex].getRandomItem());
    }

}
