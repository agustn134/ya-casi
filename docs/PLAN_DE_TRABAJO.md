# Plan de trabajo de Ya Casi

Replaneación del 23 de octubre de 2026. Entrega final confirmada por el equipo: **13 de noviembre de 2026**. La hora límite académica queda por confirmar.

Este plan sustituye las fechas operativas del cronograma original sin modificar la propuesta ni el GDD entregados. Conserva los cuatro niveles y separa avance comprobado, tareas pendientes y pruebas por realizar.

## Fuentes y punto de partida

- Propuesta de proyecto, fechada el 20 de septiembre de 2026: niveles, roles, cronograma y alcance Windows.
- GDD U1ACT3 de Palma y López, páginas 7–10: roles y sprints; 11–14: mecánicas; 15–18: plan y ejemplos de pruebas; 19–21: publicación y difusión.
- Estado real a 23 de octubre: PR #1 y PR #2 fusionados en `main`; nivel 1 implementado por Agustín, con movimiento, carrera, casilleros, USB aleatoria, reloj, HUD, pausa y resultados. La validación completa de colisiones y compilación Windows sigue pendiente en YC02.
- El inventario general, niveles 2–4, IA, flujo completo y publicación no se consideran terminados sin evidencia. Los casos y testimonios de pruebas redactados en el GDD son referencias que deben verificarse con sesiones reales; no se publican como resultados observados.

## Cambios respecto del plan original

| Elemento | Plan original | Plan reajustado |
|---|---|---|
| Nivel 1 | Carlos, 2 de octubre | Implementado por Agustín e integrado el 23 de octubre |
| Biblioteca | 5–16 de octubre | 26–30 de octubre |
| Campus | 19–30 de octubre | 2–4 de noviembre |
| Cubículo | 2–13 de noviembre | Desarrollo 2–6 de noviembre y pruebas 9–12 |
| Alfa | Tres niveles al 30 de octubre | Cuatro niveles integrados al 6 de noviembre |
| Beta | 9–12 de noviembre | Preparación el 9, pruebas externas el 9–10; correcciones el 11 |
| Demo y versión final | 13 de noviembre | Se conserva el 13 de noviembre |
| Tablero | Trello | GitHub Projects, issues e hitos |
| Implementación | Bolt / Playmaker | Reutilizar la base C# existente; validar si el docente exige scripting visual antes de ampliar alcance |

Se corrige la revisión del sprint 3 que en el documento original repetía la fecha y el objetivo de la biblioteca. No se inventan fechas de inicio o cierre de issues: los registros retrospectivos se identifican como tales.

## Cronograma y entregables

| Etapa | Inicio | Fecha objetivo | Resultado verificable |
|---|---|---|---|
| [Sprint 1 — Nivel 1 integrado](https://github.com/agustn134/ya-casi/milestone/1) | 2026-09-21 | 2026-10-23 | Registrar el nivel 1 realmente implementado por Agustín e integrado en main mediante los PR #1 y #2. Cierre real: 23 de octubre; fecha original: 2 de octubre. No incluye inventario general ni IA futura. |
| [Replaneación y validación de la base](https://github.com/agustn134/ya-casi/milestone/2) | 2026-10-23 | 2026-10-26 | Ordenar GitHub, validar colisiones del nivel 1 y confirmar responsabilidades y capacidad. La planeación se prepara por Agustín; Carlos valida y mantiene el tablero desde el 26 de octubre. |
| [Sprint 2 — Biblioteca y sistemas compartidos](https://github.com/agustn134/ya-casi/milestone/3) | 2026-10-26 | 2026-10-30 | Nivel 2 jugable con tres libros, penalización por error y bibliotecaria; inventario y flujo reutilizables. Probar patrulla del guardia con escenario provisional para reducir riesgo. |
| [Sprint 3 — Campus cubículo y alfa completa](https://github.com/agustn134/ya-casi/milestone/4) | 2026-11-02 | 2026-11-06 | Implementar niveles 3 y 4 en paralelo; integrar los cuatro niveles en una alfa Windows. No agregar sistemas nuevos después del cierre. |
| [Sprint 4 — Pruebas correcciones y entrega](https://github.com/agustn134/ya-casi/milestone/5) | 2026-11-09 | 2026-11-13 | Beta y pruebas externas 9–10 noviembre; correcciones 11; congelar versión 12; entregar Windows y presentación 13. Publicación itch.io conforme al GDD. |

## Reparto y capacidad

Agustín (@agustn134) mantiene la prioridad del producto y realiza escenarios, lógica de objetivos de biblioteca y campus, interfaz, audio, pruebas externas y material de entrega. Carlos (@elmanee) implementa inventario, flujo y menú inicial, bibliotecaria, entregas, guardia e integración. Carlos valida el reparto y administra el tablero a partir del 26 de octubre. Estas asignaciones son propuestas operativas; falta la confirmación de capacidad de cada responsable en YC04.

Estimación de trabajo restante: **42 horas de Agustín y 44 de Carlos**, aproximadamente tres horas por día hábil por persona durante tres semanas, sin contar el nivel 1 ya realizado. Son estimaciones preliminares y no compromisos confirmados. Una tarea en curso por persona como máximo; avisar de un bloqueo en la revisión diaria. Si no existe esa disponibilidad, reducir alcance secundario antes de prometer la versión completa.

| Semana | Agustín | Carlos |
|---|---:|---:|
| R0 | 2 h | 1 h |
| S2 | 13 h | 15 h |
| S3 | 15 h | 16 h |
| S4 | 12 h | 12 h |

## Tablero y reglas de cierre

Tablero: https://github.com/users/agustn134/projects/1

- Por hacer: trabajo identificado, todavía no iniciado.
- En curso: responsable trabajando activamente; máximo uno por persona.
- En revisión: criterios implementados, pendiente de revisión cruzada o prueba.
- Hecho: evidencia comprobada y cambios integrados cuando corresponde; cerrar issue.
- Los bloqueos se anotan con causa, dependencia y siguiente acción. No marcar como En curso el trabajo que nadie ha comenzado.
- Cada issue incluye fecha objetivo, responsable, estimación, dependencia y criterios de aceptación. Los hitos representan sprints; las fechas individuales están en el cuerpo de cada issue.
- Una issue de desarrollo se cierra con PR integrado y prueba. Una issue de gestión, prueba o entrega se cierra con documento, reporte o enlace verificable. No basta con que compile.
- Reunión breve diaria; planeación los lunes; revisión y retrospectiva los viernes 30 de octubre, 6 y 13 de noviembre.

## Registro de tareas

Las fechas son objetivos actuales, no fechas que ya se hayan cumplido. YC01 es el registro del avance existente; YC03 se cierra cuando tablero y plan queden publicados. Las demás issues permanecen abiertas hasta verificación.

| Código e issue | Responsable | Hito | Entrega | Estimación | Dependencias |
|---|---|---|---|---:|---|
| [YC01 Registrar el nivel 1 implementado e integrado](https://github.com/agustn134/ya-casi/issues/3) | @agustn134 | S1 | 2026-10-23 | 0 h | — |
| [YC02 Validar colisiones y recorrido completo del nivel 1](https://github.com/agustn134/ya-casi/issues/4) | @agustn134 | R0 | 2026-10-26 | 2 h | [YC01](https://github.com/agustn134/ya-casi/issues/3) |
| [YC03 Publicar replaneación issues e hitos en GitHub](https://github.com/agustn134/ya-casi/issues/5) | @agustn134 | R0 | 2026-10-23 | 0 h | — |
| [YC04 Validar reparto y mantener el tablero de sprints](https://github.com/agustn134/ya-casi/issues/6) | @elmanee | R0 | 2026-10-26 | 1 h | [YC03](https://github.com/agustn134/ya-casi/issues/5) |
| [YC05 Implementar inventario reutilizable con iconos](https://github.com/agustn134/ya-casi/issues/7) | @elmanee | S2 | 2026-10-27 | 4 h | [YC02](https://github.com/agustn134/ya-casi/issues/4), [YC04](https://github.com/agustn134/ya-casi/issues/6) |
| [YC06 Crear menú inicial y flujo entre escenas](https://github.com/agustn134/ya-casi/issues/8) | @elmanee | S2 | 2026-10-28 | 4 h | [YC05](https://github.com/agustn134/ya-casi/issues/7) |
| [YC07 Construir mapa y colisiones de la biblioteca](https://github.com/agustn134/ya-casi/issues/9) | @agustn134 | S2 | 2026-10-27 | 4 h | [YC02](https://github.com/agustn134/ya-casi/issues/4) |
| [YC08 Implementar búsqueda de tres libros y salida](https://github.com/agustn134/ya-casi/issues/10) | @agustn134 | S2 | 2026-10-28 | 4 h | [YC05](https://github.com/agustn134/ya-casi/issues/7), [YC07](https://github.com/agustn134/ya-casi/issues/9) |
| [YC09 Implementar ruido y sanción de la bibliotecaria](https://github.com/agustn134/ya-casi/issues/11) | @elmanee | S2 | 2026-10-29 | 4 h | [YC05](https://github.com/agustn134/ya-casi/issues/7), [YC07](https://github.com/agustn134/ya-casi/issues/9) |
| [YC10 Añadir tutorial breve y audio de biblioteca](https://github.com/agustn134/ya-casi/issues/12) | @agustn134 | S2 | 2026-10-29 | 2 h | [YC08](https://github.com/agustn134/ya-casi/issues/10) |
| [YC11 Probar guardia y cobertura en escenario provisional](https://github.com/agustn134/ya-casi/issues/13) | @elmanee | S2 | 2026-10-30 | 3 h | [YC04](https://github.com/agustn134/ya-casi/issues/6) |
| [YC12 Cerrar biblioteca jugable y revisar alcance restante](https://github.com/agustn134/ya-casi/issues/14) | @agustn134 | S2 | 2026-10-30 | 3 h | [YC06](https://github.com/agustn134/ya-casi/issues/8), [YC08](https://github.com/agustn134/ya-casi/issues/10), [YC09](https://github.com/agustn134/ya-casi/issues/11), [YC10](https://github.com/agustn134/ya-casi/issues/12), [YC11](https://github.com/agustn134/ya-casi/issues/13) |
| [YC13 Construir campus compacto y zonas de entrega](https://github.com/agustn134/ya-casi/issues/15) | @agustn134 | S3 | 2026-11-03 | 5 h | [YC12](https://github.com/agustn134/ya-casi/issues/14) |
| [YC14 Implementar tres entregas e inventario de proyectos](https://github.com/agustn134/ya-casi/issues/16) | @elmanee | S3 | 2026-11-03 | 4 h | [YC05](https://github.com/agustn134/ya-casi/issues/7), [YC06](https://github.com/agustn134/ya-casi/issues/8), [YC12](https://github.com/agustn134/ya-casi/issues/14) |
| [YC15 Construir cubículo nocturno y coberturas](https://github.com/agustn134/ya-casi/issues/17) | @agustn134 | S3 | 2026-11-04 | 4 h | [YC11](https://github.com/agustn134/ya-casi/issues/13), [YC12](https://github.com/agustn134/ya-casi/issues/14) |
| [YC16 Integrar guardia con sospecha captura y cobertura](https://github.com/agustn134/ya-casi/issues/18) | @elmanee | S3 | 2026-11-05 | 5 h | [YC11](https://github.com/agustn134/ya-casi/issues/13), [YC15](https://github.com/agustn134/ya-casi/issues/17) |
| [YC17 Implementar llave contraseña y victoria final](https://github.com/agustn134/ya-casi/issues/19) | @elmanee | S3 | 2026-11-06 | 4 h | [YC05](https://github.com/agustn134/ya-casi/issues/7), [YC15](https://github.com/agustn134/ya-casi/issues/17), [YC16](https://github.com/agustn134/ya-casi/issues/18) |
| [YC18 Integrar ruta de entrega y transición narrativa](https://github.com/agustn134/ya-casi/issues/20) | @agustn134 | S3 | 2026-11-04 | 3 h | [YC13](https://github.com/agustn134/ya-casi/issues/15), [YC14](https://github.com/agustn134/ya-casi/issues/16) |
| [YC19 Integrar cuatro niveles y generar alfa Windows](https://github.com/agustn134/ya-casi/issues/22) | @elmanee | S3 | 2026-11-06 | 3 h | [YC06](https://github.com/agustn134/ya-casi/issues/8), [YC12](https://github.com/agustn134/ya-casi/issues/14), [YC17](https://github.com/agustn134/ya-casi/issues/19), [YC18](https://github.com/agustn134/ya-casi/issues/20), [YC20](https://github.com/agustn134/ya-casi/issues/21) |
| [YC20 Unificar HUD audio y objetivos de los cuatro niveles](https://github.com/agustn134/ya-casi/issues/21) | @agustn134 | S3 | 2026-11-06 | 3 h | [YC10](https://github.com/agustn134/ya-casi/issues/12), [YC15](https://github.com/agustn134/ya-casi/issues/17), [YC18](https://github.com/agustn134/ya-casi/issues/20) |
| [YC21 Ejecutar matriz interna y preparar beta de pruebas](https://github.com/agustn134/ya-casi/issues/23) | @elmanee | S4 | 2026-11-09 | 3 h | [YC19](https://github.com/agustn134/ya-casi/issues/22) |
| [YC22 Realizar pruebas con tres usuarios externos](https://github.com/agustn134/ya-casi/issues/25) | @agustn134 | S4 | 2026-11-10 | 4 h | [YC21](https://github.com/agustn134/ya-casi/issues/23) |
| [YC23 Corregir bloqueos y balancear con pruebas reales](https://github.com/agustn134/ya-casi/issues/26) | @elmanee | S4 | 2026-11-11 | 4 h | [YC22](https://github.com/agustn134/ya-casi/issues/25) |
| [YC24 Actualizar README controles créditos y evidencia académica](https://github.com/agustn134/ya-casi/issues/27) | @agustn134 | S4 | 2026-11-11 | 2 h | [YC22](https://github.com/agustn134/ya-casi/issues/25) |
| [YC25 Validar regresión y congelar versión candidata](https://github.com/agustn134/ya-casi/issues/28) | @elmanee | S4 | 2026-11-12 | 3 h | [YC23](https://github.com/agustn134/ya-casi/issues/26), [YC24](https://github.com/agustn134/ya-casi/issues/27) |
| [YC26 Preparar tráiler capturas y presentación](https://github.com/agustn134/ya-casi/issues/24) | @agustn134 | S4 | 2026-11-12 | 4 h | [YC19](https://github.com/agustn134/ya-casi/issues/22) |
| [YC27 Publicar paquete Windows y entrega final](https://github.com/agustn134/ya-casi/issues/29) | @agustn134 | S4 | 2026-11-13 | 2 h | [YC25](https://github.com/agustn134/ya-casi/issues/28), [YC26](https://github.com/agustn134/ya-casi/issues/24) |
| [YC28 Verificar entrega y preparar demostración en aula](https://github.com/agustn134/ya-casi/issues/30) | @elmanee | S4 | 2026-11-13 | 2 h | [YC27](https://github.com/agustn134/ya-casi/issues/29) |

## Alcance y decisiones de recuperación

Se conservan las mecánicas del GDD: USB y casilleros; tres libros y reacción al ruido; tres entregas; llave, contraseña, laptop y vigilancia. Se reutilizan jugador, arte, tipografía y HUD del nivel 1. Se limita el campus a rutas compactas y se usa un panel narrativo en vez de una cinemática compleja.

| Punto de control | Condición | Acción |
|---|---|---|
| 26 de octubre | Capacidad o responsabilidades sin confirmar | Acordar reparto en YC04 y registrar el ajuste; no asumir aceptación de Carlos |
| 30 de octubre | Biblioteca o prototipo de guardia no pasan | Reducir decoración, tamaño de mapas, video y contenido secundario; aprobar vigilancia temporizada si la visión no es estable |
| 6 de noviembre | No hay alfa de cuatro niveles | Registrar faltantes, priorizar un recorrido mínimo completo y consultar al docente cualquier reducción de niveles; la demo de niveles 1 y 4 no sustituye automáticamente el juego completo |
| 11 de noviembre | Quedan bloqueos críticos | Detener mejoras visuales y resolver inicio, progreso, final y reintento |
| 12 de noviembre | Candidata validada | Congelar funcionalidades; permitir solo arreglos críticos comprobados |

La complejidad del guardia, el trabajo simultáneo en niveles 3 y 4 y la disponibilidad de dos personas son los riesgos principales. Prioridad P0: completar y entregar; P1: simplificable si compromete la fecha. No reconstruir sistemas que ya funcionan ni añadir niveles o mecánicas nuevos.

## Pruebas y entrega

Consultar [matriz de pruebas](MATRIZ_PRUEBAS.md). Alfa Windows el 6; beta el 9; tres pruebas externas de 20–25 minutos el 9–10; correcciones el 11; regresión y congelación el 12. Toda observación y testimonio debe provenir de una sesión real. Mantener datos de participantes anónimos en GitHub.

La entrega del 13 incluye ZIP Windows completo y probado fuera del Editor, versión identificable, README de instalación y controles, créditos y licencias, capturas reales, enlace de descarga, presentación y evidencia de pruebas. El tráiler de hasta 45 segundos se simplifica si compite con correcciones críticas. La demo en aula puede mostrar niveles 1 y 4 como prevé el GDD, conservando el recorrido completo en la versión final.

## Flujo de Git

Una rama por tarea o conjunto pequeño, con referencia a la issue. Mantener `main` jugable, revisar cambios antes de integrar y no trabajar ambos simultáneamente en una misma escena. No subir Library, Temp, Logs, credenciales ni datos privados de evaluadores. Unir PR, issue y evidencia de prueba para mantener trazabilidad.
