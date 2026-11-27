# Nivel 2 - Maratón en la Biblioteca

Escena: Assets/Scenes/Nivel02_MaratonEnLaBiblioteca.unity
Proyecto integrado: C:/Users/elmane/Documents/Universidad/10mo/barron/ya-casi
Editor: Unity 6000.3.25f1.

## Jugar
Abrir la escena y pulsar Play, o usar «Nivel 2 · Biblioteca» en el menú principal. Al ganar el nivel 1 aparece el mismo acceso. Continuar recuerda el último nivel visitado; la partida del nivel 2 comienza desde el principio.

WASD/flechas: caminar. Shift: correr. E: revisar/salir. Esc: pausa. R: reiniciar tras el resultado. Mando: stick, gatillo derecho, botón sur y Start. Botones táctiles en dispositivos móviles.

## Reglas
300 segundos. Buscar Algoritmos, Bases de datos y Redes en 30 estantes. Los tres libros se sortean sin reemplazo entre TODOS los estantes en cada partida. Cada búsqueda sin un libro nuevo resta 10 segundos, incluso al repetir el mismo estante o revisar de nuevo uno cuyo libro ya se recogió. Nunca se duplican libros. La zona de salida al sur cambia de ámbar a verde al completar el inventario. Entrar caminando con los tres libros termina el nivel automáticamente; E también funciona y la salida tiene prioridad frente a los estantes.

Correr genera un radio de ruido de 5 unidades. La bibliotecaria recorre sus puntos de patrulla y busca un camino libre al jugador cuando lo escucha. Al alcanzarlo lo detiene durante 3 segundos mientras el reloj sigue corriendo. Después hay 5 segundos de protección contra otro regaño. Caminar no genera detección.

## Escala y edición
Jugador clonado desde la escena del nivel 1: conserva escala, Rigidbody2D, Animator, PlayerInput, velocidades y animaciones. Láminas importadas a 16 píxeles por unidad, filtro Point, sin compresión ni mipmaps; decorados y Grid a escala 0.5, como los tiles del nivel 1. Cámara ortográfica de tamaño 5.

Mapa editable guardado: Tilemap de piso, muros sólidos, 30 estantes con puntos de interacción, cuatro mesas de lectura, mostrador, bibliotecaria, ruta, salida física y gestor. Ocho alumnos sentados leen con animaciones; otros seis caminan y se detienen ante los estantes para buscar libros. Sus colliders son triggers para evitar que bloqueen la salida o los puntos de búsqueda. Los sprites recortados están en Assets/Art/Library/Generated; no se modificaron las imágenes originales del Escritorio.

Se copiaron Library.png (Classroom and library Shadowless), RoomBuilder.png y LICENSE.txt de Modern Interiors de LimeZu, más cuatro pequeñas láminas reading_16x16 (Lucy, Rob, Molly y Pier) para las animaciones de lectura. El personaje, fuente, música y UI reutilizan los recursos presentes en el proyecto. Efectos de libro, error, pasos y shhh sintetizados localmente. No requiere paquetes nuevos.

## Verificación
Ya Casi > Nivel 2 > Validar biblioteca: referencias de escena.
Ya Casi > Nivel 2 > Probar partida completa: prueba en Play y reporte LibraryPlayVerification.txt.

Prueba ampliada: 24 sorteos con tres libros únicos, cambios entre partidas, 30 estantes, ocho lectores y seis alumnos móviles, búsquedas repetidas con penalización, movimiento y búsqueda de NPCs, ruido, pausa, regaño, salida bloqueada sin libros y salida al caminar con tres libros, reinicio y derrota. El resultado de la última ejecución está en LibraryPlayVerification.txt.
Revisión visual: tutorial, mapa y HUD renderizan en Unity; sin pantalla negra. Las pruebas se realizaron en el Editor, no en una compilación Windows ni en un dispositivo móvil.

## Integración
U2Presentation: acceso al nivel 2 y Continuar.
LevelOneUpgrade: acceso desde victoria del nivel 1.
EditorBuildSettings y U2Setup: escena incluida en listas de compilación Windows/WebGL.
Los scripts anteriores se respaldaron como *.before-library.bak.

El nivel 3 queda fuera de esta entrega; la victoria de biblioteca ofrece reiniciar o volver al menú.

## Ampliación de biblioteca
Ya Casi > Nivel 2 > Ampliar biblioteca y reparar salida aplica los cambios a una escena anterior. Conserva respaldo en LibraryBeforeExpansion.unity.bak. LibraryExpansionV2.txt registra la migración completada.

## Filas traseras y presentación
Se mantienen los 18 estantes originales y se añaden 12 detrás de las dos primeras filas (30 en total). Los nuevos puntos de búsqueda quedan al norte y las rutas de alumnos se ajustan al pasillo libre. Ya Casi > Nivel 2 > Completar 30 estantes aplica esta ampliación; respaldo LibraryBeforeThirtyShelves.unity.bak.

Tutorial resumido: encontrar tres libros, volver a la salida, advertencia de regaños al correr, penalización de búsquedas y controles. La derrota detiene la música de partida y reproduce U2GameConfig.gameOverSound, el mismo clip que usa el nivel 1; respeta el volumen de música y se reproduce con el tiempo de juego detenido.

## Interior y puerta de biblioteca
Ambientación con mosaicos de madera, alfombra del pasillo, alfombra de lectura y de préstamos, zócalos, ventanas, plantas, lámparas y exhibidores de novedades. Los muebles se guardan dentro de Decoración Biblioteca y son editables. Se conservan los 30 estantes y las reglas de búsqueda y salida.

Acceso sur: marco y dintel de madera, umbral, alfombra, placa y luz de estado. La puerta doble utiliza cinco fotogramas de animated_door_big_1 y se abre al recuperar los tres libros. El disparador de salida continúa funcionando al caminar.

Recursos adicionales seleccionados de Modern Interiors de LimeZu: Living_Room_Singles_Shadowless 13, 14, 17 (plantas), 35 (exhibidor), 38 (ventana), 86 (lámpara); 1_Generic_Shadowless para alfombras y animated_door_big_1 para las hojas. Solo estos ocho PNG se copiaron a Assets/Art/Library/Interior. Licencia en Assets/Art/Library/LICENSE.txt.

Menú de editor: Ya Casi > Nivel 2 > Mejorar interior y puerta. Respaldo previo LibraryBeforeInterior.unity.bak; registro LibraryInteriorV4.txt.
