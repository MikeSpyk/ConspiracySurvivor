using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// same as EventTrigger but with event handlers for each event
/// </summary>
public class AdvancedEventTrigger : EventTrigger
{
    public event EventHandler<PointerEventData> pointerEnterEvent;
    public event EventHandler<PointerEventData> pointerExitEvent;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        EventHandler<PointerEventData> handler = pointerEnterEvent;

        if (handler != null)
        {
            pointerEnterEvent(this, eventData);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        EventHandler<PointerEventData> handler = pointerExitEvent;

        if (handler != null)
        {
            pointerExitEvent(this, eventData);
        }
    }

}
