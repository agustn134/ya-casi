# Nivel 3: profesores, entregas y lectura

## Cambios de esta iteración

- Los nombres de los profesores se dibujan por encima de los personajes, con separación de la cabeza, tamaño contenido y fondo ajustado. Conservan los nombres editados en la escena.
- Los cinco profesores patrullan una zona transitable de hasta 3.6 unidades alrededor de su punto inicial. Usan rutas locales, colisiones y animaciones en cuatro direcciones; se detienen cerca de Galindo para atenderlo.
- Acercarse a un profesor pendiente activa su reto. Los encuentros normales duran 8 segundos con tres oleadas; la última entrega exige el reto final de 16 segundos con seis oleadas. El proyecto sólo se consume al entregarlo con E después del reto.
- El primer encuentro explica las reglas en un diálogo que detiene el reloj. Los siguientes se anuncian sin interrumpir la partida. El papel sale desde el profesor y su trayectoria se anuncia antes de lanzarse.
- El progreso de cada reto se conserva al cambiar de profesor. Fuera de un radio de 8 unidades del profesor activo, el reto espera; el reloj general sigue corriendo. El último profesor requiere la versión final aunque se haya visitado antes.
- El inventario muestra sólo el nombre del profesor, el número de acceso y el estado (pendiente, seleccionado o entregado). Los iconos son sprites de papel de LimeZu, reutilizados desde Modern Exteriors y guardados con recorte de sprite en Resources; sustituyen los rectángulos de colores generados por la interfaz.
- Reloj, estadísticas, interacción y ayudas usan saltos de línea. Las teclas conservan sus mayúsculas y el resto usa escritura normal.
- La quinta entrega abre un panel «¡Nivel superado!» y «Proyectos entregados: 5/5». El jugador puede leerlo antes de abrir el resumen o volver al menú.

## Con permiso

Q, LB del mando o el botón móvil «Permiso» permiten pedir paso. La petición alcanza 2.8 unidades, tiene recarga de 3 segundos y cada alumno tiene un 45 % de probabilidad de responder. Como máximo dos se apartan por petición y sólo lo hacen si encuentran espacio transitable sin atravesar otras colisiones. Cada alumno dispone además de una recarga propia de 5.5 segundos.

Quienes aceptan se apartan durante unos 2.1 segundos y luego retoman la patrulla. Se conserva el destino compartido de su grupo. La petición no garantiza que el pasillo quede libre.

## Revisión

La compilación conjunta de los scripts del juego y del editor terminó sin errores. Por petición del propietario, no se ejecutaron pruebas de juego en esta iteración. Los registros anteriores de pruebas corresponden a las versiones y fechas indicadas en ellos.
