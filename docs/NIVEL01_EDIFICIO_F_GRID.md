# Edificio F: arquitectura editable del nivel 1

El escenario se inspira en las fotos del edificio F de la UTNG: pared crema con zoclo verde, columnas rojas, ventanas altas oscuras, suelo marfil y dos carriles gris azul con flechas. Se reutilizan las piezas de Room Builder de LimeZu y el mobiliario de ThirdParty.

## Editar en Unity

Abre `Assets/Scenes/Nivel01_CaosDelCasillero.unity` y expande **Grid_EdificioF**. Tiene una celda de una unidad y un desplazamiento de media celda para alinear las juntas con los muros.

- **Planta baja** y **Segundo piso**: Tilemaps `Pisos`, `Carriles`, `Muros_crema_verde`, `Columnas_rojas` y `Ventanas_oscuras`.
- **Exterior**: Tilemaps de pasto, andadores, bordes de jardineras y volúmenes de techos.
- **Mobiliario y señalización**: puertas abiertas, pupitres, sillas, bancas, árboles, arbustos, flechas y letreros.
- **Colisiones · bases de muros y muebles**: cajas independientes del dibujo de las paredes. No añadas un TilemapCollider2D a toda la cara de pared de dos celdas: invadiría el espacio transitable.

Los Tile assets están en `Assets/Art/Level1/EdificioF`. Se pueden arrastrar a una Tile Palette o usar la paleta de interiores existente. La selección de sprites está en `Assets/Resources/LevelOneBuildingPalette.asset`.

Las paredes usan dos piezas completas apiladas, con esquinas y laterales; sus sprites no se aplastan a una línea. Las caras norte crecen hacia el fondo y las del sur hacia el exterior, para no ocultar los casilleros ni al jugador.

## Conservación del diseño

`LevelOneNightSchool.authoredLayout` apunta al Grid guardado. Al entrar en Play solo registra los escondites y activa las mecánicas: no vuelve a pintar ni reemplaza los Tilemaps. Los ajustes hechos a mano se conservan al guardar la escena.

El menú **Ya Casi → Nivel 1 → Construir edificio F en Grid** vuelve a crear ese Grid desde la paleta. Es una herramienta explícita para restaurar el diseño base: reemplaza sus ediciones manuales. Antes de construir guarda una copia en `Temp/Nivel01_AntesDeEdificioF.unity`.

## Recorrido

Se mantienen los dos pisos separados por 32 unidades, ocho aulas, 34 casilleros, pistas, candado y conserje. Las puertas de las aulas tienen tres unidades libres y el acceso a la escalera conserva su hueco original. El cambio de piso sigue exigiendo recorrer ambos tramos y el descanso de la U.

La herramienta comprueba los accesos de las ocho aulas y el recorrido de la escalera contra las bases estáticas del nuevo diseño. Exporta vistas del escenario a `Temp/EdificioF_Detalle.png` y `Temp/EdificioF_Conjunto.png`; esas imágenes son vistas del escenario, no una prueba de partida completa.
