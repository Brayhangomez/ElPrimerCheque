#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Asistente del Taller Práctico 2 ("El Primer Cheque").
// Menú Taller2 > 1 importa los recursos esenciales de TextMeshPro.
// Menú Taller2 > 2 arma las escenas MainMenu y GameScene siguiendo las fases de la guía.
public static class Taller2Builder
{
    const string SCENES = "Assets/Scenes";
    const string MAIN_MENU = SCENES + "/MainMenu.unity";
    const string GAME_SCENE = SCENES + "/GameScene.unity";

    const string VAL_IDLE = "Assets/Sprites/Personaje/Valentina_Idle.png";
    const string VAL_WALK = "Assets/Sprites/Personaje/Valentina_Walk.png";
    const string TILESET = "Assets/Sprites/Entorno/Tileset_Barrio.png";
    const string FONDO_BARRIO = "Assets/Sprites/Entorno/Fondo_Barrio.png";
    const string FONDO_MENU = "Assets/Sprites/UI/Fondo_Menu.png";
    const string BOTON = "Assets/Sprites/UI/Boton_Menu.png";

    const string CLIP_IDLE = "Assets/Animations/Player/Player_Idle.anim";
    const string CLIP_WALK = "Assets/Animations/Player/Player_Walk.anim";
    const string CONTROLLER = "Assets/Animations/Player/Player.controller";

    static readonly Color Crema = new Color32(242, 230, 208, 255);
    static readonly Color Terracota = new Color32(196, 98, 58, 255);
    static readonly Color Oscuro = new Color32(42, 30, 26, 255);

    // ---------------------------------------------------------------- 1
    [MenuItem("Taller2/1 - Importar TMP Essentials")]
    public static void ImportarTMP()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
        string root = info != null ? info.resolvedPath : Path.GetFullPath("Packages/com.unity.ugui");
        var pkg = Directory.GetFiles(root, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault();
        if (pkg == null) { Debug.LogError("Taller2: no se encontró 'TMP Essential Resources.unitypackage'."); return; }
        AssetDatabase.ImportPackage(pkg, false);
        Debug.Log("Taller2: TMP Essentials importados desde " + pkg);
    }

    // ---------------------------------------------------------------- 2
    [MenuItem("Taller2/2 - Construir escenas del taller")]
    public static void Construir()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("Taller2", "Primero ejecuta Taller2 > 1 - Importar TMP Essentials.", "OK");
            return;
        }
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Fase 3 (parte de recursos): importación y recorte de texturas
        ConfigurarTexturas();
        var tiles = CrearTiles();
        CrearPaleta(tiles);

        // Fase 4: animaciones y máquina de estados con el parámetro Speed
        ActualizarAnimaciones();

        // Fase 1: escena de menú
        ConstruirMenu();

        // Fases 2, 3, 5: escena de juego (tilemap, físicas, cámara)
        ConstruirGameScene(tiles);

        // Registro de escenas en Build Settings (índice 0 y 1)
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MAIN_MENU, true),
            new EditorBuildSettingsScene(GAME_SCENE, true)
        };

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MAIN_MENU, OpenSceneMode.Single);
        Debug.Log("Taller2: escenas MainMenu y GameScene construidas.");
    }

    // ================================================================ Texturas
    static void ConfigurarTexturas()
    {
        Recortar(VAL_IDLE, 32, 48, 32, "Valentina_Idle");
        Recortar(VAL_WALK, 32, 48, 32, "Valentina_Walk");
        Recortar(TILESET, 32, 32, 32, "Tile_Barrio", SpriteAlignment.Center, new Vector2(0.5f, 0.5f));
        Simple(FONDO_BARRIO, 32, SpriteAlignment.BottomLeft, Vector4.zero);
        Simple(FONDO_MENU, 100, SpriteAlignment.Center, Vector4.zero);
        Simple(BOTON, 32, SpriteAlignment.Center, new Vector4(4, 4, 4, 4)); // 9-slice
    }

    static TextureImporter Base(string path, float ppu)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        return imp;
    }

    static void Simple(string path, float ppu, SpriteAlignment align, Vector4 border)
    {
        var imp = Base(path, ppu);
        imp.spriteImportMode = SpriteImportMode.Single;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)align;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.spriteBorder = border;
        imp.SaveAndReimport();
    }

    // Equivalente a Sprite Editor > Slice > Grid By Cell Size
    static void Recortar(string path, int cw, int ch, float ppu, string prefix,
        SpriteAlignment align = SpriteAlignment.BottomCenter, Vector2? pivot = null)
    {
        var imp = Base(path, ppu);
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.SaveAndReimport();
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        int cols = w / cw, rows = h / ch, i = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                rects.Add(new SpriteRect
                {
                    name = prefix + "_" + i++,
                    rect = new Rect(c * cw, h - (r + 1) * ch, cw, ch),
                    alignment = align,
                    pivot = pivot ?? new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate()
                });
            }
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
            names.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static Sprite[] Sprites(string path, string prefix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Where(s => s.name.StartsWith(prefix))
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    // ================================================================ Tiles
    static Tile[] CrearTiles()
    {
        const string dir = "Assets/Tilemaps/Tiles";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Tilemaps", "Tiles");
        string[] nombres = { "Acera", "Acera_B", "Concreto", "Tierra", "Jardinera", "Ladrillo", "Baldosa", "Asfalto" };
        var sprites = Sprites(TILESET, "Tile_Barrio");
        var tiles = new Tile[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            string p = dir + "/" + nombres[Mathf.Min(i, nombres.Length - 1)] + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(p);
            if (t == null) { t = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(t, p); }
            t.sprite = sprites[i];
            t.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(t);
            tiles[i] = t;
        }
        AssetDatabase.SaveAssets();
        return tiles;
    }

    // Tile Palette (opcional): se crea por reflexión para no depender del ensamblado del editor de Tilemaps.
    static void CrearPaleta(Tile[] tiles)
    {
        const string path = "Assets/Tilemaps/Paleta_Barrio.prefab";
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var type = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnityEditor.Tilemaps.GridPaletteUtility")).FirstOrDefault(t => t != null);
                var m = type?.GetMethods().FirstOrDefault(x => x.Name == "CreateNewPalette" && x.GetParameters().Length == 6);
                if (m == null) { Debug.LogWarning("Taller2: no se pudo crear la Tile Palette automáticamente."); return; }
                var sizing = m.GetParameters()[3].ParameterType;
                m.Invoke(null, new object[] { "Assets/Tilemaps", "Paleta_Barrio", GridLayout.CellLayout.Rectangle,
                    System.Enum.ToObject(sizing, 0), Vector3.one, GridLayout.CellSwizzle.XYZ });
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            var tm = root.GetComponentInChildren<Tilemap>();
            tm.ClearAllTiles();
            for (int i = 0; i < tiles.Length; i++) tm.SetTile(new Vector3Int(i % 4, -(i / 4), 0), tiles[i]);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        catch (System.Exception e) { Debug.LogWarning("Taller2: Tile Palette omitida: " + e.Message); }
    }

    // ================================================================ Animaciones
    static void ActualizarAnimaciones()
    {
        AsignarClip(CLIP_IDLE, Sprites(VAL_IDLE, "Valentina_Idle"), 6);
        AsignarClip(CLIP_WALK, Sprites(VAL_WALK, "Valentina_Walk"), 10);

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER);
        for (int i = ctrl.parameters.Length - 1; i >= 0; i--) ctrl.RemoveParameter(i);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.states.First(s => s.state.name == "Player_Idle").state;
        var walk = sm.states.First(s => s.state.name == "Player_Walk").state;
        foreach (var st in new[] { idle, walk })
            foreach (var t in st.transitions.ToArray()) st.RemoveTransition(t);

        var aWalk = idle.AddTransition(walk);
        aWalk.hasExitTime = false; aWalk.hasFixedDuration = true; aWalk.duration = 0f;
        aWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

        var aIdle = walk.AddTransition(idle);
        aIdle.hasExitTime = false; aIdle.hasFixedDuration = true; aIdle.duration = 0f;
        aIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
    }

    static void AsignarClip(string path, Sprite[] frames, float fps)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        clip.frameRate = fps;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] }; // cierre limpio del bucle
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        EditorUtility.SetDirty(clip);
    }

    // ================================================================ Fase 1: Menú
    static void ConstruirMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Oscuro;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvas = CrearCanvas("Canvas");

        // Fondo a pantalla completa (primer hijo = telón trasero)
        var bg = UI<Image>("BackgroundImage", canvas.transform);
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FONDO_MENU);
        bg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        bg.raycastTarget = false;
        Stretch(bg.rectTransform);

        // Título y subtítulo
        var title = Texto("Titulo", canvas.transform, "EL PRIMER CHEQUE", 130, Crema, FontStyles.Bold);
        Anclar(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1700, 190));
        var sub = Texto("Subtitulo", canvas.transform, "Sobrevive a tu primer mes de independencia", 46, Crema, FontStyles.Italic);
        Anclar(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -320), new Vector2(1600, 70));

        // Panel de menú con los 3 botones
        var panel = new GameObject("MenuPanel", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        Anclar(panel, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(1500, 130));
        var hl = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 90; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false;

        var managerGo = new GameObject("MenuManager");
        var manager = managerGo.AddComponent<MainMenuManager>();

        var bPlay = Boton("Btn_Play", panel, "JUGAR");
        var bCred = Boton("Btn_Credits", panel, "CRÉDITOS");
        var bQuit = Boton("Btn_Quit", panel, "SALIR");
        UnityEventTools.AddPersistentListener(bPlay.onClick, new UnityAction(manager.PlayGame));
        UnityEventTools.AddPersistentListener(bCred.onClick, new UnityAction(manager.OpenCredits));
        UnityEventTools.AddPersistentListener(bQuit.onClick, new UnityAction(manager.QuitGame));

        // Panel emergente modal de créditos
        var cp = UI<Image>("CreditsPanel", canvas.transform);
        cp.color = new Color32(28, 20, 17, 255); // panel modal opaco para máxima legibilidad
        Stretch(cp.rectTransform);
        var ct = Texto("Titulo_Creditos", cp.transform, "INFORMACIÓN Y CRÉDITOS", 66, new Color32(222, 130, 90, 255), FontStyles.Bold);
        Anclar(ct.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -65), new Vector2(1600, 90));
        var body = Texto("Cuerpo_Creditos", cp.transform, CREDITOS, 25, Crema, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.TopLeft;
        Anclar(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(1500, 780));
        body.lineSpacing = -8;
        var bBack = Boton("Btn_CloseCredits", cp.transform, "VOLVER");
        Anclar((RectTransform)bBack.transform, new Vector2(0.5f, 0f), new Vector2(0, 62), new Vector2(340, 95));
        UnityEventTools.AddPersistentListener(bBack.onClick, new UnityAction(manager.CloseCredits));

        var so = new SerializedObject(manager);
        so.FindProperty("creditsPanel").objectReferenceValue = cp.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        cp.gameObject.SetActive(false); // estado inicial oculto

        EditorSceneManager.SaveScene(scene, MAIN_MENU);
    }

    static Canvas CrearCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static T UI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anclar(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static TextMeshProUGUI Texto(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        var t = UI<TextMeshProUGUI>(name, parent);
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static Button Boton(string name, Transform parent, string label)
    {
        var img = UI<Image>(name, parent);
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BOTON);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.4f;
        img.rectTransform.sizeDelta = new Vector2(380, 110);
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color32(255, 214, 170, 255); // resalte cálido al pasar el cursor
        cb.pressedColor = new Color32(196, 150, 120, 255);     // tono más oscuro al presionar
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        var t = Texto("Text (TMP)", img.transform, label, 50, Oscuro, FontStyles.Bold);
        Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(0, 10); // compensa la sombra inferior del sprite
        return b;
    }

    static Tile LoadTile(string n) { return AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Tiles/" + n + ".asset"); }

    // Pinta el suelo del barrio (60 casillas de ancho) y la decoración.
    static void PintarNivel(Tilemap tm, Tilemap dtm)
    {
        Tile acera = LoadTile("Acera"), aceraB = LoadTile("Acera_B"), concreto = LoadTile("Concreto"), tierra = LoadTile("Tierra"), jard = LoadTile("Jardinera");
        tm.ClearAllTiles();
        for (int x = -12; x < 48; x++)
        {
            tm.SetTile(new Vector3Int(x, -1, 0), (x & 1) == 0 ? acera : aceraB); // acera
            tm.SetTile(new Vector3Int(x, -2, 0), concreto);                        // concreto
            tm.SetTile(new Vector3Int(x, -3, 0), tierra);                          // tierra
            tm.SetTile(new Vector3Int(x, -4, 0), tierra);
        }
        tm.CompressBounds();
        // Fuerza la regeneración de la malla de colisión unificada (Composite Collider 2D)
        var tc = tm.GetComponent<TilemapCollider2D>();
        if (tc != null)
        {
            tc.compositeOperation = Collider2D.CompositeOperation.None;
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        var cc = tm.GetComponent<CompositeCollider2D>();
        if (cc != null) cc.GenerateGeometry();
        EditorUtility.SetDirty(tm);
        dtm.ClearAllTiles();
        foreach (int x in new[] { 4, 5, 19, 20, 33, 44, 45 }) dtm.SetTile(new Vector3Int(x, 0, 0), jard);
        EditorUtility.SetDirty(dtm);
        Debug.Log("Taller2: suelo pintado con " + tm.GetUsedTilesCount() + " tipos de tile.");
    }

    [MenuItem("Taller2/3 - Repintar suelo de GameScene")]
    public static void Repintar()
    {
        var scene = EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);
        var suelo = GameObject.Find("Grid/Suelo").GetComponent<Tilemap>();
        var deco = GameObject.Find("Grid/Decoracion").GetComponent<Tilemap>();
        PintarNivel(suelo, deco);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // ================================================================ GameScene
    static void ConstruirGameScene(Tile[] tiles)
    {
        if (!File.Exists(GAME_SCENE) && File.Exists(SCENES + "/SampleScene.unity"))
        {
            string err = AssetDatabase.RenameAsset(SCENES + "/SampleScene.unity", "GameScene");
            if (!string.IsNullOrEmpty(err)) Debug.LogError("Taller2: " + err);
            AssetDatabase.Refresh();
        }
        var scene = EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);

        foreach (var n in new[] { "Grid", "Fondo_Barrio", "Limites", "HUD" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        // ---- Fondo del barrio (decorativo, detrás de todo)
        var fondo = new GameObject("Fondo_Barrio");
        var fsr = fondo.AddComponent<SpriteRenderer>();
        fsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FONDO_BARRIO);
        fsr.sortingOrder = -10;
        fondo.transform.position = new Vector3(-12, 0, 0);

        // ---- Fase 2: Tilemap con colisiones unificadas
        var grid = new GameObject("Grid").AddComponent<Grid>();
        var suelo = new GameObject("Suelo");
        suelo.transform.SetParent(grid.transform, false);
        var tm = suelo.AddComponent<Tilemap>();
        suelo.AddComponent<TilemapRenderer>();
        var tcol = suelo.AddComponent<TilemapCollider2D>();
        tcol.compositeOperation = Collider2D.CompositeOperation.Merge;
        var srb = suelo.AddComponent<Rigidbody2D>();
        srb.bodyType = RigidbodyType2D.Static; // evita que el escenario caiga
        suelo.AddComponent<CompositeCollider2D>();

        // Decoración sin colisión (jardineras)
        var deco = new GameObject("Decoracion");
        deco.transform.SetParent(grid.transform, false);
        var dtm = deco.AddComponent<Tilemap>();
        var dr = deco.AddComponent<TilemapRenderer>();
        dr.sortingOrder = -1;
        PintarNivel(tm, dtm);

        // Límites invisibles a los lados del mapa
        var lim = new GameObject("Limites");
        foreach (var (n, x) in new[] { ("Limite_Izq", -12.5f), ("Limite_Der", 48.5f) })
        {
            var w = new GameObject(n);
            w.transform.SetParent(lim.transform, false);
            w.transform.position = new Vector3(x, 5, 0);
            w.AddComponent<BoxCollider2D>().size = new Vector2(1, 20);
        }

        // ---- Fase 3: Player con físicas y script de control
        var player = GameObject.Find("Player");
        player.transform.position = new Vector3(0, 0.1f, 0);
        player.transform.localScale = Vector3.one;
        var sr = player.GetComponent<SpriteRenderer>();
        sr.sprite = Sprites(VAL_IDLE, "Valentina_Idle")[0];
        var rb = GetOrAdd<Rigidbody2D>(player);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var cap = GetOrAdd<CapsuleCollider2D>(player);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.size = new Vector2(0.45f, 1.3f);
        cap.offset = new Vector2(0f, 0.68f);
        GetOrAdd<PlayerController>(player);
        var anim = player.GetComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER);

        // ---- Fase 5: cámara con zona muerta y límites
        var camGo = GameObject.FindWithTag("MainCamera");
        var cam = camGo.GetComponent<Camera>();
        cam.orthographicSize = 4f;
        cam.backgroundColor = new Color32(250, 214, 175, 255);
        camGo.transform.position = new Vector3(0, 1, -10);
        var follow = GetOrAdd<CameraFollow2D>(camGo);
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("smoothSpeed").floatValue = 5f;
        so.FindProperty("offset").vector3Value = new Vector3(0, 1, -10);
        so.FindProperty("deadZoneX").floatValue = 1.5f;
        so.FindProperty("useLimits").boolValue = true;
        so.FindProperty("minX").floatValue = -5f;   // borde izquierdo del mapa (-12) + media pantalla
        so.FindProperty("maxX").floatValue = 41f;   // borde derecho del mapa (48) - media pantalla
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- HUD sencillo con la instrucción del capítulo
        var hud = CrearCanvas("HUD");
        var info = Texto("Capitulo", hud.transform, "Capítulo 1: El primer sueldo", 44, Oscuro, FontStyles.Bold);
        info.alignment = TextAlignmentOptions.TopLeft;
        Anclar(info.rectTransform, new Vector2(0f, 1f), new Vector2(560, -60), new Vector2(1000, 60));
        var ayuda = Texto("Ayuda", hud.transform, "Usa A / D o las flechas para caminar por el barrio", 30, Oscuro, FontStyles.Normal);
        ayuda.alignment = TextAlignmentOptions.TopLeft;
        Anclar(ayuda.rectTransform, new Vector2(0f, 1f), new Vector2(560, -115), new Vector2(1000, 50));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    const string CREDITOS =
"<b>EL PRIMER CHEQUE</b> — Versión Prototipo · 2026-2\n\n" +
"<b>1. PROPÓSITO EDUCATIVO Y COMPETENCIAS</b>\n" +
"- Asignatura / Área: Educación financiera · Desarrollo de Contenidos Digitales y Multimediales\n" +
"- Competencia formativa: el jugador aprende a presupuestar su primer salario, a diferenciar\n" +
"  necesidades de deseos y a valorar el ahorro y el fondo de emergencia frente al crédito fácil.\n\n" +
"<b>2. EQUIPO DE DESARROLLO</b>\n" +
"- Diseño y Lógica de Juego (Game Design): Brayhan Gómez y Camilo Vargas\n" +
"- Programación e Integración: Brayhan Gómez y Camilo Vargas\n" +
"- Arte, Animación e Interfaces: Brayhan Gómez y Camilo Vargas\n\n" +
"<b>3. FUENTES Y REFERENCIAS ACADÉMICAS</b>\n" +
"- Banco de la República de Colombia. Educación económica y financiera. banrep.gov.co\n" +
"- Superintendencia Financiera de Colombia. Educación financiera. superfinanciera.gov.co\n" +
"- OCDE (2020). Recommendation of the Council on Financial Literacy.\n\n" +
"<b>4. ATRIBUCIÓN DE RECURSOS DE TERCEROS</b>\n" +
"- Arte y sprites: originales creados para este prototipo.\n" +
"- Tipografía: Liberation Sans (TextMeshPro), licencia SIL Open Font License.\n\n" +
"<b>AVISO LEGAL Y ACADÉMICO:</b> prototipo interactivo con fines formativos y académicos dentro de la\n" +
"asignatura. No posee fines comerciales ni lucrativos.";
}
#endif
