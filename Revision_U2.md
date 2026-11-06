# Ya Casi — revisión de la Unidad 2

Fecha de trabajo: 30 de octubre de 2026. Entrega final indicada en el instrumento: 27 de noviembre de 2026.

## Cambios del nivel 1

- Música de fondo: **Pixel art — Brutaldesign**, facilitada por Agustín desde [Pixabay](https://pixabay.com/es/music/electr%C3%B3nico-pixel-art-481480/). Reproducción en bucle y volumen independiente de los efectos.
- Ronda de **135 segundos**, sin ampliar el tiempo automáticamente por la cantidad de casilleros.
- Una USB elegida aleatoriamente por ronda.
- Casillero vacío: **−3 s**. Repetir un casillero revisado: **−5 s**, con intervalo mínimo entre pulsaciones y marca «OK» para evitar confusiones.
- Seis estudiantes recorren el pasillo; sus cuerpos actúan como obstáculos. Caminar permite maniobrar sin perder calma; chocar corriendo resta **2 s** y **1 de calma**, con protección de dos segundos entre impactos. Con cero de calma termina la ronda.
- Puntaje por revisiones nuevas y por encontrar la USB; penalización de puntaje por repetir. Al ganar, bonificación por tiempo y calma restantes. Mejor marca guardada localmente.
- Escena independiente **MenuPrincipal**, con presentación inicial, Jugar, Continuar, Opciones, Créditos y Salir. Continuar abre el nivel disponible desde el principio; no recupera una ronda a mitad de partida.
- Opciones separadas de música y efectos, calidad gráfica y explicación de controles.
- Tutorial, pausa con reanudar/reiniciar/opciones/menú y resultados de victoria/derrota con reintento y regreso al menú.
- Teclado, controles de mando y controles táctiles previstos en el código. Mando y móvil requieren comprobación con dispositivo real.

## Justificación del ajuste de diseño

La dificultad anterior se concentraba en el azar. La revisión ahora exige recordar qué casilleros se revisaron y elegir cuándo caminar o correr entre estudiantes. Las marcas visibles y el tutorial comunican las penalizaciones antes de aplicarlas. Los valores iniciales necesitan una prueba de balance con usuarios: no se presentan como dificultad definitiva.

## Qué falta para cumplir todo el instrumento

| Requisito | Situación |
|---|---|
| Nivel 1 con menú y cuatro estados | Implementado; ver registro de pruebas para la validación efectiva. |
| Cuatro niveles jugables por equipo | Pendientes los niveles 2, 3 y 4; no se simula su existencia con botones. |
| Progresión y siguiente nivel | Limitada al nivel 1 disponible. Falta integrar los niveles restantes y su desbloqueo. |
| Publicación web con URL jugable | Módulo WebGL instalado durante esta intervención. Falta confirmar la compilación, alojarla y probar la URL pública. |
| APK Android y pruebas físicas | Pendiente. Falta el módulo Android y verificar con un teléfono. |
| Ejecutable Windows | Solo puede darse por entregado si existe una compilación correcta y se prueba. |
| Música, efectos y créditos | Música y créditos integrados; conservar fuente/licencia. Revisar también los créditos de los efectos previos. |
| Manual, GDD y bitácora | Este documento registra el cambio; falta incorporarlo al GDD y a la entrega académica completa. |
| Evidencia, video y trabajo de ambos integrantes | Pendiente de registrar trabajo real de cada integrante y grabar evidencia. |
| Balance y rendimiento | Requiere jugar rondas completas, incluida USB en el último casillero, y probar PC/Web/móvil. |

Desarrollo del nivel 1: Agustín López Parra. Carlos Manuel Palma Muñoz figura como revisor del nivel 1, conforme a lo indicado por Agustín.

## Dónde ajustar y probar

1. Abrir `Assets/Scenes/MenuPrincipal.unity` y pulsar Play para probar el flujo completo.
2. Para ir directamente al nivel, abrir `Assets/Scenes/Nivel01_CaosDelCasillero.unity`.
3. Ajustar tiempo, penalizaciones, cantidad de estudiantes y música en `Assets/Resources/U2GameConfig.asset`.
4. El menú de la actividad de clase se conserva en `Assets/ActividadMenu/Scenes/MenuActividadCompleta.unity`.
5. Antes de modificar archivos se guardó un respaldo en esta carpeta de entrega.

No se ha publicado nada ni enviado cambios a GitHub durante esta intervención.
