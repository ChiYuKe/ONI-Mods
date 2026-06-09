# ONI Resource Bridge

Localhost-only Oxygen Not Included mod bridge for KAnimGUI.

## Usage

1. Build the project:

   ```powershell
   dotnet build .\ONIResourceBridge.csproj -c Release
   ```

2. Enable `ONI Resource Bridge` in ONI's mod list and restart the game.

3. Start KAnimGUI and click the game resource bridge button in the top bar.

## Endpoints

The mod binds to `127.0.0.1`, preferring port `17871` and trying up to `17890`.
It writes the selected URL to `%TEMP%\KAnimGui.ONIResourceBridge.json`.

- `GET /status`
- `GET /assets/anims`
- `GET /assets/kanim?name=<animName>`

The bridge never binds to external interfaces.
