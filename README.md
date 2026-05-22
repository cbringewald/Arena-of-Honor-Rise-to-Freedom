# Arena of Honor: Rise to Freedom

Lucha. Sobrevive. Gana tu libertad en la arena.

Proyecto Unity de combate en tercera persona ambientado en un coliseo romano.

## Estado actual

- Movimiento con caminar, correr, salto y consumo de stamina.
- Combate cuerpo a cuerpo con punos, armas equipables y hitboxes por animacion.
- Bloqueo frontal, esquiva con invencibilidad temporal y sistema de stamina.
- Enemigos con NavMesh, persecucion, ataque, defensa, retirada y huida con poca vida.
- Sistema de rondas con spawns, desbloqueo de armas, fama y victoria final.
- HUD de vida/stamina, mensajes de ronda, audio de impactos, publico, menus y pausa.

## Controles

- `WASD`: mover al gladiador.
- `Left Shift`: correr.
- `Space`: saltar.
- `Left Mouse`: atacar.
- `Right Mouse`: bloquear.
- `Q`: esquivar.
- `E`: recoger arma.

## Mejoras anadidas

- La fama ahora tiene titulos de progresion: esclavo, aspirante, campeon y gladiador libre.
- Las rondas pueden dar recuperacion de vida y stamina al superarlas.
- Las rondas pueden dar bonus de fama si el jugador gana sin recibir dano.
- El RoundManager puede encontrar automaticamente los pickups de armas si la lista esta vacia.
- Los pickups validan que exista el arma de mano antes de equiparla y reproducen sonido de recogida si esta asignado.
- El RoundManager acepta varios prefabs de enemigos por ronda y spawns manuales desde ascensores.
- La victoria final deja al jugador moverse y desbloquea celebracion con `C`.
- El sonido del arma enemiga se reproduce desde `EnemyWeaponHitbox` solo cuando hay golpe con dano real.

## Siguiente roadmap

- Ajustar en Unity las recompensas de cada ronda desde `RoundManager`.
- Crear una pequena tienda entre rondas para gastar fama en vida, stamina o armas.
- Anadir variantes de enemigo con distintos valores de vida, velocidad y agresividad.
- Asignar `ArenaElevatorSpawn` a los cuatro ascensores/gates de la arena y configurar sus prefabs por ronda.
- Crear una transicion/animacion `Celebrate` en el Animator del jugador.
- Pulir animaciones de combo y ventanas de impacto para que cada arma se sienta distinta.
