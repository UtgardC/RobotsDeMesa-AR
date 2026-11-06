# Robots de Mesa AR

Prototipo de realidad aumentada para Android. Colocá a Milo sobre una mesa real, hacelo saludar, sentarse o caminar, y desbloqueá a Nova y Byte con sus tarjetas. La app se juega en horizontal.

## Abrir el proyecto

- Unity `6000.4.6f1`, con Android Build Support, Android SDK & NDK Tools y OpenJDK.
- Escena principal en `Assets/Scenes/RobotsDeMesa.unity`.
- AR Foundation y Google ARCore XR Plugin `6.4.3`. Hace falta un Android compatible con ARCore y Servicios de Google Play para RA.

## Probar en Android

1. Abrí **File → Build Profiles**, activá el perfil de Android y verificá que la escena principal esté habilitada.
2. Conectá el celular con depuración USB habilitada, elegilo en **Run Device** y usá **Build And Run**.
3. Aceptá el permiso de cámara. Mové el celular despacio sobre una mesa iluminada y tocá el plano marcado para colocar a Milo.
4. Elegí un robot desde la colección de arriba o tocándolo en la mesa. Su botón abajo a la derecha abre los comandos. **Moverse** lo hace caminar por varios puntos dentro de la superficie detectada.
5. En **Herramientas**, elegí un cubo, una pelota o un rectángulo. El aviso de abajo indica qué pieza vas a crear al tocar el plano. Arrastrá una pieza para levantarla y soltá con impulso para lanzarla. **Limpiar** retira todas las piezas de herramientas.

## Tarjetas de la colección

Las tarjetas están en `Assets/RobotsDeMesa/Cards`. Mostrá cada imagen en otra pantalla o imprimila a **8,5 × 5,5 cm**. Ponela sobre la mesa y apuntá con la cámara. Cuando la app la reconozca, el robot aparece sobre la tarjeta y se desbloquea en la colección. Tocá su pestaña y después una zona del plano para colocarlo. Los desbloqueos quedan guardados en el dispositivo.

- [Tarjeta de Nova](Assets/RobotsDeMesa/Cards/Nova.png)
- [Tarjeta de Byte](Assets/RobotsDeMesa/Cards/Byte.png)

La cámara AR, la detección de superficies y los gestos de los robots provienen de la plantilla oficial AR Mobile de Unity. Los modelos, las tarjetas, la colección y la caja de herramientas son propios del prototipo.
