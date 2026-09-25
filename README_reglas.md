# UNO — proyecto en C#

Juego gráfico de UNO para cuatro jugadores humanos en una sola computadora. Cada persona juega por turnos con su propia mano. El proyecto guardará jugadores, resultados y movimientos en una base de datos.

> Estado: planificación y desarrollo inicial. Este README describe el comportamiento acordado; una regla descrita aquí no se considera implementada hasta que el equipo la haya programado y probado.

## Alcance y referencia de reglas

Usaremos la edición clásica de **108 cartas** documentada por Mattel en [UNO Game — instrucciones W2085](https://service.mattel.com/instruction_sheets/W2085-UNO.pdf). Esto fija una edición concreta: no se incluyen las cartas especiales de otras versiones, cartas personalizables ni reglas caseras. Si el profesor indica otra edición, actualizaremos este documento antes de cambiar el código.

Una **ronda** acaba cuando alguien se queda sin cartas. Una **partida completa** acumula rondas y termina cuando un jugador alcanza **500 puntos**. Registraremos el resultado de la partida completa y, si resulta útil, también el de cada ronda por separado.

## Baraja e inicio

- Hay 108 cartas: por cada color (rojo, amarillo, verde y azul), un 0, dos copias de cada número del 1 al 9, dos `+2`, dos `Reversa` y dos `Salto`; además, cuatro `Comodín` y cuatro `Comodín +4`.
- Se mezclan las cartas y se reparten siete a cada uno de los cuatro jugadores. El resto forma el mazo de robo; se voltea una carta para iniciar el descarte.
- El primer jugador es quien sigue al repartidor. Si la carta inicial es `+2` o `Salto`, se aplica su efecto; si es `Reversa`, comienza el repartidor y cambia el sentido; si es `Comodín`, el primer jugador elige color; si es `Comodín +4`, se devuelve al mazo y se voltea otra carta.
- Al comenzar una ronda nueva, se vuelve a mezclar y repartir. El repartidor pasa al siguiente jugador.

## Jugada normal

Una carta numerada o de acción se puede jugar cuando coincide con el **color activo**, el número o el símbolo de la carta superior. Tras un comodín, el color activo es el que eligió quien lo jugó; no se usa un supuesto color propio del comodín.

Si no se juega una carta, se roba **una**. También se puede elegir robar aunque haya una carta jugable en la mano. Si la carta recién robada se puede jugar, se permite jugar **esa carta** inmediatamente; no se permite jugar otra carta de la mano después de robar. Si no se juega la carta robada, termina el turno.

## Efectos de las cartas

| Carta | Efecto |
| --- | --- |
| `+2` | El siguiente jugador roba dos cartas y pierde su turno. |
| `Reversa` | Invierte el sentido de los turnos. Con cuatro jugadores no equivale a un salto. |
| `Salto` | El siguiente jugador pierde su turno. |
| `Comodín` | Quien lo juega elige el color activo. Puede jugarse aunque tenga otra carta válida. |
| `Comodín +4` | Quien lo juega elige color; el siguiente jugador roba cuatro y pierde su turno, salvo que desafíe con éxito. |

El `Comodín +4` **solo es legal si quien lo juega no tiene una carta del color activo anterior**. Tener una carta del mismo número o símbolo no impide usarlo. El jugador afectado puede desafiarlo antes de robar: si fue ilegal, quien jugó el `+4` roba cuatro; si fue legal, quien desafió roba seis. El jugador afectado pierde su turno en ambos casos. La comprobación se hace con la mano que tenía el jugador al colocar el `+4`.

No se apilan penalizaciones: al recibir un `+2` o `+4`, el jugador afectado no puede responder con otro `+2` o `+4` para pasar la penalización.

## UNO, final de ronda y puntuación

- Al jugar la penúltima carta, el jugador debe declarar **UNO**. Si no lo hace y otro jugador lo señala antes de que empiece el siguiente turno, roba dos cartas.
- Cuando alguien se queda sin cartas, termina la ronda. Si la última carta fue `+2` o `Comodín +4`, se aplica primero el robo correspondiente y esas cartas cuentan en la puntuación.
- El ganador de la ronda recibe los puntos de todas las cartas que conservan sus rivales: cartas numéricas, su valor; `+2`, `Reversa` y `Salto`, 20 puntos; `Comodín` y `Comodín +4`, 50 puntos.
- Las rondas continúan hasta que alguien acumule al menos 500 puntos. Esa persona gana la partida; los otros tres registran una derrota.
- Si se agota el mazo de robo, se conserva la carta superior del descarte, se mezclan las demás cartas descartadas y se forma un mazo de robo nuevo.

## Flujo en una sola computadora

La interfaz muestra la carta superior, el color activo, el sentido y el turno. Solo revela la mano del jugador actual. Antes de pasar el control al siguiente jugador, oculta la mano y muestra una pantalla de transición. Debe ofrecer acciones para jugar una carta, robar, elegir color, declarar UNO y desafiar un `+4` cuando corresponda.

## Datos que debe guardar el programa

- Identificador o nombre de los cuatro jugadores.
- Partidas completas, ganador y resultado de cada participante (ganada o perdida).
- Log cronológico con jugador, ronda, turno y acción: carta jugada, robo, color elegido, desafío, penalización, declaración de UNO y fin de ronda/partida.

El esquema definitivo y su diagrama entidad–relación se documentarán en el reporte. La interfaz, el motor de reglas y el acceso a datos se mantendrán separados para poder probar las reglas sin abrir el formulario.

## Organización del equipo

| Área | Entregable |
| --- | --- |
| Reglas y modelo | Baraja, validación de jugadas, turnos, efectos, puntuación y pruebas de casos especiales. |
| Interfaz | Pantallas de juego, mano privada, botones y visualización del estado. |
| Base de datos | Jugadores, partidas, resultados, movimientos y diagrama entidad–relación. |
| Integración y reporte | Integrar ramas, probar partidas completas y preparar el documento solicitado. |

Cada integrante trabaja en su rama, realiza commits con su cuenta y abre un pull request hacia `main`. El reporte incluirá los commits dentro del intervalo que pide el profesor, la imagen de las ramas generadas y los problemas encontrados con sus soluciones.

## Casos mínimos para comprobar las reglas

1. Carta del mismo color, número o símbolo: aceptada; carta sin coincidencia: rechazada.
2. Robo voluntario: solo se permite jugar la carta recién robada.
3. `Reversa` cambia el siguiente jugador según el sentido actual; `Salto` omite exactamente uno.
4. `+2` hace robar dos y perder turno, sin permitir apilar.
5. `+4` legal e ilegal, con desafío exitoso y fallido; registrar el color activo previo.
6. Penalización por no declarar UNO dentro del plazo correspondiente.
7. Última carta `+2` o `+4`: aplicar robo antes de contar puntos.
8. Mazo agotado: reciclar descartes sin incluir la carta superior.
9. Tras varias rondas, detener la partida al alcanzar 500 puntos y registrar un ganador y tres derrotas.

## Fuente

Mattel, [*UNO Game — instrucciones W2085*](https://service.mattel.com/instruction_sheets/W2085-UNO.pdf), edición de 108 cartas. La implementación seguirá esta referencia para resolver dudas de reglas.
