# HUD compacto del nivel 3

- Progreso en la esquina superior izquierda y reloj en la derecha. Pausa y Ayuda son botones pequeños junto al borde superior.
- Inventario centrado en el borde inferior, de 1060 × 96 unidades de referencia. Los nombres usan 23 puntos y el estado, 17.
- El profesor cercano (hasta 3.2 unidades) resalta su proyecto con borde azul. La selección conserva su color propio; no se cambia automáticamente el proyecto elegido.
- Durante un reto sólo aparece «Esquiva · N s», en una tarjeta pequeña bajo el progreso. La tarjeta de interacción se oculta durante el reto y al alejarse del profesor.
- La interacción muestra nombre, número de proyecto y «E: entregar» cuando corresponde. Se sitúa junto al borde inferior izquierdo; en móvil, en la zona central sobre el inventario.
- Impactos, errores y peticiones de paso usan un único aviso corto de 2.8 segundos, con entrada de 0.12 segundos y salida de 0.45. El fondo también desaparece. Su tiempo se detiene con la pausa.
- Los controles completos se muestran al inicio y se consultan con Ayuda. Ayuda detiene la partida y se cierra con su botón o Esc/Start.
- Recorrido, amonestaciones, impactos y descuentos se consultan en Pausa y en el resumen final.
- El minimapa ocupa 236 × 322 unidades, a la derecha bajo el reloj. Los letreros de profesores se muestran sólo cerca y se desvanecen al alejarse.
- Paneles y menús reducen la fuente un 15 % con un límite de 48 para títulos. El ajuste es idempotente: abrir opciones varias veces no vuelve a encoger los textos. El HUD compacto respeta sus tamaños propios.

Esta iteración conserva las mecánicas y penalizaciones. Se realiza una revisión conjunta de compilación, sin ejecutar pruebas de juego por cada cambio.
