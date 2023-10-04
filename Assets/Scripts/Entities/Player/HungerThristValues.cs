using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HungerThristValues
{
    private static Dictionary<HungerEffects, float> m_hungerValues = new Dictionary<HungerEffects, float>();
    private static Dictionary<ThirstEffects, float> m_thirstValues = new Dictionary<ThirstEffects, float>();

    static HungerThristValues()
    {
        m_hungerValues.Add(HungerEffects.Base, 0.00347f); // you need to eat at the very latest 8h realtime
        m_hungerValues.Add(HungerEffects.Moving, 0.014f);

        m_thirstValues.Add(ThirstEffects.Base, 0.00347f); // you need to drink at the very latest 8h realtime
        m_thirstValues.Add(ThirstEffects.Moving, 0.05f);
    }

    public static float getHungerValue(HungerEffects effect)
    {
        return m_hungerValues[effect];
    }

    public static float getThirstValue(ThirstEffects effect)
    {
        return m_thirstValues[effect];
    }
}
