# Matriz de pruebas de Ya Casi

Estado inicial al 23 de octubre de 2026: **pendiente de ejecutar como matriz formal**. Que una función exista o compile no equivale a pasar la prueba. Registrar versión, fecha, responsable y resultado real en cada ejecución.

| ID | Escenario | Resultado esperado |
|---|---|---|
| T01 | Caminar y correr en ocho direcciones | Velocidad coherente, animación correcta y sin atravesar sólidos |
| T02 | Acercarse y alejarse de un casillero | Solo se resalta el más cercano dentro del rango; E no interactúa a distancia |
| T03 | Revisar casillero incorrecto dos veces | Solo una penalización, marca de revisado y feedback legible |
| T04 | Encontrar USB y cruzar salida | Salida se desbloquea, victoria única y estadísticas coherentes |
| T05 | Agotar reloj por espera y por penalización | Derrota sin valores negativos ni acciones posteriores |
| T06 | Pausar, continuar, reiniciar y cambiar escena | Reloj detenido en pausa, controles restaurados y escala temporal correcta |
| T07 | Nueva ronda | Estado limpio y exactamente una USB; no exigir que cambie en cada reinicio, puede repetir por azar |
| T08 | Rodear banca, maceta y bote | Collider cubre base; no hay atascos ni barreras invisibles |
| T09 | Inventario | Alta, baja y cantidades correctas; sin recogidas duplicadas |
| T10 | Biblioteca | Lista de tres libros, error penalizado una vez y salida solo al completar |
| T11 | Bibliotecaria | Correr dispara reacción en rango; inmovilización tres segundos y espera cinco segundos |
| T12 | Campus | Tres entregas válidas únicas y transición narrativa al cubículo |
| T13 | Guardia | Patrulla estable; sospecha por visión libre, cobertura válida y captura al máximo |
| T14 | Laptop | Requiere llave y contraseña; victoria final solo con ambas |
| T15 | Recorrido completo | Menú → niveles 1–4 → final sin bloqueo ni audio superpuesto |
| T16 | Resoluciones | HUD y menús legibles a 1366×768 y 1920×1080 |
| T17 | ZIP Windows limpio | Inicia fuera del Editor con todas sus carpetas y controles funcionales |
| T18 | Usuario externo sin explicación oral | Entiende objetivos y controles; registrar observaciones reales |

## Registro de ejecución

| Fecha | Versión o commit | ID | Responsable | Resultado real | Evidencia o issue |
|---|---|---|---|---|---|
| Pendiente | — | — | — | No ejecutado | — |

Severidad crítica: no inicia, pierde progreso esencial, no permite terminar o reiniciar. Alta: mecánica central falla. Media: problema con alternativa. Baja: detalle visual o sonoro sin bloqueo.

En pruebas externas usar identificadores anónimos E1, E2 y E3 y un cuestionario breve sobre objetivo, controles, dificultad y claridad. No copiar como resultados reales los ejemplos de errores, tiempos o testimonios del GDD.
