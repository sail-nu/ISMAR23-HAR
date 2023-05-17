using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loggable : MonoBehaviour
{
    public DataLogger dataLogger;

    public string description;
    private DateTime interactionStartTime;
    private DateTime interactionEndTime;
    private string dateTimeFormat = "dd:MM:yyyy HH:mm:ss:ffff";

    public string GetDescription()
    {
        return description;
    }

    public void SetInteractionStartTime()
    {
        interactionStartTime = System.DateTime.Now;
    }

    public void LogInteraction()
    {
        if (dataLogger && dataLogger.isActiveAndEnabled)
        {        
            interactionEndTime = System.DateTime.Now;
        
            // If no start time was set, use end time
            if (interactionStartTime == null)
            {
                interactionStartTime = interactionEndTime;
            }

            dataLogger.LogInteraction(description, interactionStartTime.ToString(dateTimeFormat), interactionEndTime.ToString(dateTimeFormat));
        }
    }
}
