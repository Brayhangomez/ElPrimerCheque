# El Primer Cheque (Unity 2D)

Serious game de educación financiera hecho en **Unity 6 (6000.0.56f1, Universal 2D)** para la materia *Desarrollo de Contenidos Digitales y Multimediales* de las Unidades Tecnológicas de Santander, con la Ing. Luz Karime Castellanos Joya.

**Integrantes:** Brayhan Gómez y Camilo Vargas

## De qué trata

Valentina tiene 22 años, se acaba de graduar y recibe su primer sueldo. En tres capítulos tiene que aprender la regla 50/30/20, diferenciar necesidades de deseos, usar el crédito con cuidado y armar un fondo de emergencia, tomando decisiones con consecuencias.

| Capítulo | Rol | Dificultad |
|---|---|---|
| 1. El primer sueldo | Tutorial guiado | 5 monedas, 2 preguntas, sin reloj, intentos y pistas ilimitados |
| 2. La quincena | Aplicar lo aprendido | 6 monedas, 2 preguntas, 4:00, 3 vidas, 3 pistas (-25 pts) |
| 3. Fin de mes (reto final) | Integrar y evaluar | 7 monedas, 1 pregunta + reto de 3 preguntas, 2:30, 3 vidas, 1 pista (-50 pts), de noche |

## Momento 3 (Actividades 1 y 2)

- **Audio:** 4 pistas de música (menú, nivel, reto final, créditos) y 11 efectos, todo propio. `AudioManager` persistente con `DontDestroyOnLoad` y `AudioMixer` (grupos Musica y SFX con parámetros expuestos `MusicVol` y `SfxVol`).
- **Bus de eventos:** `GameEvents` (moneda, acierto, error, diálogo, panel, pista, nivel completado, derrota). El audio, las animaciones y las partículas se enganchan a estos eventos sin depender unos de otros.
- **Animaciones:** Valentina camina, corre (Shift), interactúa, celebra y se lamenta (triggers en el Animator). Paneles con `UIPop`, partículas con pool (`EffectPool`), luces 2D de noche en el capítulo 3.
- **Dificultad:** cada capítulo tiene un `LevelConfig` (ScriptableObject) en `Assets/ScriptableObjects/Niveles`.
- **Retroalimentación:** preguntas de decisión con acierto/error (color + ícono + texto + sonido + animación), barra de progreso, vidas, reloj y puntaje.
- **Ayudas:** pistas contextuales (tecla H), tutorial del capítulo 1 y mensajes de error que explican el porqué.
- **Accesibilidad:** texto grande, alto contraste, volumen de música y efectos, silenciar, apagar la vibración de cámara, pausa en cualquier momento y controles por teclado y mouse. Las opciones se guardan con PlayerPrefs.
- **Optimización:** Sprite Atlas (`Assets/Sprites/Atlas_ElPrimerCheque.spriteatlasv2`), música en Streaming y efectos con Decompress On Load. En el capítulo 3 los batches bajaron de 19 a 13.

## Controles

- **A / D o flechas:** caminar · **Shift:** correr
- **E o Espacio:** hablar / interactuar / pasar el diálogo
- **1, 2, 3 o clic:** responder
- **H:** pedir una pista
- **Esc o P:** pausa y opciones

## Estructura de scripts (Assets/Scripts)

| Carpeta | Scripts |
|---|---|
| `Core` | `GameEvents`, `AudioManager`, `GameSettings`, `SceneMusic` |
| `Gameplay` | `LevelConfig`, `LevelManager`, `EffectPool` |
| `UI` | `QuestionPanel`, `FeedbackUI`, `HintSystem`, `TutorialHints`, `PauseMenu`, `OptionsPanel`, `AccessibleText`, `AccessiblePanel`, `UIPop`, `UIButtonSound` |
| raíz | `PlayerController`, `GameInput`, `NPCDialogue`, `InteractableObject`, `CollectibleItem`, `GameHUDManager`, `MainMenuManager`, `CameraFollow2D`, `SpriteLoop` |
| `Herramientas` | `CapturaEvidencia` (F12 guarda una captura en el editor) |

Los menús `Taller4` del editor arman las escenas (`1 - Montar Momento 3`), crean el Sprite Atlas (`2 - Optimizar`) y exportan la build de Windows (`3 - Exportar build`).

## Cómo abrirlo

1. Clonar el repositorio.
2. En Unity Hub: **Add > Add project from disk** y escoger la carpeta.
3. Abrir con Unity **6000.0.56f1** y cargar `Assets/Scenes/MainMenu.unity`.
4. Darle Play.

La build de Windows se entrega aparte en un .zip (abrir `ElPrimerCheque.exe`).

Video del Previo 2: https://youtu.be/9RSO1jVqFNk
