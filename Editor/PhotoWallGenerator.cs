using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PeanutTools_PhotoWallGenerator;

public class PhotoWallGenerator : EditorWindow
{
    string pathToPhotosFolder = "";
    bool randomizeOrder = true;
    Transform targetTransform;
    bool emptyTarget = true;
    string pathToPrefab = "";
    float columnWidth = 0;
    float rowHeight = 0;
    int rowCount = 1;
    int columnCount = 0;
    bool createInstancePerPhoto = true;

    Vector2 scrollPosition;
    private static string pathToTemp = "";
    private static System.Random random = new System.Random();

    [MenuItem("PeanutTools/Photo Wall Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<PhotoWallGenerator>();
        window.titleContent = new GUIContent("Photo Wall Generator");
        window.minSize = new Vector2(250, 50);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        CustomGUI.BoldLabel("Photo Wall Generator");
        CustomGUI.ItalicLabel("Display your VR photos inside your world.");

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();

        CustomGUI.BoldLabel("Step 1: Select your photos folder");

        if (GUILayout.Button("Select folder")) {
            string absolutePath = EditorUtility.OpenFolderPanel("Select a folder with your photos", Application.dataPath, "");
            CustomGUI.ItalicLabel("This folder should contain only 1080p PNG photos (does not look into sub-folders).");

            if (absolutePath != "") {
                pathToPhotosFolder = absolutePath;
            }
        }

        if (pathToPhotosFolder != "") {
            GUILayout.Label(pathToPhotosFolder);
        }

        CustomGUI.SmallLineGap();
        
        createInstancePerPhoto = GUILayout.Toggle(createInstancePerPhoto, "Create a prefab instance per photo");
        
        CustomGUI.SmallLineGap();

        if (createInstancePerPhoto) {
            CustomGUI.BoldLabel("Step 2: Select the output gameobject");
            CustomGUI.ItalicLabel("This object will contain all photos.");

            CustomGUI.SmallLineGap();

            targetTransform = (Transform)EditorGUILayout.ObjectField("Select an object", targetTransform, typeof(Transform));
            
            CustomGUI.SmallLineGap();

            CustomGUI.BoldLabel("Step 3: Select the prefab");
            CustomGUI.ItalicLabel("This prefab is used for each photo. It must contain a child named \"Photo\" that has a renderer (like a cube).");

            CustomGUI.SmallLineGap();

            if (GUILayout.Button("Select prefab")) {
                string absolutePath = EditorUtility.OpenFilePanel("Select a prefab", Application.dataPath, "prefab");

                if (absolutePath != "") {
                    pathToPrefab = absolutePath;
                }
            }

            if (pathToPrefab != "") {
                GUILayout.Label(pathToPrefab);
            }
            
            CustomGUI.SmallLineGap();

            CustomGUI.BoldLabel("Step 4: Settings");

            CustomGUI.SmallLineGap();
            
            rowCount = EditorGUILayout.IntField("Rows", rowCount); 
            columnCount = EditorGUILayout.IntField("Columns", columnCount);
            CustomGUI.ItalicLabel("0 to disable");

            CustomGUI.SmallLineGap();
            
            columnWidth = EditorGUILayout.FloatField("Column Width", columnWidth);
            rowHeight = EditorGUILayout.FloatField("Row Height", rowHeight);
            CustomGUI.ItalicLabel("0 if only using 1 row");

            CustomGUI.SmallLineGap();
            
            randomizeOrder = GUILayout.Toggle(randomizeOrder, "Randomize order");

            CustomGUI.SmallLineGap();
            
            emptyTarget = GUILayout.Toggle(emptyTarget, "Empty target");
        } else {
            CustomGUI.BoldLabel("Step 2: Select the gameobject");
            CustomGUI.ItalicLabel("This object should contain a child per photo. Each one should contain a child named \"Photo\" that has a renderer (like a cube).");
            
            CustomGUI.SmallLineGap();

            targetTransform = (Transform)EditorGUILayout.ObjectField("Select an object", targetTransform, typeof(Transform));
        }
        
        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("Step 5: Generate");

        CustomGUI.SmallLineGap();

        EditorGUI.BeginDisabledGroup(!GetIsReadyToGenerate());
        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
        CustomGUI.ItalicLabel("Warning: This will freeze Unity while it works");

        if (GUILayout.Button("Use Existing Output"))
        {
            GenerateWithExistingOutput();
        }
        EditorGUI.EndDisabledGroup();
        
        CustomGUI.SmallLineGap();

        GUILayout.Label("You will need to manually change texture import settings to 8K");

        CustomGUI.SmallLineGap();

        CustomGUI.MyLinks("unity-photo-wall");

        EditorGUILayout.EndScrollView();
    }

    bool GetIsReadyToGenerate() {
        if (pathToPhotosFolder == "") {
            return false;
        }

        if (createInstancePerPhoto && pathToPrefab == "") {
            return false;
        }

        if (targetTransform == null) {
            return false;
        }

        return true;
    }

    string[] GetFilePathsInsideDir(string dir) {
        return Directory.GetFiles(dir);
    }

    bool GetIsValidFilePaths(string[] paths) {
        if (paths.Length == 0) {
            return false;
        }

        // TODO: Check if any PNGs/JPGs

        return true;
    }

    void PrepareTempDir() {
        pathToTemp = Path.Combine(Path.GetTempPath(), "unity-photo-wall");

        Debug.Log("Temp path: " + pathToTemp);

        bool exists = Directory.Exists(pathToTemp);

        if (exists) {
            new DirectoryInfo(pathToTemp).Delete(true);
        }

        Directory.CreateDirectory(pathToTemp);
    }

    void PrepareOutputDir() {
        string pathToOutputDir = GetOutputDirPath();

        bool exists = Directory.Exists(pathToOutputDir);

        Debug.Log("Output dir: " + pathToOutputDir);

        if (exists) {
            return;
        }

        Directory.CreateDirectory(pathToOutputDir);
    }

    // source: https://stackoverflow.com/a/1344242/1215393
    string GenerateRandomFileName() {
        int length = 10;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    void CopyImagesToTemp(string[] filePaths) {
        Debug.Log("Copying " + filePaths.Length + " images to temp...");

        foreach (var filePath in filePaths) {
            string fileName = Path.GetFileName(filePath);

            if (randomizeOrder) {
                string fileExtensionWithDot = Path.GetExtension(fileName);
                fileName = GenerateRandomFileName() + fileExtensionWithDot;
            }
            
            string newPath = Path.Combine(pathToTemp, fileName);

            Debug.Log(filePath + " => " + newPath);

            File.Copy(filePath, newPath);
        }

        Debug.Log("Images copied");
    }

    void Generate()
    {
        PrepareTempDir();

        var filePaths = GetFilePathsInsideDir(pathToPhotosFolder);

        if (!GetIsValidFilePaths(filePaths)) {
            Debug.LogError("Directory is not valid");
            return;
        }

        CopyImagesToTemp(filePaths);

        GenerateMontage();

        AssetDatabase.Refresh();

        if (emptyTarget && createInstancePerPhoto) {
            EmptyTarget();
        }

        GeneratePhotos();
    }

    void GenerateWithExistingOutput()
    {
        if (emptyTarget && createInstancePerPhoto) {
            EmptyTarget();
        }

        GeneratePhotos();
    }

    void EmptyTarget() {
        Debug.Log("Emptying target...");

        var children = new List<GameObject>();
        foreach (Transform child in targetTransform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
    }

    Texture LoadOutputImage() {
        var pathToOutputImage = Path.Combine("Assets", GetOutputDirPathInsideAssets(), "output.png");
        Debug.Log("Loading image asset " + pathToOutputImage + "...");
        return (Texture)AssetDatabase.LoadAssetAtPath<Texture>(pathToOutputImage);
    }

    GameObject LoadPrefab() {
        string pathToPrefabInsideProject = pathToPrefab.Replace(Application.dataPath, "Assets");
        Debug.Log("Loading prefab asset " + pathToPrefabInsideProject + "...");
        return (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>(pathToPrefabInsideProject);
    }

    Material CreateMaterialForPhoto(Material existingMaterial, int index) {
        var newMaterial = Instantiate(existingMaterial);

        var fileName = "photo-" + index + ".mat";

        var pathInsideAssets = Path.Combine("Assets", GetOutputDirPathInsideAssets(), fileName);

        AssetDatabase.CreateAsset(newMaterial, pathInsideAssets);

        return newMaterial;
    }

    void GeneratePhotos() {
        var filePaths = GetFilePathsInsideDir(pathToPhotosFolder);

        Debug.Log("Generating " + filePaths.Length + " photos...");

        var image = LoadOutputImage();
        
        if (image == null) {
            Debug.LogError("Failed to load output image as texture");
            return;
        }
        
        GameObject prefab = null;

        if (createInstancePerPhoto) {
            prefab = LoadPrefab();

            if (prefab == null) {
                Debug.LogError("Failed to load prefab");
                return;
            }
        }

        // TODO: Handle different photo heights
        int singleImageHeight = 1080;

        int fullImageHeight = filePaths.Length * singleImageHeight;

        float scaleY = (100f / filePaths.Length) / 100f;

        int rowProgress = 0;
        int columnProgress = 0;

        for (var i = 0; i < filePaths.Length; i++) {
            Transform photoTransform = null;

            if (createInstancePerPhoto) {
                var photoObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                photoObject.transform.SetParent(targetTransform);
                photoObject.name = prefab.name;

                photoObject.transform.localPosition = new Vector3((float)columnProgress * columnWidth, (float)rowProgress * rowHeight, 0);
                
                photoTransform = photoObject.transform.Find("Photo");
            } else {
                try {
                    var child = targetTransform.GetChild(i);
                    photoTransform = child.Find("Photo");
                } catch (Exception err) {
                    Debug.LogError("There is no child at index " + i);
                    return;
                }
            }

            if (photoTransform == null) {
                Debug.LogError((createInstancePerPhoto ? "Prefab" : "Target child at index " + i + "") + " does not contain a child named \"Photo\"");
                return;
            }

            var renderer = photoTransform.GetComponent<Renderer>();

            if (renderer == null) {
                Debug.LogError((createInstancePerPhoto ? "Prefab" : "Target child at index " + i + "") + " photo transform does not contain a renderer component");
                return;
            }
            
            var material = CreateMaterialForPhoto(renderer.sharedMaterial, i);

            material.SetTexture("_MainTex", image);
            material.SetTexture("_EmissionMap", image);

            int yPos = i * 1080;
            float yPosAsPercentage = (float)yPos / (float)fullImageHeight;

            material.mainTextureScale = new Vector2(1, scaleY);

            material.SetTextureOffset("_MainTex", new Vector2(1, yPosAsPercentage));

            renderer.sharedMaterial = material;

            columnProgress++;

            if (columnProgress == columnCount) {
                rowProgress++;
                columnProgress = 0;
            }

            // var meshFilter = photoTransform.GetComponent<MeshFilter>();

            // if (meshFilter == null) {
            //     Debug.LogError("Prefab\'s photo transform does not contain a mesh component");
            //     return;
            // }

            // var uvs = meshFilter.sharedMesh.uv;

            // for (var uvsI = 0; uvsI < uvs.Length; uvsI++) {
            //     var uv = uvs[uvsI];
            //     uvs[uvsI] = new Vector2(uv.x, uv.y + yPos);
            // }

            // meshFilter.sharedMesh.SetUVs(0, uvs);
        }

        Debug.Log("Done generating photos");
    }

    string  GetOutputDirPathInsideAssets() {
        string photosFolderName = Path.GetFileName(pathToPhotosFolder);
        return Path.Combine("unity-photo-wall", photosFolderName);
    }

    string GetOutputDirPath() {
        return Path.Combine(Application.dataPath, GetOutputDirPathInsideAssets());
    }

    string GetOutputImagePath() {
        return Path.Combine(GetOutputDirPath(), "output.png");
    }

    void GenerateMontage() {
        string[] imagePaths = GetFilePathsInsideDir(pathToTemp);

        PrepareOutputDir();

        string pathToOutputImage = GetOutputImagePath();

        string args = "montage \"" + pathToTemp + "\\*.png\" -tile 1x -geometry 1920x1080 \"" + pathToOutputImage + "\"";

        Debug.Log("Spawning magick with args: " + args);
        
        StringBuilder outputStringBuilder = new StringBuilder();

        var startInfo = new System.Diagnostics.ProcessStartInfo("magick", args);
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        var proc = new System.Diagnostics.Process();
        proc.StartInfo = startInfo;
        proc.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
        proc.ErrorDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
        proc.Start();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            var output = outputStringBuilder.ToString();
            var prefixMessage = "";

            throw new Exception("Process exited with non-zero exit code of: " + proc.ExitCode + Environment.NewLine + 
            "Output from process: " + outputStringBuilder.ToString());
        }

        Debug.Log("Complete");
    }
}
