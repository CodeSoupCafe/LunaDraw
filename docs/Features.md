### **I. Functional Requirements (Core Features)**

| ID | Feature Description | Requirements | Citations |
| :---- | :---- | :---- | :---- |
| **F.1** | **Drawing Canvas** | Support drawing on a blank canvas or imported photos (doodling on pictures).1 | 1 |
| **F.2** | **Core Utility Tools** | Provide universally accessible Undo and Redo functions.1 | 1 |
| **F.3** | **Magical Brush Set** | Must include a portfolio of high-impact brush effects (24+ options).1 Core effects required: Glow, Neon, Star Sparkles/Glitter, Fireworks, Rainbow, Crayon, Spray, and Ribbon.1 | 1 |
| **F.4** | **Movie Mode (Time-Lapse)** | Automatically record the drawing procedure in the background and allow users to play back the drawing process as a short film/animation.1 | 1 |
| **F.5** | **Art Management** | A built-in Art Gallery must securely store both completed drawings and the associated 'Movie Mode' animation procedures.1 | 1 |
| **F.6** | **Coloring Efficiency** | Must include a "Magical pattern paint bucket" (Fill tool) for quickly coloring large areas, accommodating older children (ages 6–8).3 | 3 |

### **II. User Experience (UX/UI) Requirements**

| ID | Feature Description | Requirements | Citations |
| :---- | :---- | :---- | :---- |
| **U.1** | **Ergonomic Design** | All interactive elements (buttons, icons) must be large and easily tappable, adhering to a minimum physical target size of **2cm x 2cm**.4 Provide generous spacing between buttons to prevent accidental activation.4 | 4 |
| **U.2** | **Interface Clarity** | Design must be simple, unambiguous, and uncluttered, with minimal reliance on text in favor of large, clear icons and visual cues.1 | 1 |
| **U.3** | **Feedback & Engagement** | Provide immediate, positive Multi-Sensory Feedback (visual cues, fun animations, and positive sounds) for all key actions (e.g., brush selection, saving).5 | 5 |
| **U.4** | **Color Experience** | The default/prominent brush should utilize an automatic color cycling effect (the "Rainbow" brush), as this "surprise" element is highly engaging for young users and reduces cognitive load.2 | 2 |
| **U.5** | **Guidance** | Use brief visual and audio demonstrations upon first use to show how to draw or interact, reducing the need for explicit instructions.7 | 7 |

### **III. Non-Functional Requirements (Constraints)**

| ID | Constraint Category | Requirements | Citations |
| :---- | :---- | :---- | :---- |
| **NF.1** | **Monetization Policy** | Application must be **completely free and permanently ad-free** (no in-app advertising, banner ads, or pop-up ads).2 | 2 |
| **NF.2** | **Technical Connectivity** | Application must be fully functional and robust in Offline mode (no Wi-Fi/Internet required).1 | 1 |
| **NF.3** | **Glow/Neon Implementation** | Luminous effects must be implemented using optimized shaders (not standard particles), utilizing **additive blending** and a global screen-space **Bloom filter** to create a convincing soft, light-emitting halo.8 | 8 |
| **NF.4** | **Glitter/Sparkle Implementation** | Glitter/Sparkle must be implemented using shader-based dynamic reflection simulation (e.g., Dot Product with noise) for high mobile performance.9 The effect must leverage **HDR colors** and the **Bloom filter** to create realistic, dynamic sparkle.9 | 9 |
| **NF.5** | **Brush Micro-Interactions** | Specific brushes (e.g., Ribbon, Star) should integrate micro-animations or subtle movement (organic animation) to increase the "magic" and sensory engagement.11 | 11 |
