# Navegación entre niveles — 27 de noviembre de 2026

## Cambios

- Pantallas de victoria con continuar al siguiente nivel, reiniciar y volver al menú.
- Recorrido explícito 1 → 2 → 3 → 4; el nivel 3 deja de marcar el final de campaña.
- Nivel 1: las opciones aparecen desde la primera pantalla de victoria; se elimina el salto automático al resumen.
- Nivel 4: cierre de campaña, reinicio y menú.
- La opción Continuar conserva el siguiente nivel disponible tras una victoria.
- Las escenas de prueba y respaldo quedan fuera de la selección de siguiente nivel.

## Validación real

Compilación de 71 archivos C# del proyecto activo con Roslyn de Unity 6000.3.25f1: código de salida 0, sin errores. Advertencias de campos serializados preexistentes. Revisión del diff sin errores de espacios en los archivos modificados. Las capturas aportadas por el usuario muestran victorias de niveles 2 y 3 antes de esta corrección. No se ha realizado un recorrido completo posterior a la corrección ni una compilación Windows en esta sesión.

## Issues relacionadas y cierre solicitado

El propietario solicita relacionar y cerrar en el PR todos los issues abiertos de niveles 2 y 4. En la consulta actual no hay issues abiertos del nivel 2; los 14 abiertos están asignados al sprint 4. Las referencias de cierre del PR ejecutan esa decisión de administración al fusionar. No significan que se hayan cumplido todos los criterios originales.

Se conserva la revisión histórica `docs/REVISION_ISSUES_2026-11-18.md`. Siguen sin comprobarse en esta sesión: video de cobertura (#13), llave/nota y sospecha acumulada del diseño original (#17–#19), audio y fuentes en el recorrido completo (#21), alfa/beta Windows y matriz completa (#22–#23), tráiler/presentación (#24), tres usuarios externos (#25), balance y regresión completos (#26, #28), documentación final y publicación/entrega académica (#27, #29–#30). El PR no aporta evidencia de estas actividades. El alcance actual del nivel 4 usa terminal admin/admin, sensores, registro de 8 y regreso con Software.

Issues: #13, #17, #18, #19, #21, #22, #23, #24, #25, #26, #27, #28, #29, #30
