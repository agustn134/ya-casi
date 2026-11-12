# Nivel 1: interfaz y cortesía

La escena `Nivel01_CaosDelCasillero` conserva la búsqueda aleatoria de la USB y el regreso a la salida. `Level01Manager` crea `LevelOneUpgrade` al iniciar; los cambios se montan automáticamente sobre la escena existente.

- Objetivo y progreso de la USB arriba a la izquierda; reloj, pausa y ayuda arriba a la derecha.
- Inventario con silueta de memoria USB y calma abajo a la izquierda.
- Indicación del casillero únicamente dentro del alcance de interacción. El contorno identifica el casillero seleccionado; los revisados conservan una marca pequeña al acercarse.
- Un solo aviso temporal, visible 2.8 segundos con desvanecimiento final. No se mantienen las franjas del HUD anterior.
- Fuente Redaction 35 Regular compartida con el campus, con su licencia SIL OFL incluida.
- Ayuda inicial con contexto de Galindo y controles en líneas separadas; vuelve a consultarse con Ayuda o F1. Pausa y ayuda detienen el reloj.
- Estadísticas de casilleros, choques, descuentos y puntaje en pausa y resumen. La victoria presenta «Nivel superado» antes del resumen.

## Controles

| Acción | Teclado | Mando |
| --- | --- | --- |
| Caminar | WASD / flechas | Stick |
| Correr | Shift | Gatillo |
| Revisar | E | Botón sur |
| Pedir paso | Q | LB |
| Pausa | Esc | Start |
| Ayuda | F1 / botón Ayuda | Botón Ayuda |

En móvil se añaden controles táctiles y un botón «Con permiso».

## Pedir paso

Cada alumno cercano tiene una probabilidad del 45% de aceptar; se apartan como máximo dos por petición dentro de un radio de 2.5 unidades. Hay 3 segundos entre peticiones y 5.5 segundos entre intentos sobre el mismo alumno. Si encuentra espacio libre, se aparta hasta una unidad durante 2 segundos y retoma su ruta. Los desplazamientos consideran mobiliario, paredes, jugador y otros estudiantes.

Las reglas de tiempo, casilleros, puntuación y calma conservan los valores de `U2GameConfig`. Pedir paso no garantiza despejar el pasillo y no sustituye la penalización por chocar al correr.

Los cambios de este documento corresponden a la rama `nivel/01-caos-del-casillero`.

## Compatibilidad del editor

El proyecto usa Unity **6000.3.25f1 (6.3 LTS)**, como indica `ProjectSettings/ProjectVersion.txt`. Los paquetes de `Packages/manifest.json` y `Packages/packages-lock.json` están alineados con ese editor: Animation 13.0.6, Common 12.0.4, Aseprite 3.0.2, PSD Importer 12.0.2, SpriteShape 13.0.0, Tilemap Extras 6.0.3, Tooling 1.0.4, URP 17.3.0 y UGUI 2.0.0.

El módulo `com.unity.modules.physicscore2d` pertenece a una generación posterior y se eliminó de las dependencias. Las versiones 2D más recientes que tenía esta rama producían errores dentro de `InternalEngineBridge.cs` y `RuleOverrideTile.cs`; la corrección se hace en los archivos de paquetes del proyecto, conservando la caché como contenido generado por Unity.
