using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// class to detect if the local player is looking at this object and notifing other classes, so that for example a text can be shown or a button can be pressable
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerRaycastTarget : MonoBehaviour
{
    [SerializeField] private bool m_useInViewText = false;
    [SerializeField] public string m_inViewText = "SAMPLE TEXT";

    private bool m_playerIsLooking = false;

    public bool isPlayerLookingAt { get { return m_playerIsLooking; } }

    /// <summary>
    /// fired during the frame the player starts looking at this object
    /// </summary>
    public event EventHandler<EventArgs> PlayerRaycastStartHitEvent;
    /// <summary>
    /// fired during the frame the player stops looking at this object
    /// </summary>
    public event EventHandler<EventArgs> PlayerRaycastEndHitEvent;
    /// <summary>
    /// fired every frame the player looks at this ovject
    /// </summary>
    public event EventHandler<EventArgs> PlayerRaycastHitEvent;
    /// <summary>
    /// fired every frame the player presses the use button while looking at this object
    /// </summary>
    public event EventHandler<EventArgs> PlayerUsedEvent;
    /// <summary>
    /// fired during the frame the player starts pressing the use button while looking at this object
    /// </summary>
    public event EventHandler<EventArgs> PlayerStartUseEvent;
    /// <summary>
    /// fired during the frame the player stops pressing the use button while looking at this object
    /// </summary>
    public event EventHandler<EventArgs> PlayerStopUseEvent;

    private int m_lastFrameHit = -1;
    private int m_lastFrameUsed = -1;

    protected void Update()
    {
        if (Time.frameCount - m_lastFrameHit == 2)
        {
            onPlayerRaycastEndHit();
        }

        if (Time.frameCount - m_lastFrameUsed == 2)
        {
            onPlayerStopUse();
        }
    }

    protected void OnDestroy()
    {
        if (m_playerIsLooking)
        {
            GUIManager.singleton.setInteractableText("");
        }
    }

    virtual protected void onPlayerRaycastStartHit()
    {
        m_playerIsLooking = true;

        if (m_useInViewText)
        {
            GUIManager.singleton.setInteractableText(m_inViewText);
        }

        EventHandler<EventArgs> handler = PlayerRaycastStartHitEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerRaycastStartHitEvent(this, args);
        }
    }

    virtual protected void onPlayerRaycastEndHit()
    {
        m_playerIsLooking = false;

        if (m_useInViewText)
        {
            GUIManager.singleton.setInteractableText("");
        }

        EventHandler<EventArgs> handler = PlayerRaycastEndHitEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerRaycastEndHitEvent(this, args);
        }
    }

    virtual protected void onPlayerRaycastHit()
    {
        EventHandler<EventArgs> handler = PlayerRaycastHitEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerRaycastHitEvent(this, args);
        }
    }

    virtual protected void onPlayerUsed()
    {
        EventHandler<EventArgs> handler = PlayerUsedEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerUsedEvent(this, args);
        }
    }

    virtual protected void onPlayerStartUse()
    {
        EventHandler<EventArgs> handler = PlayerStartUseEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerStartUseEvent(this, args);
        }
    }

    virtual protected void onPlayerStopUse()
    {
        EventHandler<EventArgs> handler = PlayerStopUseEvent;

        if (handler != null)
        {
            EventArgs args = EventArgs.Empty;

            PlayerStopUseEvent(this, args);
        }
    }

    public void registerPlayerRaycastHit()
    {
        onPlayerRaycastHit();

        if (Time.frameCount - m_lastFrameHit > 1)
        {
            onPlayerRaycastStartHit();
        }

        m_lastFrameHit = Time.frameCount;
    }

    public void registerPlayerUsedAction()
    {
        onPlayerUsed();

        if (Time.frameCount - m_lastFrameUsed > 1)
        {
            onPlayerStartUse();
        }

        m_lastFrameUsed = Time.frameCount;
    }
}
