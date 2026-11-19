# ¡Ya Casi!

Videojuego académico de aventura y puzzles **2D con vista cenital**, ambientado en la vida universitaria de la UTNG. El jugador debe recuperar sus materiales y completar entregas antes de que termine el tiempo.

**Asignatura:** Creación de Videojuegos · **Docente:** Gabriel Barrón Rodríguez  
**Equipo:** Agustín López Parra ([agustn134](https://github.com/agustn134)) y Carlos Manuel Palma Muñoz ([elmanee](https://github.com/elmanee)).

## Estado del proyecto

**En desarrollo.** El código integrado revisado está en la rama [`nivel/02-biblioteca`](https://github.com/agustn134/ya-casi/tree/nivel%2F02-biblioteca). Esta descripción no implica que esos cambios ya estén integrados en `main` ni que exista una versión final publicada.

| Nivel | Avance disponible |
|---|---|
| 1 — El caos del casillero | Misterio de la USB, edificio F nocturno, vigilancia, pistas y revisión de casilleros. |
| 2 — Maratón en la Biblioteca | Tres referencias por sectores, HUD, pistas con coste, carros, piso mojado y exterior con pasto y calle. |
| 3 — La Ruta de Entrega | Cinco proyectos, profesores, inventario, penalizaciones, temporizador y menú del nivel. |
| 4 — Callejón y edificio F | Apertura narrativa, guardias/perros, alarma, sensores, cubículo y laptop de Torres, regreso con Software. |

El nivel 4 actual se aparta del plan original de llave, contraseña y sospecha. Las issues conservan sus criterios hasta acordar esa diferencia. Quedan pendientes la validación integral, las pruebas con usuarios, el paquete final de Windows y la entrega académica. La fecha del plan fue el **13 de noviembre de 2026**; no se afirma que la entrega haya sido aceptada.

[Revisión de issues, milestones y commits para el docente](docs/REVISION_ISSUES_2026-11-18.md).

## Cómo probar el avance

1. Clonar el repositorio y agregar su carpeta a Unity Hub.
2. Abrir con **Unity 6.3 LTS — 6000.3.25f1** y esperar la importación de recursos.
3. Abrir `Assets/Scenes/MenuPrincipal.unity` y pulsar **Play** para elegir nivel.
4. Consultar la introducción y los controles de cada misión antes de comenzar.

| Acción | Control |
|---|---|
| Moverse | WASD o flechas |
| Correr | Shift |
| Interactuar / revisar | E |
| Biblioteca: revelar pista (−8 s) | H o botón Pista |
| Nivel 1: pistas / pedir permiso | N / Q |
| Nivel 3: seleccionar proyecto | 1–5, Tab o clic |
| Nivel 4: caminar despacio / golpe | Ctrl / Espacio |
| Pausar o continuar | Esc |
| Reiniciar al terminar | R o botón Reiniciar |

**Tecnologías:** Unity, C#, Universal Render Pipeline 2D, Tilemaps, Input System y TextMesh Pro. Plataformas objetivo: **Windows y Web**. La generación o publicación de una nueva build se registra por separado de la disponibilidad del código.

## Recursos y créditos

Los paquetes gráficos se obtuvieron de **[LimeZu en itch.io](https://limezu.itch.io/)**. El equipo desarrolla el juego y adapta sus escenarios; la autoría de estos recursos corresponde a LimeZu.

| Paquete | Contenido |
|---|---|
| [Modern Interiors — RPG Tileset](https://limezu.itch.io/moderninteriors) | Interiores, mobiliario, personajes y objetos para ambientar los edificios |
| [Modern Exteriors — RPG Tileset](https://limezu.itch.io/modernexteriors) | Exteriores, edificios, vegetación y objetos del campus |
| [Modern Office — Revamped](https://limezu.itch.io/modernoffice) | Mobiliario y elementos de oficina para los escenarios |

**Tipografías:** [Pixelify Sans](https://fonts.google.com/specimen/Pixelify+Sans) y Redaction 35, con licencias SIL Open Font License incluidas en `Assets/Art/Fonts/PixelifySans/OFL.txt` y `Assets/Art/Fonts/Redaction_35/OFL.txt`.

Los recursos de terceros conservan las condiciones de licencia de sus autores; los enlaces anteriores identifican sus fuentes originales.

## Planeación y seguimiento

- [Plan de trabajo y sprints](docs/PLAN_DE_TRABAJO.md)
- [Tablero de GitHub Projects](https://github.com/users/agustn134/projects/1) — acceso del equipo
- [Issues y tareas](https://github.com/agustn134/ya-casi/issues?q=label%3Aplaneacion)
- [Hitos de entrega](https://github.com/agustn134/ya-casi/milestones)
- [Matriz de pruebas](docs/MATRIZ_PRUEBAS.md)

<details>
<summary>Material visual del proyecto</summary>

<img width="1600" height="892" alt="image" src="https://github.com/user-attachments/assets/387810ca-6087-4a52-8532-698f61d62952" />

<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/92f9bab1-5150-4588-a1d7-f631e02f38aa" />

<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/40161e4a-92fc-46bc-b497-701a3975bef2" />

</details>


## Atribución del nivel 1

El desarrollo completo del nivel 1 fue realizado por **Agustín López Parra (@agustn134)**. **Carlos Manuel Palma Muñoz (@elmanee) participó únicamente como revisor**, sin colaborar en su desarrollo, según lo confirmado por Agustín. [Desglose del Sprint 1](docs/SPRINT_1_NIVEL_1.md).


## Responsables por nivel

| Nivel y sprint | Responsable |
|---|---|
| 1 — El Caos del Casillero | Agustín López Parra (@agustn134) |
| 2 — Maratón en la Biblioteca | Carlos Manuel Palma Muñoz (@elmanee) |
| 3 — La Ruta de Entrega | Agustín López Parra (@agustn134) |
| 4 — El Cubículo | Carlos Manuel Palma Muñoz (@elmanee) |

Todas las issues de los sprints 1 y 3 corresponden a Agustín; las de los sprints 2 y 4 a Carlos. Actualizado el 28 de octubre de 2026.

## Música original

Composición y producción: **Agustín López Parra**, desarrollador de ¡Ya Casi!, en **FL Studio 25**.

- `YaCASI-Soundtrack-InicioMenu.mp3`: menú del nivel 3.
- `YaCASI-Soundtrack-Nivel3.mp3`: música del nivel 3, en bucle durante la partida.
- `YaCASI-Soundtrack-GameOver-TiempoFinal.mp3`: fin de tiempo (tres minutos) del nivel 3.

Para probar: abrir `Assets/Scenes/MenuNivel03.unity`, pulsar Play y elegir Jugar nivel 3.

## Nivel 3 — inventario y entregas (YC14)

Cinco proyectos distintos, cinco profesores y tres minutos. Seleccionar con 1–5, Tab o clic; entregar con E. Esc abre la pausa con continuar, reiniciar, opciones y menú principal. La interfaz comparte Pixelify Sans, paneles y ajustes de audio con el nivel 1.

Las amonestaciones restan tiempo: proyecto equivocado, 5 segundos; entrega repetida a un profesor que ya recibió su proyecto, 8 segundos. El HUD muestra el número de amonestaciones y el descuento acumulado. Si la sanción agota el reloj, se pierde la partida.

[Alcance y comprobaciones de YC14](docs/YC14_ENTREGAS_INVENTARIO.md). Escena provisional: `Assets/Scenes/Nivel03_PruebaEntregas.unity`.


## Publicación y evidencia

[Commits de la rama integrada](https://github.com/agustn134/ya-casi/commits/nivel%2F02-biblioteca) · [Project de sprints](https://github.com/users/agustn134/projects/1) · [Releases](https://github.com/agustn134/ya-casi/releases). En la revisión del 18 de noviembre de 2026 no había Releases publicadas; por tanto no se proporciona una descarga final de Windows.

[Demo web existente](https://deluxe-kelpie-853da0.netlify.app/): publicación anterior, que no confirma por sí sola los cambios de esta rama ni una validación completa. [Documentación del nivel 4](docs/NIVEL04_EL_CALLEJON.md) y [capturas y reportes históricos](Level4Work/). Las pruebas nuevas quedan a cargo del propietario.
