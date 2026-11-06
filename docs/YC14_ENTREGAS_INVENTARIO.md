# YC14 — Entregas e inventario de proyectos

Relacionado con #16. Rama de trabajo: `nivel/03-ruta-de-entrega`, junto con el avance del PR #49, por indicación del propietario. El trabajo del nivel 3 continúa en esta misma rama.

## Alcance acordado

El propietario pidió conservar **cinco entregas**, en lugar de las tres de la descripción original, para aumentar la complejidad. El HUD muestra **0/5 a 5/5**. Se mantienen los cinco profesores (V, Cafetería, Biblioteca, F y CGTI) y los tres minutos de juego.

Cada proyecto empieza en el inventario con un icono provisional de documento. Sus nombres se generan desde los letreros de los profesores: PROYECTO DE PROF. IRIDIAN, PROYECTO DE PROF. BARRIENTOS, PROYECTO DE PROF. MURO, PROYECTO DE PROF. TORRES y PROYECTO DE PROF. BARRON. Para cambiarlos manualmente, desactivar `Project Names Follow Professors` en `Entregas_Nivel03` y editar `Project Names`, sin modificar los identificadores. Cada profesor acepta solamente su proyecto; una selección incorrecta no se consume y resta 5 segundos; repetir una entrega ya realizada no aumenta el progreso y resta 8 segundos. Cada intento incorrecto suma una amonestación. Las sanciones pueden provocar derrota y no aumentan el cronómetro de recorrido. Los valores son editables en Inspector (`Wrong Project Penalty` y `Repeat Delivery Penalty`). No se requieren recogidas adicionales en este alcance.

## Sistemas y dependencias

- `ProjectInventory`: inventario reutilizable por identificador y cantidad, independiente de Unity y de la interfaz.
- `ProjectDeliverySession`: progreso, validación de entregas, reloj, pausa, victoria y derrota; admite tres o cinco objetivos.
- La interfaz reutiliza `U2Presentation`, `U2GameConfig.font` (Pixelify Sans), el Canvas escalable, los paneles, botones y opciones del nivel 1.
- Se mantiene la música original del nivel 3 y de derrota de Agustín López Parra. Las opciones comparten los mismos ajustes de música, efectos y calidad del nivel 1.
- El menú principal permite entrar al nivel 3. El nivel 1 conserva sus reglas y su tiempo propios.
- No se encontró un inventario reutilizable previo de la issue #7 en esta rama; se añade uno independiente. Esto no acredita ni cierra #7, #8 o #14. La biblioteca y sus dependencias deben validarse con su responsable antes de integrar el flujo completo.

## Controles

WASD/flechas/joystick: mover. Shift/gatillo: correr. 1–5 o clic: elegir proyecto. Tab/RB: siguiente proyecto disponible. E/botón sur: entregar. Esc/Start/botón Pausa: pausar. R: reiniciar tras el resultado. En móvil se muestran controles y el inventario admite selección táctil.

## Comprobaciones

Las reglas se probaron **antes de integrar el mapa** con proyectos y profesores provisionales. `Tests/YC14/DeliveryRulesChecks.cs` cubre inicio, error de proyecto, ausencia del objeto, consumo, repetición, orden libre, pausa, reanudación, victoria, derrota y reinicio, para tres y cinco entregas. Resultado: 45 comprobaciones satisfactorias; salida en `Tests/YC14/UltimaEjecucion.txt`. Repetir con `Tests/YC14/Run-RulesChecks.ps1`.

La escena `Assets/Scenes/Nivel03_PruebaEntregas.unity` permite probar el controlador y la interfaz con cinco profesores sobre un suelo provisional, sin estudiantes ni campus final. En Unity, `Ya Casi > Nivel 3 > Verificar entregas YC14` ejecuta las comprobaciones de integración y genera `Tests/YC14/IntegracionUnity.txt`. Ejecutado en Unity 6000.3.25f1: **18 comprobaciones satisfactorias de la versión inicial**, incluida la fuente del nivel 1, entrega incorrecta, inventario, repetición, pausa de ambos relojes, reanudación, victoria 5/5, reinicio y derrota con TiempoFinal. Evidencia real en `Tests/YC14/IntegracionUnity.txt`.

Para la prueba manual final: abrir MenuPrincipal o MenuNivel03, entrar al campus, seleccionar un proyecto equivocado y comprobar que permanece, entregar el correcto, repetir, pausar varios segundos, modificar el volumen, reanudar, completar 5/5, reiniciar y provocar derrota por tiempo. Confirmar fuente, legibilidad, recorrido de los estudiantes y transición musical. Adjuntar evidencia al PR y actualizar los criterios de #16 al alcance de cinco entregas antes de cerrarla.
