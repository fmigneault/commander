using System;
using UnityEditor;
using UnityEngine;

using Cameras;

namespace Editor
{
    [CustomEditor(typeof(SelectorManager))]
    public class SelectorEditor : UnityEditor.Editor
    {
        private SelectorManager selectorManager { get { return target as SelectorManager; } }


        public override void OnInspectorGUI()
        {
            // Draw any public defined variable in the script
            DrawDefaultInspector();

            // Update selected values for tags
            serializedObject.Update();
            selectorManager.SelectTags = ListTagSelector.ShowList("Can be selected Tags:", 
                                                                  serializedObject.FindProperty("SelectTags"),
                                                                  selectorManager.SelectTags);
            selectorManager.AttackTags = ListTagSelector.ShowList("Can be attacked Tags:",
                                                                  serializedObject.FindProperty("AttackTags"),
                                                                  selectorManager.AttackTags);
            selectorManager.BuilderTag = EditorGUILayout.TagField("Builder Tag: ", selectorManager.BuilderTag);
            selectorManager.BuildingTag = EditorGUILayout.TagField("Building Tag: ", selectorManager.BuildingTag);
            selectorManager.ButtonTag = EditorGUILayout.TagField("Button Tag: ", selectorManager.ButtonTag);

            // Apply changes
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}

