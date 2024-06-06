using HarmonyLib;
using System;
using System.Reflection;
using UnityEditor.Presets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace CoderScripts.MeshEditorPlus
{
    public class RendererLightingSettingsPatches
    {
        private static readonly Type RendererLightingSettingsType = AccessTools.TypeByName("UnityEditor.RendererLightingSettings");

        private static class Styles
        {

            public static readonly int[] receiveGILightmapValues = new int[2] { 1, 2 };

            public static readonly GUIContent[] receiveGILightmapStrings = new GUIContent[2]
            {
            EditorGUIUtility.TrTextContent("Lightmaps"),
            EditorGUIUtility.TrTextContent("Light Probes")
            };

            public static readonly GUIContent lightingSettings = EditorGUIUtility.TrTextContent("Lighting");

            public static readonly GUIContent importantGI = EditorGUIUtility.TrTextContent("Prioritize Illumination", "When enabled, the object will be marked as a priority object and always included in lighting calculations. Useful for objects that will be strongly emissive to make sure that other objects will be illuminated by this object.");

            public static readonly GUIContent stitchLightmapSeams = EditorGUIUtility.TrTextContent("Stitch Seams", "When enabled, seams in baked lightmaps will get smoothed.");

            public static readonly GUIContent lightmapParameters = EditorGUIUtility.TrTextContent("Lightmap Parameters", "Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");

            public static readonly GUIContent clampedPackingResolution = EditorGUIUtility.TrTextContent("Object's size in the realtime lightmap has reached the maximum size. If you need higher resolution for this object, divide it into smaller meshes.");

            public static readonly GUIContent zeroAreaPackingMesh = EditorGUIUtility.TrTextContent("Mesh used by the renderer has zero UV or surface area. Non zero area is required for lightmapping.");

            public static readonly GUIContent uvOverlap = EditorGUIUtility.TrTextContent("This GameObject has overlapping UVs. Please adjust Mesh Importer settings or increase chart padding in your modeling package.");

            public static readonly GUIContent scaleInLightmap = EditorGUIUtility.TrTextContent("Scale In Lightmap", "Specifies the relative size of object's UVs within a lightmap. A value of 0 will result in the object not being lightmapped, but still contribute lighting to other objects in the Scene.");

            public static readonly GUIContent albedoScale = EditorGUIUtility.TrTextContent("Albedo Scale", "Specifies the relative size of object's UVs within its albedo texture that is used when calculating the influence on surrounding objects.");

            public static readonly GUIContent lightmapSettings = EditorGUIUtility.TrTextContent("Lightmapping");

            public static readonly GUIContent castShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "Specifies whether a geometry creates shadows or not when a shadow-casting Light shines on it.");

            public static readonly GUIContent receiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "When enabled, any shadows cast from other objects are drawn on the geometry.");

            public static readonly GUIContent staticShadowCaster = EditorGUIUtility.TrTextContent("Static Shadow Caster", "When enabled, Unity considers this renderer as being static for the sake of shadow rendering. If the SRP implements cached shadow maps, this field indicates to the render pipeline what renderers are considered static and what renderers are considered dynamic.");

            public static readonly GUIContent receiveGITitle = EditorGUIUtility.TrTextContent("Receive Global Illumination", "If enabled, this GameObject receives global illumination from lightmaps or Light Probes. To use lightmaps, Contribute Global Illumination must be enabled.");

            public static readonly GUIContent giNotEnabledInfo = EditorGUIUtility.TrTextContent("Lightmapping settings are currently disabled. Enable Baked Global Illumination or Realtime Global Illumination to display these settings.");

            public static readonly GUIContent isPresetInfo = EditorGUIUtility.TrTextContent("The Contribute Global Illumination property cannot be stored in a preset.");
        }

        public static bool Skip = true;

        [HarmonyPatch]
        class RendererLightingSettings
        {
            [HarmonyTargetMethod]
            static MethodBase TargetMethod() => AccessTools.Method(RendererLightingSettingsType, "RenderSettings");

            [HarmonyPrefix]
            static bool RenderSettingsPrefix(object __instance, bool showLightmapSettings)
            {
                if (Skip)
                    return true;
                var _base = Traverse.Create(__instance);
                var m_SerializedObject = _base.Field<SerializedObject>("m_SerializedObject");
                var m_GameObjectsSerializedObject = _base.Field<SerializedObject>("m_GameObjectsSerializedObject");
                var targetObjectsCount = _base.Field("m_GameObjectsSerializedObject").Property<int>("targetObjectsCount");
                var m_ReceiveGI = _base.Field<SerializedProperty>("m_ReceiveGI");
                var isPreset = _base.Property<bool>("isPreset");
                var showLightingSettings = _base.Property("showLightingSettings").Property<bool>("value");
                var m_StaticEditorFlags = _base.Field<SerializedProperty>("m_StaticEditorFlags");
                var isPrefabAsset = _base.Property<bool>("isPrefabAsset");
                var hasMultipleDifferentValuesBitwise = _base.Field("m_StaticEditorFlags").Property<int>("hasMultipleDifferentValuesBitwise");
                var m_CastShadows = _base.Field<SerializedProperty>("m_CastShadows");
                var m_ReceiveShadows = _base.Field<SerializedProperty>("m_ReceiveShadows");
                var m_StaticShadowCaster = _base.Field<SerializedProperty>("m_StaticShadowCaster");
                var m_ImportantGI = _base.Field<SerializedProperty>("m_ImportantGI");
                var m_StitchLightmapSeams = _base.Field<SerializedProperty>("m_StitchLightmapSeams");
                var m_LightmapParameters = _base.Field<SerializedProperty>("m_LightmapParameters");
                var m_Renderers = _base.Field<IEnumerable<Renderer>>("m_Renderers").Value.ToArray();
                var thisShowLightmapSettings = _base.Property("showLightmapSettings").Property<bool>("value");

                /*
                var _styles = _base;
                var lightingSettings = _styles.Field<GUIContent>("lightingSettings").Value;
                var castShadows = _styles.Field<GUIContent>("castShadows").Value;
                var receiveShadows = _styles.Field<GUIContent>("receiveShadows").Value;
                var staticShadowCaster = _styles.Field<GUIContent>("staticShadowCaster").Value;
                var isPresetInfo = _styles.Field<GUIContent>("isPresetInfo").Value;
                var giNotEnabledInfo = _styles.Field<GUIContent>("giNotEnabledInfo").Value;
                var receiveGITitle = _styles.Field<GUIContent>("receiveGITitle").Value;
                var receiveGILightmapStrings = _styles.Field<IEnumerable<GUIContent>>("receiveGILightmapStrings").Value.ToArray();
                var receiveGILightmapValues = _styles.Field<IEnumerable<int>>("receiveGILightmapValues").Value.ToArray();
                var importantGI = _styles.Field<GUIContent>("importantGI").Value;
                var albedoScale = _styles.Field<GUIContent>("albedoScale").Value;
                var lightmapSettings = _styles.Field<GUIContent>("lightmapSettings").Value;
                var scaleInLightmap = _styles.Field<GUIContent>("scaleInLightmap").Value;
                var stitchLightmapSeams = _styles.Field<GUIContent>("stitchLightmapSeams").Value;
                var lightmapParameters = _styles.Field<GUIContent>("lightmapParameters").Value;
                var zeroAreaPackingMesh = _styles.Field<GUIContent>("zeroAreaPackingMesh").Value;
                var clampedPackingResolution = _styles.Field<GUIContent>("clampedPackingResolution").Value;
                var uvOverlap = _styles.Field<GUIContent>("uvOverlap").Value;
                */

                var _lightmapping = Traverse.Create(typeof(Lightmapping));
                var GetLightingSettingsOrDefaultsFallback = _lightmapping.Method("GetLightingSettingsOrDefaultsFallback");
                var IsUsingDeferredRenderingPath = Traverse.Create(typeof(SceneView)).Method("IsUsingDeferredRenderingPath");
                var ContributeGISettings = _base.Method("ContributeGISettings");
                var LightmapScaleGUI = _base.Method("LightmapScaleGUI", new Type[] {typeof(bool), typeof(GUIContent), typeof(bool)});
                var LightmapParametersGUI = _base.Method("LightmapParametersGUI", new Type[] {typeof(SerializedProperty), typeof(GUIContent)});
                var RendererUVSettings = _base.Method("RendererUVSettings");
                var ShowAtlasGUI = _base.Method("ShowAtlasGUI", new Type[] {typeof(int), typeof(bool)});
                var ShowRealtimeLMGUI = _base.Method("ShowRealtimeLMGUI", new Type[] {typeof(Renderer)});
                var HasZeroAreaMesh = _lightmapping.Method("HasZeroAreaMesh", new Type[] { typeof(Renderer) });
                var DisplayMeshWarning = _base.Method("DisplayMeshWarning");
                var HasClampedResolution = _lightmapping.Method("HasClampedResolution", new Type[] { typeof(Renderer) });
                var HasUVOverlaps = _base.Method("HasUVOverlaps", new Type[] { typeof(Renderer) });
                


                if (m_SerializedObject.Value == null || m_GameObjectsSerializedObject.Value == null || targetObjectsCount.Value == 0)
                {
                    return false;
                }
                LightingSettings lightingSettingsOrDefaultsFallback = GetLightingSettingsOrDefaultsFallback.GetValue<LightingSettings>();
                LightingSettings.Lightmapper lightmapper = lightingSettingsOrDefaultsFallback.lightmapper;
                bool bakedGI = lightingSettingsOrDefaultsFallback.bakedGI;
                bool realtimeGI = lightingSettingsOrDefaultsFallback.realtimeGI;
                m_GameObjectsSerializedObject.Value.Update();
                ReceiveGI receiveGI = (ReceiveGI)m_ReceiveGI.Value.intValue;
                bool flag = isPreset.Value || (m_StaticEditorFlags.Value.intValue & 1) != 0;
                bool flag2 = (isPreset.Value || isPrefabAsset.Value || realtimeGI || (bakedGI && lightmapper == LightingSettings.Lightmapper.ProgressiveCPU)) && SupportedRenderingFeatures.active.enlighten;
                bool flag3 = m_ReceiveGI.Value.hasMultipleDifferentValues || (!isPreset.Value && (hasMultipleDifferentValuesBitwise.Value & 1) != 0);
                //showLightingSettings.Value = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingSettings.Value, Styles.lightingSettings);
                //if (showLightingSettings.Value)
                showLightingSettings.Value = StylesManager.Foldout(showLightingSettings.Value, Styles.lightingSettings, body: () =>
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_CastShadows.Value, Styles.castShadows, true);
                    bool disabled = IsUsingDeferredRenderingPath.GetValue<bool>();
                    if (SupportedRenderingFeatures.active.receiveShadows)
                    {
                        using (new EditorGUI.DisabledScope(disabled))
                        {
                            EditorGUILayout.PropertyField(m_ReceiveShadows.Value, Styles.receiveShadows, true);
                        }
                    }
                    if (m_CastShadows.Value.hasMultipleDifferentValues || m_CastShadows.Value.intValue != 0)
                    {
                        RenderPipelineAsset currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
                        if (currentRenderPipeline != null)
                        {
                            EditorGUILayout.PropertyField(m_StaticShadowCaster.Value, Styles.staticShadowCaster);
                        }
                    }
                    if (!showLightmapSettings)
                    {
                        EditorGUI.indentLevel--;
                        //EditorGUILayout.EndFoldoutHeaderGroup();
                        return;
                    }
                    using (new EditorGUI.DisabledScope(isPreset.Value))
                    {
                        flag = ContributeGISettings.GetValue<bool>();
                    }
                    if (isPreset.Value)
                    {
                        EditorGUILayout.HelpBox(Styles.isPresetInfo.text, MessageType.Info);
                    }
                    if (!(bakedGI || realtimeGI) && flag && !isPrefabAsset.Value && !isPreset.Value)
                    {
                        EditorGUILayout.HelpBox(Styles.giNotEnabledInfo.text, MessageType.Info);
                        EditorGUI.indentLevel--;
                        //EditorGUILayout.EndFoldoutHeaderGroup();
                        return;
                    }
                    if (flag)
                    {
                        Rect controlRect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(controlRect, Styles.receiveGITitle, m_ReceiveGI.Value);
                        EditorGUI.BeginChangeCheck();
                        receiveGI = (ReceiveGI)EditorGUI.IntPopup(controlRect, Styles.receiveGITitle, (int)receiveGI, Styles.receiveGILightmapStrings, Styles.receiveGILightmapValues);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_ReceiveGI.Value.intValue = (int)receiveGI;
                        }
                        EditorGUI.EndProperty();
                        if (flag2)
                        {
                            EditorGUILayout.PropertyField(m_ImportantGI.Value, Styles.importantGI);
                        }
                        if (receiveGI == ReceiveGI.LightProbes && !flag3)
                        {
                            LightmapScaleGUI.GetValue(true, Styles.albedoScale, true);
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.showMixedValue = flag3;
                            receiveGI = (ReceiveGI)EditorGUILayout.IntPopup(Styles.receiveGITitle, 2, Styles.receiveGILightmapStrings, Styles.receiveGILightmapValues);
                            EditorGUI.showMixedValue = false;
                        }
                    }
                    EditorGUI.indentLevel--;
                });
                //EditorGUILayout.EndFoldoutHeaderGroup();
                if (!(showLightmapSettings && flag) || receiveGI != ReceiveGI.Lightmaps || flag3)
                {
                    return false;
                }
                //thisShowLightmapSettings.Value = EditorGUILayout.BeginFoldoutHeaderGroup(thisShowLightmapSettings.Value, Styles.lightmapSettings);
                //if (thisShowLightmapSettings.Value)
                
                thisShowLightmapSettings.Value = StylesManager.Foldout(thisShowLightmapSettings.Value, Styles.lightmapSettings, body: () =>
                {
                    EditorGUILayout.Space(2f);
                    EditorGUI.indentLevel++;
                    bool flag4 = isPreset.Value || isPrefabAsset.Value || (bakedGI && lightmapper != LightingSettings.Lightmapper.ProgressiveCPU);
                    LightmapScaleGUI.GetValue(true, Styles.scaleInLightmap, false);
                    if (flag4)
                    {
                        EditorGUILayout.PropertyField(m_StitchLightmapSeams.Value, Styles.stitchLightmapSeams);
                    }
                    LightmapParametersGUI.GetValue(m_LightmapParameters.Value, Styles.lightmapParameters);
                    if (flag2)
                    {
                        RendererUVSettings.GetValue();
                    }
                    if (m_Renderers != null && m_Renderers.Length != 0)
                    {
                        ShowAtlasGUI.GetValue(m_Renderers[0].GetInstanceID(), true);
                        ShowRealtimeLMGUI.GetValue(m_Renderers[0]);
                        if (HasZeroAreaMesh.GetValue<bool>(m_Renderers[0]))
                        {
                            EditorGUILayout.HelpBox(Styles.zeroAreaPackingMesh.text, MessageType.Warning);
                        }
                        DisplayMeshWarning.GetValue();
                        if (flag2 && HasClampedResolution.GetValue<bool>(m_Renderers[0]))
                        {
                            EditorGUILayout.HelpBox(Styles.clampedPackingResolution.text, MessageType.Warning);
                        }
                        if (flag4 && HasUVOverlaps.GetValue<bool>(m_Renderers[0]))
                        {
                            EditorGUILayout.HelpBox(Styles.uvOverlap.text, MessageType.Warning);
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2f);
                });
                EditorGUILayout.EndFoldoutHeaderGroup();
                return false;
            }

        }
    }


}
