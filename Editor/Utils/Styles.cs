using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace CoderScripts.MeshEditorPlus
{
    internal static class LocalData
    {
        public static bool OtherSettings;
    }


    internal class Styles
    {
        public readonly GUIContent legacyClampBlendShapeWeightsInfo = EditorGUIUtility.TrTextContent("Note that BlendShape weight range is clamped. This can be disabled in Player Settings.");

        public readonly GUIContent meshNotSupportingSkinningInfo = EditorGUIUtility.TrTextContent("The assigned mesh is missing either bone weights with bind pose, or blend shapes. This might cause the mesh not to render in the Player. If your mesh does not have either bone weights with bind pose, or blend shapes, use a Mesh Renderer instead of Skinned Mesh Renderer.");

        public readonly GUIContent bounds = EditorGUIUtility.TrTextContent("Bounds", "The bounding box that encapsulates the mesh.");

        public readonly GUIContent editBounds = EditorGUIUtility.TrTextContent("Edit Bounds", "Edit bounding volume.\n\n - Hold Alt after clicking control handle to pin center in place.\n - Hold Shift after clicking control handle to scale uniformly.");

        public readonly GUIContent quality = EditorGUIUtility.TrTextContent("Quality", "Number of bones to use per vertex during skinning.");

        public readonly GUIContent updateWhenOffscreen = EditorGUIUtility.TrTextContent("Update When Offscreen", "If an accurate bounding volume representation should be calculated every frame. ");

        public readonly GUIContent mesh = EditorGUIUtility.TrTextContent("Mesh", "The mesh used by this renderer.");

        public readonly GUIContent rootBone = EditorGUIUtility.TrTextContent("Root Bone", "Transform with which the bounds move, and the space in which skinning is computed.");

        public static readonly GUIContent rayTracingModeStyle = EditorGUIUtility.TrTextContent("Ray Tracing Mode", "Describes how renderer will update for ray tracing");

        public static readonly GUIContent otherSettings = EditorGUIUtility.TrTextContent("Additional Settings");

        public static readonly GUIContent dynamicOcclusion = EditorGUIUtility.TrTextContent("Dynamic Occlusion", "Controls if dynamic occlusion culling should be performed for this renderer.");

        public static readonly GUIContent motionVectors = EditorGUIUtility.TrTextContent("Motion Vectors", "Specifies whether the Mesh Renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture.");

        public static readonly GUIContent skinnedMotionVectors = EditorGUIUtility.TrTextContent("Skinned Motion Vectors", "Enabling Skinned Motion Vectors will allow generation of high precision motion vectors for the Skinned Mesh. This is achieved by keeping the skinning results of the previous frame in memory thus increasing the memory usage.");

        public static readonly GUIContent renderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");

        public static readonly GUIContent rendererPriority = EditorGUIUtility.TrTextContent("Priority", "Sets the priority value that the render pipeline uses to calculate the rendering order.");


        public static readonly GUIContent[] rayTracingModeOptions = (from x in (from x in Enum.GetNames(typeof(RayTracingMode))
                                                                                select ObjectNames.NicifyVariableName(x)).ToArray()
                                                                     select new GUIContent(x)).ToArray();

        public static readonly GUIContent rayTracingGeomStyle = EditorGUIUtility.TrTextContent("Ray Trace Procedurally", "Specifies whether to treat geometry as defined by shader (true) or as a normal mesh (false)");

        public readonly Color BoundingBoxHandleColor = new Color(255f, 255f, 255f, 150f) / 255f;

        public static readonly GUIStyle singleButtonStyle = "EditModeSingleButton";

        public readonly GUIStyle headerBackground = "RL Header";

        public readonly GUIStyle ShurikenFoldoutButton = new GUIStyle(EditorStyles.miniButtonRight)
        {
            fixedWidth = 0,
            fixedHeight = 21f
        };

        public readonly GUIStyle ShurikenFoldout = new GUIStyle("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.boldLabel).font,
            border = new RectOffset(5, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f)

        };

        public readonly GUIStyle HelpBoxFoldout = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(0, 0, 0, 1),
            margin = new RectOffset(0, 0, 0, 0)
        };



    }

    internal static class StylesManager
    {
        private static Styles _styles;

        public static Styles Styles 
        { 
            get 
            { 
                if (_styles == null) _styles = new Styles();
                return _styles; 
            } 
        }

        public static bool Foldout(bool isExpanded, GUIContent header, GUIContent nameButton = null, Action body = null, Action<Rect> actionButton = null, bool hierarchy = false, bool disabledGroup = false)
        {
            using(new EditorGUI.DisabledGroupScope(disabledGroup))
            {
                var HelpBoxFoldout = isExpanded ? Styles.HelpBoxFoldout : GUIStyle.none;
                using (new EditorGUILayout.VerticalScope(HelpBoxFoldout))
                {
                    var value = FoldoutModule(!disabledGroup ? isExpanded : false, header, nameButton, actionButton, hierarchy);
                    if (!disabledGroup)
                        isExpanded = value;
                    if (!disabledGroup ? isExpanded : false)
                    {
                        body();
                    }

                }
            }
            return isExpanded;
        }


        public static bool FoldoutModule(bool isExpanded, GUIContent header, GUIContent nameButton = null, Action<Rect> actionButton = null, bool hierarchy = false)
        {
            var rect = GUILayoutUtility.GetRect(16f, 20f, Styles.ShurikenFoldout);

            if (EditorGUIUtility.hierarchyMode && hierarchy)
            {
                rect.xMin -= EditorStyles.inspectorDefaultMargins.padding.left - EditorStyles.inspectorDefaultMargins.padding.right;
                rect.xMax += EditorStyles.inspectorDefaultMargins.padding.right;
                rect.xMin -= 2;
                rect.xMax -= 4;
            }

            rect = EditorGUI.IndentedRect(rect);
            var clickRect = new Rect(rect);
            var toggleRect = new Rect(rect.x + 2f, rect.y, 18f, 18f);

            var lastRect = GUILayoutUtility.GetLastRect();
            
            var labelRect = new Rect(rect);
            var labelContent = new GUIContent(header);
            labelRect.size = GUI.skin.label.CalcSize(labelContent);
            labelRect.x += toggleRect.width - 2;

            Event e = Event.current;

            if(e.type == EventType.Repaint)
            {
                GUI.Box(rect, "", Styles.ShurikenFoldout);
                EditorStyles.foldout.Draw(toggleRect, false, false, isExpanded, false);
                GUI.Label(labelRect, labelContent);
            }

            float labelButtonSize = 0f;
            if (nameButton != null)
            {
                labelButtonSize = GUI.skin.label.CalcSize(nameButton).x + 4;
                clickRect.width -= labelButtonSize;
                var buttonRect = new Rect(lastRect.width + 19, lastRect.y - 1, 0, 21);
                buttonRect.x -= labelButtonSize;
                buttonRect.width = labelButtonSize;
                actionButton(buttonRect);
            }

            if(e.type == EventType.MouseUp && clickRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                Event.current.Use();
                GUI.changed = true;
                return !isExpanded;
            }

            return isExpanded;
        }

        public static void BeginContainer(bool expand = false)
        {
            GUIStyle guistyle = new GUIStyle();
            guistyle.margin = new RectOffset(5, 5, 0, 0);
            GUIStyle guistyle2 = new GUIStyle();
            EditorGUILayout.BeginVertical(guistyle2, new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(expand)
            });
            EditorGUILayout.BeginVertical(guistyle, new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(expand)
            });
        }

        public static void EndContainer()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
    }

}
