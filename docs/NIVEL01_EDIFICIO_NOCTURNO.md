# Nivel 1: edificio nocturno

Escena: `Assets/Scenes/Nivel01_CaosDelCasillero.unity`. Rama: `nivel/01-caos-del-casillero`. Conserva la USB, la libreta, las tres pistas y el candado del misterio.

El diseño visual vigente está guardado en **Grid_EdificioF**, con Tilemaps editables inspirados en las fotos del edificio F. Consulta [Arquitectura del edificio F](NIVEL01_EDIFICIO_F_GRID.md) para pintar paredes, pisos y exterior.

## Recorrido

- Dos plantas separadas por 32 unidades de altura en el mapa, con pasillos de aproximadamente 82 unidades de largo.
- La fila Azul, con sus 17 casilleros, está en planta baja. La fila Verde y sus 17 casilleros están arriba.
- Cuatro aulas por planta, numeradas 101–104 y 201–204. Puertas abiertas con huecos transitables, pupitres, sillas, pizarrones y zonas oscuras.
- Nota en el salón 101; compañero en el pasillo del segundo piso; aviso en el salón 203.
- La salida sigue en el extremo izquierdo de planta baja. Solo permite ganar con la USB.
- Tiempo inicial: cinco minutos, ajustable en `LevelOneNightSchool.roundSeconds`.

Los gráficos usan suelo, paredes y escalones de Room Builder de LimeZu y muebles de los recursos ThirdParty existentes. La arquitectura utiliza `Assets/Resources/LevelOneBuildingPalette.asset`; puertas y mobiliario conservan las referencias de `Assets/Resources/LevelOneNightPalette.asset`.

## Escalera en U

Está en el extremo derecho de ambos pasillos. El jugador entra al tramo izquierdo, asciende hasta el descanso, gira hacia el tramo derecho y lo recorre hasta el final. Solo completar esa secuencia cambia de piso. El descenso usa la escalera del segundo piso. Hay una transición de 0.36 segundos y protección contra transportes encadenados.

Los tramos tienen tres unidades de ancho y un separador con collider. Se conserva el seguimiento centrado de la cámara al cambiar de piso. La transición no detiene el reloj.

## Conserje y escondites

Un único conserje patrulla el edificio con un cono de linterna visible. Su visión respeta paredes, casilleros y pupitres. El cono tiene seis unidades de alcance y 65 grados; necesita ver al jugador durante un segundo para iniciar una persecución.

Correr cerca o revisar un casillero equivocado genera ruido. El conserje investiga el punto del pasillo más cercano. Si cambia de piso, tarda al menos seis segundos y no llega por la escalera si el jugador está demasiado cerca de ese lugar.

Para esconderse basta entrar a un salón, alejarse del marco de la puerta y dejar de correr. El HUD confirma «Estás oculto». El jugador sigue pudiendo caminar y leer las pistas. Se reduce su luz en las zonas oscuras; no es necesario pulsar una tecla adicional.

Al ser alcanzado pierde ocho segundos, vuelve a un punto cercano del pasillo y obtiene ocho segundos de protección. Conserva todas las pistas, la combinación y la USB. Si la sanción agota el tiempo, se mantiene la derrota normal.

La IA, la sospecha y la persecución respetan la pausa, la libreta y el candado. La vigilancia no se añade a otros niveles.

## Generación y comprobaciones

`LevelOneNightSchool.authoredLayout` referencia el Grid guardado en la escena. Si existe esa referencia, solo registra los escondites y no reconstruye el decorado. Las ediciones manuales de los Tilemaps se conservan. El Grid y el decorado antiguos se mantienen inactivos para evitar colisiones duplicadas; el generador anterior queda como respaldo para escenas sin arquitectura guardada.

Se comprobaron los accesos de las ocho aulas y los puntos de pistas contra los colliders estáticos. Las escaleras se muestrearon en 3 134 posiciones por planta, con un margen de 0.3 unidades y el desplazamiento de los pies del personaje: ambos recorridos y sus destinos quedan libres.
