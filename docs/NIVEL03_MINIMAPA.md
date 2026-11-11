# Minimapa del campus

El nivel 3 crea una segunda cámara ortográfica fija con norte arriba. Renderiza una textura de 512 × 512 mostrada en una RawImage del HUD. Se encuadran todos los caminos y edificios; no se siguen los movimientos de la cámara principal.

La capa Minimap (índice 8) contiene solamente la geometría simplificada y los marcadores. La cámara principal la excluye; la cámara del mapa muestra únicamente esa capa. Los árboles, estudiantes, letreros y objetos lanzados no saturan esta vista.

- Flecha azul: Galindo, con orientación según su desplazamiento.
- Círculos dorados 1–5: profesores pendientes, en el mismo orden del inventario.
- Círculos verdes: proyectos entregados.
- M o el botón de la cabecera: ocultar/desplegar. Ocultarlo detiene la cámara sin recrear su textura.

El minimapa aparece debajo de la cabecera, a la derecha, sobre la zona del inventario. En móvil comienza plegado y usa un panel menor para conservar espacio para Esquivar.

La geometría de caminos y edificios se reúne en mallas sin colisiones. La cámara adicional no tiene AudioListener, luces propias ni postprocesado. Al salir del nivel se liberan la textura, las mallas y los objetos creados, y se restaura la máscara de la cámara principal.

Prueba automatizada en Unity: **Ya Casi > Nivel 3 > Verificar minimapa**. La evidencia se escribe en Tests/YC14/MinimapaUnity.txt. Comprueba encuadre, cámaras/capas, píxeles renderizados, seguimiento del jugador, actualización de profesores y entregas, pausa y plegado.

Verificado el 11 de noviembre de 2026 en Unity 6000.3.25f1: 27 comprobaciones aprobadas. La lectura de la textura confirma que hay caminos, profesores dorados y jugador azul visibles. También se revisó la colocación del panel en el campus final. La compilación completa de scripts pasó sin errores; la prueba automatizada utiliza la escena provisional.
