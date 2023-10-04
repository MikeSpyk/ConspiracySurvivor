
public struct StorableItemTemplate
{
    public enum ItemType { Default = 0, NormalGun = 1, Simple = 2 } // needed to determine which GUI-type to display to this item. For example: a gun has attachments the GUI needs to show while a Resource has no additional infomations

    public StorableItemTemplate(string name, string displayName, ItemType itemType, int GUIIconIndex, bool stackable, int maxStackSize, int ID , string description, int worldModelIndex)
    {
        m_name = name;
        m_displayName = displayName;
        m_itemType = itemType;
        m_GUIIconIndex = GUIIconIndex;
        m_stackable = stackable;
        m_maxStackSize = maxStackSize;
        m_description = description;
        m_ID = ID;
        m_worldModelIndex = worldModelIndex;
    }

    public string m_name;
    public string m_displayName;
    public ItemType m_itemType;
    public int m_GUIIconIndex;
    public bool m_stackable;
    public int m_maxStackSize;
    public string m_description;
    public int m_ID;
    public int m_worldModelIndex;
}
