using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PixelShooter3D
{
    public class AutoLevelGeneratorWindow : EditorWindow
    {
        // Grid Settings
        bool keepAspectRatio = false;
        int minRows = 10, maxRows = 30;
        int minCols = 10, maxCols = 25;

        // Color Settings
        int minColors = 3, maxColors = 6;

        // Pig/Deck Settings
        int minHolding = 3, maxHolding = 5;
        int minPigsPerCol = 2, maxPigsPerCol = 6;

        // Ammo Settings
        float ammoMultiplier = 1.0f;

        // Order Settings
        bool randomizeImageOrder = false;

        Vector2 scrollPos;

        const string ImagesFolderName = "AutomaticLevelGeneratorImages";

        [MenuItem("Tools/Auto Level Generator")]
        public static void ShowWindow()
        {
            GetWindow<AutoLevelGeneratorWindow>("Auto Level Generator");
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Auto Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // --- Grid Settings ---
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            keepAspectRatio = EditorGUILayout.Toggle("Keep Aspect Ratio", keepAspectRatio);
            if (keepAspectRatio)
            {
                DrawMinMaxInt("Size", ref minRows, ref maxRows, 1, 50);
                minCols = minRows;
                maxCols = maxRows;
            }
            else
            {
                DrawMinMaxInt("Rows", ref minRows, ref maxRows, 1, 50);
                DrawMinMaxInt("Columns", ref minCols, ref maxCols, 1, 50);
            }
            EditorGUILayout.Space(5);

            // --- Color Settings ---
            EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
            DrawMinMaxInt("Colors per level", ref minColors, ref maxColors, 2, 20);
            EditorGUILayout.Space(5);

            // --- Pig/Deck Settings ---
            EditorGUILayout.LabelField("Pig/Deck Settings", EditorStyles.boldLabel);
            DrawMinMaxInt("Holding Containers", ref minHolding, ref maxHolding, 1, 10);
            DrawMinMaxInt("Pigs per column", ref minPigsPerCol, ref maxPigsPerCol, 1, 10);
            EditorGUILayout.Space(5);

            // --- Ammo Settings ---
            EditorGUILayout.LabelField("Ammo Settings", EditorStyles.boldLabel);
            ammoMultiplier = EditorGUILayout.FloatField("Ammo multiplier", ammoMultiplier);
            if (ammoMultiplier < 0.1f) ammoMultiplier = 0.1f;
            EditorGUILayout.Space(5);

            // --- Order Settings ---
            EditorGUILayout.LabelField("Order Settings", EditorStyles.boldLabel);
            randomizeImageOrder = EditorGUILayout.Toggle("Randomize image order", randomizeImageOrder);
            EditorGUILayout.Space(10);

            // --- Generate Button ---
            if (GUILayout.Button("Generate Levels", GUILayout.Height(30)))
            {
                GenerateLevels();
            }

            EditorGUILayout.Space(5);

            // --- Image count info ---
            string imagesPath = GetImagesFolderPath();
            int imageCount = 0;
            List<string> nonReadableImages = new List<string>();
            if (Directory.Exists(imagesPath))
            {
                string[] files = GetImageFiles(imagesPath);
                imageCount = files.Length;
                foreach (var file in files)
                {
                    string ap = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                    TextureImporter ti = AssetImporter.GetAtPath(ap) as TextureImporter;
                    if (ti != null && !ti.isReadable)
                        nonReadableImages.Add(Path.GetFileName(file));
                }
            }
            EditorGUILayout.HelpBox(
                $"Images found: {imageCount} in {ImagesFolderName}/",
                imageCount > 0 ? MessageType.Info : MessageType.Warning
            );

            if (nonReadableImages.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{nonReadableImages.Count} image(s) not Read/Write enabled:\n" +
                    string.Join(", ", nonReadableImages) +
                    "\n\nClick below to fix, or enable manually in each texture's Import Settings.",
                    MessageType.Error
                );
                if (GUILayout.Button("Enable Read/Write on All Images"))
                {
                    EnableReadWriteOnAllImages();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawMinMaxInt(string label, ref int min, ref int max, int clampMin, int clampMax)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(130));
            EditorGUILayout.LabelField("Min", GUILayout.Width(28));
            min = EditorGUILayout.IntField(min, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            max = EditorGUILayout.IntField(max, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            min = Mathf.Clamp(min, clampMin, clampMax);
            max = Mathf.Clamp(max, min, clampMax);
        }

        void EnableReadWriteOnAllImages()
        {
            string imagesPath = GetImagesFolderPath();
            if (!Directory.Exists(imagesPath)) return;

            string[] files = GetImageFiles(imagesPath);
            int fixedCount = 0;
            List<string> failedFiles = new List<string>();

            foreach (var file in files)
            {
                string ap = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (ti != null && !ti.isReadable)
                {
                    try
                    {
                        ti.isReadable = true;
                        ti.SaveAndReimport();
                        fixedCount++;
                    }
                    catch
                    {
                        failedFiles.Add(Path.GetFileName(file));
                    }
                }
            }

            if (failedFiles.Count > 0)
            {
                EditorUtility.DisplayDialog("Partial Fix",
                    $"Enabled Read/Write on {fixedCount} image(s).\n\n" +
                    $"Failed on {failedFiles.Count}: {string.Join(", ", failedFiles)}\n\n" +
                    "Please enable Read/Write manually in their Import Settings.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Done",
                    $"Enabled Read/Write on {fixedCount} image(s).", "OK");
            }
        }

        string GetImagesFolderPath()
        {
            return Path.Combine(Application.dataPath, "Pixel Shooter 3D", "Resources", ImagesFolderName);
        }

        string GetLevelsFolderPath()
        {
            return Path.Combine(Application.dataPath, "Pixel Shooter 3D", "Resources", "Levels");
        }

        string[] GetImageFiles(string folderPath)
        {
            var files = new List<string>();
            string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tga" };
            foreach (var ext in extensions)
            {
                files.AddRange(Directory.GetFiles(folderPath, ext));
            }
            return files.ToArray();
        }

        int FindNextLevelIndex()
        {
            string levelsPath = GetLevelsFolderPath();
            if (!Directory.Exists(levelsPath)) return 1;

            int maxIndex = 0;
            var files = Directory.GetFiles(levelsPath, "NewLevel*.json");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var match = Regex.Match(name, @"^NewLevel_(\d+)$");
                if (match.Success)
                {
                    int idx = int.Parse(match.Groups[1].Value);
                    if (idx > maxIndex) maxIndex = idx;
                }
                else if (name == "NewLevel")
                {
                    if (maxIndex < 1) maxIndex = 0;
                }
            }
            return maxIndex + 1;
        }

        void GenerateLevels()
        {
            string imagesPath = GetImagesFolderPath();
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("No Images",
                    $"Created folder: Resources/{ImagesFolderName}/\n\nPlace images there and try again.",
                    "OK");
                return;
            }

            string[] imageFiles = GetImageFiles(imagesPath);
            if (imageFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("No Images",
                    $"No images found in Resources/{ImagesFolderName}/\n\nSupported: PNG, JPG, BMP, TGA",
                    "OK");
                return;
            }

            // Shuffle image order if enabled (Fisher-Yates)
            if (randomizeImageOrder)
            {
                for (int i = imageFiles.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    string temp = imageFiles[i];
                    imageFiles[i] = imageFiles[j];
                    imageFiles[j] = temp;
                }
            }

            int startIndex = FindNextLevelIndex();
            int endIndex = startIndex + imageFiles.Length - 1;

            bool proceed = EditorUtility.DisplayDialog("Generate Levels?",
                $"Generate {imageFiles.Length} level(s)?\n\n" +
                $"From: NewLevel_{startIndex}\n" +
                $"To: NewLevel_{endIndex}\n\n" +
                $"Images from: {ImagesFolderName}/",
                "Generate", "Cancel");

            if (!proceed) return;

            string levelsPath = GetLevelsFolderPath();
            if (!Directory.Exists(levelsPath))
                Directory.CreateDirectory(levelsPath);

            int currentIndex = startIndex;
            int successCount = 0;

            foreach (var imageFile in imageFiles)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Generating Levels",
                        $"Processing {Path.GetFileName(imageFile)}... ({successCount + 1}/{imageFiles.Length})",
                        (float)successCount / imageFiles.Length);

                    string json = GenerateLevelFromImage(imageFile, $"NewLevel_{currentIndex}");
                    if (json != null)
                    {
                        string outputPath = Path.Combine(levelsPath, $"NewLevel_{currentIndex}.json");
                        File.WriteAllText(outputPath, json);
                        currentIndex++;
                        successCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"AutoLevelGenerator: Failed to process {Path.GetFileName(imageFile)}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"AutoLevelGenerator: Error processing {Path.GetFileName(imageFile)}: {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Done!",
                $"Generated {successCount} level(s) successfully.\n\n" +
                $"Files saved to Resources/Levels/",
                "OK");
        }

        string GenerateLevelFromImage(string imagePath, string levelName)
        {
            // Step 1: Randomize parameters
            int rows = Random.Range(minRows, maxRows + 1);
            int cols = keepAspectRatio ? rows : Random.Range(minCols, maxCols + 1);
            int numColors = Random.Range(minColors, maxColors + 1);
            int holdingCount = Random.Range(minHolding, maxHolding + 1);

            // Step 2: Process image into grid
            string assetPath = "Assets" + imagePath.Substring(Application.dataPath.Length).Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            bool wasReadable = true;

            if (importer != null)
            {
                wasReadable = importer.isReadable;
                if (!wasReadable)
                {
                    try
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                    catch
                    {
                        Debug.LogError($"AutoLevelGenerator: Could not enable Read/Write on {Path.GetFileName(imagePath)}. " +
                            "Please enable it manually: select the texture in Project, Inspector > Import Settings > Read/Write Enabled.");
                        return null;
                    }

                    // Verify it actually took effect
                    importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        Debug.LogError($"AutoLevelGenerator: Read/Write failed to enable on {Path.GetFileName(imagePath)}. " +
                            "Please enable it manually: select the texture in Project, Inspector > Import Settings > Read/Write Enabled.");
                        return null;
                    }
                }
            }

            Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (sourceTexture == null)
            {
                Debug.LogError($"AutoLevelGenerator: Could not load texture at {assetPath}");
                return null;
            }

            // Resize image to grid dimensions using RenderTexture
            Color[] pixels = ResizeAndReadPixels(sourceTexture, cols, rows);

            // Restore original readability setting
            if (importer != null && !wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            if (pixels == null) return null;

            // Step 2.5: K-means color quantization
            Color[] palette;
            int[] pixelAssignments;
            KMeansQuantize(pixels, numColors, out palette, out pixelAssignments);

            // Build the grid (1-based color codes)
            int[][] grid = new int[rows][];
            for (int r = 0; r < rows; r++)
            {
                grid[r] = new int[cols];
                for (int c = 0; c < cols; c++)
                {
                    int pixelIndex = r * cols + c;
                    grid[r][c] = pixelAssignments[pixelIndex] + 1; // 1-based
                }
            }

            // Step 3: Generate palette hex strings
            List<string> paletteHex = new List<string>();
            foreach (var color in palette)
            {
                paletteHex.Add("#" + ColorUtility.ToHtmlStringRGBA(color));
            }

            // Step 4: Generate pig deck
            // Count blocks per color code
            int[] blocksPerColor = new int[numColors];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int code = grid[r][c]; // 1-based
                    blocksPerColor[code - 1]++;
                }
            }

            // Create deck columns with random pig counts
            int[] pigsPerColumnCounts = new int[holdingCount];
            for (int i = 0; i < holdingCount; i++)
            {
                pigsPerColumnCounts[i] = Random.Range(minPigsPerCol, maxPigsPerCol + 1);
            }

            // Distribute pig colors across columns, ensuring every color is represented
            // First, compute total pigs
            int totalPigs = 0;
            foreach (int count in pigsPerColumnCounts) totalPigs += count;

            // Track how many pigs of each color we assign
            int[] pigsOfColor = new int[numColors];
            // List of (column, colorCode1based, ammo) for building deck
            List<int[]> pigEntries = new List<int[]>(); // [column, colorCode, ammo]

            // Assign pig colors: cycle through palette colors across all slots
            int colorCycleIndex = 0;
            for (int col = 0; col < holdingCount; col++)
            {
                for (int p = 0; p < pigsPerColumnCounts[col]; p++)
                {
                    int colorIdx = colorCycleIndex % numColors;
                    pigsOfColor[colorIdx]++;
                    pigEntries.Add(new int[] { col, colorIdx + 1, 0 }); // ammo filled later
                    colorCycleIndex++;
                }
            }

            // Ensure every color has at least one pig
            for (int i = 0; i < numColors; i++)
            {
                if (pigsOfColor[i] == 0 && pigEntries.Count > 0)
                {
                    // Find a color with more than one pig and reassign one
                    int donorIdx = -1;
                    int maxPigCount = 0;
                    for (int j = 0; j < numColors; j++)
                    {
                        if (pigsOfColor[j] > maxPigCount)
                        {
                            maxPigCount = pigsOfColor[j];
                            donorIdx = j;
                        }
                    }
                    if (donorIdx >= 0 && pigsOfColor[donorIdx] > 1)
                    {
                        // Find a pig entry with donorIdx color and reassign
                        for (int e = 0; e < pigEntries.Count; e++)
                        {
                            if (pigEntries[e][1] == donorIdx + 1)
                            {
                                pigEntries[e][1] = i + 1;
                                pigsOfColor[donorIdx]--;
                                pigsOfColor[i]++;
                                break;
                            }
                        }
                    }
                }
            }

            // Calculate ammo for each pig
            foreach (var entry in pigEntries)
            {
                int colorCode = entry[1]; // 1-based
                int colorIdx = colorCode - 1;
                int blocks = blocksPerColor[colorIdx];
                int pigs = pigsOfColor[colorIdx];
                if (pigs <= 0) pigs = 1;
                int ammo = Mathf.CeilToInt((float)blocks / pigs * ammoMultiplier);
                if (ammo < 1) ammo = 1;
                entry[2] = ammo;
            }

            // Step 5: Calculate layout settings
            float gridSize = 10.0f / Mathf.Max(rows, cols);
            float blockScale = gridSize * 2;

            // Step 6: Build LevelData and export JSON
            LevelData tempLevel = ScriptableObject.CreateInstance<LevelData>();
            tempLevel.levelName = levelName;

            // Palette
            tempLevel.palette.Clear();
            foreach (var color in palette)
            {
                tempLevel.palette.Add(color);
            }

            // Grid layout
            tempLevel.layoutRows.Clear();
            for (int r = 0; r < rows; r++)
            {
                LevelRow row = new LevelRow();
                for (int c = 0; c < cols; c++)
                {
                    row.cells.Add(grid[r][c]);
                }
                tempLevel.layoutRows.Add(row);
            }

            // Deck
            tempLevel.deck.Clear();
            for (int i = 0; i < holdingCount; i++)
            {
                tempLevel.deck.Add(new DeckColumn());
            }
            foreach (var entry in pigEntries)
            {
                int col = entry[0];
                int colorCode = entry[1];
                int ammo = entry[2];
                tempLevel.deck[col].pigs.Add(new PigInfo(colorCode, ammo));
            }

            // Settings
            tempLevel.maxTraces = 5;
            tempLevel.gridSize = gridSize;
            tempLevel.blockScale = blockScale;
            tempLevel.slotSpacing = 1.4f;
            tempLevel.beltCornerRadius = 1.5f;
            tempLevel.trayStackSpacing = 0.15f;
            tempLevel.deckColSpacing = 3.0f;
            tempLevel.deckRowSpacing = 3.0f;
            tempLevel.holdingCount = holdingCount;
            tempLevel.holdingSpacing = 3.0f;
            tempLevel.canColorize = true;
            tempLevel.maxColors = numColors;
            tempLevel.overlayOffset = Vector2.zero;
            tempLevel.overlayScale = Vector2.one;

            string json = tempLevel.ExportToJson();
            Object.DestroyImmediate(tempLevel);

            return json;
        }

        Color[] ResizeAndReadPixels(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Graphics.Blit(source, rt);

            Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = resized.GetPixels();
            Object.DestroyImmediate(resized);

            return pixels;
        }

        void KMeansQuantize(Color[] pixels, int k, out Color[] centroids, out int[] assignments)
        {
            int pixelCount = pixels.Length;
            assignments = new int[pixelCount];
            centroids = new Color[k];

            // Initialize centroids from evenly spaced pixel samples
            for (int i = 0; i < k; i++)
            {
                int sampleIdx = (int)((float)i / k * pixelCount);
                sampleIdx = Mathf.Clamp(sampleIdx, 0, pixelCount - 1);
                centroids[i] = pixels[sampleIdx];
            }

            // Iterate k-means (max 20 rounds)
            for (int iteration = 0; iteration < 20; iteration++)
            {
                bool changed = false;

                // Assign each pixel to nearest centroid
                for (int p = 0; p < pixelCount; p++)
                {
                    float bestDist = float.MaxValue;
                    int bestIdx = 0;
                    for (int c = 0; c < k; c++)
                    {
                        float dist = ColorDistanceSq(pixels[p], centroids[c]);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = c;
                        }
                    }
                    if (assignments[p] != bestIdx)
                    {
                        assignments[p] = bestIdx;
                        changed = true;
                    }
                }

                if (!changed) break;

                // Recalculate centroids
                float[] sumR = new float[k];
                float[] sumG = new float[k];
                float[] sumB = new float[k];
                float[] sumA = new float[k];
                int[] counts = new int[k];

                for (int p = 0; p < pixelCount; p++)
                {
                    int cluster = assignments[p];
                    sumR[cluster] += pixels[p].r;
                    sumG[cluster] += pixels[p].g;
                    sumB[cluster] += pixels[p].b;
                    sumA[cluster] += pixels[p].a;
                    counts[cluster]++;
                }

                for (int c = 0; c < k; c++)
                {
                    if (counts[c] > 0)
                    {
                        centroids[c] = new Color(
                            sumR[c] / counts[c],
                            sumG[c] / counts[c],
                            sumB[c] / counts[c],
                            sumA[c] / counts[c]
                        );
                    }
                }
            }
        }

        float ColorDistanceSq(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }
}
