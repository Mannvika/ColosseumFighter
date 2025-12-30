# Colosseum Fighter (Unity NGO)

A multiplayer twin-stick shooter tech demo implementing **Server-Authoritative logic** with **Client-Side Prediction** and **Reconciliation** (Rollback) using Unity Netcode for Game Objects (NGO).

![CSP](https://github.com/user-attachments/assets/fb004f7e-225e-40d5-83dc-b9c2dbe88d6d)
> *Above: Client performing instant movement inputs despite 200ms simulated network latency.*

![RollbackGif](https://github.com/user-attachments/assets/ff490544-8a8d-4a03-bcac-a7a4a7f46b4c)
> *Above: Client snapping back to server expected position.*

## Project Focus
This project demonstrates the implementation of networking concepts to ensure responsive gameplay in a high-latency environment.

### Custom Rollback Networking (`PlayerController.cs`)
* **Input Packing:** Inputs (Movement, Mouse Pos, Bitmasked Buttons) are packed into structs and sent to the server every tick.
* **Circular Buffers:** The client maintains an Input Buffer and State Buffer.
* **Prediction:** The client simulates physics locally immediately upon input.
* **Reconciliation:** When the server sends a position/velocity/state payload the client compares it to the history buffer. If the error exceeds the client resets to the server state and re-simulates all subsequent inputs to the present frame.

### Data-Driven Ability System
Abilities are modular `ScriptableObjects` (`AbilityBase`), decoupled from the network transport layer.
* **State Management:** The system handles complex state transitions (Normal -> Casting -> Dashing -> Blocking) using a finite state machine logic within `PlayerAbilitySystem`.
* **Rollback Compatibility:** Visuals (`TransientVisual`) and cooldowns support rollback. If a client mispredicts an ability cast, the visual and state are corrected during reconciliation.
  
**Engine:** Unity 6
**Networking:** Unity Netcode for Game Objects (NGO)
**Physics:** Unity 2D Physics
**Language:** C#
