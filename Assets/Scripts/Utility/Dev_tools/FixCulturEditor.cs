using System.Globalization;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
public static class FixCultureEditor
{
    static FixCultureEditor()
    {
        Debug.Log("Mike: this is a temporary fix for broken animations fixed in unity 2019.1. Remove when upgrading to this version. https://issuetracker.unity3d.com/issues/windows-editor-uses-os-locale-settings-i-dot-e-commas-instead-of-dots-in-float-inspector-fields-with-experimental-net-4-dot-6  ");
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
#endif

public static class FixCultureRuntime
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void FixCulture()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
