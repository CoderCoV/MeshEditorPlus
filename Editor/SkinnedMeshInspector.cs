using HarmonyLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;


namespace CoderScripts.MeshEditorPlus
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    internal class SkinnedMeshInspector : RendererEditorBase
    {
        #region Fields

        private SerializedProperty m_AABB;

        private SerializedProperty m_DirtyAABB;

        private SerializedProperty m_BlendShapeWeights;

        private SerializedProperty m_Quality;

        private SerializedProperty m_UpdateWhenOffscreen;

        private SerializedProperty m_Mesh;

        private SerializedProperty m_RootBone;

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        private Vector2 m_BlendShapeScroll = Vector2.zero;
        private string m_BlendShapeSearch = string.Empty;
        private BlendShapeRList m_BlendShapeList;

        private SkinnedMeshRenderer m_Renderer;

        #endregion Fields

        public override void OnEnable()
        {
            base.OnEnable();
            m_Renderer = target as SkinnedMeshRenderer;
            m_AABB = serializedObject.FindProperty("m_AABB");
            m_DirtyAABB = serializedObject.FindProperty("m_DirtyAABB");
            m_BlendShapeWeights = serializedObject.FindProperty("m_BlendShapeWeights");
            m_Quality = serializedObject.FindProperty("m_Quality");
            m_UpdateWhenOffscreen = serializedObject.FindProperty("m_UpdateWhenOffscreen");
            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_RootBone = serializedObject.FindProperty("m_RootBone");
            m_BoundsHandle.SetColor(StylesManager.Styles.BoundingBoxHandleColor);

            m_BlendShapeList = new BlendShapeRList(m_Renderer, m_BlendShapeWeights);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            m_BlendShapeList = null;
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                serializedObject.Update();

                OnBoundsGUI();
                EditorGUILayout.Space(2f);
                OnBlendShapeGUI();
                EditorGUILayout.Space(2f);
                OnMaterialGUI();
                EditorGUILayout.Space(2f);
                OnMeshSettingsGUI();
                EditorGUILayout.Space(2f);
                OnOtherSettingsGUI(false, true);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnBoundsGUI()
        {
            var nameButton = new GUIContent("Edit Bounds");

            m_AABB.isExpanded = StylesManager.Foldout(m_AABB.isExpanded, StylesManager.Styles.bounds, nameButton, () =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AABB, new GUIContent());
                if (EditorGUI.EndChangeCheck())
                {
                    m_DirtyAABB.boolValue = false;
                }
            }, (Rect rect) =>
            {
                bool value = EditMode.IsOwner(this);
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    bool flag = false;
                    flag = GUI.Toggle(rect, value, nameButton, StylesManager.Styles.ShurikenFoldoutButton);
                    if (changed.changed)
                        EditMode.ChangeEditMode(flag ? EditMode.SceneViewEditMode.Collider : EditMode.SceneViewEditMode.None, m_Renderer.bounds, this);
                }
            }, true);
        }

        private void OnBlendShapeGUI()
        {

            int num = (!(m_Renderer.sharedMesh == null)) ? m_Renderer.sharedMesh.blendShapeCount : 0;
            m_BlendShapeWeights.isExpanded = StylesManager.Foldout(m_BlendShapeWeights.isExpanded, new GUIContent("BlendShape"), body: () =>
            {
                int num2 = m_Renderer.sharedMesh.blendShapeCount;
                var ScrollHeight = (num2 > 15 ? 15 : num2) * 18;
                //if (!string.IsNullOrEmpty(m_BlendShapeSearch))
                    //ScrollHeight = 15 * 20;
                m_BlendShapeSearch = EditorGUILayout.TextField(m_BlendShapeSearch, new GUIStyle("SearchTextField"));
                m_BlendShapeScroll = GUILayout.BeginScrollView(m_BlendShapeScroll, false, false, GUILayout.Height(ScrollHeight + 2));

                m_BlendShapeList.DrawList(m_BlendShapeSearch);

                GUILayout.EndScrollView();
            }, hierarchy: true, disabledGroup: num == 0);
        }

        private void OnMeshSettingsGUI()
        {
            m_Mesh.isExpanded = StylesManager.Foldout(m_Mesh.isExpanded, new GUIContent("Mesh"), body: () =>
            {
                EditorGUILayout.PropertyField(m_Mesh, StylesManager.Styles.mesh);
                EditorGUILayout.PropertyField(m_RootBone, StylesManager.Styles.rootBone);
                EditorGUILayout.PropertyField(m_UpdateWhenOffscreen, StylesManager.Styles.updateWhenOffscreen);
                EditorGUILayout.PropertyField(m_Quality, StylesManager.Styles.quality);

            }, hierarchy: true);
        }

        #region Gizmos
        public void OnSceneGUI()
        {
            if (!base.target)
            {
                return;
            }
            SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)base.target;

            var _base = Traverse.Create(skinnedMeshRenderer);
            var actualRootBone = _base.Property<Transform>("actualRootBone").Value;


            if (skinnedMeshRenderer.updateWhenOffscreen)
            {
                Bounds bounds = skinnedMeshRenderer.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;
                Handles.DrawWireCube(center, size);
                return;
            }
            using (new Handles.DrawingScope(actualRootBone.localToWorldMatrix))
            {
                Bounds localBounds = skinnedMeshRenderer.localBounds;
                m_BoundsHandle.center = localBounds.center;
                m_BoundsHandle.size = localBounds.size;
                m_BoundsHandle.handleColor = ((EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this)) ? m_BoundsHandle.wireframeColor : Color.clear);
                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(skinnedMeshRenderer, "Resize Bounds");
                    skinnedMeshRenderer.localBounds = new Bounds(m_BoundsHandle.center, m_BoundsHandle.size);
                }
            }
        }
        #endregion Gizmos
    }
}




