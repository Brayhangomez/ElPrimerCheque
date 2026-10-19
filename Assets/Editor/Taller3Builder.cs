#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

// Asistente del Taller 3 / Previo 2 ("El Primer Cheque"): NPC con diálogo, objetos interactivos,
// monedas coleccionables y HUD. También exporta la build de Windows y graba el video demostrativo.
public static class Taller3Builder
{
    const string GAME_SCENE = "Assets/Scenes/GameScene.unity";
    const string MAIN_MENU = "Assets/Scenes/MainMenu.unity";
    const string RAFAEL = "Assets/Sprites/Personaje/DonRafael_Idle.png";
    const string MONEDA = "Assets/Sprites/Objetos/Moneda.png";
    const string MONEDA_ICONO = "Assets/Sprites/UI/Moneda_Icono.png";
    const string MONEDA_VACIA = "Assets/Sprites/UI/Moneda_Vacia.png";
    const string ALCANCIA = "Assets/Sprites/UI/Alcancia.png";
    const string TECLA_E = "Assets/Sprites/UI/Tecla_E.png";
    const string CARTEL = "Assets/Sprites/Objetos/Cartel_Banco.png";
    const string VITRINA = "Assets/Sprites/Objetos/Vitrina_Tienda.png";
    const string BOTON = "Assets/Sprites/UI/Boton_Menu.png";

    static readonly Color Crema = new Color32(242, 230, 208, 255);
    static readonly Color Terracota = new Color32(196, 98, 58, 255);
    static readonly Color TerracotaClaro = new Color32(222, 130, 90, 255);
    static readonly Color Oscuro = new Color32(42, 30, 26, 255);
    static readonly Color Salvia = new Color32(156, 175, 136, 255);
    static readonly Color Dorado = new Color32(232, 190, 72, 255);

    // ================================================================ 1. Montaje
    [MenuItem("Taller3/1 - Montar NPC, objetos, monedas y HUD")]
    public static void Montar()
    {
        EditorSceneManager.SaveOpenScenes();
        ImportarArte();
        var scene = EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);

        foreach (var n in new[] { "NPC_DonRafael", "Obj_Vitrina", "Obj_CartelBanco", "Coleccionables", "GameHUDManager" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        var player = GameObject.Find("Player");
        player.tag = "Player";

        var hud = GameObject.Find("HUD");
        foreach (Transform t in hud.transform.Cast<Transform>().ToList())
            if (t.name.StartsWith("T3_") || t.name == "Ayuda" || t.name == "Capitulo") Object.DestroyImmediate(t.gameObject);

        // ---------- HUD: panel de misión (arriba izquierda)
        var misionBg = Panel("T3_PanelMision", hud.transform, new Color(0.95f, 0.9f, 0.82f, 0.88f));
        Anclar(misionBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(980, 150));
        var capitulo = Texto("Capitulo", misionBg.transform, "Capítulo 1: El primer sueldo", 40, Oscuro, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(capitulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -18), new Vector2(940, 54));
        var objetivo = Texto("ObjetivoTexto", misionBg.transform, "Misión: ahorra 5 monedas para tu fondo de emergencia", 32, Terracota, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(objetivo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -78), new Vector2(940, 50));

        // ---------- HUD: contador + mini-inventario (arriba derecha)
        var contBg = Panel("T3_PanelAhorro", hud.transform, new Color(0.95f, 0.9f, 0.82f, 0.88f));
        Anclar(contBg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -36), new Vector2(520, 150));
        var icono = UI<Image>("IconoMoneda", contBg.transform);
        icono.sprite = Load<Sprite>(MONEDA_ICONO); icono.preserveAspect = true;
        Anclar(icono.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(56, 56));
        var contador = Texto("ContadorTexto", contBg.transform, "Ahorro: 0 / 5", 40, Oscuro, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        Anclar(contador.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -16), new Vector2(400, 56));
        var slots = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            slots[i] = UI<Image>("Slot_" + (i + 1), contBg.transform);
            slots[i].sprite = Load<Sprite>(MONEDA_VACIA); slots[i].preserveAspect = true;
            Anclar(slots[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26 + i * 96, -84), new Vector2(52, 52));
        }

        // ---------- HUD: ayuda de controles (abajo izquierda)
        var ayudaBg = Panel("Ayuda", hud.transform, new Color32(42, 30, 26, 200));
        Anclar(ayudaBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 24), new Vector2(1060, 52));
        var ayuda = Texto("AyudaTexto", ayudaBg.transform, "A / D o flechas: caminar   ·   E: hablar / interactuar   ·   Esc: volver al menú", 26, Crema, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(ayuda.rectTransform);

        // ---------- Panel de diálogo (abajo)
        var dlg = Panel("T3_DialoguePanel", hud.transform, new Color32(42, 30, 26, 250));
        Anclar(dlg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1500, 260), new Vector2(0.5f, 0));
        var dlgBorde = Panel("Borde", dlg.transform, Terracota); Stretch(dlgBorde.rectTransform); dlgBorde.rectTransform.offsetMin = new Vector2(0, 252);
        var npcName = Texto("NPCNameText", dlg.transform, "Don Rafael", 40, Dorado, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(npcName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -26), new Vector2(900, 52));
        var dlgText = Texto("DialogueContentText", dlg.transform, "...", 34, Crema, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        dlgText.textWrappingMode = TextWrappingModes.Normal;
        Anclar(dlgText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -84), new Vector2(1420, 140));
        var dlgHint = Texto("Indicador", dlg.transform, "Presiona E para continuar...", 24, TerracotaClaro, FontStyles.Italic, TextAlignmentOptions.BottomRight);
        Anclar(dlgHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 18), new Vector2(600, 36), new Vector2(1, 0));
        dlg.gameObject.SetActive(false);

        // ---------- Panel de información educativa (centro)
        var info = Panel("T3_InfoPanel", hud.transform, new Color32(242, 230, 208, 248));
        Anclar(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1100, 470));
        var infoBorde = Panel("Franja", info.transform, Terracota);
        Anclar(infoBorde.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 18), new Vector2(0.5f, 1));
        var infoTitle = Texto("InfoTitulo", info.transform, "Título", 42, Terracota, FontStyles.Bold, TextAlignmentOptions.Top);
        Anclar(infoTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(1000, 60), new Vector2(0.5f, 1));
        var infoText = Texto("InfoTexto", info.transform, "...", 32, Oscuro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        infoText.textWrappingMode = TextWrappingModes.Normal;
        Anclar(infoText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(980, 280), new Vector2(0.5f, 1));
        var infoHint = Texto("Indicador", info.transform, "Presiona E para cerrar", 24, Terracota, FontStyles.Italic, TextAlignmentOptions.Bottom);
        Anclar(infoHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(800, 36), new Vector2(0.5f, 0));
        info.gameObject.SetActive(false);

        // ---------- Panel de misión cumplida
        var done = Panel("T3_MissionCompletePanel", hud.transform, new Color32(42, 30, 26, 240));
        Anclar(done.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 400));
        var alc = UI<Image>("Alcancia", done.transform); alc.sprite = Load<Sprite>(ALCANCIA); alc.preserveAspect = true;
        Anclar(alc.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(150, 130), new Vector2(0.5f, 1));
        var doneT = Texto("Titulo", done.transform, "¡Misión cumplida!", 54, Dorado, FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(doneT.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(940, 70), new Vector2(0.5f, 1));
        var doneB = Texto("Mensaje", done.transform, "Ahorraste las 5 monedas de tu fondo de emergencia.\nVuelve a hablar con Don Rafael para terminar el capítulo.", 30, Crema, FontStyles.Normal, TextAlignmentOptions.Center);
        Anclar(doneB.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(940, 110), new Vector2(0.5f, 1));
        done.gameObject.SetActive(false);

        // ---------- GameHUDManager
        var mgrGo = new GameObject("GameHUDManager");
        var mgr = mgrGo.AddComponent<GameHUDManager>();
        var so = new SerializedObject(mgr);
        so.FindProperty("counterText").objectReferenceValue = contador;
        so.FindProperty("objectiveText").objectReferenceValue = objetivo;
        so.FindProperty("missionCompletePanel").objectReferenceValue = done.gameObject;
        var sp = so.FindProperty("inventorySlots"); sp.arraySize = 5;
        for (int i = 0; i < 5; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.FindProperty("emptySlotSprite").objectReferenceValue = Load<Sprite>(MONEDA_VACIA);
        so.FindProperty("filledSlotSprite").objectReferenceValue = Load<Sprite>(MONEDA_ICONO);
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- NPC Don Rafael (frente a los apartamentos)
        var rafaelFrames = Sprites(RAFAEL, "DonRafael_Idle");
        var npc = new GameObject("NPC_DonRafael");
        npc.transform.position = new Vector3(-6f, 0f, 0f);
        var nsr = npc.AddComponent<SpriteRenderer>();
        nsr.sprite = rafaelFrames[0]; nsr.sortingLayerName = "Characters"; nsr.sortingOrder = 0;
        var loop = npc.AddComponent<SpriteLoop>();
        var lso = new SerializedObject(loop);
        var lf = lso.FindProperty("frames"); lf.arraySize = rafaelFrames.Length;
        for (int i = 0; i < rafaelFrames.Length; i++) lf.GetArrayElementAtIndex(i).objectReferenceValue = rafaelFrames[i];
        lso.FindProperty("framesPerSecond").floatValue = 3f;
        lso.ApplyModifiedPropertiesWithoutUndo();
        var ncol = npc.AddComponent<CircleCollider2D>(); ncol.isTrigger = true; ncol.radius = 1.7f; ncol.offset = new Vector2(0, 0.75f);
        var npcPrompt = Prompt(npc.transform, new Vector3(0, 1.95f, 0));
        var dialogo = npc.AddComponent<NPCDialogue>();
        var dso = new SerializedObject(dialogo);
        Color cR = Crema, cV = Dorado;
        SetLines(dso.FindProperty("conversation"), new[] {
            ("Don Rafael", cR, "¡Buenas, Valentina! Bienvenida al edificio. Recuerde que el arriendo se paga el primer día de cada mes."),
            ("Valentina", cV, "Sí, don Rafael. Hoy me pagaron mi primer sueldo... ¡y ya me lo quiero gastar todo!"),
            ("Don Rafael", cR, "Cuidado con eso, mija. Use la regla 50/30/20: 50% para necesidades, 30% para gustos y 20% para ahorrar."),
            ("Valentina", cV, "¿Y ese 20% para qué me sirve?"),
            ("Don Rafael", cR, "Para su fondo de emergencia. Junte 5 monedas de ahorro por el barrio y no caiga en las tentaciones de la tienda ni del banco."),
            ("Valentina", cV, "¡Entendido! Voy por esas 5 monedas.")
        });
        SetLines(dso.FindProperty("missionCompleteConversation"), new[] {
            ("Don Rafael", cR, "¡Lo logró! Con ese fondo de emergencia ya está preparada para cualquier imprevisto."),
            ("Valentina", cV, "Y sin endeudarme con la tarjeta. ¡Mi primer mes va muy bien!"),
            ("Don Rafael", cR, "Así es: primero se ahorra y después se gasta. Esa es la clave de la independencia.")
        });
        dso.FindProperty("dialoguePanel").objectReferenceValue = dlg.gameObject;
        dso.FindProperty("nameText").objectReferenceValue = npcName;
        dso.FindProperty("contentText").objectReferenceValue = dlgText;
        dso.FindProperty("interactionPrompt").objectReferenceValue = npcPrompt;
        dso.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Objetos interactivos
        Interactivo("Obj_Vitrina", VITRINA, new Vector3(15f, 0f, 0f), new Vector2(3.2f, 2f), 1.75f, info, infoTitle, infoText,
            "Vitrina de la tienda: ¿necesidad o deseo?",
            "Las etiquetas ROJAS son deseos (tenis de marca, celular nuevo) y las VERDES son necesidades (pan, leche).\n\nAntes de comprar pregúntate: ¿lo necesito o solo lo quiero? Primero cubre tus necesidades y deja los gustos para tu 30%.");
        Interactivo("Obj_CartelBanco", CARTEL, new Vector3(27f, 0f, 0f), new Vector2(3.2f, 2f), 1.85f, info, infoTitle, infoText,
            "Cartel del banco: la tarjeta de crédito",
            "La tarjeta de crédito NO es dinero extra: es una deuda. Si solo pagas la cuota mínima, los intereses hacen que termines pagando mucho más.\n\nÚsala solo si puedes pagar el total cada mes.");

        // ---------- Monedas coleccionables
        var cont = new GameObject("Coleccionables");
        var coinFrames = Sprites(MONEDA, "Moneda");
        float[] xs = { 4f, 9f, 20f, 34f, 42f };
        for (int i = 0; i < xs.Length; i++)
        {
            var c = new GameObject("Moneda_" + (i + 1));
            c.transform.SetParent(cont.transform, false);
            c.transform.position = new Vector3(xs[i], 0.75f, 0f);
            var csr = c.AddComponent<SpriteRenderer>(); csr.sprite = coinFrames[0]; csr.sortingLayerName = "Characters"; csr.sortingOrder = 2;
            var cl = c.AddComponent<SpriteLoop>();
            var cso = new SerializedObject(cl);
            var cf = cso.FindProperty("frames"); cf.arraySize = coinFrames.Length;
            for (int k = 0; k < coinFrames.Length; k++) cf.GetArrayElementAtIndex(k).objectReferenceValue = coinFrames[k];
            cso.FindProperty("framesPerSecond").floatValue = 8f;
            cso.FindProperty("bobAmplitude").floatValue = 0.08f;
            cso.ApplyModifiedPropertiesWithoutUndo();
            var cc = c.AddComponent<CircleCollider2D>(); cc.isTrigger = true; cc.radius = 0.3f;
            c.AddComponent<CollectibleItem>();
        }

        // ---------- Ajustes del Player
        var pc = player.GetComponent<PlayerController>();
        var pso = new SerializedObject(pc);
        pso.FindProperty("moveSpeed").floatValue = 6f;
        pso.ApplyModifiedPropertiesWithoutUndo();

        PlayerSettings.productName = "El Primer Cheque";
        PlayerSettings.companyName = "UTS - Brayhan Gomez y Camilo Vargas";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Taller3: NPC, objetos interactivos, monedas y HUD montados en GameScene.");
    }

    // ================================================================ 2. Build de Windows
    [MenuItem("Taller3/2 - Exportar build de Windows (.zip)")]
    public static void BuildWindows()
    {
        EditorSceneManager.SaveOpenScenes();
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string dir = Path.Combine(root, "Builds", "ElPrimerCheque_Windows");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { MAIN_MENU, GAME_SCENE },
            locationPathName = Path.Combine(dir, "ElPrimerCheque.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Taller3: la build falló: " + report.summary.result);
            return;
        }
        // Quita la carpeta de símbolos de depuración (no hace falta para jugar)
        foreach (var d in Directory.GetDirectories(dir, "*DoNotShip*")) Directory.Delete(d, true);
        string zip = Path.Combine(root, "Builds", "ElPrimerCheque_Windows.zip");
        if (File.Exists(zip)) File.Delete(zip);
        System.IO.Compression.ZipFile.CreateFromDirectory(dir, zip, System.IO.Compression.CompressionLevel.Optimal, true);
        Debug.Log($"Taller3: build lista en {zip} ({new FileInfo(zip).Length / 1048576f:0.0} MB)");
    }

    // ================================================================ 3. Grabación del video
    [MenuItem("Taller3/3 - Grabar video demo (desde el menú)")]
    public static void GrabarDemo()
    {
        EditorSceneManager.SaveOpenScenes();
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Grabacion"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "GRABAR.flag"), "1");
        EditorSceneManager.OpenScene(MAIN_MENU, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    // ================================================================ helpers
    static T Load<T>(string p) where T : Object => AssetDatabase.LoadAssetAtPath<T>(p);

    static void ImportarArte()
    {
        Grid(RAFAEL, 32, 48, "DonRafael_Idle", SpriteAlignment.BottomCenter, new Vector2(0.5f, 0f));
        Grid(MONEDA, 16, 16, "Moneda", SpriteAlignment.Center, new Vector2(0.5f, 0.5f));
        Single(MONEDA_ICONO, 16, SpriteAlignment.Center);
        Single(MONEDA_VACIA, 16, SpriteAlignment.Center);
        Single(ALCANCIA, 32, SpriteAlignment.Center);
        Single(TECLA_E, 32, SpriteAlignment.Center);
        Single(CARTEL, 32, SpriteAlignment.BottomCenter);
        Single(VITRINA, 32, SpriteAlignment.BottomCenter);
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

    static void Single(string path, float ppu, SpriteAlignment align)
    {
        var imp = Base(path, ppu);
        imp.spriteImportMode = SpriteImportMode.Single;
        var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)align; s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    // Equivalente a Sprite Editor > Slice > Grid By Cell Size
    static void Grid(string path, int cw, int ch, string prefix, SpriteAlignment align, Vector2 pivot)
    {
        var imp = Base(path, 32);
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.SaveAndReimport();
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        var f = new SpriteDataProviderFactories(); f.Init();
        var dp = f.GetSpriteEditorDataProviderFromObject(imp); dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < w / cw; i++)
            rects.Add(new SpriteRect { name = prefix + "_" + i, rect = new Rect(i * cw, 0, cw, ch), alignment = align, pivot = pivot, spriteID = GUID.Generate() });
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static Sprite[] Sprites(string path, string prefix) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(s => s.name.StartsWith(prefix))
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();

    static GameObject Prompt(Transform parent, Vector3 localPos)
    {
        var p = new GameObject("IndicadorE");
        p.transform.SetParent(parent, false);
        p.transform.localPosition = localPos;
        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = Load<Sprite>(TECLA_E); sr.sortingLayerName = "Characters"; sr.sortingOrder = 6;
        var l = p.AddComponent<SpriteLoop>();
        var so = new SerializedObject(l);
        so.FindProperty("bobAmplitude").floatValue = 0.1f;
        so.FindProperty("bobSpeed").floatValue = 4f;
        so.ApplyModifiedPropertiesWithoutUndo();
        p.SetActive(false);
        return p;
    }

    static void Interactivo(string name, string spritePath, Vector3 pos, Vector2 trigger, float promptY,
        Image info, TMP_Text infoTitle, TMP_Text infoText, string title, string body)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Load<Sprite>(spritePath); sr.sortingOrder = 0;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = trigger; col.offset = new Vector2(0, trigger.y / 2f);
        var prompt = Prompt(go.transform, new Vector3(0, promptY, 0));
        var io = go.AddComponent<InteractableObject>();
        var so = new SerializedObject(io);
        so.FindProperty("promptFloatingText").objectReferenceValue = prompt;
        so.FindProperty("infoDisplayPanel").objectReferenceValue = info.gameObject;
        so.FindProperty("infoTitleComponent").objectReferenceValue = infoTitle;
        so.FindProperty("infoTextComponent").objectReferenceValue = infoText;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("educationalContent").stringValue = body;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLines(SerializedProperty arr, (string who, Color color, string text)[] lines)
    {
        arr.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speakerName").stringValue = lines[i].who;
            e.FindPropertyRelative("nameColor").colorValue = lines[i].color;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
        }
    }

    static T UI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    static Image Panel(string name, Transform parent, Color c)
    {
        var img = UI<Image>(name, parent);
        img.color = c; img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anclar(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = pivot ?? new Vector2(aMin.x == aMax.x ? aMin.x : 0.5f, aMin.y == aMax.y ? aMin.y : 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static TextMeshProUGUI Texto(string name, Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var t = UI<TextMeshProUGUI>(name, parent);
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
        t.raycastTarget = false;
        return t;
    }
}
#endif
