# ¡Ya Casi!

Videojuego académico de aventura y puzzles **2D con vista cenital**, ambientado en la vida universitaria de la UTNG. El jugador debe recuperar sus materiales y completar entregas antes de que termine el tiempo.

**Asignatura:** Creación de Videojuegos · **Docente:** Gabriel Barrón Rodríguez  
**Equipo:** Agustín López Parra ([agustn134](https://github.com/agustn134)) y Carlos Manuel Palma Muñoz ([elmanee](https://github.com/elmanee)).

## Estado del proyecto

**En desarrollo.** El nivel 1, *El caos del casillero*, está implementado e integrado en `main`: búsqueda de una USB aleatoria, penalización por casillero incorrecto, temporizador, HUD, pausa, reinicio y pantallas de resultado.

Los niveles de **biblioteca, ruta de entrega y cubículo** están planeados y pendientes de desarrollo y validación. La entrega final está programada para el **13 de noviembre de 2026**; aún no se presenta como versión final publicada.

## Cómo probar el avance

1. Clonar el repositorio y agregar su carpeta a Unity Hub.
2. Abrir con **Unity 6.3 LTS — 6000.3.25f1** y esperar la importación de recursos.
3. Abrir `Assets/Scenes/Nivel01_CaosDelCasillero.unity` y pulsar **Play**.
4. Revisar los casilleros, encontrar la USB y llegar a la salida antes de que se agote el reloj.

| Acción | Control |
|---|---|
| Moverse | WASD o flechas |
| Correr | Shift |
| Revisar un casillero | E |
| Pausar o continuar | Esc |
| Reiniciar al terminar | R o botón Reiniciar |

**Tecnologías:** Unity, C#, Universal Render Pipeline 2D, Tilemaps, Input System y TextMesh Pro. Plataforma objetivo: **Windows**.

## Recursos y créditos

Los paquetes gráficos se obtuvieron de **[LimeZu en itch.io](https://limezu.itch.io/)**. El equipo desarrolla el juego y adapta sus escenarios; la autoría de estos recursos corresponde a LimeZu.

| Paquete | Contenido |
|---|---|
| [Modern Interiors — RPG Tileset](https://limezu.itch.io/moderninteriors) | Interiores, mobiliario, personajes y objetos para ambientar los edificios |
| [Modern Exteriors — RPG Tileset](https://limezu.itch.io/modernexteriors) | Exteriores, edificios, vegetación y objetos del campus |
| [Modern Office — Revamped](https://limezu.itch.io/modernoffice) | Mobiliario y elementos de oficina para los escenarios |

**Tipografía:** [Pixelify Sans en Google Fonts](https://fonts.google.com/specimen/Pixelify+Sans), con licencia SIL Open Font License incluida en `Assets/Art/Fonts/PixelifySans/OFL.txt`.

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
