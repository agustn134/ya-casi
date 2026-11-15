# Nivel 3: última entrega y retroalimentación

La mecánica vigente incorpora retos en los cinco profesores, mejoras del inventario y «Con permiso». Está descrita en [NIVEL03_MEJORAS_ENTREGAS.md](NIVEL03_MEJORAS_ENTREGAS.md). Este documento y sus registros de pruebas describen la primera versión del reto final.

La retroalimentación docente pidió aumentar la dificultad de la última entrega con objetos lanzados. Se conservan las cinco entregas, el inventario y el límite de tres minutos.

## Comportamiento

- Al completar cuatro proyectos y acercarse al profesor pendiente, aparece un diálogo breve. Cualquier profesor puede ser el último, según la ruta elegida.
- El diálogo detiene el reloj y los personajes. Después empieza un desafío de 16 segundos con seis oleadas de papel. Las tres últimas añaden un segundo objeto.
- Una línea naranja anuncia cada trayectoria durante al menos 0.8 segundos. El objeto apunta a la posición anunciada; no persigue al jugador.
- Un impacto resta cinco segundos, conserva el proyecto y produce un destello, una breve ralentización y una sacudida leve de cámara. Hay un segundo de protección para evitar impactos consecutivos inmediatos.
- Espacio o el botón este del mando permiten un esquive de 0.16 segundos, con recarga de 1.8 segundos y protección durante el esquive. En móvil existe un botón «Esquivar». Se mantienen la huella sobre Camino_Tilemap y las colisiones con estudiantes y edificios.
- Pausar detiene objetos, avisos, recarga y desafío. Al terminar los 16 segundos se limpian los objetos. Todavía hace falta acercarse al profesor y entregar con E; esperar no gana automáticamente.
- Se mantienen las penalizaciones por proyecto equivocado (5 s) y entrega repetida (8 s).

## Texto y presentación

Redaction 35 Regular se aplica al HUD, inventario, letreros, menú del nivel, opciones y paneles. Se crea un atlas SDF estático persistente con español y acentos para no depender de generar caracteres durante la ejecución web. Se conserva la licencia SIL OFL en la carpeta de la fuente y se añade el crédito a The Redaction Project.

La fuente tiene desgaste intencional. Los textos pequeños del HUD tienen un mínimo de 28 puntos y fondos oscuros. Los botones móviles usan WASD porque Redaction 35 no contiene las flechas Unicode.

El comienzo presenta un intercambio entre Galindo y una compañera. Antes de la última entrega hay otro intercambio con el profesor; el resultado incluye un cierre corto. Esta mejora no implementa una transición a una escena de cubículo.

## Ajustes

En el controlador CampusDeliveryObjectives de la escena se puede desactivar `Enable Final Challenge` o cambiar `Thrown Object Penalty`. En CampusPathPlayer se ajustan velocidad, duración y recarga del esquive. El calendario de oleadas se define en FinalDeliveryChallengeState.

La escena Nivel03_PruebaEntregas mantiene el desafío desactivado para comprobar por separado las reglas de inventario. El verificador específico lo habilita durante su prueba.

## Comprobaciones

Ejecutado el 11 de noviembre de 2026: 77 comprobaciones de reglas, 21 de entregas en Unity y 36 de esquive/reto final en Unity, todas aprobadas. Compilación completa de scripts sin errores. La revisión visual se realizó en el campus; las pruebas automatizadas usan la escena provisional.

- Reglas sin Unity: `Tests/YC14/Run-RulesChecks.ps1`; incluye inventario, penalizaciones, impactos y calendario de oleadas. Evidencia: `Tests/YC14/UltimaEjecucion.txt`.
- Unity: menú **Ya Casi > Nivel 3 > Verificar entregas YC14**. Evidencia: `Tests/YC14/IntegracionUnity.txt`.
- Unity: menú **Ya Casi > Nivel 3 > Verificar reto final**. Comprueba orden variable, bloqueo 4/5, diálogo, impactos, inmunidad, pausa con objetos en vuelo y victoria posterior a entregar. Evidencia: `Tests/YC14/RetoFinalUnity.txt`.

Antes de publicar una compilación nueva conviene jugar el recorrido completo en el mapa final y en el navegador para ajustar dificultad y tamaño del texto a la resolución objetivo.
