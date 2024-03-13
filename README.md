# Proyecto de Conducción Autónoma para CETEDEX

## Generación de rutas transitables por robots a partir de modelos 3D en escenarios no estructurados

> Desarrollado en el GGGJ (Grupo de Gráficos y Geomática de Jaén)

[Demo de Prototipo del Generador de Trayectos 🗺](https://github.com/FreyzerFault/Proyecto-Campo/releases/tag/Release)

[Repositorio del Generador de Terreno ⛰](https://github.com/FreyzerFault/Procedural-Terrain)

## Introducción

### Entorno del problema

- Terrenos de olivar.
  - Escaneadas con LiDAR.
  - Generados Proceduralmente.
- Terrenos forestales y de montaña.
  - Generados Proceduralmente.
- Terreno de pendiente variable.
- Espacio no estructurado (sin rutas definidas).
- Obstáculos naturales.

### Restricciones

- Vehículo no holónomo => grados de libertad restringido (vehículo 4 ruedas - convencional)
- Potencia del vehículo limitada (pendiente máxima)
- Escenario no estructurado
- Espacio acotado (limitado a una zona)

## Pathfinding

Algoritmo utilizado:
A\* (Algoritmo de búsqueda del trayecto óptimo)

Suponiendo que la altura del terreno es muestreable, se genera un espacio discreto y navegable, donde se realiza una búsqueda del trayecto más óptimo.

### Mejoras adicionales

- Penaliza giros innecesarios o excesivamente cerrados, obteniendo un trayecto suave
- Penaliza posiciones de mayor pendiente, buscando mayor estabilidad y menor riesgo para el vehículo.

## Mejoras de Postprocesado al Trayecto Generado

### Suavizar el trayecto con B-Splines (curvas suaves)

### RRT\* (Rapidly-Exploring Random Tree)

Variación del A\* que no necesita un espacio predefinido discreto.
Realiza la búsqueda en un espacio continuo. Una alternativa más flexible y muy utilizada en la conducción autónoma de vehículos holónomos.

> ¿Qué aporta al Proyecto?
>
> Nos permite generar un trayecto óptimo a la vez que cumple con las restricciones de movimiento del vehículo.

En el caso del algoritmo utilizado, el A*, necesita un postprocesamiento del trayecto resultante a uno válido (utilizando curvas), o mecanismos auxiliares con el controlador PID o el algoritmo de seguimiento de trayectorias.
El RRT* podría evitar el postprocesamiento y facilitar el seguimiento del vehículo.

[Video Demo del RRT\*](https://www.youtube.com/watch?v=1WZEQtg8ZZ4&t=62s)
[Video Demo del RRT\* aplicado a la conducción autónoma](https://www.youtube.com/watch?v=6Pngam882hM)

## Modelo del Vehículo virtual planteado

### Algoritmos de Seguimiento del Camino en Tiempo Real

Paper sobre distintos algoritmos usados en conducción autónoma:
[Steering Behaviors For Autonomous Characters - Craig Reynolds](https://www.red3d.com/cwr/steer/)

Demo del algoritmo más básico de seguimiento de un camino:
[Path Following Demo - The Nature of Code - The Coding Train / Daniel Shiffman](https://editor.p5js.org/codingtrain/sketches/dqM054vBV)

### Algoritmo Dubins y Reeds-Shepp

Muy utilizado en conducción autónoma.
A partir de un punto inicial y final, calcula la combinación de movimientos óptima, restringidos a los posibles en un vehículo holónomo con un radio de giro máximo.

> Reeds-Shepp es una mejora del Dubins añadiendo la marcha atrás.

["Shortest Path for the Dubins Car" - Wolfram Demonstrations Project - Aaron T. Becker and Shiva Shahrokhi](https://demonstrations.wolfram.com/ShortestPathForTheDubinsCar/)

["Shortest Path for Forward and Reverse Motion of a Car" - Wolfram Demonstrations Project - Francesco Bernardini and Aaron T. Becker](https://demonstrations.wolfram.com/ShortestPathForForwardAndReverseMotionOfACar/)

### Controlador PID

Mecanismo de control que a través de un lazo de retroalimentación permite regular unas variables en función del error respecto al estado objetivo.

En nuestro caso, ajustando la velocidad lineal y angular del vehículo, reduciendo el error al regular la aceleración y giro del vehículo, funcionando como una abstracción entre el estado deseado (velocidad) y los posibles movimientos del vehículo (aceleración, frenado y giro).

[García, Diego Antonio & Bravo, Fernando & Cuesta, Federico & Ollero, Anibal. (2010). Planificación de Trayectorias con el Algoritmo RRT. Aplicación a Robots No Holónomos.](https://www.researchgate.net/publication/28141977_Planificacion_de_Trayectorias_con_el_Algoritmo_RRT_Aplicacion_a_Robots_No_Holonomos)
