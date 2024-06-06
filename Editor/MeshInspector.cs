using UnityEngine;
using UnityEditor;
using System.Linq;
using HarmonyLib;
using System;
using Object = UnityEngine.Object;

namespace CoderScripts.MeshEditorPlus
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshRenderer))]
    internal class MeshInspector : RendererEditorBase
    {
        private static readonly Type SpeedTreeMaterialFixerType = AccessTools.TypeByName("UnityEditor.SpeedTreeMaterialFixer");

        private class Styles
        {
            public static readonly GUIContent materialWarning = EditorGUIUtility.TrTextContent("This renderer has more materials than the Mesh has submeshes. Multiple materials will be applied to the same submesh, which costs performance. Consider using multiple shader passes.");

            public static readonly GUIContent staticBatchingWarning = EditorGUIUtility.TrTextContent("This Renderer uses static batching and instanced Shaders. When the Player is active, instancing is disabled. If you want instanced Shaders at run time, disable static batching.");
        }

        private SerializedObject m_GameObjectsSerializedObject;

        private SerializedProperty m_GameObjectStaticFlags;

        private MeshRenderer m_Renderer;
        private SerializedProperty m_Mesh;

        public override void OnEnable()
        {
            var hideInspector = Traverse.Create(this).Property<bool>("hideInspector").Value;
            if (!hideInspector)
            {
                base.OnEnable();
                Object[] objs = base.targets.Select((Object t) => ((MeshRenderer)t).gameObject).ToArray();
                m_GameObjectsSerializedObject = new SerializedObject(objs);
                m_GameObjectStaticFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");
                Lightmapping.lightingDataUpdated += LightingDataUpdatedRepaint;
                m_Renderer = target as MeshRenderer;
                var MeshFilter = m_Renderer.GetComponent<MeshFilter>();
                if (MeshFilter != null)
                {
                    m_Mesh = new SerializedObject(MeshFilter).FindProperty("m_Mesh");
                    MeshFilter.hideFlags = HideFlags.HideInInspector;
                }

            }

        }

        public void OnDisable()
        {
            Lightmapping.lightingDataUpdated -= LightingDataUpdatedRepaint;
        }

        private void LightingDataUpdatedRepaint()
        {
            if (m_Lighting.Property<bool>("showLightmapSettings").Value)
            {
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {

            var GetBatchingForPlatform = Traverse.Create(typeof(PlayerSettings)).Method("GetBatchingForPlatform", new Type[]
            {
                typeof(BuildTarget),
                typeof(int),
                typeof(int)
            });
            var MaterialsUseInstancingShader = Traverse.Create(typeof(ShaderUtil)).Method("MaterialsUseInstancingShader", new Type[]
            {
                typeof(SerializedProperty)
            });
            var DoFixerUI = Traverse.Create(SpeedTreeMaterialFixerType).Method("DoFixerUI", new Type[]
            {
                typeof(GameObject)
            });

            base.serializedObject.Update();
            if (m_Mesh != null)
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(m_Mesh);
                }


            bool flag = false;
            if (!m_Materials.hasMultipleDifferentValues)
            {
                MeshFilter component = ((MeshRenderer)base.serializedObject.targetObject).GetComponent<MeshFilter>();
                flag = component != null && component.sharedMesh != null && m_Materials.arraySize > component.sharedMesh.subMeshCount;
            }
            Tree component2 = ((MeshRenderer)base.serializedObject.targetObject).GetComponent<Tree>();
            bool flag2 = component2 != null;
            bool flag3 = flag2 && component2.data == null;
            using (new EditorGUI.DisabledScope(flag2 && !flag3))
            {
                OnMaterialGUI();
            }
            EditorGUILayout.Space(2f);
            if (!m_Materials.hasMultipleDifferentValues && flag)
            {
                EditorGUILayout.HelpBox(Styles.materialWarning.text, MessageType.Warning, wide: true);
            }
            if (MaterialsUseInstancingShader.GetValue<bool>(m_Materials))
            {
                m_GameObjectsSerializedObject.Update();
                var args = new object[] { EditorUserBuildSettings.activeBuildTarget, null, null };
                GetBatchingForPlatform.GetValue(args);
                int staticBatching = (int)args[1]; 
                if (!m_GameObjectStaticFlags.hasMultipleDifferentValues && ((uint)m_GameObjectStaticFlags.intValue & 4u) != 0 && staticBatching != 0)
                {
                    EditorGUILayout.HelpBox(Styles.staticBatchingWarning.text, MessageType.Warning, wide: true);
                }
            }
            OnOtherSettingsGUI(true, showLightmappSettings: true);
            if (base.targets.Length == 1)
            {
                DoFixerUI.GetValue((base.target as MeshRenderer).gameObject);
            }
            base.serializedObject.ApplyModifiedProperties();
        }
    }
}
