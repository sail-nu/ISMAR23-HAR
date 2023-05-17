using UnityEngine;
using UnityEditor;

public class MaterialGenerator
{
    [MenuItem("Assets/Create Materials")]
    private static void CreateMaterials()
    {
        // If there is no selection, do nothing
        if (Selection.objects.Length < 1 || Selection.objects[0].GetType() != typeof(Texture2D))
        {
            Debug.Log("No textures selected or first object is not a texture");
            return;
        }

        // Create a folder to save materials in
        string parentFolderPath = AssetDatabase.GetAssetPath(Selection.objects[0] as Texture2D);
        parentFolderPath = parentFolderPath.Substring(0, parentFolderPath.LastIndexOf('/'));
        Debug.Log(parentFolderPath);
        string guid = AssetDatabase.CreateFolder(parentFolderPath, "Materials");
        string materialsFolderPath = AssetDatabase.GUIDToAssetPath(guid);
        Debug.Log("New folder created at: " + materialsFolderPath);

        // Convert textures/images to materials
        foreach (Object o in Selection.objects)
        {

            if (o.GetType() != typeof(Texture2D))
            {
                Debug.LogError("This isn't a texture: " + o);
                continue;
            }

            Debug.Log("Creating material from: " + o);

            Texture2D selected = o as Texture2D;

            Material material = new Material(Shader.Find("Mixed Reality Toolkit/Standard"));
            material.mainTexture = (Texture)o;

            string newAssetName = materialsFolderPath + "/" + selected.name + ".mat";

            AssetDatabase.CreateAsset(material, newAssetName);

            AssetDatabase.SaveAssets();

        }
        Debug.Log("Done!");
    }
}