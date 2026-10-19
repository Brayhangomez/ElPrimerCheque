# El Primer Cheque – Previo 2 (Unity 2D)

Serious game de educación financiera hecho en **Unity 6 (6000.0.56f1, Universal 2D)** para la materia *Desarrollo de Contenidos Digitales y Multimediales* (UTS), con la Ing. Luz Karime Castellanos Joya.

**Integrantes:** Brayhan Gómez y Camilo Vargas

## De qué trata

Valentina tiene 22 años, se acaba de graduar y recibe su primer sueldo. En este primer capítulo tiene que aprender la regla 50/30/20 y ahorrar 5 monedas para su fondo de emergencia, sin caer en las tentaciones de la tienda ni de la tarjeta de crédito.

## Qué tiene este previo

| Requisito | Cómo quedó en el juego |
|---|---|
| NPC con diálogo (tecla E) | **Don Rafael**, el arrendador. Explica la regla 50/30/20 y da la misión. Al terminarla tiene un diálogo final distinto. |
| 2 objetos interactivos | **Vitrina de la tienda** (necesidades vs. deseos) y **cartel del banco** (la tarjeta de crédito es deuda). Muestran "Presiona E" y un panel con texto educativo. |
| 5 coleccionables | **Monedas de ahorro** repartidas por el barrio. Se recogen una sola vez y desaparecen. |
| HUD | Contador "Ahorro: x / 5", objetivo actual, mini inventario con íconos y panel de **¡Misión cumplida!** |

## Controles

- **A / D o flechas:** caminar
- **E:** hablar / interactuar / pasar el diálogo
- **Esc:** volver al menú

## Scripts (Assets/Scripts)

| Script | Qué hace |
|---|---|
| `PlayerController.cs` | Movimiento con Rigidbody2D, giro del sprite y parámetro `Speed` del Animator. |
| `GameInput.cs` | Lee el teclado en un solo lugar y permite bloquear el movimiento durante diálogos. |
| `NPCDialogue.cs` | Trigger del NPC, aviso de E, diálogo línea por línea con nombre de quien habla y conversación final. |
| `InteractableObject.cs` | Objetos con aviso "Presiona E" y panel de información educativa. |
| `CollectibleItem.cs` | Moneda coleccionable (trigger, se cuenta una sola vez y se destruye). |
| `GameHUDManager.cs` | Contador, objetivo, inventario, panel de misión cumplida y regreso al menú. |
| `MainMenuManager.cs` | Botones Jugar, Créditos y Salir del menú principal. |
| `CameraFollow2D.cs` | Cámara que sigue al jugador con zona muerta y límites del nivel. |
| `SpriteLoop.cs` | Animación simple por cuadros para la moneda y Don Rafael. |
| `Demo/DemoDirector.cs` | Herramienta que usamos para grabar el video demostrativo (solo se activa desde el menú Taller3 del editor). |

## Cómo abrirlo

1. Clonar el repositorio.
2. En Unity Hub: **Add > Add project from disk** y escoger la carpeta.
3. Abrir con Unity **6000.0.56f1** y cargar `Assets/Scenes/MainMenu.unity`.
4. Darle Play.

La build de Windows se entrega aparte en un .zip (abrir `ElPrimerCheque.exe`).
