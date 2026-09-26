# Bathurst Minimap

Este es un mod, inspirado en el de [nyconing para Nürburgring](https://www.gta5-mods.com/scripts/vans123-s-nurburgring-nordschleife-minimap), pero adaptado al circuito de [Bathurst](https://www.gta5-mods.com/es/maps/bathurst-mount-panorama-add-on-fivem) de [ON3FLY3R](https://www.gta5-mods.com/es/users/ON3FLY3R).

Este proyecto se distribuye bajo la licencia MIT.

`Bathurst.png` está trazada a partir de telemetría del juego (posiciones grabadas dando una vuelta al circuito), así que el trazado es exacto.

## Archivos

- `BathurstMinimap.dll` — Script principal, dibuja el mapa y el pin. Incluye directamente dentro del código los puntos de telemetría real usados para detectar si el jugador está cerca del circuito.
- `BathurstMinimap.ini` — Archivo de configuración del circuito y de la posición/tamaño del mapa/pin en pantalla.
- `BathurstMinimap/Bathurst.png` — Trazado real del circuito, generado desde telemetría.
- `BathurstMinimap/pin.png` — El mismo marcador que nyconing.

## Configuración (`BathurstMinimap.ini`)

```ini
[Circuit]
OffsetX=0
OffsetY=0
OnTrackDistance=20

[Map]
PosX=-80
PosY=0
Width=400
Height=400

[Pin]
Width=6
Height=6
```

- `OffsetX`/`OffsetY`: Desplazamiento del circuito, por si el mod del mapa se mueve de sitio en el mundo de GTA (solo compensa traslación, no rotación).
- `OnTrackDistance`: Qué tan cerca (en unidades del juego, aproximadamente metros) necesita estar el jugador del circuito para que el mapa aparezca.
- `PosX`/`PosY`: Esquina superior izquierda del mapa en pantalla (resolución de referencia 1280x720).
- `Width`/`Height`: Tamaño del mapa en pantalla.
- `[Pin] Width`/`Height`: Tamaño del punto que marca tu posición.
- Usa punto (`.`) como separador decimal, no coma.
- Si el archivo falta o algún valor no es válido, el script avisa por pantalla y usa el valor por defecto para ese campo (no rompe el resto).
- Tras editarlo, recarga los scripts (o reinicia el juego) para que se aplique.

## 1. Compilar

Puedes compilarlo en Visual Studio (proyecto Class Library .NET Framework +
paquete NuGet `ScriptHookVDotNet3`), o simplemente dejar los `.cs` sueltos en
la carpeta `scripts/` de GTA V — SHVDN los compila solo al iniciar el juego.

## 2. Instalar

Copia los archivos a la carpeta `scripts/` de GTA V:
- `BathurstMinimap.dll`
- `BathurstMinimap.ini`
- `BathurstMinimap/`
  - `Bathurst.png`
  - `pin.png`

Requiere tener instalado el mapa [Bathurst de ON3FLY3R](https://www.gta5-mods.com/es/maps/bathurst-mount-panorama-add-on-fivem).

## Comportamiento

- El mapa **aparece solo cuando estás cerca del circuito real**. Fuera del circuito no se dibuja nada.
- Ajusta `OnTrackDistance` en `BathurstMinimap.ini` si quieres que el radio de detección sea más estricto o más permisivo.

## Notas

- Si el pin se ve ligeramente desplazado en alguna zona concreta del trazado, dímelo y reviso el ajuste.
- Requiere Script Hook V + Script Hook V .NET (ScriptHookVDotNet3).
