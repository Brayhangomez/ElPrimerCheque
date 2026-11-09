#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Asistente del Momento 3 (Actividades 1 y 2) de "El Primer Cheque":
// audio con AudioMixer y AudioManager persistente, animaciones por eventos, partículas, luces,
// 3 niveles con LevelConfig, preguntas de decisión, retroalimentación, pistas, tutorial,
// menú de pausa y opciones de accesibilidad. También optimiza (Sprite Atlas) y exporta la build.
public static class Taller4Builder
{
    const string GAME_SCENE = "Assets/Scenes/GameScene.unity";
    const string NIVEL2 = "Assets/Scenes/Nivel2.unity";
    const string NIVEL3 = "Assets/Scenes/Nivel3.unity";
    const string MAIN_MENU = "Assets/Scenes/MainMenu.unity";

    const string MUSICA = "Assets/Audio/Musica";
    const string SFX = "Assets/Audio/SFX";
    const string MIXER = "Assets/Audio/Mezclador_Principal.mixer";
    const string AUDIO_PREFAB = "Assets/Resources/AudioManager.prefab";
    const string CONFIGS = "Assets/ScriptableObjects/Niveles";
    const string EFECTOS = "Assets/Prefabs/Efectos";
    const string CONTROLLER = "Assets/Animations/Player/Player.controller";

    const string VAL_INT = "Assets/Sprites/Personaje/Valentina_Interactuar.png";
    const string VAL_EXITO = "Assets/Sprites/Personaje/Valentina_Exito.png";
    const string VAL_FALLO = "Assets/Sprites/Personaje/Valentina_Fallo.png";
    const string IC_OK = "Assets/Sprites/UI/Icono_Acierto.png";
    const string IC_ERR = "Assets/Sprites/UI/Icono_Error.png";
    const string IC_PISTA = "Assets/Sprites/UI/Icono_Pista.png";
    const string IC_RELOJ = "Assets/Sprites/UI/Icono_Reloj.png";
    const string CORAZON = "Assets/Sprites/UI/Corazon.png";
    const string CORAZON_V = "Assets/Sprites/UI/Corazon_Vacio.png";
    const string BLANCO = "Assets/Sprites/UI/Blanco.png";
    const string PARTICULA = "Assets/Sprites/Efectos/Particula.png";
    const string BOTON = "Assets/Sprites/UI/Boton_Menu.png";
    const string ALCANCIA = "Assets/Sprites/UI/Alcancia.png";
    const string MONEDA_ICONO = "Assets/Sprites/UI/Moneda_Icono.png";
    const string MONEDA_VACIA = "Assets/Sprites/UI/Moneda_Vacia.png";

    static readonly Color Crema = new Color32(242, 230, 208, 255);
    static readonly Color Terracota = new Color32(196, 98, 58, 255);
    static readonly Color TerracotaClaro = new Color32(222, 130, 90, 255);
    static readonly Color Oscuro = new Color32(42, 30, 26, 255);
    static readonly Color Salvia = new Color32(110, 150, 90, 255);
    static readonly Color Dorado = new Color32(232, 190, 72, 255);

    // ================================================================ 1. Montaje completo
    [MenuItem("Taller4/1 - Montar Momento 3 (audio, animaciones, niveles y accesibilidad)", false, 1)]
    public static void MontarTodo()
    {
        EditorSceneManager.SaveOpenScenes();
        ImportarRecursos();
        var mixer = CrearMezclador();
        CrearAudioManager(mixer);
        CrearConfigs();
        ActualizarAnimator();
        CrearEfectos();
        ConstruirNivel(2);
        ConstruirNivel(3);
        ConstruirNivel(1);
        ActualizarMenu();
        ConfigurarBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(GAME_SCENE, OpenSceneMode.Single);
        Debug.Log("Taller4: Momento 3 montado (audio, animaciones, 3 niveles, retroalimentación y accesibilidad).");
    }

    [MenuItem("Taller4/Abrir escena/Menú principal", false, 20)] static void AbrirMenu() => Abrir(MAIN_MENU);
    [MenuItem("Taller4/Abrir escena/Nivel 1 (GameScene)", false, 21)] static void Abrir1() => Abrir(GAME_SCENE);
    [MenuItem("Taller4/Abrir escena/Nivel 2", false, 22)] static void Abrir2() => Abrir(NIVEL2);
    [MenuItem("Taller4/Abrir escena/Nivel 3", false, 23)] static void Abrir3() => Abrir(NIVEL3);
    static void Abrir(string p) { EditorSceneManager.SaveOpenScenes(); EditorSceneManager.OpenScene(p, OpenSceneMode.Single); }

    [MenuItem("Taller4/Borrar progreso y opciones guardadas", false, 40)]
    static void BorrarPrefs() { PlayerPrefs.DeleteAll(); GameSettings.Load(); Debug.Log("Taller4: PlayerPrefs borrados."); }

    // ================================================================ Recursos
    static void ImportarRecursos()
    {
        AssetDatabase.Refresh();
        foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { MUSICA }))
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g));
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;          // pistas largas: se leen del disco
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.5f;
            s.preloadAudioData = false;
            imp.defaultSampleSettings = s;
            imp.loadInBackground = true;
            imp.SaveAndReimport();
        }
        foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { SFX }))
        {
            var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g));
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;   // efectos cortos: listos en memoria
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }
        Recortar(VAL_INT, "Valentina_Interactuar");
        Recortar(VAL_EXITO, "Valentina_Exito");
        Recortar(VAL_FALLO, "Valentina_Fallo");
        foreach (var p in new[] { IC_OK, IC_ERR, IC_PISTA, IC_RELOJ, CORAZON, CORAZON_V }) Simple(p, 16);
        Simple(BLANCO, 4);
        Simple(PARTICULA, 8);
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

    static void Simple(string path, float ppu)
    {
        var imp = Base(path, ppu);
        imp.spriteImportMode = SpriteImportMode.Single;
        var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Center; s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    // Igual que Sprite Editor > Slice > Grid By Cell Size (32x48, pivote abajo al centro)
    static void Recortar(string path, string prefix)
    {
        var imp = Base(path, 32);
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.SaveAndReimport();
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        var f = new SpriteDataProviderFactories(); f.Init();
        var dp = f.GetSpriteEditorDataProviderFromObject(imp); dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < w / 32; i++)
            rects.Add(new SpriteRect { name = prefix + "_" + i, rect = new Rect(i * 32, 0, 32, 48), alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0f), spriteID = GUID.Generate() });
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static Sprite[] Sprites(string path, string prefix) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(s => s.name.StartsWith(prefix))
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();

    static T Load<T>(string p) where T : Object => AssetDatabase.LoadAssetAtPath<T>(p);

    static void Carpeta(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Carpeta(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // ================================================================ AudioMixer (grupos Musica y SFX)
    // Unity no trae una API pública para crear mezcladores, así que se usa la misma función interna
    // que usa el menú Create > Audio Mixer. Si algo falla, el AudioManager controla el volumen directo.
    static AudioMixer CrearMezclador()
    {
        var existing = Load<AudioMixer>(MIXER);
        if (existing != null && existing.FindMatchingGroups("Musica").Length > 0) return existing;
        try
        {
            var asm = typeof(Editor).Assembly;
            var ctrlType = asm.GetType("UnityEditor.Audio.AudioMixerController");
            var groupType = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var pathType = asm.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            if (existing != null) AssetDatabase.DeleteAsset(MIXER);
            var ctrl = ctrlType.GetMethod("CreateMixerControllerAtPath", all).Invoke(null, new object[] { MIXER });
            var master = ctrlType.GetProperty("masterGroup", all).GetValue(ctrl);
            var createGroup = ctrlType.GetMethod("CreateNewGroup", all);
            var addChild = ctrlType.GetMethod("AddChildToParent", all);
            var addExposed = ctrlType.GetMethods(all).First(m => m.Name == "AddExposedParameter" && m.GetParameters().Length == 1);
            var nombres = new List<string>();
            foreach (var (grupo, param) in new[] { ("Musica", "MusicVol"), ("SFX", "SfxVol") })
            {
                var g = createGroup.Invoke(ctrl, new object[] { grupo, false });
                addChild.Invoke(ctrl, new object[] { g, master });
                try { ctrlType.GetMethod("AddGroupToCurrentView", all)?.Invoke(ctrl, new object[] { g }); } catch { /* opcional: solo afecta la vista */ }
                var guid = (GUID)groupType.GetMethod("GetGUIDForVolume", all).Invoke(g, null);
                var p = System.Activator.CreateInstance(pathType, new object[] { g, guid });
                addExposed.Invoke(ctrl, new object[] { p });
                nombres.Add(param);
            }
            // renombra los parámetros expuestos ("MyExposedParam" -> MusicVol / SfxVol)
            var expProp = ctrlType.GetProperty("exposedParameters", all);
            var arr = (System.Array)expProp.GetValue(ctrl);
            var elemType = arr.GetType().GetElementType();
            var nameField = elemType.GetField("name", all);
            for (int i = 0; i < arr.Length && i < nombres.Count; i++)
            {
                var e = arr.GetValue(i); nameField.SetValue(e, nombres[i]); arr.SetValue(e, i);
            }
            expProp.SetValue(ctrl, arr);
            try { ctrlType.GetMethod("OnSubAssetChanged", all)?.Invoke(ctrl, null); } catch { }
            EditorUtility.SetDirty((Object)ctrl);
            AssetDatabase.SaveAssets();
            var mixer = Load<AudioMixer>(MIXER);
            Debug.Log("Taller4: AudioMixer creado con grupos Musica y SFX (parámetros MusicVol y SfxVol).");
            return mixer;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Taller4: no se pudo crear el AudioMixer automáticamente, se usa volumen directo. " + e.Message);
            return null;
        }
    }

    static void CrearAudioManager(AudioMixer mixer)
    {
        Carpeta("Assets/Resources");
        var go = new GameObject("AudioManager");
        var music = new GameObject("Musica").AddComponent<AudioSource>(); music.transform.SetParent(go.transform);
        var sfx = new GameObject("Efectos").AddComponent<AudioSource>(); sfx.transform.SetParent(go.transform);
        music.playOnAwake = false; music.loop = true; sfx.playOnAwake = false;
        if (mixer != null)
        {
            var gm = mixer.FindMatchingGroups("Musica"); var gs = mixer.FindMatchingGroups("SFX");
            if (gm.Length > 0) music.outputAudioMixerGroup = gm[0];
            if (gs.Length > 0) sfx.outputAudioMixerGroup = gs[0];
        }
        var am = go.AddComponent<AudioManager>();
        var so = new SerializedObject(am);
        so.FindProperty("musicSource").objectReferenceValue = music;
        so.FindProperty("sfxSource").objectReferenceValue = sfx;
        so.FindProperty("mixer").objectReferenceValue = mixer;
        void Asignar(string prop, string path) => so.FindProperty(prop).objectReferenceValue = Load<AudioClip>(path);
        Asignar("menuMusic", MUSICA + "/Musica_Menu.ogg");
        Asignar("levelMusic", MUSICA + "/Musica_Nivel.ogg");
        Asignar("tenseMusic", MUSICA + "/Musica_RetoFinal.ogg");
        Asignar("creditsMusic", MUSICA + "/Musica_Creditos.ogg");
        Asignar("clickClip", SFX + "/SFX_Click.wav");
        Asignar("collectClip", SFX + "/SFX_Moneda.wav");
        Asignar("correctClip", SFX + "/SFX_Acierto.wav");
        Asignar("wrongClip", SFX + "/SFX_Error.wav");
        Asignar("dialogueClip", SFX + "/SFX_Dialogo.wav");
        Asignar("panelClip", SFX + "/SFX_Panel.wav");
        Asignar("hintClip", SFX + "/SFX_Pista.wav");
        Asignar("levelClip", SFX + "/SFX_NivelCompletado.wav");
        Asignar("victoryClip", SFX + "/SFX_Victoria.wav");
        Asignar("defeatClip", SFX + "/SFX_Derrota.wav");
        Asignar("stepClip", SFX + "/SFX_Paso.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(go, AUDIO_PREFAB);
        Object.DestroyImmediate(go);
    }

    // ================================================================ LevelConfig (ScriptableObjects)
    static LevelConfig Config(int n) => Load<LevelConfig>($"{CONFIGS}/Nivel{n}_Config.asset");

    static void CrearConfigs()
    {
        Carpeta(CONFIGS);
        for (int n = 1; n <= 3; n++)
        {
            string p = $"{CONFIGS}/Nivel{n}_Config.asset";
            var c = Load<LevelConfig>(p);
            if (c == null) { c = ScriptableObject.CreateInstance<LevelConfig>(); AssetDatabase.CreateAsset(c, p); }
            c.levelNumber = n;
            switch (n)
            {
                case 1:
                    c.chapterTitle = "Capítulo 1: El primer sueldo";
                    c.pedagogicalRole = "Tutorial guiado: el jugador aprende los controles y la regla 50/30/20.";
                    c.nextScene = "Nivel2"; c.music = MusicTrack.Nivel;
                    c.coinsRequired = 5; c.requireFinalTalk = true;
                    c.timeLimit = 0; c.lives = 0; c.hintsAvailable = -1; c.hintCost = 0; c.wrongAnswerPenalty = 0;
                    c.showInfoBeforeQuestion = true; c.showTutorial = true;
                    break;
                case 2:
                    c.chapterTitle = "Capítulo 2: La quincena";
                    c.pedagogicalRole = "Aplicación con retos intermedios: decisiones con consecuencia (vidas) y tiempo holgado.";
                    c.nextScene = "Nivel3"; c.music = MusicTrack.Nivel;
                    c.coinsRequired = 6; c.requireFinalTalk = true;
                    c.timeLimit = 240; c.lives = 3; c.hintsAvailable = 3; c.hintCost = 25; c.wrongAnswerPenalty = 50;
                    c.showInfoBeforeQuestion = true; c.showTutorial = false;
                    break;
                default:
                    c.chapterTitle = "Capítulo 3: Fin de mes (reto final)";
                    c.pedagogicalRole = "Integración y evaluación: tiempo ajustado, 1 pista, reto final de 3 preguntas y puntaje final.";
                    c.nextScene = ""; c.music = MusicTrack.RetoFinal;
                    c.coinsRequired = 7; c.requireFinalTalk = true;
                    c.timeLimit = 150; c.lives = 3; c.hintsAvailable = 1; c.hintCost = 50; c.wrongAnswerPenalty = 75;
                    c.showInfoBeforeQuestion = false; c.showTutorial = false;
                    break;
            }
            c.pointsPerCoin = 100; c.pointsCorrectFirstTry = 200; c.pointsCorrectRetry = 100; c.pointsPerSecondLeft = 2;
            EditorUtility.SetDirty(c);
        }
        AssetDatabase.SaveAssets();
    }

    // ================================================================ Animator: interactuar, éxito y fallo
    static void ActualizarAnimator()
    {
        var clipInt = Clip("Assets/Animations/Player/Player_Interactuar.anim", Sprites(VAL_INT, "Valentina_Interactuar"), 12, true);
        var clipOk = Clip("Assets/Animations/Player/Player_Exito.anim", Sprites(VAL_EXITO, "Valentina_Exito"), 9, false);
        var clipFail = Clip("Assets/Animations/Player/Player_Fallo.anim", Sprites(VAL_FALLO, "Valentina_Fallo"), 5, true);

        var ctrl = Load<AnimatorController>(CONTROLLER);
        var keep = new HashSet<string> { "Speed" };
        for (int i = ctrl.parameters.Length - 1; i >= 0; i--) if (!keep.Contains(ctrl.parameters[i].name)) ctrl.RemoveParameter(i);
        ctrl.AddParameter(new AnimatorControllerParameter { name = "WalkSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        foreach (var t in new[] { "Interactuar", "Exito", "Fallo" }) ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        foreach (var cs in sm.states.ToArray())
            if (cs.state.name == "Player_Interactuar" || cs.state.name == "Player_Exito" || cs.state.name == "Player_Fallo") sm.RemoveState(cs.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        var idle = sm.states.First(s => s.state.name == "Player_Idle").state;
        var walk = sm.states.First(s => s.state.name == "Player_Walk").state;
        walk.speedParameterActive = true; walk.speedParameter = "WalkSpeed";   // al correr la animación va más rápido

        void Estado(string name, AnimationClip clip, string trigger, Vector3 pos)
        {
            var st = sm.AddState(name, pos);
            st.motion = clip;
            var t = sm.AddAnyStateTransition(st);
            t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            var back = st.AddTransition(idle);
            back.hasExitTime = true; back.exitTime = 1f; back.hasFixedDuration = true; back.duration = 0f;
        }
        Estado("Player_Interactuar", clipInt, "Interactuar", new Vector3(520, 40, 0));
        Estado("Player_Exito", clipOk, "Exito", new Vector3(520, 120, 0));
        Estado("Player_Fallo", clipFail, "Fallo", new Vector3(520, 200, 0));
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
    }

    static AnimationClip Clip(string path, Sprite[] frames, float fps, bool holdLast)
    {
        var clip = Load<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves();
        clip.frameRate = fps;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        int extra = holdLast ? 3 : 1;      // los últimos cuadros se sostienen un momento
        var keys = new ObjectReferenceKeyframe[frames.Length + extra];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        for (int k = 0; k < extra; k++) keys[frames.Length + k] = new ObjectReferenceKeyframe { time = (frames.Length + k) / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ================================================================ Partículas (prefabs)
    static void CrearEfectos()
    {
        Carpeta(EFECTOS);
        Carpeta("Assets/Materials");
        const string matPath = "Assets/Materials/Particulas.mat";
        var mat = Load<Material>(matPath);
        if (mat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.mainTexture = Load<Texture2D>(PARTICULA);
        EditorUtility.SetDirty(mat);

        Particulas("Particulas_Moneda", mat, 14, 0.55f, 3.2f, 0.12f, 0.22f, 0.8f,
            new[] { new Color32(255, 220, 90, 255), new Color32(255, 245, 190, 255) });
        Particulas("Particulas_Acierto", mat, 18, 0.8f, 2.6f, 0.12f, 0.24f, -0.15f,
            new[] { new Color32(140, 210, 110, 255), new Color32(242, 230, 208, 255) });
        Particulas("Confeti", mat, 60, 2.2f, 6f, 0.14f, 0.26f, 0.6f,
            new[] { new Color32(196, 98, 58, 255), new Color32(232, 190, 72, 255), new Color32(110, 150, 90, 255), new Color32(242, 230, 208, 255) });
        AssetDatabase.SaveAssets();
    }

    static void Particulas(string name, Material mat, int count, float life, float speed, float sMin, float sMax, float gravity, Color32[] colors)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.5f; main.loop = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(sMin, sMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count + 10;                 // límite para cuidar el rendimiento
        var grad = new ParticleSystem.MinMaxGradient(colors[0], colors[colors.Length - 1]) { mode = ParticleSystemGradientMode.TwoColors };
        main.startColor = grad;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.sortingLayerName = "Characters"; r.sortingOrder = 20;
        PrefabUtility.SaveAsPrefabAsset(go, $"{EFECTOS}/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    // ================================================================ Niveles
    static readonly string[] T4Raiz = { "T4_Sistemas", "T4_Luces", "EventSystem", "T4_Efectos" };

    static void ConstruirNivel(int n)
    {
        Taller3Builder.Montar();                       // base del Previo 2 (NPC, objetos, monedas, HUD)
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var r in T4Raiz) { var o = GameObject.Find(r); if (o != null) Object.DestroyImmediate(o); }
        var hud = GameObject.Find("HUD");
        foreach (Transform t in hud.transform.Cast<Transform>().ToList()) if (t.name.StartsWith("T4_")) Object.DestroyImmediate(t.gameObject);
        var config = Config(n);
        var player = GameObject.Find("Player");
        var npc = GameObject.Find("NPC_DonRafael");
        var lpj = player.transform.Find("T4_LuzJugador"); if (lpj != null) Object.DestroyImmediate(lpj.gameObject);

        // ---------- EventSystem (para los botones de pausa, preguntas y opciones)
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // ---------- HUD: panel de misión más alto con objetivos en lista y barra de progreso
        var mision = hud.transform.Find("T3_PanelMision").GetComponent<Image>();
        Anclar(mision.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(980, 236));
        var capitulo = mision.transform.Find("Capitulo").GetComponent<TextMeshProUGUI>();
        capitulo.text = config.chapterTitle;
        var objetivo = mision.transform.Find("ObjetivoTexto").GetComponent<TextMeshProUGUI>();
        objetivo.fontSize = 28; objetivo.color = Oscuro; objetivo.fontStyle = FontStyles.Normal;
        objetivo.textWrappingMode = TextWrappingModes.Normal; objetivo.lineSpacing = 4;
        Anclar(objetivo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -74), new Vector2(940, 112));
        var barraBg = Panel("T4_BarraFondo", mision.transform, new Color32(42, 30, 26, 60));
        Anclar(barraBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 18), new Vector2(820, 22));
        var barra = UI<Image>("Relleno", barraBg.transform);
        barra.sprite = Load<Sprite>(BLANCO); barra.type = Image.Type.Filled; barra.fillMethod = Image.FillMethod.Horizontal;
        barra.fillAmount = 0f; barra.color = Salvia; barra.raycastTarget = false; Stretch(barra.rectTransform);
        var pct = Texto("T4_Porcentaje", mision.transform, "0%", 24, Oscuro, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        Anclar(pct.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 12), new Vector2(110, 34));

        // ---------- HUD: inventario con 7 espacios (cada nivel muestra los que necesita)
        var ahorro = hud.transform.Find("T3_PanelAhorro");
        for (int i = 1; i <= 7; i++) { var s = ahorro.Find("Slot_" + i); if (s != null) Object.DestroyImmediate(s.gameObject); }
        var slots = new Image[7];
        for (int i = 0; i < 7; i++)
        {
            slots[i] = UI<Image>("Slot_" + (i + 1), ahorro);
            slots[i].sprite = Load<Sprite>(MONEDA_VACIA); slots[i].preserveAspect = true; slots[i].raycastTarget = false;
            Anclar(slots[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26 + i * 68, -84), new Vector2(48, 48));
        }

        // ---------- HUD: vidas, tiempo, puntaje y botón de pista
        var estado = Panel("T4_PanelEstado", hud.transform, new Color(0.95f, 0.9f, 0.82f, 0.88f));
        Anclar(estado.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -198), new Vector2(520, 128));
        var vidas = new GameObject("Vidas", typeof(RectTransform)).GetComponent<RectTransform>();
        vidas.SetParent(estado.transform, false);
        Anclar(vidas, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -12), new Vector2(170, 44));
        var hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            hearts[i] = UI<Image>("Corazon_" + (i + 1), vidas);
            hearts[i].sprite = Load<Sprite>(CORAZON); hearts[i].preserveAspect = true; hearts[i].raycastTarget = false;
            Anclar(hearts[i].rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 54, 0), new Vector2(46, 40));
        }
        var reloj = new GameObject("Tiempo", typeof(RectTransform)).GetComponent<RectTransform>();
        reloj.SetParent(estado.transform, false);
        Anclar(reloj, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -12), new Vector2(200, 44));
        var relojIc = UI<Image>("Icono", reloj); relojIc.sprite = Load<Sprite>(IC_RELOJ); relojIc.preserveAspect = true; relojIc.raycastTarget = false;
        Anclar(relojIc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(42, 42));
        var tiempo = Texto("TiempoTexto", reloj, "4:00", 36, Oscuro, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        Anclar(tiempo.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(54, 0), new Vector2(140, 44));
        var puntos = Texto("PuntosTexto", estado.transform, "Puntos: 0", 30, Terracota, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        Anclar(puntos.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 12), new Vector2(240, 48));
        var bPista = Boton("Btn_Pista", estado.transform, "Pista (H)", new Vector2(230, 56), 26);
        Anclar((RectTransform)bPista.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 10), new Vector2(230, 56));
        var pistaIc = UI<Image>("Icono", bPista.transform); pistaIc.sprite = Load<Sprite>(IC_PISTA); pistaIc.preserveAspect = true; pistaIc.raycastTarget = false;
        Anclar(pistaIc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 4), new Vector2(34, 34));
        var pistaLabel = bPista.GetComponentInChildren<TextMeshProUGUI>();
        pistaLabel.rectTransform.offsetMin = new Vector2(40, 10);

        // ---------- Ayuda de controles (abajo)
        var ayuda = hud.transform.Find("Ayuda").GetComponent<Image>();
        Anclar(ayuda.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 24), new Vector2(1380, 52));
        ayuda.GetComponentInChildren<TextMeshProUGUI>().text = "A / D o flechas: caminar  ·  Shift: correr  ·  E: interactuar  ·  1-2-3: responder  ·  H: pista  ·  Esc: pausa";

        hud.transform.Find("T3_InfoPanel/Indicador").GetComponent<TextMeshProUGUI>().text = "Presiona E para continuar";

        // ---------- Panel de misión cumplida (texto por nivel)
        var done = hud.transform.Find("T3_MissionCompletePanel");
        done.Find("Titulo").GetComponent<TextMeshProUGUI>().text = "¡Objetivos cumplidos!";
        done.Find("Mensaje").GetComponent<TextMeshProUGUI>().text = $"Ya tienes las {config.coinsRequired} monedas de ahorro y tus decisiones están listas.\nVuelve con Don Rafael para cerrar el capítulo.";

        // ---------- Retroalimentación (mensaje flotante)
        var toast = UI<CanvasGroup>("T4_Retroalimentacion", hud.transform);
        var toastRt = (RectTransform)toast.transform;
        Anclar(toastRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -345), new Vector2(980, 168), new Vector2(0.5f, 1));
        var toastBg = UI<Image>("Fondo", toast.transform); toastBg.color = new Color32(62, 120, 60, 245); toastBg.raycastTarget = false; Stretch(toastBg.rectTransform);
        var toastIc = UI<Image>("Icono", toast.transform); toastIc.preserveAspect = true; toastIc.raycastTarget = false;
        Anclar(toastIc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(84, 84));
        var toastT = Texto("Titulo", toast.transform, "¡Buena decisión!", 36, Color.white, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Anclar(toastT.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -14), new Vector2(830, 46));
        var toastB = Texto("Mensaje", toast.transform, "...", 26, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        toastB.textWrappingMode = TextWrappingModes.Normal;
        Anclar(toastB.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -60), new Vector2(830, 100));

        // ---------- Burbuja del tutorial (solo nivel 1)
        var tut = UI<CanvasGroup>("T4_Tutorial", hud.transform);
        Anclar((RectTransform)tut.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(1180, 70), new Vector2(0.5f, 0));
        var tutBg = UI<Image>("Fondo", tut.transform); tutBg.color = new Color32(60, 80, 130, 235); tutBg.raycastTarget = false; Stretch(tutBg.rectTransform);
        var tutT = Texto("Texto", tut.transform, "Usa A / D para caminar", 30, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(tutT.rectTransform);
        var tutorial = tut.gameObject.AddComponent<TutorialHints>();
        Set(tutorial, ("bubble", tut), ("bubbleText", tutT));

        // ---------- Panel de preguntas
        var qp = Panel("T4_PanelPregunta", hud.transform, new Color32(242, 230, 208, 250));
        Anclar(qp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1260, 640));
        var qFranja = Panel("Franja", qp.transform, Terracota);
        Anclar(qFranja.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 18), new Vector2(0.5f, 1));
        var qHeader = Texto("Encabezado", qp.transform, "Vitrina", 32, Terracota, FontStyles.Bold, TextAlignmentOptions.Top);
        Anclar(qHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1180, 46), new Vector2(0.5f, 1));
        var qText = Texto("Pregunta", qp.transform, "¿...?", 36, Oscuro, FontStyles.Bold, TextAlignmentOptions.Top);
        qText.textWrappingMode = TextWrappingModes.Normal;
        Anclar(qText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(1160, 130), new Vector2(0.5f, 1));
        var optBtns = new Button[3]; var optTxt = new TextMeshProUGUI[3]; var optIc = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var b = UI<Image>("Opcion_" + (i + 1), qp.transform);
            b.color = Crema;
            Anclar(b.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -236 - i * 108), new Vector2(1160, 94), new Vector2(0.5f, 1));
            var borde = b.gameObject.AddComponent<Outline>(); borde.effectColor = Terracota; borde.effectDistance = new Vector2(3, -3);
            var btn = b.gameObject.AddComponent<Button>();
            var cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color32(255, 225, 190, 255);
            cb.selectedColor = new Color32(255, 225, 190, 255); cb.pressedColor = new Color32(220, 190, 160, 255); cb.disabledColor = Color.white;
            btn.colors = cb;
            var t = Texto("Texto", b.transform, "1. Opción", 30, Oscuro, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            Anclar(t.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(1010, 84));
            var ic = UI<Image>("Icono", b.transform); ic.preserveAspect = true; ic.raycastTarget = false; ic.enabled = false;
            Anclar(ic.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(64, 64));
            optBtns[i] = btn; optTxt[i] = t; optIc[i] = ic;
        }
        var qHint = Texto("Indicador", qp.transform, "Responde con 1, 2 o 3 (o haz clic en la opción)", 24, Terracota, FontStyles.Italic, TextAlignmentOptions.Bottom);
        Anclar(qHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(1000, 34), new Vector2(0.5f, 0));

        // ---------- Pausa y opciones
        var pausa = Overlay("T4_Pausa", hud.transform, new Vector2(760, 700), "PAUSA");
        var bCont = Boton("Btn_Continuar", pausa.transform, "CONTINUAR", new Vector2(560, 96), 40);
        var bRein = Boton("Btn_Reiniciar", pausa.transform, "REINICIAR CAPÍTULO", new Vector2(560, 96), 34);
        var bOpc = Boton("Btn_Opciones", pausa.transform, "OPCIONES", new Vector2(560, 96), 40);
        var bMenu = Boton("Btn_Menu", pausa.transform, "MENÚ PRINCIPAL", new Vector2(560, 96), 36);
        Columna(new[] { bCont, bRein, bOpc, bMenu }, -150, 116);
        var opciones = Opciones("T4_Opciones", hud.transform, out var bVolverOpc);

        // ---------- Victoria y derrota
        var vict = Overlay("T4_Victoria", hud.transform, new Vector2(1140, 700), "¡Capítulo completado!");
        var victTitle = vict.transform.Find("Titulo").GetComponent<TextMeshProUGUI>(); victTitle.color = Dorado;
        var alc = UI<Image>("Alcancia", vict.transform); alc.sprite = Load<Sprite>(ALCANCIA); alc.preserveAspect = true; alc.raycastTarget = false;
        Anclar(alc.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(120, 100), new Vector2(0.5f, 1));
        var victBody = Texto("Resumen", vict.transform, "...", 32, Crema, FontStyles.Normal, TextAlignmentOptions.Top);
        Anclar(victBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(1040, 260), new Vector2(0.5f, 1));
        var bNext = Boton("Btn_Siguiente", vict.transform, "SIGUIENTE CAPÍTULO", new Vector2(480, 96), 34);
        var bCred = Boton("Btn_Creditos", vict.transform, "VER CRÉDITOS", new Vector2(480, 96), 36);
        var bRep = Boton("Btn_Repetir", vict.transform, "REPETIR", new Vector2(250, 96), 34);
        var bMenuV = Boton("Btn_MenuV", vict.transform, "MENÚ", new Vector2(250, 96), 34);
        Anclar((RectTransform)bNext.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-265, 50), new Vector2(480, 96), new Vector2(0.5f, 0));
        Anclar((RectTransform)bCred.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-265, 50), new Vector2(480, 96), new Vector2(0.5f, 0));
        Anclar((RectTransform)bRep.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(140, 50), new Vector2(250, 96), new Vector2(0.5f, 0));
        Anclar((RectTransform)bMenuV.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(410, 50), new Vector2(250, 96), new Vector2(0.5f, 0));

        var derr = Overlay("T4_Derrota", hud.transform, new Vector2(1040, 560), "Se acabó el mes...");
        derr.transform.Find("Titulo").GetComponent<TextMeshProUGUI>().color = TerracotaClaro;
        var derrBody = Texto("Motivo", derr.transform, "...", 32, Crema, FontStyles.Normal, TextAlignmentOptions.Top);
        derrBody.textWrappingMode = TextWrappingModes.Normal;
        Anclar(derrBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(920, 220), new Vector2(0.5f, 1));
        var bRetry = Boton("Btn_Reintentar", derr.transform, "REINTENTAR", new Vector2(420, 96), 38);
        var bMenuD = Boton("Btn_MenuD", derr.transform, "MENÚ PRINCIPAL", new Vector2(420, 96), 34);
        Anclar((RectTransform)bRetry.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-225, 50), new Vector2(420, 96), new Vector2(0.5f, 0));
        Anclar((RectTransform)bMenuD.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(225, 50), new Vector2(420, 96), new Vector2(0.5f, 0));

        // ---------- Sistemas del nivel
        var sist = new GameObject("T4_Sistemas");
        var lm = sist.AddComponent<LevelManager>();
        Set(lm, ("config", config), ("victoryPanel", Raiz(vict)), ("victoryTitle", victTitle), ("victoryBody", victBody),
            ("nextButton", bNext.gameObject), ("creditsButton", bCred.gameObject), ("defeatPanel", Raiz(derr)), ("defeatBody", derrBody),
            ("player", player.transform), ("npc", npc.transform));
        var fb = sist.AddComponent<FeedbackUI>();
        Set(fb, ("toast", toast), ("background", toastBg), ("icon", toastIc), ("titleText", toastT), ("bodyText", toastB),
            ("correctIcon", Load<Sprite>(IC_OK)), ("wrongIcon", Load<Sprite>(IC_ERR)), ("hintIcon", Load<Sprite>(IC_PISTA)), ("infoIcon", Load<Sprite>(ALCANCIA)));
        var hs = sist.AddComponent<HintSystem>();
        Set(hs, ("hintCounterText", pistaLabel), ("coinsRoot", GameObject.Find("Coleccionables").transform));
        var pm = sist.AddComponent<PauseMenu>();
        Set(pm, ("pausePanel", Raiz(pausa)), ("optionsPanel", opciones.gameObject), ("firstButton", bCont.gameObject));
        var qpanel = sist.AddComponent<QuestionPanel>();
        Set(qpanel, ("panel", qp.gameObject), ("headerText", qHeader), ("questionText", qText),
            ("correctIcon", Load<Sprite>(IC_OK)), ("wrongIcon", Load<Sprite>(IC_ERR)));
        SetArray(qpanel, "optionButtons", optBtns); SetArray(qpanel, "optionTexts", optTxt); SetArray(qpanel, "optionIcons", optIc);
        var pool = sist.AddComponent<EffectPool>();
        Set(pool, ("coinBurstPrefab", Load<ParticleSystem>(EFECTOS + "/Particulas_Moneda.prefab")),
            ("correctBurstPrefab", Load<ParticleSystem>(EFECTOS + "/Particulas_Acierto.prefab")),
            ("confettiPrefab", Load<ParticleSystem>(EFECTOS + "/Confeti.prefab")));

        // botones
        for (int i = 0; i < 3; i++) UnityEventTools.AddIntPersistentListener(optBtns[i].onClick, qpanel.Choose, i);
        Click(bPista, hs.RequestHint);
        Click(bCont, pm.Resume); Click(bRein, pm.RestartLevel); Click(bOpc, pm.OpenOptions); Click(bMenu, pm.BackToMenu);
        Click(bVolverOpc, pm.CloseOptions);
        Click(bNext, lm.LoadNext); Click(bCred, lm.GoToCredits); Click(bRep, lm.Retry); Click(bMenuV, lm.GoToMenu);
        Click(bRetry, lm.Retry); Click(bMenuD, lm.GoToMenu);

        // ---------- HUD manager: referencias nuevas
        var hudMgr = Object.FindFirstObjectByType<GameHUDManager>();
        Set(hudMgr, ("chapterText", capitulo), ("progressFill", barra), ("progressText", pct), ("livesRoot", vidas.gameObject),
            ("heartFull", Load<Sprite>(CORAZON)), ("heartEmpty", Load<Sprite>(CORAZON_V)), ("timerRoot", reloj.gameObject),
            ("timerText", tiempo), ("scoreText", puntos), ("emptySlotSprite", Load<Sprite>(MONEDA_VACIA)), ("filledSlotSprite", Load<Sprite>(MONEDA_ICONO)));
        SetArray(hudMgr, "inventorySlots", slots); SetArray(hudMgr, "hearts", hearts);
        var hso = new SerializedObject(hudMgr); hso.FindProperty("missionPanelSeconds").floatValue = 5f; hso.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Contenido del nivel: diálogos, preguntas, monedas e iluminación
        ContenidoNivel(n, npc);
        Monedas(n);
        Iluminacion(n, player);

        // ---------- Accesibilidad y animaciones de UI en todos los paneles
        Accesibilidad(hud);
        foreach (var nombre in new[] { "T3_DialoguePanel", "T3_InfoPanel", "T3_MissionCompletePanel", "T4_PanelPregunta", "T4_Pausa", "T4_Opciones", "T4_Victoria", "T4_Derrota" })
        {
            var go = hud.transform.Find(nombre).gameObject;
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            if (go.GetComponent<UIPop>() == null) go.AddComponent<UIPop>();
        }
        foreach (var b in hud.GetComponentsInChildren<Button>(true))
            if (!b.name.StartsWith("Opcion_") && b.GetComponent<UIButtonSound>() == null) b.gameObject.AddComponent<UIButtonSound>();

        // estados iniciales
        qp.gameObject.SetActive(false); Raiz(pausa).SetActive(false); opciones.gameObject.SetActive(false);
        Raiz(vict).SetActive(false); Raiz(derr).SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        if (n == 1) EditorSceneManager.SaveScene(scene);
        else EditorSceneManager.SaveScene(scene, n == 2 ? NIVEL2 : NIVEL3);
        Debug.Log($"Taller4: nivel {n} construido ({config.chapterTitle}).");
    }

    // ---------------------------------------------------------------- textos de cada nivel
    static void ContenidoNivel(int n, GameObject npc)
    {
        Color cR = Crema, cV = Dorado;
        var dso = new SerializedObject(npc.GetComponent<NPCDialogue>());
        var vit = GameObject.Find("Obj_Vitrina").GetComponent<InteractableObject>();
        var ban = GameObject.Find("Obj_CartelBanco").GetComponent<InteractableObject>();
        if (n == 1)
        {
            Lines(dso, "conversation", ("Don Rafael", cR, "¡Buenas, Valentina! Bienvenida al edificio. Recuerde que el arriendo se paga el primer día de cada mes."),
                ("Valentina", cV, "Sí, don Rafael. Hoy me pagaron mi primer sueldo... ¡y ya me lo quiero gastar todo!"),
                ("Don Rafael", cR, "Cuidado con eso, mija. Use la regla 50/30/20: 50% para necesidades, 30% para gustos y 20% para ahorrar."),
                ("Valentina", cV, "¿Y ese 20% para qué me sirve?"),
                ("Don Rafael", cR, "Para su fondo de emergencia. Junte 5 monedas de ahorro por el barrio y pase por la tienda y el banco: allá la van a poner a prueba."),
                ("Valentina", cV, "¡Entendido! Voy por esas monedas."));
            Lines(dso, "reminderConversation", ("Don Rafael", cR, "¿Cómo va? Todavía le falta algo: mire su objetivo arriba a la izquierda. Si se pierde, presione H para una pista."));
            Lines(dso, "missionCompleteConversation", ("Don Rafael", cR, "¡Lo logró! Con ese fondo de emergencia ya está preparada para cualquier imprevisto."),
                ("Valentina", cV, "Y sin endeudarme con la tarjeta. ¡Mi primer mes va muy bien!"),
                ("Don Rafael", cR, "Así es: primero se ahorra y después se gasta. Ahora viene la quincena... ahí sí toca apretarse."));
            Objeto(vit, "la vitrina de la tienda", "Vitrina de la tienda: ¿necesidad o deseo?",
                "Las etiquetas ROJAS son deseos (tenis de marca, celular nuevo) y las VERDES son necesidades (pan, leche).\n\nAntes de comprar pregúntate: ¿lo necesito o solo lo quiero? Primero cubre tus necesidades y deja los gustos para tu 30%.",
                Q("Te quedan $60.000 para esta semana. ¿Qué compras primero?", 0,
                    "El mercado es una necesidad: va primero, dentro de tu 50%.",
                    "Eso es un deseo (etiqueta roja). Primero las necesidades; los gustos salen de tu 30%.",
                    "El mercado: arroz, huevos y leche", "Unos audífonos que están en oferta", "Una pizza para celebrar el sueldo"));
            Objeto(ban, "el cartel del banco", "Cartel del banco: la tarjeta de crédito",
                "La tarjeta de crédito NO es dinero extra: es una deuda. Si solo pagas la cuota mínima, los intereses hacen que termines pagando mucho más.\n\nÚsala solo si puedes pagar el total cada mes.",
                Q("El banco te ofrece una tarjeta «sin cuota de manejo». ¿Qué significa usarla?", 1,
                    "Exacto: la tarjeta es un préstamo. Úsala solo si puedes pagar el total cada mes.",
                    "La tarjeta no es plata tuya: es una deuda. Si no pagas el total, los intereses la hacen crecer.",
                    "Que es plata extra que me regala el banco", "Que es una deuda: lo que gaste lo debo pagar, con intereses si me atraso", "Que es parte de mi ahorro"));
        }
        else if (n == 2)
        {
            Lines(dso, "conversation", ("Don Rafael", cR, "Valentina, ya pagó el arriendo, muy bien. Pero ahora le quedan 15 días con poquita plata."),
                ("Valentina", cV, "Sí... y Camila me está invitando a un concierto este fin de semana."),
                ("Don Rafael", cR, "Piénselo bien. Junte 6 monedas para el ahorro y decida bien en la tienda y en el banco. Tiene 4 minutos y 3 vidas: cada mala decisión le cuesta una."),
                ("Valentina", cV, "¡Listo! Esta quincena no me coge desprevenida."));
            Lines(dso, "reminderConversation", ("Don Rafael", cR, "Todavía le falta: revise su objetivo. Tranquila, el reloj se detiene mientras lee o habla."));
            Lines(dso, "missionCompleteConversation", ("Don Rafael", cR, "¡Bien administrada esa quincena! Ahorró y no se endeudó."),
                ("Valentina", cV, "Y hasta le propuse a Camila un plan más barato. ¡Nos fue genial!"),
                ("Don Rafael", cR, "Eso es saber decir que no a tiempo. Pero ojo: se viene fin de mes..."));
            Objeto(vit, "la vitrina de la tienda", "Vitrina de la tienda: el plan con Camila",
                "Camila, tu amiga, siempre tiene un plan: conciertos, ropa, comidas afuera. Los planes con amigos están bien, pero salen de tu 30% para gustos, no de la plata del mercado ni del arriendo.",
                Q("Camila te invita a un concierto de $180.000 y te quedan $250.000 para 15 días. ¿Qué haces?", 1,
                    "Bien: cuidas tus necesidades y no pierdes a tu amiga. Un plan barato también es plan.",
                    "Así te quedarías sin plata para lo básico o con una deuda. Primero las necesidades.",
                    "Voy: después miro cómo pago el mercado", "Le digo que esta vez no y le propongo un plan más barato", "Le pido prestado a alguien para poder ir"));
            Objeto(ban, "el cartel del banco", "Cartel del banco: el préstamo «fácil»",
                "Los créditos rápidos por celular parecen fáciles, pero muchos cobran intereses muy altos. Antes de pedir un préstamo revisa siempre la tasa de interés y cuánto vas a pagar en total.",
                Q("Te llega un mensaje: «Préstamo pre-aprobado en 5 minutos, sin papeles». ¿Qué revisas primero?", 0,
                    "Correcto: la tasa y el costo total dicen cuánto te cuesta de verdad ese préstamo.",
                    "Lo rápido no es lo importante: un préstamo con intereses altos te puede endeudar por meses.",
                    "La tasa de interés y el costo total del préstamo", "Que la plata me llegue rápido", "Nada: si es fácil, seguro es bueno"));
        }
        else
        {
            Lines(dso, "conversation", ("Don Rafael", cR, "Llegó fin de mes, Valentina. Mañana se paga el arriendo y es hora de demostrar lo que aprendió."),
                ("Valentina", cV, "Estoy lista, don Rafael. ¿Qué tengo que hacer?"),
                ("Don Rafael", cR, "Junte 7 monedas, decida bien en la tienda y supere el reto del banco: son 3 preguntas. Tiene 2 minutos y medio y solo 1 pista."),
                ("Valentina", cV, "Primero se ahorra y después se gasta. ¡Vamos!"));
            Lines(dso, "reminderConversation", ("Don Rafael", cR, "¡Rápido, que se acaba el mes! Mire su objetivo arriba a la izquierda."));
            Lines(dso, "missionCompleteConversation", ("Don Rafael", cR, "¡Lo logró, Valentina! Sobrevivió a su primer mes de independencia."),
                ("Valentina", cV, "Ahorré, pagué el arriendo y no caí en deudas. ¡Me siento orgullosa!"),
                ("Don Rafael", cR, "Y eso le va a servir toda la vida: presupuesto, ahorro y cero deudas innecesarias."));
            Objeto(vit, "la vitrina de la tienda", "Vitrina de la tienda: la bonificación",
                "Cuando recibes un ingreso extra también aplica la regla 50/30/20: no es plata para gastarse toda de una.",
                Q("El Sr. Torres, tu jefe, te da una bonificación de $300.000. ¿Cómo la repartes con la regla 50/30/20?", 0,
                    "Exacto: 50% = $150.000, 30% = $90.000 y 20% = $60.000.",
                    "La regla es 50% necesidades, 30% gustos y 20% ahorro: $150.000, $90.000 y $60.000.",
                    "$150.000 necesidades, $90.000 gustos y $60.000 ahorro", "Todo en gustos: es un premio", "$100.000 para cada cosa"));
            Objeto(ban, "el banco (reto final)", "Reto final del banco",
                "El banco quiere saber si ya estás lista para manejar tu plata sola. Responde las 3 preguntas.",
                Q("Tienes una deuda de $400.000 en la tarjeta y te cobran intereses. ¿Qué te conviene?", 1,
                    "Pagar el total corta los intereses. La cuota mínima alarga la deuda.",
                    "Con la cuota mínima o con otra tarjeta la deuda crece por los intereses.",
                    "Pagar solo la cuota mínima", "Pagar el total o lo máximo que pueda", "Sacar otra tarjeta para pagar esta"),
                Q("¿Para qué sirve el fondo de emergencia?", 1,
                    "Así es: es tu colchón para lo inesperado.",
                    "El fondo de emergencia no es para gustos: es solo para imprevistos.",
                    "Para aprovechar descuentos", "Para imprevistos: una enfermedad, un daño o quedarse sin trabajo", "Para salir los fines de semana"),
                Q("Si tu sueldo es $1.800.000, ¿cuánto deberías ahorrar al mes con la regla 50/30/20?", 1,
                    "Correcto: el 20% de $1.800.000 es $360.000.",
                    "El 20% de $1.800.000 es $360.000 (el 50% serían $900.000 y el 10%, $180.000).",
                    "$180.000", "$360.000", "$900.000"));
        }
        dso.ApplyModifiedPropertiesWithoutUndo();
    }

    static DecisionQuestion Q(string q, int correct, string why, string whyNot, params string[] opts) =>
        new DecisionQuestion { question = q, options = opts, correctIndex = correct, whyCorrect = why, whyWrong = whyNot };

    static void Objeto(InteractableObject io, string place, string title, string info, params DecisionQuestion[] qs)
    {
        // chulo verde sobre el objeto cuando la decisión queda resuelta
        var old = io.transform.Find("T4_Resuelto"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var mark = new GameObject("T4_Resuelto");
        mark.transform.SetParent(io.transform, false);
        mark.transform.localPosition = new Vector3(1.3f, 2.1f, 0);
        var sr = mark.AddComponent<SpriteRenderer>(); sr.sprite = Load<Sprite>(IC_OK); sr.sortingLayerName = "Characters"; sr.sortingOrder = 6;
        mark.transform.localScale = Vector3.one * 1.2f;
        mark.SetActive(false);

        var so = new SerializedObject(io);
        so.FindProperty("title").stringValue = title;
        so.FindProperty("educationalContent").stringValue = info;
        so.FindProperty("placeName").stringValue = place;
        so.FindProperty("solvedMark").objectReferenceValue = mark;
        var arr = so.FindProperty("questions"); arr.arraySize = qs.Length;
        for (int i = 0; i < qs.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("question").stringValue = qs[i].question;
            var o = e.FindPropertyRelative("options"); o.arraySize = qs[i].options.Length;
            for (int k = 0; k < qs[i].options.Length; k++) o.GetArrayElementAtIndex(k).stringValue = qs[i].options[k];
            e.FindPropertyRelative("correctIndex").intValue = qs[i].correctIndex;
            e.FindPropertyRelative("whyCorrect").stringValue = qs[i].whyCorrect;
            e.FindPropertyRelative("whyWrong").stringValue = qs[i].whyWrong;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Lines(SerializedObject so, string prop, params (string who, Color c, string text)[] lines)
    {
        var arr = so.FindProperty(prop); arr.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speakerName").stringValue = lines[i].who;
            e.FindPropertyRelative("nameColor").colorValue = lines[i].c;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
        }
    }

    // ---------------------------------------------------------------- monedas por nivel
    static void Monedas(int n)
    {
        float[] xs = n == 1 ? new[] { 4f, 9f, 20f, 34f, 42f }
                   : n == 2 ? new[] { -9f, 2f, 10f, 19f, 33f, 45f }
                   : new[] { -10f, -1f, 7f, 17f, 31f, 39f, 46f };
        var cont = GameObject.Find("Coleccionables").transform;
        var modelo = cont.GetChild(0).gameObject;
        var actuales = cont.Cast<Transform>().Select(t => t.gameObject).ToList();
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject c = i < actuales.Count ? actuales[i] : Object.Instantiate(modelo, cont);
            c.name = "Moneda_" + (i + 1);
            c.transform.position = new Vector3(xs[i], 0.75f, 0f);
        }
        for (int i = xs.Length; i < actuales.Count; i++) Object.DestroyImmediate(actuales[i]);
    }

    // ---------------------------------------------------------------- luces (tarde y noche)
    static void Iluminacion(int n, GameObject player)
    {
        var global = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
        if (global != null)
        {
            global.color = n == 1 ? Color.white : n == 2 ? new Color(1f, 0.82f, 0.74f) : new Color(0.5f, 0.55f, 0.9f);
            global.intensity = n == 1 ? 1f : n == 2 ? 0.9f : 0.42f;
            EditorUtility.SetDirty(global);
        }
        if (n < 3) return;
        // nivel 3 (noche): los postes del fondo se encienden y Valentina lleva una luz suave
        var luces = new GameObject("T4_Luces");
        foreach (float x in new[] { -3.4f, 12.1f, 23.8f, 38.0f })
        {
            var l = new GameObject("Luz_Poste").AddComponent<Light2D>();
            l.transform.SetParent(luces.transform);
            l.transform.position = new Vector3(x, 2.3f, 0);
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.85f, 0.55f); l.intensity = 1.1f;
            l.pointLightInnerRadius = 0.3f; l.pointLightOuterRadius = 4.5f;
        }
        var lj = new GameObject("T4_LuzJugador").AddComponent<Light2D>();
        lj.transform.SetParent(player.transform, false);
        lj.transform.localPosition = new Vector3(0, 0.8f, 0);
        lj.lightType = Light2D.LightType.Point;
        lj.color = new Color(1f, 0.95f, 0.85f); lj.intensity = 0.7f;
        lj.pointLightInnerRadius = 0.5f; lj.pointLightOuterRadius = 3.2f;
    }

    // ---------------------------------------------------------------- accesibilidad
    static readonly HashSet<string> Acentos = new HashSet<string> { "Capitulo", "NPCNameText", "InfoTitulo", "Titulo", "Encabezado", "PuntosTexto" };
    static readonly HashSet<string> Paneles = new HashSet<string> { "T3_PanelMision", "T3_PanelAhorro", "T4_PanelEstado", "Ayuda", "T3_DialoguePanel", "T3_InfoPanel", "T3_MissionCompletePanel", "T4_PanelPregunta" };

    static void Accesibilidad(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.GetComponentInParent<Button>(true) != null) continue;     // los botones conservan su estilo
            if (t.transform.parent != null && t.transform.parent.name == "T4_Retroalimentacion") continue;
            if (t.GetComponentInParent<Toggle>(true) != null || t.GetComponentInParent<Slider>(true) != null) continue;
            if (t.name == "AyudaTexto") continue;                         // la barra de controles conserva su tamaño
            var a = t.GetComponent<AccessibleText>();
            if (a == null) a = t.gameObject.AddComponent<AccessibleText>();
            var so = new SerializedObject(a);
            so.FindProperty("accent").boolValue = Acentos.Contains(t.name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var img in root.GetComponentsInChildren<Image>(true))
            if (Paneles.Contains(img.name) && img.GetComponent<AccessiblePanel>() == null) img.gameObject.AddComponent<AccessiblePanel>();
    }

    // ================================================================ Menú principal
    static void ActualizarMenu()
    {
        var scene = EditorSceneManager.OpenScene(MAIN_MENU, OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        foreach (Transform t in canvas.transform.Cast<Transform>().ToList()) if (t.name.StartsWith("T4_")) Object.DestroyImmediate(t.gameObject);
        var manager = Object.FindFirstObjectByType<MainMenuManager>();
        var panel = canvas.transform.Find("MenuPanel");
        foreach (var n in new[] { "Btn_Instructions", "Btn_Options" }) { var o = panel.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }

        var hl = panel.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 28;
        ((RectTransform)panel).sizeDelta = new Vector2(1800, 130);
        var bIns = Boton("Btn_Instructions", panel, "INSTRUCCIONES", new Vector2(320, 104), 34);
        var bOpt = Boton("Btn_Options", panel, "OPCIONES", new Vector2(320, 104), 38);
        foreach (var n in new[] { "Btn_Play", "Btn_Credits", "Btn_Quit" })
        {
            var b = (RectTransform)panel.Find(n);
            b.sizeDelta = new Vector2(320, 104);
            b.GetComponentInChildren<TextMeshProUGUI>().fontSize = 40;
        }
        panel.Find("Btn_Play").SetSiblingIndex(0); bIns.transform.SetSiblingIndex(1); bOpt.transform.SetSiblingIndex(2);
        panel.Find("Btn_Credits").SetSiblingIndex(3); panel.Find("Btn_Quit").SetSiblingIndex(4);

        // ---------- Instrucciones (controles, objetivo, ayudas y capítulos)
        var ins = UI<Image>("T4_Instrucciones", canvas.transform);
        ins.color = new Color32(28, 20, 17, 255); Stretch(ins.rectTransform);
        var it = Texto("Titulo", ins.transform, "INSTRUCCIONES", 66, TerracotaClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(it.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1600, 90), new Vector2(0.5f, 1));
        var ib = Texto("Cuerpo", ins.transform, INSTRUCCIONES, 29, Crema, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        ib.textWrappingMode = TextWrappingModes.Normal; ib.lineSpacing = 2;
        Anclar(ib.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1560, 740), new Vector2(0.5f, 1));
        ib.gameObject.AddComponent<AccessibleText>();
        var bInsBack = Boton("Btn_CloseInstructions", ins.transform, "VOLVER", new Vector2(340, 95), 46);
        Anclar((RectTransform)bInsBack.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(340, 95), new Vector2(0.5f, 0));

        // ---------- Opciones
        var opt = Opciones("T4_Opciones", canvas.transform, out var bOptBack);
        opt.gameObject.AddComponent<UIPop>();

        // ---------- créditos actualizados (música y efectos propios)
        var credBody = canvas.transform.Find("CreditsPanel/Cuerpo_Creditos").GetComponent<TextMeshProUGUI>();
        credBody.text = CREDITOS; credBody.fontSize = 24;

        // ---------- conexiones
        Click(bIns, manager.OpenInstructions); Click(bInsBack, manager.CloseInstructions);
        Click(bOpt, manager.OpenOptions); Click(bOptBack, manager.CloseOptions);
        Set(manager, ("instructionsPanel", ins.gameObject), ("optionsPanel", opt.gameObject), ("firstButton", panel.Find("Btn_Play").gameObject));
        foreach (var b in canvas.GetComponentsInChildren<Button>(true))
            if (b.GetComponent<UIButtonSound>() == null) b.gameObject.AddComponent<UIButtonSound>();
        foreach (var go in new[] { ins.gameObject, canvas.transform.Find("CreditsPanel").gameObject })
        {
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            if (go.GetComponent<UIPop>() == null) go.AddComponent<UIPop>();
        }
        ins.gameObject.SetActive(false); opt.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ConfigurarBuildSettings()
    {
        EditorBuildSettings.scenes = new[] { MAIN_MENU, GAME_SCENE, NIVEL2, NIVEL3 }.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
    }

    // ================================================================ 2. Optimización
    [MenuItem("Taller4/2 - Optimizar (Sprite Atlas y compresión)", false, 2)]
    public static void Optimizar()
    {
        EditorSceneManager.SaveOpenScenes();
        const string path = "Assets/Sprites/Atlas_ElPrimerCheque.spriteatlasv2";
        if (AssetDatabase.LoadMainAssetAtPath(path) == null)
        {
            var atlas = new SpriteAtlasAsset();
            atlas.Add(new Object[] {
                Load<DefaultAsset>("Assets/Sprites/Personaje"), Load<DefaultAsset>("Assets/Sprites/Objetos"),
                Load<DefaultAsset>("Assets/Sprites/UI"), Load<DefaultAsset>("Assets/Sprites/Efectos") });
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4, enableAlphaDilation = false };
        imp.textureSettings = new SpriteAtlasTextureSettings { filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true, readable = false };
        var plat = imp.GetPlatformSettings("DefaultTexturePlatform");
        plat.maxTextureSize = 2048; plat.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SetPlatformSettings(plat);
        imp.includeInBuild = true;
        imp.SaveAndReimport();
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

        // el fondo del barrio no necesita más resolución de la que tiene
        var fondo = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Entorno/Fondo_Barrio.png");
        fondo.maxTextureSize = 2048; fondo.SaveAndReimport();
        Debug.Log("Taller4: Sprite Atlas creado y empaquetado (personajes, objetos, UI y efectos en una sola textura).");
    }

    // ================================================================ 3. Build
    [MenuItem("Taller4/3 - Exportar build de Windows (.zip)", false, 3)]
    public static void Build()
    {
        EditorSceneManager.SaveOpenScenes();
        ConfigurarBuildSettings();
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string dir = Path.Combine(root, "Builds", "ElPrimerCheque_Windows");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MAIN_MENU, GAME_SCENE, NIVEL2, NIVEL3 },
            locationPathName = Path.Combine(dir, "ElPrimerCheque.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) { Debug.LogError("Taller4: la build falló: " + report.summary.result); return; }
        foreach (var d in Directory.GetDirectories(dir, "*DoNotShip*")) Directory.Delete(d, true);
        string zip = Path.Combine(root, "Builds", "ElPrimerCheque_Windows.zip");
        if (File.Exists(zip)) File.Delete(zip);
        System.IO.Compression.ZipFile.CreateFromDirectory(dir, zip, System.IO.Compression.CompressionLevel.Optimal, true);
        Debug.Log($"Taller4: build lista en {zip} ({new FileInfo(zip).Length / 1048576f:0.0} MB)");
    }

    // ================================================================ helpers de UI
    static void Set(Object target, params (string prop, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (p, v) in pairs)
        {
            var sp = so.FindProperty(p);
            if (sp == null) { Debug.LogError($"Taller4: {target.GetType().Name} no tiene el campo '{p}'"); continue; }
            sp.objectReferenceValue = v;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray<T>(Object target, string prop, T[] values) where T : Object
    {
        var so = new SerializedObject(target);
        var sp = so.FindProperty(prop); sp.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Click(Button b, UnityAction a) => UnityEventTools.AddPersistentListener(b.onClick, a);

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

    static Button Boton(string name, Transform parent, string label, Vector2 size, float font)
    {
        var img = UI<Image>(name, parent);
        img.sprite = Load<Sprite>(BOTON);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.4f;
        img.rectTransform.sizeDelta = size;
        var b = img.gameObject.AddComponent<Button>();
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color32(255, 214, 170, 255);
        cb.selectedColor = new Color32(255, 214, 170, 255);       // foco visible al navegar con teclado
        cb.pressedColor = new Color32(196, 150, 120, 255);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        var t = Texto("Text (TMP)", img.transform, label, font, Oscuro, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(8, 10);
        t.rectTransform.offsetMax = new Vector2(-8, 0);
        t.enableAutoSizing = true;              // textos largos (INSTRUCCIONES) no se salen del botón
        t.fontSizeMax = font; t.fontSizeMin = font * 0.6f;
        return b;
    }

    static void Columna(Button[] bs, float top, float step)
    {
        for (int i = 0; i < bs.Length; i++)
        {
            var rt = (RectTransform)bs[i].transform;
            Anclar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, top - i * step), rt.sizeDelta, new Vector2(0.5f, 1));
        }
    }

    static GameObject Raiz(Image caja) => caja.transform.parent.gameObject;

    // Fondo oscuro a pantalla completa con un panel centrado y su título
    static Image Overlay(string name, Transform parent, Vector2 size, string title)
    {
        var dim = UI<Image>(name, parent);
        dim.color = new Color(0, 0, 0, 0.55f); dim.raycastTarget = true; Stretch(dim.rectTransform);
        var box = Panel("Caja", dim.transform, new Color32(42, 30, 26, 250));
        Anclar(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        box.gameObject.AddComponent<AccessiblePanel>();
        var franja = Panel("Franja", box.transform, Terracota);
        Anclar(franja.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 14), new Vector2(0.5f, 1));
        // los hijos se ponen dentro de la caja para que todo quede centrado
        var t = Texto("Titulo", box.transform, title, 58, Crema, FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(size.x - 60, 80), new Vector2(0.5f, 1));
        return box;   // se devuelve la caja; su padre (fondo oscuro) se activa con ella
    }

    // Panel de opciones: sliders de volumen y casillas de accesibilidad
    static Image Opciones(string name, Transform parent, out Button volver)
    {
        var dim = UI<Image>(name, parent);
        dim.color = new Color(0, 0, 0, 0.6f); dim.raycastTarget = true; Stretch(dim.rectTransform);
        dim.gameObject.AddComponent<CanvasGroup>();
        var box = Panel("Caja", dim.transform, new Color32(42, 30, 26, 252));
        Anclar(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, 800));
        var franja = Panel("Franja", box.transform, Terracota);
        Anclar(franja.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 14), new Vector2(0.5f, 1));
        var t = Texto("Titulo", box.transform, "OPCIONES", 58, Crema, FontStyles.Bold, TextAlignmentOptions.Center);
        Anclar(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1000, 76), new Vector2(0.5f, 1));

        Slider Fila(string label, string id, float y)
        {
            var l = Texto("Etiqueta", box.transform, label, 32, Crema, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Anclar(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, y), new Vector2(420, 50));
            var sGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sGo.name = "Slider_" + id;
            sGo.transform.SetParent(box.transform, false);
            var s = sGo.GetComponent<Slider>();
            Anclar((RectTransform)sGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(500, y - 5), new Vector2(480, 36));
            sGo.transform.Find("Background").GetComponent<Image>().color = new Color32(90, 70, 60, 255);
            sGo.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Terracota;
            var h = sGo.transform.Find("Handle Slide Area/Handle").GetComponent<Image>(); h.color = Crema;
            ((RectTransform)h.transform).sizeDelta = new Vector2(36, 0);
            var cb = s.colors; cb.highlightedColor = Dorado; cb.selectedColor = Dorado; s.colors = cb;
            s.minValue = 0; s.maxValue = 1; s.value = 0.7f;
            return s;
        }
        Toggle Casilla(string label, string id, float y)
        {
            var tg = DefaultControls.CreateToggle(new DefaultControls.Resources());
            tg.name = "Toggle_" + id;
            tg.transform.SetParent(box.transform, false);
            Object.DestroyImmediate(tg.transform.Find("Label").gameObject);   // se cambia el texto por TextMeshPro
            Anclar((RectTransform)tg.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, y), new Vector2(900, 52));
            var bg = (RectTransform)tg.transform.Find("Background");
            Anclar(bg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(46, 46));
            bg.GetComponent<Image>().color = Crema;
            var ck = bg.Find("Checkmark").GetComponent<Image>(); ck.sprite = Load<Sprite>(IC_OK); ck.color = Color.white;
            ck.rectTransform.anchorMin = Vector2.zero; ck.rectTransform.anchorMax = Vector2.one;
            ck.rectTransform.offsetMin = new Vector2(4, 4); ck.rectTransform.offsetMax = new Vector2(-4, -4);
            var l = Texto("Etiqueta", tg.transform, label, 32, Crema, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Anclar(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(820, 52));
            var t2 = tg.GetComponent<Toggle>();
            var cb = t2.colors; cb.highlightedColor = Dorado; cb.selectedColor = Dorado; t2.colors = cb;
            return t2;
        }
        var music = Fila("Volumen de la música", "Musica", -140);
        var sfx = Fila("Volumen de los efectos", "Efectos", -210);
        var mute = Casilla("Silenciar todo el sonido", "Silenciar", -290);
        var large = Casilla("Texto grande en diálogos y carteles", "TextoGrande", -365);
        var contrast = Casilla("Alto contraste (fondos negros, texto blanco)", "AltoContraste", -440);
        var shake = Casilla("Vibración de la cámara en los errores", "Vibracion", -515);
        var nota = Texto("Nota", box.transform, "Los cambios se guardan solos. En el juego también puedes pausar con Esc.", 24, TerracotaClaro, FontStyles.Italic, TextAlignmentOptions.Center);
        Anclar(nota.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(980, 40), new Vector2(0.5f, 0));
        volver = Boton("Btn_VolverOpciones", box.transform, "VOLVER", new Vector2(340, 95), 44);
        Anclar((RectTransform)volver.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(340, 95), new Vector2(0.5f, 0));
        var op = dim.gameObject.AddComponent<OptionsPanel>();
        Set(op, ("musicSlider", music), ("sfxSlider", sfx), ("muteToggle", mute), ("largeTextToggle", large), ("contrastToggle", contrast), ("shakeToggle", shake));
        return dim;
    }

    const string INSTRUCCIONES =
"<b><color=#DE825A>¿DE QUÉ SE TRATA?</color></b>\n" +
"Eres Valentina y acabas de recibir tu primer sueldo. En cada capítulo debes juntar monedas de ahorro y tomar buenas decisiones con tu plata para llegar a fin de mes.\n\n" +
"<b><color=#DE825A>CONTROLES</color></b>\n" +
"A / D o flechas: caminar     ·     Shift: correr\n" +
"E o Espacio: hablar, leer carteles y pasar el diálogo\n" +
"1, 2, 3 o clic: elegir una respuesta     ·     H: pedir una pista     ·     Esc o P: pausa y opciones\n\n" +
"<b><color=#DE825A>AYUDAS</color></b>\n" +
"• Arriba a la izquierda siempre ves tu objetivo y una barra de progreso.\n" +
"• Verde con √ es una buena decisión; rojo con × es una decisión para revisar, y siempre te explicamos por qué.\n" +
"• El reloj se detiene mientras lees un diálogo, un cartel o una pregunta.\n" +
"• En Opciones puedes subir el tamaño del texto, activar alto contraste, bajar la música o quitar la vibración.\n\n" +
"<b><color=#DE825A>CAPÍTULOS</color></b>\n" +
"1. El primer sueldo: tutorial, sin tiempo, sin vidas y con pistas ilimitadas.\n" +
"2. La quincena: 3 vidas, 4 minutos y 3 pistas.\n" +
"3. Fin de mes (reto final): 3 vidas, 2 minutos y medio, 1 pista y un reto de 3 preguntas.";

    const string CREDITOS =
"<b>EL PRIMER CHEQUE</b> — Momento 3 · 2026-2\n\n" +
"<b>1. PROPÓSITO EDUCATIVO</b>\n" +
"- Educación financiera para jóvenes: presupuesto 50/30/20, necesidades vs. deseos, crédito responsable y fondo de emergencia.\n\n" +
"<b>2. EQUIPO DE DESARROLLO</b>\n" +
"- Diseño, programación, arte, audio y documentación: Brayhan Gómez y Camilo Vargas\n" +
"- Docente: Ing. Luz Karime Castellanos Joya · Desarrollo de Contenidos Digitales y Multimediales · UTS\n\n" +
"<b>3. FUENTES Y REFERENCIAS</b>\n" +
"- Banco de la República de Colombia. Educación económica y financiera. banrep.gov.co\n" +
"- Superintendencia Financiera de Colombia. Educación financiera. superfinanciera.gov.co\n" +
"- OCDE (2020). Recommendation of the Council on Financial Literacy.\n\n" +
"<b>4. RECURSOS Y LICENCIAS</b>\n" +
"- Sprites, animaciones e iconos: originales creados por el equipo.\n" +
"- Música (4 pistas) y efectos de sonido (11): composición original del equipo hecha por síntesis (estilo chiptune). Sin recursos de terceros.\n" +
"- Tipografía: Liberation Sans (TextMeshPro), licencia SIL Open Font License.\n" +
"- Motor: Unity 6 (licencia Personal).\n\n" +
"<b>AVISO:</b> proyecto académico sin fines comerciales.";
}
#endif
