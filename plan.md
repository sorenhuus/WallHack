# Wallhack FPS Development Plan

## Project Goal
Build an FPS game that explores anti-wallhack security by implementing server-authoritative visibility. Only share player positions with clients when they have line-of-sight to each other.

## Development Steps

### 1. Choose & Setup Networking Framework
- Research networking solutions (Unity Netcode for GameObjects, Mirror, Photon, Fishnet)
- Evaluate based on server authority support, ease of use, and cost
- Install and configure chosen framework
- Set up basic server-client connection
- Create simple test scene with network manager

### 2. Basic FPS Mechanics
- Player movement (WASD, sprint, crouch) - built with networking in mind
- Camera controls (mouse look, first-person perspective)
- Shooting mechanics (weapon handling, firing, recoil)
- Basic player health and damage system
- Crosshair and basic UI
- Ensure all mechanics work in networked context

### 3. Network Synchronization
- Implement client-server architecture fully
- Basic player spawning and connection handling
- Synchronize player transforms (position, rotation)
- Synchronize shooting and damage across network
- Implement server authority for all gameplay systems
- Client-side prediction and lag compensation basics

### 4. Server-Side Visibility/Line-of-Sight Checks
- Implement raycasting system on server to detect player visibility
- Create visibility graph/matrix tracking which players can see each other
- Handle occlusion by walls and obstacles
- Optimize raycast performance (spatial partitioning, update frequency)
- Consider peripheral vision cone vs 360-degree checks

### 5. Player Position Culling
- Modify network sync to only send position updates when visible
- Implement visibility state changes (appear/disappear smoothly)
- Handle edge cases (player goes behind cover mid-fight)
- Client-side prediction and interpolation for visible players only
- Prevent information leaks through other game systems (audio, particles)

### 6. Testing Framework
- Create test scenarios with walls and different player positions
- Verify clients only receive data for visible players
- Network traffic analysis to confirm data reduction
- Performance benchmarking (raycast overhead vs bandwidth savings)
- Edge case testing (fast movement, multiple players, complex geometry)

## Notes
- Server authority is critical - never trust client position data
- Balance between security and gameplay smoothness
- Consider fog of war/last known position for gameplay feel
