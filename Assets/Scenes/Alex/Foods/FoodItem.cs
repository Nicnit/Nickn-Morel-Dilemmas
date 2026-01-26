using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

public enum tags
{
    //individual leaning
    chopped = 1,
    cracked = 2,
    fried = 3,
    //multiple leaning
    boiled = 4,
    baked = 5,
    mixed = 6
}
[Serializable]
public class Modifier
{
    public List<tags> tag;
    public Sprite itemSprite;
}
[Serializable]
public class ModifiedFoodItem
{
    public List<tags> tag;
    public FoodItem foodItem;
}
[Serializable]
public class recipe
{
    public List<ModifiedFoodItem> foodList;
    public tags step;
}

[CreateAssetMenu(fileName = "FoodItem", menuName = "FoodItem", order = 0)]
public class FoodItem : ScriptableObject
{
    [HideInInspector]
    public string itemName;
    [HideInInspector]
    public Sprite defaultSprite;
    [HideInInspector]
    public List<Modifier> modifiers;
    [HideInInspector]
    public recipe recipe;
}


[CustomEditor(typeof(FoodItem))]
public class ItemDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //updates
        DrawDefaultInspector();
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();
        //

        var itemName = serializedObject.FindProperty("itemName");
        var modifiers = serializedObject.FindProperty("modifiers");


        FoodItem script = (FoodItem)target;

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(itemName, new GUIContent("Name"));
        script.defaultSprite = (Sprite)EditorGUILayout.ObjectField(
            "Default Sprite",
            script.defaultSprite,
            typeof(Sprite),
            false
        );

        for(int i =0; i < script.modifiers.Count; i++)
        {
            Modifier modifier = script.modifiers[i];

            EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Modifier", EditorStyles.boldLabel);

                    GUILayout.Button("+", GUILayout.Width(20));

                    script.modifiers[i].itemSprite = (Sprite)EditorGUILayout.ObjectField(
                        "Default Sprite",
                        script.modifiers[i].itemSprite,
                        typeof(Sprite),
                        false
                    );

                EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+"))
        {
            script.modifiers.Add(new Modifier());
        }



        // endupdates
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(script);
        }
        serializedObject.ApplyModifiedProperties();
        //
    }
}