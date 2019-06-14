using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
    [CustomPropertyDrawer(typeof(FPD_OverridableFloatAttribute))]
    public class FPD_OverridableFloat : PropertyDrawer
    {
        FPD_OverridableFloatAttribute Attribute { get { return ((FPD_OverridableFloatAttribute)base.attribute); } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var boolProp = property.serializedObject.FindProperty(Attribute.BoolVarName);
            var valProp = property.serializedObject.FindProperty(Attribute.TargetVarName);

            Color disabled = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            Color preCol = GUI.color;
            if (!boolProp.boolValue) GUI.color = disabled; else GUI.color = preCol;


            GUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 90;
            EditorGUILayout.PropertyField(boolProp, new GUILayoutOption[1] { GUILayout.Width(105f) } );

            GUILayout.ExpandWidth(true);
            EditorGUIUtility.labelWidth = 14;
            EditorGUILayout.PropertyField(valProp, new GUIContent(" "));

            EditorGUIUtility.labelWidth = 0;

            GUILayout.EndHorizontal();

            GUI.color = preCol;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -EditorGUIUtility.standardVerticalSpacing;
        }

    }



    // -------------------------- Next F Property Drawer -------------------------- \\



    [CustomPropertyDrawer(typeof(BackgroundColorAttribute))]
    public class BackgroundColorDecorator : DecoratorDrawer
    {
        BackgroundColorAttribute Attribute { get { return ((BackgroundColorAttribute)base.attribute); } }
        public override float GetHeight() { return 0; }

        public override void OnGUI(Rect position)
        {
            GUI.backgroundColor = Attribute.Color;
        }
    }


    // -------------------------- Next F Property Drawer -------------------------- \\


    [CustomPropertyDrawer(typeof(FPD_WidthAttribute))]
    public class FPD_Width : PropertyDrawer
    {
        FPD_WidthAttribute Attribute { get { return ((FPD_WidthAttribute)base.attribute); } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUIUtility.labelWidth = Attribute.LabelWidth;
            EditorGUI.PropertyField(position, property);
            EditorGUIUtility.labelWidth = 0;
        }
    }

}

