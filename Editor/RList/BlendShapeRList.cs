using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace CoderScripts.MeshEditorPlus
{
    public class BlendShapeRList
    {
        class BlendShapeObject
        {
            public string name;

            public float MinFrameWeight = 0f;
            public float MaxFrameWeight = 0f;

            public SerializedProperty FloatProperty;
        }

        private List<BlendShapeObject> Items;
        private bool Init;

        public BlendShapeRList(SkinnedMeshRenderer mesh, SerializedProperty blendShapeWeights)
        {
            Init = false;
            Items = new List<BlendShapeObject>();

            Mesh sharedMesh = mesh.sharedMesh;

            int num = (!(mesh.sharedMesh == null)) ? mesh.sharedMesh.blendShapeCount : 0;
            int num2 = blendShapeWeights.arraySize;
            for (int i = 0; i < num; i++)
            {
                var bsObject = new BlendShapeObject();

                bsObject.name = sharedMesh.GetBlendShapeName(i);

                int blendShapeFrameCount = sharedMesh.GetBlendShapeFrameCount(i);
                for (int j = 0; j < blendShapeFrameCount; j++)
                {
                    float blendShapeFrameWeight = sharedMesh.GetBlendShapeFrameWeight(i, j);
                    bsObject.MinFrameWeight = Mathf.Min(blendShapeFrameWeight, bsObject.MinFrameWeight);
                    bsObject.MaxFrameWeight = Mathf.Max(blendShapeFrameWeight, bsObject.MaxFrameWeight);
                }
                if(!(i < num2))
                {
                    blendShapeWeights.arraySize = num;
                    num2 = num;
                    blendShapeWeights.serializedObject.ApplyModifiedProperties();
                }
                bsObject.FloatProperty = blendShapeWeights.GetArrayElementAtIndex(i);
                Items.Add(bsObject);
            }
            Init = true;
        }

        public void DrawList(string search)
        {
            if (!Init)
                return;
            foreach (var currentItem in Items)
            {
                var name = currentItem.name;
                var min = currentItem.MinFrameWeight;
                var max = currentItem.MaxFrameWeight;

                if (!string.IsNullOrEmpty(search) && !name.ToLower().Contains(search.ToLower()))
                    continue;

                EditorGUILayout.Slider(currentItem.FloatProperty, min, max, name, GUILayout.Height(16f));
            }
        }
    }
}
