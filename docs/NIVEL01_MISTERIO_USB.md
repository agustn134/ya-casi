# El casillero equivocado

Escena: `Assets/Scenes/Nivel01_CaosDelCasillero.unity`. Rama de trabajo: `nivel/01-caos-del-casillero`.

La versión actual transcurre de noche en dos plantas. La nota está en el salón 101, el compañero en el pasillo superior y el aviso en el salón 203. La fila Azul queda abajo y la Verde arriba; se comunican con una escalera en U. El recorrido dispone de tres minutos y treinta segundos y aulas oscuras para ocultarse del conserje. Véase [Edificio nocturno](NIVEL01_EDIFICIO_NOCTURNO.md).

## Partida

Galindo recibe un mensaje: movieron sus cosas durante la limpieza. Tiene que investigar tres fuentes del pasillo, deducir el casillero de su USB, abrir un candado y regresar a la salida antes del cierre.

1. Una nota indica la fila y la combinación de tres símbolos.
2. Un compañero recuerda si el número es par o impar.
3. Un aviso delimita el intervalo de casilleros habilitados.

Las tres restricciones dejan exactamente una solución. Hay 17 casilleros en la fila Azul y 17 en la Verde. Cada partida vuelve a elegir objetivo, intervalo y combinación; el contenido de todas las pistas se genera a partir de esa misma ronda.

E o el botón sur investiga la fuente cercana. N o el botón Libreta permite consultar permanentemente las pistas recogidas. El candado ofrece Sol, Luna, Hoja y Estrella; se eligen tres en orden. La libreta sigue accesible desde el candado. Leer y resolver detiene el reloj; las sanciones sí descuentan tiempo.

## Riesgos y recompensas

- Casillero equivocado: penalización de casillero vacío de `U2GameConfig` (3 s por defecto).
- Volver a revisar uno equivocado: penalización de repetición (5 s por defecto).
- Combinación incorrecta: 4 s. Puede agotar el reloj y provocar derrota.
- Cada pista nueva suma 75 puntos; recuperar la USB conserva su recompensa.
- El resumen muestra pistas, casilleros equivocados, errores del candado, choques, tiempo y puntaje.
- Reconocimientos: Detective del pasillo, Sin equivocaciones y Sin choques.

El objetivo no entrega la USB hasta reunir las tres pistas y resolver el candado. Encontrarlo por azar antes de investigar no permite saltarse el puzzle. Al recuperar la USB cambia la música y tres alumnos de planta baja comienzan a marcharse por la salida. Los demás mantienen cruces, paseos y grupos con pausas; Q/LB conserva la petición aleatoria de paso. Cada planta tiene un director que intenta colocar hasta 13 alumnos, evitando superposición inicial.

## Escenario e integración

Ambas filas reutilizan sprites de casilleros existentes, con colliders e interacciones orientadas hacia su pasillo. Los casilleros están guardados en la escena. El misterio y los personajes se configuran al iniciar `LevelOneUpgrade`.

Se integra también el campus del nivel 3 desde develop, manteniendo las mejoras del nivel 1 y las dependencias compatibles con Unity 6000.3.25f1. La cámara del nivel 1 sigue al jugador en ambos ejes. Los pasillos, las ocho aulas, las escaleras y el exterior se guardan en **Grid_EdificioF**, con [Tilemaps editables](NIVEL01_EDIFICIO_F_GRID.md). `LevelOneNightSchool` registra los escondites sin reemplazar el diseño al jugar. El decorado antiguo permanece inactivo para evitar superposiciones.

Música del nivel 1 y del regreso: NIVEL1NUEVACANCION.mp3, seleccionada por el propietario. La música inicial y los efectos conservan sus atribuciones anteriores.

## Comprobaciones

El modelo puro se comprobó con 18 000 rondas y 644 004 verificaciones: unicidad de la respuesta deducida desde el texto, combinaciones correctas e incorrectas, casos de una sola casilla, filas desiguales y reproducción por semilla. Los detalles están en `Tests/Level1Mystery/README.md`.
