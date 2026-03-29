# TODO

## Basic FPS Mechanics
- Shooting mechanics (weapon handling, firing, recoil)
- Basic player health and damage system
- Crosshair and basic UI
- Ensure all mechanics work in networked context

## CheckerFloor shader
The current solution uses a material asset reference to get the URP Lit shader into the build.
A cleaner approach: add **Universal Render Pipeline/Lit** to **Project Settings → Graphics → Always Included Shaders**,
then remove the `baseMaterial` field from `CheckerFloor.cs` and go back to plain `Shader.Find`.
Also delete the now-unused material asset at `Assets/Materials/New Material`.
