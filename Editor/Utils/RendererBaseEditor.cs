using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CoderScripts.MeshEditorPlus
{
    internal class RendererEditorBase : Editor
    {

        private static readonly Type SortingLayerEditorUtilityType = AccessTools.TypeByName("UnityEditor.SortingLayerEditorUtility");
        private static readonly Type RendererLightingSettingsType = AccessTools.TypeByName("UnityEditor.RendererLightingSettings");
        private static readonly Type SavedBoolType = AccessTools.TypeByName("UnityEditor.SavedBool");
        private static readonly ConstructorInfo lightingSettingsCtor = AccessTools.Constructor(RendererLightingSettingsType, new Type[] { typeof(SerializedObject) });
        private static readonly ConstructorInfo savedBoolCtor = AccessTools.Constructor(SavedBoolType, new Type[] { typeof(string), typeof(bool) });

        protected SerializedProperty m_Materials;
        private MaterialRlist m_MaterialRList;
        private Vector2 m_MaterialScroll = Vector2.zero;

        private static string[] m_DefaultRenderingLayerNames;
        private static string[] m_DefaultPrefixedRenderingLayerNames;
        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;
        private SerializedProperty m_DynamicOccludee;
        private SerializedProperty m_RenderingLayerMask;
        private SerializedProperty m_RendererPriority;
        private SerializedProperty m_SkinnedMotionVectors;
        private SerializedProperty m_MotionVectors; 
        internal static string[] defaultRenderingLayerNames
        {
            get
            {
                if (m_DefaultRenderingLayerNames == null)
                {
                    m_DefaultRenderingLayerNames = new string[32];
                    for (int i = 0; i < m_DefaultRenderingLayerNames.Length; i++)
                    {
                        m_DefaultRenderingLayerNames[i] = $"Layer{i + 1}";
                    }
                }
                return m_DefaultRenderingLayerNames;
            }
        }
        internal static string[] defaultPrefixedRenderingLayerNames
        {
            get
            {
                if (m_DefaultPrefixedRenderingLayerNames == null)
                {
                    m_DefaultPrefixedRenderingLayerNames = new string[32];
                    for (int i = 0; i < m_DefaultPrefixedRenderingLayerNames.Length; i++)
                    {
                        m_DefaultPrefixedRenderingLayerNames[i] = $"{i}: {defaultRenderingLayerNames[i]}";
                    }
                }
                return m_DefaultPrefixedRenderingLayerNames;
            }
        }
        private SerializedProperty m_RayTracingMode;
        private SerializedProperty m_RayTraceProcedural;
        private Probes m_Probes;
        private SavedBool m_ShowProbeSettings;
        private SavedBool m_ShowOtherSettings;
        private SavedBool m_ShowRayTracingSettings;
        protected Traverse m_Lighting;

        public virtual void OnEnable() 
        {
            m_Materials = serializedObject.FindProperty("m_Materials");
            m_MaterialRList = new MaterialRlist(m_Materials);

            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_DynamicOccludee = serializedObject.FindProperty("m_DynamicOccludee");
            m_RenderingLayerMask = base.serializedObject.FindProperty("m_RenderingLayerMask");
            m_RendererPriority = base.serializedObject.FindProperty("m_RendererPriority");
            m_MotionVectors = base.serializedObject.FindProperty("m_MotionVectors");
            m_SkinnedMotionVectors = base.serializedObject.FindProperty("m_SkinnedMotionVectors");

            m_RayTracingMode = base.serializedObject.FindProperty("m_RayTracingMode");
            m_RayTraceProcedural = base.serializedObject.FindProperty("m_RayTraceProcedural");
            m_ShowRayTracingSettings = new SavedBool($"{target.GetType()}.ShowRayTracingSettings", true);

            m_ShowProbeSettings = new SavedBool($"{target.GetType()}.ShowProbeSettings", false);
            m_ShowOtherSettings = new SavedBool($"{target.GetType()}.ShowOtherSettings", false);
            var instance = lightingSettingsCtor.Invoke(new object[] { serializedObject });
            m_Lighting = Traverse.Create(instance);

            m_Lighting.Property("showLightingSettings").SetValue(savedBoolCtor.Invoke(new object[] { $"{base.target.GetType()}.ShowLightingSettings", true }));
            m_Lighting.Property("showLightmapSettings").SetValue(savedBoolCtor.Invoke(new object[] { $"{base.target.GetType()}.ShowLightmapSettings", true }));
            m_Lighting.Property("showBakedLightmap").SetValue(savedBoolCtor.Invoke(new object[] { $"{base.target.GetType()}.ShowBakedLightmapSettings", false }));
            m_Lighting.Property("showRealtimeLightmap").SetValue(savedBoolCtor.Invoke(new object[] { $"{base.target.GetType()}.ShowRealtimeLightmapSettings", false }));

            m_Probes = new Probes();
            m_Probes.Initialize(serializedObject);
        }

        public virtual void OnDisable()
        {
            RendererLightingSettingsPatches.Skip = true;
        }

        public void OnMaterialGUI()
        {
            var button = EditorGUIUtility.IconContent("Toolbar Plus", "|Add Element");
            m_Materials.isExpanded = StylesManager.Foldout(m_Materials.isExpanded, new GUIContent("Materials"), button, () =>
            {
                int num2 = m_Materials.arraySize;
                var ScrollHeight = num2 > 7 ? 8 * 21 : m_MaterialRList.GetHeight();
                m_MaterialScroll = GUILayout.BeginScrollView(m_MaterialScroll, false, false, GUILayout.Height(ScrollHeight));

                m_MaterialRList.DrawList();

                GUILayout.EndScrollView();
            }, (Rect rect) =>
            {
                if (m_Materials.isExpanded)
                    if (GUI.Button(rect, button, StylesManager.Styles.ShurikenFoldoutButton))
                        m_MaterialRList.Add();
            }, hierarchy: true);
        }

        public void OnOtherSettingsGUI(bool showMotionVectors, bool showSkinnedMotionVectors = false, bool showLightmappSettings = false, bool showSortingLayerFields = false)
        {
            
            m_ShowOtherSettings.value = StylesManager.Foldout(m_ShowOtherSettings, new GUIContent("Other"), body: () =>
            {
                StylesManager.BeginContainer();
                GUILayout.Space(2f);

                OnLightingGUI(showLightmappSettings);
                OnRayTracingGUI();
                EditorGUILayout.Space(2f);
                OnProbesGUI();
                EditorGUILayout.Space(2f);
                OtherSettingsGUI(showMotionVectors, showSkinnedMotionVectors, showSortingLayerFields);

                GUILayout.Space(3f);
                StylesManager.EndContainer();
            }, hierarchy: true);
        }

        public void OnLightingGUI(bool showLightmappSettings)
        {
            var RenderSettings = m_Lighting.Method("RenderSettings", new Type[] { typeof(bool)});
            RendererLightingSettingsPatches.Skip = false;
            RenderSettings.GetValue(showLightmappSettings);
        }

        public void OnRayTracingGUI()
        {
            if (SystemInfo.supportsRayTracing)
            {
                var Popup = Traverse.Create(typeof(EditorGUILayout)).Method("Popup", new Type[] { typeof(SerializedProperty), typeof(GUIContent[]), typeof(GUIContent), typeof(GUILayoutOption[]) });

                m_ShowRayTracingSettings.value = StylesManager.Foldout(m_ShowRayTracingSettings.value, new GUIContent("Ray Tracing"), body: () =>
                {
                    EditorGUILayout.Space(2f);
                    EditorGUI.indentLevel++;
                    Popup.GetValue(m_RayTracingMode, Styles.rayTracingModeOptions, Styles.rayTracingModeStyle, null);
                    EditorGUILayout.PropertyField(m_RayTraceProcedural, Styles.rayTracingGeomStyle);
                    EditorGUI.indentLevel--;
                });

            }
        }

        private void OnProbesGUI()
        {
            m_ShowProbeSettings.value = StylesManager.Foldout(m_ShowProbeSettings, new GUIContent("Probes"), body: () =>
            {
                m_Probes.OnGUI(targets, (Renderer)target, false);
            });
        }

        public void OtherSettingsGUI(bool showMotionVectors, bool showSkinnedMotionVectors = false, bool showSortingLayerFields = false)
        {
            var RenderSortingLayerFields = Traverse.Create(SortingLayerEditorUtilityType).Method("RenderSortingLayerFields", new object[] { m_SortingOrder, m_SortingLayerID });

            //m_MotionVectors.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_MotionVectors.isExpanded, Styles.otherSettings);
            //if (m_MotionVectors.isExpanded)

            m_MotionVectors.isExpanded = StylesManager.Foldout(m_MotionVectors.isExpanded, Styles.otherSettings, body: () =>
            {
                EditorGUI.indentLevel++;
                if (SupportedRenderingFeatures.active.motionVectors)
                {
                    if (showMotionVectors)
                    {
                        EditorGUILayout.PropertyField(m_MotionVectors, Styles.motionVectors, true);
                    }
                    else if (showSkinnedMotionVectors)
                    {
                        EditorGUILayout.PropertyField(m_SkinnedMotionVectors, Styles.skinnedMotionVectors, true);
                    }
                }
                EditorGUILayout.PropertyField(m_DynamicOccludee, Styles.dynamicOcclusion);
                if (showSortingLayerFields)
                {
                    RenderSortingLayerFields.GetValue(m_SortingOrder, m_SortingLayerID);
                }
                DrawRenderingLayer(m_RenderingLayerMask, base.target as Renderer, base.targets.ToArray());
                if (SupportedRenderingFeatures.active.rendererPriority)
                    EditorGUILayout.PropertyField(m_RendererPriority, Styles.rendererPriority);
                EditorGUI.indentLevel--;
            });
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        internal static void DrawRenderingLayer(SerializedProperty layerMask, Renderer target, UnityEngine.Object[] targets)
        {
            RenderPipelineAsset currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            if (!(currentRenderPipeline != null) || target == null)
            {
                return;
            }
            EditorGUI.showMixedValue = layerMask.hasMultipleDifferentValues;
            int renderingLayerMask = (int)target.renderingLayerMask;
            string[] prefixedRenderingLayerMaskNames = currentRenderPipeline.prefixedRenderingLayerMaskNames;
            if (prefixedRenderingLayerMaskNames == null)
            {
                prefixedRenderingLayerMaskNames = defaultPrefixedRenderingLayerNames;
            }
            EditorGUI.BeginChangeCheck();
            Rect controlRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(controlRect, Styles.renderingLayerMask, layerMask);
            renderingLayerMask = EditorGUI.MaskField(controlRect, Styles.renderingLayerMask, renderingLayerMask, prefixedRenderingLayerMaskNames);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Set rendering layer mask");
                foreach (UnityEngine.Object @object in targets)
                {
                    Renderer renderer = @object as Renderer;
                    if (renderer != null)
                    {
                        renderer.renderingLayerMask = (uint)renderingLayerMask;
                        EditorUtility.SetDirty(@object);
                    }
                }
            }
            EditorGUI.showMixedValue = false;
        }


    }
}
