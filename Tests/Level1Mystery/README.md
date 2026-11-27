# Reglas de «El casillero equivocado»

`MysteryRoundChecks.cs` se ejecuta como consola C# junto con `Assets/Scripts/LockerMysteryRound.cs`, sin depender del motor Unity.

Comprueba 18 000 rondas en nueve distribuciones de casilleros, incluyendo filas de un solo casillero y filas con tamaños diferentes. Deduce la respuesta leyendo las pistas escritas y verifica que señalen exactamente un casillero con USB. También comprueba secuencias correctas, símbolos incorrectos en cada posición, códigos incompletos, coordenadas inválidas, configuraciones imposibles y reproducción por semilla.

Resultado de la ejecución: **18 000 rondas, 644 004 comprobaciones correctas**.

Las filas usan índices desde cero (`Azul = 0`, `Verde = 1`); los números de casillero comienzan en uno. Los cuatro símbolos son `Sol`, `Luna`, `Hoja` y `Estrella`.
