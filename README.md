# Robots de Mesa AR

Prototipo de realidad aumentada para Android. Milo es un robot de escritorio de 22 cm que podés colocar, mover y girar sobre una superficie real. Usá el panel de acciones para que salude, dé unos pasos sobre la mesa, se siente o se levante. **Retirar** libera la mesa para colocarlo de nuevo.

## Abrir el proyecto

- Unity `6000.4.6f1`, con Android Build Support, Android SDK & NDK Tools y OpenJDK.
- Escena principal en `Assets/Scenes/RobotsDeMesa.unity`.
- AR Foundation y Google ARCore XR Plugin `6.4.2`. Hace falta un Android compatible con ARCore y Servicios de Google Play para RA.

## Probar en Android

1. Abrí **File → Build Profiles**, activá el perfil de Android y verificá que la escena principal esté habilitada.
2. Conectá el celular con depuración USB habilitada, elegilo en **Run Device** y usá **Build And Run**.
3. Aceptá el permiso de cámara. Mové el celular sobre una mesa bien iluminada hasta que aparezca la superficie detectada.
4. Tocá la superficie para colocar a Milo. Seleccionalo y arrastralo para moverlo o usá dos dedos para girarlo. Probá **Saludar**, **Moverse**, **Sentarse** y **Retirar**. Si Milo tiene poco espacio para caminar, escaneá una zona más amplia o acercalo al centro del plano.

La base de cámara AR, detección de superficies y gestos proviene de la plantilla oficial AR Mobile de Unity. El modelo, la animación, la interfaz y el flujo de un robot por mesa son propios del prototipo.
