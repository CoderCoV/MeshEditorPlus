using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal class Probes
{
    private SerializedProperty m_LightProbeUsage;

    internal bool IsExpanded
    {
        get => m_LightProbeUsage.isExpanded;
        set => m_LightProbeUsage.isExpanded = value;
    }

    private SerializedProperty m_LightProbeVolumeOverride;

    private SerializedProperty m_ReflectionProbeUsage;

    private SerializedProperty m_ProbeAnchor;

    private SerializedProperty m_ReceiveShadows;

    private GUIContent m_LightProbeUsageStyle = EditorGUIUtility.TrTextContent("Light Probes", "Specifies how Light Probes will handle the interpolation of lighting and occlusion. Disabled if the object is set to receive Global Illumination from lightmaps.");

    private GUIContent m_LightProbeVolumeOverrideStyle = EditorGUIUtility.TrTextContent("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");

    private GUIContent m_ReflectionProbeUsageStyle = EditorGUIUtility.TrTextContent("Reflection Probes", "Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");

    private GUIContent m_ProbeAnchorStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes and reflection probes.");

    private GUIContent m_ProbeAnchorNoReflectionProbesStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes.");

    private GUIContent m_DeferredNote = EditorGUIUtility.TrTextContent("In Deferred Shading, all objects receive shadows and get per-pixel reflection probes.");

    private GUIContent m_LightProbeVolumeNote = EditorGUIUtility.TrTextContent("A valid Light Probe Proxy Volume component could not be found.");

    private GUIContent m_LightProbeVolumeUnsupportedNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");

    private GUIContent m_LightProbeVolumeUnsupportedOnTreesNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is not supported on tree rendering. Simple 'Blend Probes' mode will be used instead.");

    private GUIContent m_LightProbeCustomNote = EditorGUIUtility.TrTextContent("The Custom Provided mode requires SH properties to be sent via MaterialPropertyBlock.");

    private GUIContent[] m_ReflectionProbeUsageOptions = (from x in (from x in Enum.GetNames(typeof(ReflectionProbeUsage))
                                                                     select ObjectNames.NicifyVariableName(x)).ToArray()
                                                          select new GUIContent(x)).ToArray();

    private List<ReflectionProbeBlendInfo> m_BlendInfo = new List<ReflectionProbeBlendInfo>();

    private GUIContent probeAnchorStyle
    {
        get
        {
            if (!SupportedRenderingFeatures.active.reflectionProbes)
            {
                return m_ProbeAnchorNoReflectionProbesStyle;
            }
            return m_ProbeAnchorStyle;
        }
    }

    internal void Initialize(SerializedObject serializedObject)
    {
        m_LightProbeUsage = serializedObject.FindProperty("m_LightProbeUsage");
        m_LightProbeVolumeOverride = serializedObject.FindProperty("m_LightProbeVolumeOverride");
        m_ReflectionProbeUsage = serializedObject.FindProperty("m_ReflectionProbeUsage");
        m_ProbeAnchor = serializedObject.FindProperty("m_ProbeAnchor");
        m_ReceiveShadows = serializedObject.FindProperty("m_ReceiveShadows");
    }

    internal bool IsUsingLightProbeProxyVolume(int selectionCount)
    {
        return (selectionCount == 1 && m_LightProbeUsage.intValue == 2) || (selectionCount > 1 && !m_LightProbeUsage.hasMultipleDifferentValues && m_LightProbeUsage.intValue == 2);
    }

    internal void RenderLightProbeProxyVolumeWarningNote(Renderer renderer, int selectionCount)
    {

        var AreLightProbesAllowed = Traverse.Create(typeof(LightProbes)).Method("AreLightProbesAllowed", new Type[] { typeof(Renderer) });
        //LightProbes.AreLightProbesAllowed(renderer)
        if (!IsUsingLightProbeProxyVolume(selectionCount))
        {
            return;
        }
        if (LightProbeProxyVolume.isFeatureSupported && SupportedRenderingFeatures.active.lightProbeProxyVolumes)
        {
            LightProbeProxyVolume component = renderer.GetComponent<LightProbeProxyVolume>();
            bool flag = renderer.lightProbeProxyVolumeOverride == null || renderer.lightProbeProxyVolumeOverride.GetComponent<LightProbeProxyVolume>() == null;
            if (component == null && flag && AreLightProbesAllowed.GetValue<bool>(renderer))
            {
                EditorGUILayout.HelpBox(m_LightProbeVolumeNote.text, MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(m_LightProbeVolumeUnsupportedNote.text, MessageType.Warning);
        }
    }

    internal void RenderReflectionProbeUsage(bool useMiniStyle, bool isDeferredRenderingPath, bool isDeferredReflections)
    {
        var Popup = Traverse.Create(typeof(EditorGUILayout)).Method("Popup", new object[] { m_ReflectionProbeUsage, m_ReflectionProbeUsageOptions, m_ReflectionProbeUsageStyle, new GUILayoutOption[0] });
        var GUIPopup = Traverse.Create("ModuleUI").Method("GUIPopup");


        if (!SupportedRenderingFeatures.active.reflectionProbes)
        {
            return;
        }
        using (new EditorGUI.DisabledScope(isDeferredRenderingPath))
        {
            if (!useMiniStyle)
            {
                if (isDeferredReflections)
                {
                    EditorGUILayout.EnumPopup(m_ReflectionProbeUsageStyle, (m_ReflectionProbeUsage.intValue != 0) ? ReflectionProbeUsage.Simple : ReflectionProbeUsage.Off);
                }
                else
                {
                    Popup.GetValue(m_ReflectionProbeUsage, m_ReflectionProbeUsageOptions, m_ReflectionProbeUsageStyle, null);
                }
            }
            else if (isDeferredReflections)
            {
                GUIPopup.GetValue(m_ReflectionProbeUsageStyle, 3, m_ReflectionProbeUsageOptions);
            }
            else
            {
                GUIPopup.GetValue(m_ReflectionProbeUsageStyle, m_ReflectionProbeUsage, m_ReflectionProbeUsageOptions);
            }
        }
    }

    internal void RenderLightProbeUsage(int selectionCount, Renderer renderer, bool useMiniStyle, bool lightProbeAllowed)
    {
        var GUIObject = Traverse.Create("ModuleUI").Method("GUIObject");
        var GUIEnumPopup = Traverse.Create("ModuleUI").Method("GUIEnumPopup");

        using (new EditorGUI.DisabledScope(!lightProbeAllowed))
        {
            if (lightProbeAllowed)
            {
                if (useMiniStyle)
                {
                    EditorGUI.BeginChangeCheck();
                    Enum @enum = GUIEnumPopup.GetValue<Enum>(m_LightProbeUsageStyle, (LightProbeUsage)m_LightProbeUsage.intValue, m_LightProbeUsage);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_LightProbeUsage.intValue = (int)(LightProbeUsage)(object)@enum;
                    }
                }
                else
                {
                    Rect controlRect = EditorGUILayout.GetControlRect(true, 18f, EditorStyles.popup);
                    EditorGUI.BeginProperty(controlRect, m_LightProbeUsageStyle, m_LightProbeUsage);
                    EditorGUI.BeginChangeCheck();
                    Enum enum2 = EditorGUI.EnumPopup(controlRect, m_LightProbeUsageStyle, (LightProbeUsage)m_LightProbeUsage.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_LightProbeUsage.intValue = (int)(LightProbeUsage)(object)enum2;
                    }
                    EditorGUI.EndProperty();
                }
                if (!m_LightProbeUsage.hasMultipleDifferentValues)
                {
                    if (m_LightProbeUsage.intValue == 2 && SupportedRenderingFeatures.active.lightProbeProxyVolumes)
                    {
                        EditorGUI.indentLevel++;
                        if (useMiniStyle)
                        {
                            GUIObject.GetValue(m_LightProbeVolumeOverrideStyle, m_LightProbeVolumeOverride);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(m_LightProbeVolumeOverride, m_LightProbeVolumeOverrideStyle);
                        }
                        EditorGUI.indentLevel--;
                    }
                    else if (m_LightProbeUsage.intValue == 4)
                    {
                        EditorGUI.indentLevel++;
                        if (!Application.isPlaying)
                        {
                            EditorGUILayout.HelpBox(m_LightProbeCustomNote.text, MessageType.Info);
                        }
                        else if (!renderer.HasPropertyBlock())
                        {
                            EditorGUILayout.HelpBox(m_LightProbeCustomNote.text, MessageType.Error);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else if (useMiniStyle)
            {
                GUIEnumPopup.GetValue(m_LightProbeUsageStyle, LightProbeUsage.Off, m_LightProbeUsage);
            }
            else
            {
                EditorGUILayout.EnumPopup(m_LightProbeUsageStyle, LightProbeUsage.Off);
            }
        }
        renderer.TryGetComponent<Tree>(out var component);
        if (component != null && m_LightProbeUsage.intValue == 2)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(m_LightProbeVolumeUnsupportedOnTreesNote.text, MessageType.Warning);
            EditorGUI.indentLevel--;
        }
    }

    internal bool RenderProbeAnchor(bool useMiniStyle)
    {
        var GUIObject = Traverse.Create("ModuleUI").Method("GUIObject");
        bool flag = !m_ReflectionProbeUsage.hasMultipleDifferentValues && m_ReflectionProbeUsage.intValue != 0 && SupportedRenderingFeatures.active.reflectionProbes;
        bool flag2 = !m_LightProbeUsage.hasMultipleDifferentValues && m_LightProbeUsage.intValue != 0;
        bool flag3 = flag || flag2;
        if (flag3)
        {
            if (!useMiniStyle)
            {
                EditorGUILayout.PropertyField(m_ProbeAnchor, probeAnchorStyle);
            }
            else
            {
                GUIObject.GetValue(probeAnchorStyle, m_ProbeAnchor);
            }
        }
        return flag3;
    }

    internal void OnGUI(UnityEngine.Object[] selection, Renderer renderer, bool useMiniStyle)
    {
        EditorGUI.indentLevel++;
        var IsUsingDeferredRenderingPath = Traverse.Create(typeof(SceneView)).Method("IsUsingDeferredRenderingPath");
        var AreLightProbesAllowed = Traverse.Create(typeof(LightProbes)).Method("AreLightProbesAllowed", new Type[] { typeof(Renderer) });

        int selectionCount = 1;
        bool flag = IsUsingDeferredRenderingPath.GetValue<bool>();
        bool flag2 = flag && GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled;
        bool lightProbeAllowed = true;
        if (selection != null)
        {
            foreach (UnityEngine.Object @object in selection)
            {
                if (!AreLightProbesAllowed.GetValue<bool>((Renderer)@object))
                {
                    lightProbeAllowed = false;
                    break;
                }
            }
            selectionCount = selection.Length;
        }
        RenderLightProbeUsage(selectionCount, renderer, useMiniStyle, lightProbeAllowed);
        RenderLightProbeProxyVolumeWarningNote(renderer, selectionCount);
        RenderReflectionProbeUsage(useMiniStyle, flag, flag2);
        bool flag3 = RenderProbeAnchor(useMiniStyle);
        if (flag3 && !m_ReflectionProbeUsage.hasMultipleDifferentValues && m_ReflectionProbeUsage.intValue != 0 && SupportedRenderingFeatures.active.reflectionProbes && !flag2)
        {
            renderer.GetClosestReflectionProbes(m_BlendInfo);
            ShowClosestReflectionProbes(m_BlendInfo);
        }
        bool flag4 = !m_ReceiveShadows.hasMultipleDifferentValues && m_ReceiveShadows.boolValue;
        if ((flag && flag4) || (flag2 && flag3))
        {
            EditorGUILayout.HelpBox(m_DeferredNote.text, MessageType.Info);
        }
        EditorGUI.indentLevel--;

    }

    internal static void ShowClosestReflectionProbes(List<ReflectionProbeBlendInfo> blendInfos)
    {
        float num = 20f;
        float num2 = 70f;
        using (new EditorGUI.DisabledScope(disabled: true))
        {
            for (int i = 0; i < blendInfos.Count; i++)
            {
                Rect rect = GUILayoutUtility.GetRect(0f, 18f);
                rect = EditorGUI.IndentedRect(rect);
                float width = rect.width - num - num2;
                Rect position = rect;
                position.width = num;
                GUI.Label(position, "#" + i, EditorStyles.miniLabel);
                position.x += position.width;
                position.width = width;
                EditorGUI.ObjectField(position, blendInfos[i].probe, typeof(ReflectionProbe), allowSceneObjects: true);
                position.x += position.width;
                position.width = num2;
                GUI.Label(position, "Weight " + blendInfos[i].weight.ToString("f2", CultureInfo.InvariantCulture.NumberFormat), EditorStyles.miniLabel);
            }
        }
    }

    internal static string[] GetFieldsStringArray()
    {
        return new string[4] { "m_LightProbeUsage", "m_LightProbeVolumeOverride", "m_ReflectionProbeUsage", "m_ProbeAnchor" };
    }
}
