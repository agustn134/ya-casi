# YC18 — Contexto inicial del nivel 3

Se añade el panel breve solicitado por el propietario, sin cinemática. Aparece al entrar o reiniciar el campus y mantiene detenidos el reloj y los personajes hasta pulsar **Comenzar**. También permite abrir los controles y las reglas.

Texto: «Galindo necesita entregar los proyectos para aprobar las unidades de sus materias. Los profesores lo esperan en distintos edificios de la universidad. Debe llegar con el proyecto correcto antes de que cierre el sistema de entregas».

Se conservan las cinco entregas acordadas para YC14. Los proyectos y los avisos usan los nombres de los letreros de los profesores: Prof. Iridian, Prof. Barrientos, Prof. Muro, Prof. Torres y Prof. Barron. El inventario muestra `PROYECTO DE PROF. NOMBRE`.

## Edición en Unity

Seleccionar `Entregas_Nivel03` y revisar `Campus Delivery Objectives`:

- `Project Names Follow Professors`: activado para generar los nombres desde el letrero hijo de cada profesor (`CampusBuildingLabel > Building Name`). El menú contextual `Actualizar nombres desde profesores` actualiza las listas en el editor; también se sincronizan al iniciar la partida.
- Para nombres de proyectos independientes, desactivar esa opción y editar `Project Names`. Conservar `Project Ids`: son las claves del inventario.
- `Contexto narrativo inicial > Narrative Intro`: permite editar la historia. `Show Narrative Intro` controla la aparición del panel.

Esto cubre el contexto de entrada solicitado. **La issue #20 no se da por cerrada:** su mensaje de sistema cerrado al completar las entregas y su transición al cubículo siguen pendientes de implementación e integración con el nivel 4.
