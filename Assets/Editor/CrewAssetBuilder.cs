using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports the regular 4 x 2 animation sheets and builds reusable character prefabs.</summary>
[InitializeOnLoad]
public static class CrewAssetBuilder
{
    private const string Root = "Assets/Art/Crew/Animated";
    private static readonly string[] Roles = { "Commander", "Medic", "Pilot", "Engineer" };

    static CrewAssetBuilder()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    private static void BuildIfMissing()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (Roles.All(role => File.Exists($"{Root}/{role}/{role}Sheet.png")) &&
            Roles.Any(role => !File.Exists($"Assets/Prefabs/Crew/{role}.prefab"))) Build();
    }

    [MenuItem("Tools/Crew/Rebuild Animated Characters")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Prefabs/Crew");
        AssetDatabase.Refresh();
        foreach (string role in Roles) BuildRole(role);
        AssetDatabase.SaveAssets();
        Debug.Log("Crew assets ready: four transparent characters, Idle/Walk clips and prefabs.");
    }

    private static void BuildRole(string role)
    {
        string folder = $"{Root}/{role}";
        string sheetPath = $"{folder}/{role}Sheet.png";
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Missing sheet: {sheetPath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
        float width = texture.width / 4f;
        float height = texture.height / 2f;
        // Every cell has the same pixel scale and pivot: no per-frame auto-crop jitter.
        importer.spritePixelsPerUnit = height / 2.2f;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects();
        var rects = new SpriteRect[8];
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 4; column++)
        {
            int index = row * 4 + column;
            string name = $"{role}_{(row == 0 ? "Idle" : "Walk")}_{column}";
            rects[index] = new SpriteRect
            {
                name = name,
                rect = Rect.MinMaxRect(Mathf.RoundToInt(column * width),
                    Mathf.RoundToInt((1 - row) * height), Mathf.RoundToInt((column + 1) * width),
                    Mathf.RoundToInt((2 - row) * height)),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.06f),
                spriteID = previous.FirstOrDefault(rect => rect.name == name)?.spriteID ?? GUID.Generate()
            };
        }
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
        if (sprites.Length != 8) throw new InvalidOperationException($"{role}: expected eight sprites.");
        AnimationClip idle = BuildClip(folder, "Idle", sprites.Take(4).ToArray(), 4);
        AnimationClip walk = BuildClip(folder, "Walk", sprites.Skip(4).ToArray(), 6);
        string controllerPath = $"{folder}/{role}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.parameters = new[] { new AnimatorControllerParameter { name = "Walking", type = AnimatorControllerParameterType.Bool } };
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        var idleState = machine.AddState("Idle");
        idleState.motion = idle;
        var walkState = machine.AddState("Walk");
        walkState.motion = walk;
        machine.defaultState = idleState;
        var start = idleState.AddTransition(walkState);
        start.hasExitTime = false;
        start.duration = 0;
        start.AddCondition(AnimatorConditionMode.If, 0, "Walking");
        var stop = walkState.AddTransition(idleState);
        stop.hasExitTime = false;
        stop.duration = 0;
        stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");

        var character = new GameObject(role);
        try
        {
            var renderer = character.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[0];
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            character.AddComponent<Animator>().runtimeAnimatorController = controller;
            var component = character.AddComponent<CrewCharacter>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("role").enumValueIndex = Array.IndexOf(Roles, role);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(character, $"Assets/Prefabs/Crew/{role}.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(character); }
    }

    private static AnimationClip BuildClip(string folder, string name, Sprite[] sprites, float rate)
    {
        string path = $"{folder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = rate;
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i <= sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / rate, value = sprites[i % sprites.Length] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }
}
