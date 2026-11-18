# Plan de trabajo de Ya Casi

Reparto actualizado el **28 de octubre de 2026** por indicación del propietario. Entrega final confirmada: **13 de noviembre de 2026**, con hora académica por confirmar. Este reparto sustituye la distribución de responsabilidades de la replaneación del 23 de octubre; no modifica las fechas históricas ni atribuye como terminado trabajo pendiente.

## Responsables por nivel y sprint

| Sprint | Nivel | Responsable de todas sus issues | Fecha objetivo del hito |
|---|---|---|---|
| 1 | El Caos del Casillero | Agustín López Parra (@agustn134) | Integrado el 23 de octubre; completado |
| 2 | Maratón en la Biblioteca | Carlos Manuel Palma Muñoz (@elmanee) | 30 de octubre |
| 3 | La Ruta de Entrega | Agustín López Parra (@agustn134) | 6 de noviembre |
| 4 | El Cubículo | Carlos Manuel Palma Muñoz (@elmanee) | 13 de noviembre |

En el nivel 1, Agustín realizó todo el desarrollo; Carlos únicamente revisó, conforme a la información confirmada por Agustín. La issue que registra esa revisión está asignada a Agustín para su seguimiento, sin convertir a Carlos en desarrollador del nivel. [Desglose del nivel 1](SPRINT_1_NIVEL_1.md).

El hito de replaneación queda separado de los cuatro sprints: #5 corresponde a la planeación publicada por Agustín y #6 a la confirmación y mantenimiento del tablero por Carlos. No se cuentan como desarrollo de un nivel.

## Alcance de cada sprint

- **Sprint 1 — Agustín:** movimiento, animación, carrera, escenario, cámara, decoración, colisiones, casilleros, USB, temporizador, HUD, pausa, resultados, audio e integración del nivel 1. Completado según los registros y confirmación del propietario.
- **Sprint 2 — Carlos:** inventario compartido, menú inicial y flujo entre escenas; mapa de biblioteca, tres libros, penalizaciones, ruido, bibliotecaria, tutorial, audio y validación del nivel 2.
- **Sprint 3 — Agustín:** mapa compacto del campus, tres proyectos y profesores, lógica de entregas y transición narrativa al nivel 4.
- **Sprint 4 — Carlos:** prototipo de guardia, escenario del cubículo, coberturas y sospecha, captura, llave, contraseña y laptop. También asume las issues transversales de HUD global, integración, pruebas, documentación, tráiler, publicación y verificación final que se mantienen en este hito.

## Calendario que se conserva

| Fecha | Entregable |
|---|---|
| 30 de octubre | Biblioteca jugable y prototipo de guardia |
| 2–6 de noviembre | Campus y cubículo en paralelo; alfa integrada de cuatro niveles el día 6 |
| 9–10 de noviembre | Beta y tres sesiones de pruebas externas |
| 11 de noviembre | Correcciones y documentación |
| 12 de noviembre | Regresión, versión candidata y material de presentación |
| 13 de noviembre | Paquete Windows, publicación y entrega académica |

El Sprint 4 comienza su prototipo el 30 de octubre y continúa en paralelo al Sprint 3. Los hitos reflejan el nivel responsable, no cuatro periodos necesariamente consecutivos. Las fechas individuales existentes de las issues se conservan; los registros retrospectivos no se retrofechan.

## Capacidad y riesgos

El nuevo reparto concentra en Carlos los niveles 2 y 4 y las tareas de cierre. Las estimaciones anteriores de 42 y 44 horas ya no representan este reparto y deben recalcularse en #6. No se ha confirmado disponibilidad nueva. Si la capacidad resulta insuficiente, acordar el ajuste antes de incumplir dependencias; no cambiar fechas ni dar por aceptada la carga sin registrarlo.

Puntos de control: biblioteca y prototipo el 30 de octubre, alfa completa el 6 de noviembre y congelación el 12. Reducir decoración, mapas y edición del video si amenazan la entrega. Si el guardia no es estable, evaluar la alternativa de vigilancia temporizada del GDD. Reducir niveles requiere acuerdo con el docente; una demo de niveles 1 y 4 no sustituye automáticamente la versión completa.

## Tablero y criterios de cierre

[GitHub Projects](https://github.com/users/agustn134/projects/1) · [Issues](https://github.com/agustn134/ya-casi/issues) · [Hitos](https://github.com/agustn134/ya-casi/milestones)

Por hacer → En curso → En revisión → Hecho. Máximo una tarea activa por persona. Cada issue tiene responsable, hito y criterios de aceptación. Cerrar desarrollo con cambios integrados y evidencia; cerrar pruebas o gestión con resultado verificable. Registrar bloqueos y dependencias. Mantener revisión breve diaria y revisión semanal.

## Issues por sprint

| Issue | Tarea | Responsable | Fecha objetivo conservada | Estado |
|---|---|---|---|---|
| **Sprint 1** | **El Caos del Casillero** | **@agustn134** | | |
| [#3](https://github.com/agustn134/ya-casi/issues/3) | [YC01] Registrar el nivel 1 implementado e integrado | @agustn134 | 2026-10-23 | Completada |
| [#4](https://github.com/agustn134/ya-casi/issues/4) | [YC02] Validar colisiones y recorrido completo del nivel 1 | @agustn134 | 2026-10-23 | Completada |
| [#32](https://github.com/agustn134/ya-casi/issues/32) | [S1-01] Configurar Unity 2D y movimiento del jugador | @agustn134 | 2026-10-23 | Completada |
| [#33](https://github.com/agustn134/ya-casi/issues/33) | [S1-02] Implementar animaciones y carrera | @agustn134 | 2026-10-23 | Completada |
| [#34](https://github.com/agustn134/ya-casi/issues/34) | [S1-03] Construir tilemaps y seguimiento de cámara | @agustn134 | 2026-10-23 | Completada |
| [#35](https://github.com/agustn134/ya-casi/issues/35) | [S1-04] Ambientar el pasillo y organizar la decoración | @agustn134 | 2026-10-23 | Completada |
| [#36](https://github.com/agustn134/ya-casi/issues/36) | [S1-05] Implementar interacción y estado de casilleros | @agustn134 | 2026-10-23 | Completada |
| [#37](https://github.com/agustn134/ya-casi/issues/37) | [S1-06] Implementar USB aleatoria y desbloqueo de salida | @agustn134 | 2026-10-23 | Completada |
| [#38](https://github.com/agustn134/ya-casi/issues/38) | [S1-07] Implementar temporizador penalizaciones y alertas | @agustn134 | 2026-10-23 | Completada |
| [#39](https://github.com/agustn134/ya-casi/issues/39) | [S1-08] Diseñar HUD y configurar tipografía | @agustn134 | 2026-10-23 | Completada |
| [#40](https://github.com/agustn134/ya-casi/issues/40) | [S1-09] Implementar pausa y conectar el prefab del menú | @agustn134 | 2026-10-23 | Completada |
| [#41](https://github.com/agustn134/ya-casi/issues/41) | [S1-10] Implementar resultados y reinicio de partida | @agustn134 | 2026-10-23 | Completada |
| [#42](https://github.com/agustn134/ya-casi/issues/42) | [S1-11] Integrar efectos de sonido y pasos | @agustn134 | 2026-10-23 | Completada |
| [#43](https://github.com/agustn134/ya-casi/issues/43) | [S1-12] Integrar nivel 1 y publicar cambios en GitHub | @agustn134 | 2026-10-23 | Completada |
| [#44](https://github.com/agustn134/ya-casi/issues/44) | [S1-REV] Registrar la revisión del nivel 1 realizada por Carlos | @agustn134 | 2026-10-23 | Completada |
| **Sprint 2** | **Maratón en la Biblioteca** | **@elmanee** | | |
| [#7](https://github.com/agustn134/ya-casi/issues/7) | [YC05] Implementar inventario reutilizable con iconos | @elmanee | 2026-10-27 | Pendiente |
| [#8](https://github.com/agustn134/ya-casi/issues/8) | [YC06] Crear menú inicial y flujo entre escenas | @elmanee | 2026-10-28 | Pendiente |
| [#9](https://github.com/agustn134/ya-casi/issues/9) | [YC07] Construir mapa y colisiones de la biblioteca | @elmanee | 2026-10-27 | Pendiente |
| [#10](https://github.com/agustn134/ya-casi/issues/10) | [YC08] Implementar búsqueda de tres libros y salida | @elmanee | 2026-10-28 | Pendiente |
| [#11](https://github.com/agustn134/ya-casi/issues/11) | [YC09] Implementar ruido y sanción de la bibliotecaria | @elmanee | 2026-10-29 | Pendiente |
| [#12](https://github.com/agustn134/ya-casi/issues/12) | [YC10] Añadir tutorial breve y audio de biblioteca | @elmanee | 2026-10-29 | Pendiente |
| [#14](https://github.com/agustn134/ya-casi/issues/14) | [YC12] Cerrar biblioteca jugable y revisar alcance restante | @elmanee | 2026-10-30 | Pendiente |
| **Sprint 3** | **La Ruta de Entrega** | **@agustn134** | | |
| [#15](https://github.com/agustn134/ya-casi/issues/15) | [YC13] Construir campus compacto y zonas de entrega | @agustn134 | 2026-11-03 | Pendiente |
| [#16](https://github.com/agustn134/ya-casi/issues/16) | [YC14] Implementar tres entregas e inventario de proyectos | @agustn134 | 2026-11-03 | Pendiente |
| [#20](https://github.com/agustn134/ya-casi/issues/20) | [YC18] Integrar ruta de entrega y transición narrativa | @agustn134 | 2026-11-04 | Pendiente |
| **Sprint 4** | **El Cubículo** | **@elmanee** | | |
| [#13](https://github.com/agustn134/ya-casi/issues/13) | [YC11] Probar guardia y cobertura en escenario provisional | @elmanee | 2026-10-30 | Pendiente |
| [#17](https://github.com/agustn134/ya-casi/issues/17) | [YC15] Construir cubículo nocturno y coberturas | @elmanee | 2026-11-04 | Pendiente |
| [#18](https://github.com/agustn134/ya-casi/issues/18) | [YC16] Integrar guardia con sospecha captura y cobertura | @elmanee | 2026-11-05 | Pendiente |
| [#19](https://github.com/agustn134/ya-casi/issues/19) | [YC17] Implementar llave contraseña y victoria final | @elmanee | 2026-11-06 | Pendiente |
| [#21](https://github.com/agustn134/ya-casi/issues/21) | [YC20] Unificar HUD audio y objetivos de los cuatro niveles | @elmanee | 2026-11-06 | Pendiente |
| [#22](https://github.com/agustn134/ya-casi/issues/22) | [YC19] Integrar cuatro niveles y generar alfa Windows | @elmanee | 2026-11-06 | Pendiente |
| [#23](https://github.com/agustn134/ya-casi/issues/23) | [YC21] Ejecutar matriz interna y preparar beta de pruebas | @elmanee | 2026-11-09 | Pendiente |
| [#24](https://github.com/agustn134/ya-casi/issues/24) | [YC26] Preparar tráiler capturas y presentación | @elmanee | 2026-11-12 | Pendiente |
| [#25](https://github.com/agustn134/ya-casi/issues/25) | [YC22] Realizar pruebas con tres usuarios externos | @elmanee | 2026-11-10 | Pendiente |
| [#26](https://github.com/agustn134/ya-casi/issues/26) | [YC23] Corregir bloqueos y balancear con pruebas reales | @elmanee | 2026-11-11 | Pendiente |
| [#27](https://github.com/agustn134/ya-casi/issues/27) | [YC24] Actualizar README controles créditos y evidencia académica | @elmanee | 2026-11-11 | Pendiente |
| [#28](https://github.com/agustn134/ya-casi/issues/28) | [YC25] Validar regresión y congelar versión candidata | @elmanee | 2026-11-12 | Pendiente |
| [#29](https://github.com/agustn134/ya-casi/issues/29) | [YC27] Publicar paquete Windows y entrega final | @elmanee | 2026-11-13 | Pendiente |
| [#30](https://github.com/agustn134/ya-casi/issues/30) | [YC28] Verificar entrega y preparar demostración en aula | @elmanee | 2026-11-13 | Pendiente |

## Fuentes y validación

Propuesta de proyecto y GDD U1ACT3 de Palma y López: alcance de cuatro niveles, mecánicas, pruebas y entrega Windows. Se utiliza la implementación C# existente y GitHub Projects para seguimiento. Los documentos originales no se modifican. Los ejemplos de testimonios y errores del GDD no se presentan como pruebas ejecutadas. Consultar [matriz de pruebas](MATRIZ_PRUEBAS.md).
