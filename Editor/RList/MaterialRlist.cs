using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CoderScripts.MeshEditorPlus
{
    internal class MaterialRlist
    {

        public ReorderableList RList;
        public SerializedProperty Items;

        public MaterialRlist(SerializedProperty list)
        {
            Items = list;
            RList = new ReorderableList(list.serializedObject, list, true, false, false, false);
            RList.showDefaultBackground = false;
            RList.drawElementCallback = DrawElement;
            RList.footerHeight = 0;
            RList.headerHeight = 0;
        }

        public void DrawList()
        {
            Rect rect = GUILayoutUtility.GetRect(100, RList.GetHeight());
            //rect.y -= 4;
            rect.xMax--;
            rect.xMin++;
            RList.DoList(rect);
        }

        public float GetHeight() => RList.GetHeight();

        public void Add()
        {
            Items.InsertArrayElementAtIndex(Items.arraySize);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (Items.arraySize <= index) return;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2;
            rect.width -= 20;
            EditorGUI.PropertyField(rect, Items.GetArrayElementAtIndex(index), GUIContent.none);

            rect.y++;
            rect.x += rect.width + 3;
            rect.width = 20;

            GUIStyle preButton = "RL FooterButton";
            bool remove = GUI.Button(rect, EditorGUIUtility.IconContent("Toolbar Minus", "|Remove"), preButton);
            if(remove) 
                Items.DeleteArrayElementAtIndex(index);
        }
    }
}
