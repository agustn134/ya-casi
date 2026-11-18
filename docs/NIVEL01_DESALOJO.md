# Cierre del edificio F

El nivel 1 tiene hasta 72 estudiantes, distribuidos con separaciones irregulares en tres carriles a lo largo de ambas plantas. Inician la salida con una espera de cero a cinco segundos; no recorren bucles cortos mientras esperan. Arriba, cada alumno tiene un lugar distinto en una fila de 1.65 unidades de separación, extendida por el pasillo. Solo quien está al frente entra a la escalera, con una separación mínima de cuatro segundos; al hacerlo, los demás avanzan a sus siguientes lugares. Recorren ambos tramos de la U, cambian de planta y continúan hacia la salida. Los carriles cruzan la puerta por posiciones distintas. Los alumnos desaparecen tras cruzarla. Conservan la petición aleatoria de paso y la evasión de obstáculos de CampusStudent.

El HUD indica «Choques: 0/3» en lugar de «Calma». Cualquier choque físico entre Galindo y un alumno descuenta dos segundos, incluso caminando o si el alumno lo alcanza. Hay dos segundos de protección entre descuentos para no contar varias veces el mismo contacto. El tercer choque conserva la derrota que antes dependía del contador de calma.

Los casilleros tienen placas grandes A-01…A-17 (Azul, planta baja) y V-01…V-17 (Verde, planta alta), además de letreros por grupos. La libreta explica esos códigos. El modelo de pistas y la validación del candado de símbolos no se modificaron.

Dos intendentes patrullan, uno por planta, y corren durante la persecución. Comparten el contador de capturas y los ocho segundos de protección: un encuentro no puede generar dos amonestaciones simultáneas. Primera captura: «El edificio está cerrando. Debes salir». Segunda: «Última advertencia, Galindo». Tercera: expulsión y derrota identificada en el resumen. Las primeras dos capturas conservan la USB y las pistas, trasladan a un punto seguro y dan protección temporal. El HUD muestra los avisos acumulados y el estado del vigilante de la planta actual.

La profundidad compara los pies de Galindo, alumnos e intendentes con la base de cada mueble. Las paredes se separan por filas de renderizado al jugar, conservando el Grid editable original. Los papeles quedan sobre el suelo y debajo de los personajes. La ambientación añade mochilas, papeleras y sillas apartadas con sprites existentes de ThirdParty, configurados en Resources/LevelOneAmbience.asset. Las papeleras y las sillas tienen colisión en su base.

El aviso de umbral oscuro solo aparece en los huecos de las ocho puertas. El escondite se restringe al interior de cada aula; estar junto a una pared exterior no cuenta como salón.

Las puertas de los ocho salones giran al acercarse Galindo y se cierran al alejarse. La animación es automática, sin agregar otra tecla; sus marcos y el paso de las aulas conservan la geometría estática. El cambio de planta añade un fundido con indicación de subida o bajada. La cámara sigue centrada y se aproxima a 4.3 unidades ortográficas.

Shift se consulta mientras está físicamente pulsado y se borra el estado de entrada al perder el foco o desactivar el controlador. La carrera se desactiva durante la pausa. Se reutiliza el atlas Redaction 35 del proyecto para el nivel y el menú principal.

El menú muestra niveles 1–4 en orden. Solo activa escenas incluidas en la compilación, excluyendo pruebas. En esta rama solo están integrados los niveles 1 y 3; 2 y 4 aparecen como «Próximamente». Continuar abre la última escena de nivel jugada, guardada en YaCasi.LastPlayedScene.

Comprobación realizada: compilación C# y ejecución en Unity durante 35 segundos con 72 alumnos iniciales; quedaron 60 tras cruzar la salida. Se verificaron dos intendentes y que el punto exterior junto a la pared de planta alta no produce aviso de salón. Evidencia en `docs/evidencia/Nivel01_Desalojo.txt`. No equivale a una partida completa ni comprueba todos los encuentros posibles entre multitudes y jugador.

Comprobación adicional de la fila superior: en 110 segundos descendieron 21 de sus 36 alumnos y quedaron tres alumnos en la zona de entrada a la escalera. Evidencia en `docs/evidencia/Nivel01_FilaPlantaAlta.txt`. Se comparó el método SubmitCombination con la versión anterior: sin cambios.

## Más presión junto a los casilleros

Dos intendentes por planta (cuatro en total) patrullan en direcciones contrarias, vuelven por el pasillo al llegar a sus extremos y comparten el contador de advertencias. El HUD muestra el vigilante más alerta de la planta.

Un tercio de los alumnos recorre temporalmente circuitos frente a los grupos de casilleros y cruza hacia el carril interior en ambos sentidos. Después se incorpora al desalojo. Sus posiciones futuras en la fila superior se reservan desde el inicio para evitar que los recién llegados desplacen a los que ya esperan.

Esta ampliación se implementó sin ejecutar compilación, comprobaciones ni partidas de prueba, por solicitud del propietario.

## Presión de vigilancia y tiempo

Tiempo inicial: 210 segundos (3:30), como ajuste inicial de dificultad, no como mínimo demostrado. Se cambia en LevelOneNightSchool.roundSeconds. Visión de los conserjes: alcance 10 y ángulo 90 grados. Correr de forma sostenida durante 3.5 segundos puede iniciar una persecución por ruido a una distancia de hasta 14 unidades de la misma planta. La persecución dura hasta ocho segundos tras el último estímulo; entrar al aula y dejar de correr permite ocultarse.

El lateral derecho indica dirección, cercanía y persecución del conserje más próximo de la planta. La planta alta dedica la mitad de sus alumnos a circuitos ante casilleros, hasta entre 75 y 103 segundos antes de incorporarse a la fila de salida.

Este ajuste se entrega sin compilación ni pruebas, por solicitud del propietario.
