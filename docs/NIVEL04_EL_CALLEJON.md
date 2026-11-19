# Nivel 4 · El callejón y el edificio F

Rama local: `nivel/04-cubiculo`.
Escena: `Assets/Scenes/Nivel04_ElCallejon.unity`.
Plataforma: PC, teclado y mouse.

## Recorrido

Galindo está con sus amigos en el callejón sur de Politécnico Nacional. Recibe en su celular el resultado REPROBADO de Bases de Datos, termina su cerveza y trepa por el tramo de malla que elija. La apertura conserva sus globos de diálogo y ambientación nocturna.

En el campus no quedan alumnos. Ocho guardias y cuatro perros patrullan y persiguen por visión o, en el caso de los perros, también por el ruido de correr. Los golpes los aturden temporalmente; no los eliminan. Los perseguidores buscan rutas sin atravesar los edificios y pierden el rastro si Galindo se aleja.

El objetivo es llegar al panel del edificio F. Galindo conecta un cable del celular y una terminal animada representa una conexión local ficticia con `admin / admin`. La alarma queda desactivada durante **180 segundos**. El reloj empieza al terminar esta acción, continúa durante transiciones y laptop, y se pausa con Esc.

Una transición negra lleva al interior. El pasillo usa piso blanco y gris, muros amarillos, columnas rojas, aulas cerradas, ventanas, casilleros, bancas y luz tenue, tomando las fotografías como referencia. Los baños y cubículos inferiores están cerrados. Las escaleras están al fondo. Arriba, la puerta de Torres está a la izquierda; las otras puertas permanecen cerradas.

Cinco sensores intermitentes bloquean tramos de los pasillos. Un haz rojo detecta movimiento; hay que esperar a que se apague para cruzarlo. Activar un sensor o agotar los tres minutos termina el intento.

El cubículo de Torres tiene proporción vertical, escritorio, hojas, libros, silla y laptop. Su interfaz provisional de SIGA incluye grupos, información y calificaciones: seleccionar **GIDS6093-E**, editar a Galindo y guardar **8**. Es una simulación dentro del juego; no utiliza servicios externos. La interfaz visual definitiva queda pendiente de la referencia que enviará el usuario.

Después hay que regresar por las escaleras y salir del F antes de que vuelva la alarma. Al salir se detiene el reloj. El regreso a la malla no tiene temporizador; Galindo puede salir por cualquier tramo del ancho jugable y terminar al reunirse con **Team Software**.

## Alumnos sólidos y conflictos

24 alumnos con 10 personajes distintos, divididos en Software, Redes, Entornos y Mecatrónicos. Tienen cuerpos cinemáticos y colliders sólidos en los pies. El motor del jugador usa barridos físicos para evitar atravesarlos.

- Caminar y rozarlos no provoca pelea.
- Primer choque corriendo o golpe: advertencia.
- Segundo choque cercano: empujón.
- Tercer choque dentro de una ventana de siete segundos: persiguen el afectado y uno de sus amigos.
- Un contacto continuo cuenta una sola vez: hay 0.95 segundos entre impactos válidos.
- Al alejarse más de siete unidades durante tres segundos, se calman y vuelven a su grupo.

Galindo tiene cinco puntos de resistencia. Recibir ataques genera empujón y una breve protección contra impactos consecutivos. Sin resistencia se puede reiniciar el nivel.

## Controles

- WASD o flechas: moverse.
- Shift: correr.
- Ctrl: caminar despacio.
- Espacio: golpe de alcance corto.
- E o botones de interfaz: continuar, trepar, conectar el celular, usar puertas, escaleras y laptop, o reunirse con Software.
- Esc: pausa.

## Edición y pruebas

Los interiores, marcadores, sensores y perseguidores están guardados bajo `MISIÓN · Edificio F, cubículos y vigilancia`, dentro del objeto del nivel 4. Los alumnos y las luces permanecen editables en su jerarquía original. El campus se reutiliza de una copia del nivel 3; las escenas anteriores y el renderizador compartido no se modifican.

Menú: `Ya Casi > Nivel 4 > Probar misión completa`.
Reporte: `Level4Work/VerificacionMision.txt`.
La prueba comprueba reglas de choques, cuerpos sólidos, reloj de 180 segundos, visión y aturdimiento, sensores, transiciones, grupo correcto, guardado del 8 y regreso a Software. Para recorrer los objetivos rápidamente, la automatización coloca al jugador junto a los puntos de interacción; la jugabilidad y equilibrio manuales pueden seguir afinándose.

Respaldos: `Level4Work/AntesDeMisionF.unity.bak` y el respaldo previo de la calle nocturna. Arte de alumnos y guardias: LimeZu, con licencia copiada en el proyecto. Perros, mobiliario provisional y elementos de interfaz se construyen localmente. Este avance no se ha subido a GitHub.

Última verificación: 75 comprobaciones aprobadas en Play. Incluye colisiones físicas con alumnos, combate, guardias, sensores, credenciales manuales, pausa y reloj de 180 segundos, recorrido del F, guardado del 8 y regreso a Software. También verifica pasto transitable, paso entre árboles, bases de troncos sólidas, esquinas de edificios y amplitud del nuevo interior. El recorrido automático coloca al jugador junto a objetivos; no sustituye una prueba manual de equilibrio.

## Pasto y rediseño del F

El pasto permite caminar libremente. Hay 91 bases de troncos con colisión independiente; las copas no bloquean el paso. Las mismas bases alimentan la navegación y el minimapa. Las fachadas mantienen colisión en los pies del jugador.

El F tiene un vestíbulo y una galería superior más amplios, piso cerámico blanco y azul, ventanales nocturnos, pilares rojos, zoclos, bancos, asientos, casilleros y señalización. Se conserva el cubículo amueblado de Torres y los sensores abarcan el nuevo ancho. Menú para regenerar: Ya Casi > Nivel 4 > Actualizar pasto y nuevo diseño F. Respaldo: Level4Work/AntesDePastoYVestibulo.unity.bak.


Cubículo de Torres: escritorio de roble en L con mamparas verdes, marco de aluminio, cajonera con tiradores, patas metálicas, laptop con teclado/pantalla/trackpad, mouse, café y apuntes. Colisión independiente del tablero y retorno. Verificación posterior: 75 comprobaciones aprobadas; laptop y misión completas funcionales. Captura: Level4Work/10-Torres.png.

Galerías amuebladas: módulos abiertos de oficina a ambos lados de planta baja y superior. Cada uno tiene mamparas verdes, mesa en L, cajones, computadora visible, silla, librero y apuntes. El centro queda libre y se conserva el acceso a Torres entre los módulos. El mobiliario lateral tiene colisión propia.

Distribución corregida: planta baja con aulas cerradas, puertas numeradas, pizarrones y seis pupitres con sillas por salón. Planta alta con recintos de cubículos al fondo, a ambos lados del pasillo, con directorio y placas de entrada; el mobiliario queda dentro de las oficinas, fuera del corredor. El acceso interactivo a Torres conserva la posición y la transición al cubículo detallado.

Entrada y salida de Torres: puerta interior abierta, jambas, umbral metálico, tapete con flecha y letrero SALIDA AL PASILLO. Aparición interior en (153, 2.35); al regresar al pasillo, Galindo se coloca 1.1 unidades a la derecha del marcador de la puerta para evitar quedar dentro del marco.

Dificultad aumentada: barra de vida animada de 100 a 0 (cinco impactos, 20 por golpe), verde/amarillo/rojo. Perros: velocidad 5.3, detección 8, oyen carrera a 11, pierden rastro tras 8 segundos. Guardias: velocidad 4, alcance 9 y memoria de 6 segundos. Recalculan persecución cada 0.4 segundos. Sensores activos 3.8 segundos y seguros 2.2 por ciclo de 6; reloj del F conserva 180 segundos. Golpear y empujar siguen disponibles.
