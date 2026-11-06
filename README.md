# Robots de Mesa AR

Prototipo de realidad aumentada para Android. Milo es un robot de escritorio de 22 cm que podés colocar, mover y girar sobre una superficie real. Usá el botón de Milo abajo a la derecha para que salude, dé unos pasos sobre la mesa, se siente o se levante. **Retirar** te permite colocarlo de nuevo. La caja de herramientas de la izquierda agrega piezas a la mesa.

## Abrir el proyecto

- Unity `6000.4.6f1`, con Android Build Support, Android SDK & NDK Tools y OpenJDK.
- Escena principal en `Assets/Scenes/RobotsDeMesa.unity`.
- AR Foundation y Google ARCore XR Plugin `6.4.2`. Hace falta un Android compatible con ARCore y Servicios de Google Play para RA.

## Probar en Android

1. Abrí **File → Build Profiles**, activá el perfil de Android y verificá que la escena principal esté habilitada.
2. Conectá el celular con depuración USB habilitada, elegilo en **Run Device** y usá **Build And Run**.
3. Aceptá el permiso de cámara. Mové el celular sobre una mesa bien iluminada hasta que aparezca la superficie detectada.
4. Tocá la superficie para colocar a Milo. Tocá su botón de la derecha para abrir **Saludar**, **Moverse**, **Sentarse** y **Retirar**. El menú se cierra al elegir la acción.
5. Abrí la caja de la izquierda y elegí **Cubo**, **Pelota** o **Rectángulo**. Tocá la superficie marcada para colocar la pieza. Podés tocar y arrastrar tanto a Milo como a las piezas, y girarlos con dos dedos.

La base de cámara AR, detección de superficies y gestos proviene de la plantilla oficial AR Mobile de Unity. El modelo, la animación, la interfaz y el flujo de un robot por mesa son propios del prototipo.
