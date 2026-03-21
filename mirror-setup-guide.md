# Mirror Setup Guide for Wallhack FPS

## Step 1: Install Mirror

### Option A: Unity Asset Store (Recommended)
1. Open Unity Asset Store in browser or Unity Editor
2. Search for "Mirror Networking"
3. Download and import into project
4. Accept all files in import dialog

### Option B: Unity Package Manager (Git URL)
1. Open Unity Editor
2. Go to `Window > Package Manager`
3. Click `+` button (top left)
4. Select "Add package from git URL"
5. Enter: `https://github.com/MirrorNetworking/Mirror.git`
6. Wait for installation to complete

---

## Step 2: Create Network Manager Scene

1. **Create a new scene**: `File > New Scene > Basic (Built-in)` or `Universal 3D`
2. **Save as**: `Scenes/NetworkManager` (create Scenes folder if needed)

3. **Create Network Manager GameObject**:
   - Right-click in Hierarchy
   - `Create Empty`
   - Rename to "NetworkManager"
   - Add Component: `Network Manager`
   - Add Component: `Telepathy Transport` (or `KCP Transport` for better performance)

4. **Configure Network Manager**:
   - Network Address: `localhost`
   - Transport: Drag the Transport component to the Transport field
   - Don't configure player prefab yet (we'll do this next)

5. **Add Network Manager HUD** (for testing):
   - Select NetworkManager GameObject
   - Add Component: `Network Manager HUD`
   - This adds simple UI buttons for Host/Client/Server

---

## Step 3: Create Networked Player Prefab

1. **Create Player GameObject**:
   - Create `3D Object > Capsule` in scene
   - Rename to "Player"
   - Add a `Plane` below it for ground reference

2. **Add Camera**:
   - Create child object: `Create Empty` as child of Player
   - Rename to "CameraHolder"
   - Position at (0, 0.6, 0) - roughly head height
   - Add `Main Camera` as child of CameraHolder
   - Position Camera at (0, 0, 0)
   - Rotation (0, 0, 0)

3. **Add Network Components**:
   - Select Player GameObject
   - Add Component: `Network Identity`
   - Add Component: `Network Transform` (syncs position/rotation)

4. **Create Simple Movement Script**:
   - Create folder: `Assets/Scripts`
   - Create C# script: `PlayerMovement.cs`
   - (We'll write this code next)

5. **Make it a Prefab**:
   - Create folder: `Assets/Prefabs`
   - Drag Player from Hierarchy to Prefabs folder
   - Delete Player from scene (prefab only spawned at runtime)

6. **Assign to Network Manager**:
   - Select NetworkManager GameObject
   - Find "Player Prefab" field
   - Drag Player prefab from Prefabs folder into this field

---

## Step 4: Basic Player Movement Script

Here's a simple networked movement script to test with:

```csharp
using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;

    private CharacterController controller;
    private Transform cameraHolder;
    private float verticalRotation = 0f;

    void Start()
    {
        // Only set up for local player
        if (!isLocalPlayer) return;

        // Get components
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // Find camera holder
        cameraHolder = transform.Find("CameraHolder");

        // Lock cursor for FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Only control local player
        if (!isLocalPlayer) return;

        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Simple gravity
        controller.Move(Vector3.down * 9.81f * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
}
```

---

## Step 5: Test the Connection

1. **Apply Script to Player Prefab**:
   - Open Player prefab
   - Add `PlayerMovement` component
   - Save prefab

2. **Disable Other Cameras**:
   - Make sure Player prefab's camera is disabled by default
   - In `PlayerMovement.Start()`, enable camera only for local player:
   ```csharp
   if (cameraHolder != null)
   {
       Camera cam = cameraHolder.GetComponentInChildren<Camera>();
       if (cam != null) cam.enabled = true;
   }
   ```

3. **Run the Test**:
   - Play the scene in Unity Editor
   - Click "Host (Server + Client)" button
   - You should spawn and be able to move around

4. **Test Multiple Clients** (Build and Run):
   - `File > Build Settings`
   - Add NetworkManager scene to build
   - Check "Development Build"
   - Click "Build and Run"
   - This opens a standalone build
   - In standalone: Click "Client" and connect to localhost
   - In Unity Editor: Click "Host"
   - You should see both players and they should sync movement

---

## Troubleshooting

**Player not spawning:**
- Make sure Player prefab is assigned to Network Manager
- Check Network Identity is on Player prefab
- Make sure scene with Network Manager is being run

**Movement not syncing:**
- Verify Network Transform is on Player
- Check that only local player is being controlled (`if (!isLocalPlayer) return;`)

**Camera issues:**
- Disable camera on prefab by default
- Enable only for local player in code
- Make sure Camera tag is set to "MainCamera"

**Can't connect:**
- Check firewall settings
- Verify Transport component is assigned to Network Manager
- Try different transport (KCP, Telepathy, etc.)

---

## Next Steps After Testing
Once you have basic movement syncing:
1. Add shooting mechanics (with server authority)
2. Implement health system
3. Add spawn points
4. Create test level with walls for visibility testing
