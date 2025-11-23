

## **Software Requirements Specification: Child-Centric Drawing Application**

This document outlines the essential features and constraints necessary for developing a free, ad-free drawing application targeting children aged 3–8.

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

#### **Works cited**

1. Kids Doodle \- Paint & Draw \- Apps on Google Play, accessed November 23, 2025, [https://play.google.com/store/apps/details?id=com.doodlejoy.studio.kidsdoojoy\&hl=en\_US](https://play.google.com/store/apps/details?id=com.doodlejoy.studio.kidsdoojoy&hl=en_US)  
2. ‎Joy Doodle: Movie Color & Draw App \- App Store, accessed November 23, 2025, [https://apps.apple.com/us/app/joy-doodle-movie-color-draw/id460712294](https://apps.apple.com/us/app/joy-doodle-movie-color-draw/id460712294)  
3. Drawing with Carl \- App Store \- Apple, accessed November 23, 2025, [https://apps.apple.com/us/app/drawing-with-carl/id480645514](https://apps.apple.com/us/app/drawing-with-carl/id480645514)  
4. Design considerations for kids. Designing UI and UX for young kids | by Sulakshana | Bootcamp | Medium, accessed November 23, 2025, [https://medium.com/design-bootcamp/design-considerations-for-kids-48ec9bf2b18](https://medium.com/design-bootcamp/design-considerations-for-kids-48ec9bf2b18)  
5. Top 10 UI/UX Design Tips for Child-Friendly Interfaces \- Aufait UX, accessed November 23, 2025, [https://www.aufaitux.com/blog/ui-ux-designing-for-children/](https://www.aufaitux.com/blog/ui-ux-designing-for-children/)  
6. Designing apps for young kids | by Rubens Cantuni | UX Collective, accessed November 23, 2025, [https://uxdesign.cc/designing-apps-for-young-kids-part-1-ff54c46c773b](https://uxdesign.cc/designing-apps-for-young-kids-part-1-ff54c46c773b)  
7. How to Design Amazing Apps for Kids – Best Practices \- Cygnis Media, accessed November 23, 2025, [https://cygnis.co/blog/designing-apps-for-kids-best-practices/](https://cygnis.co/blog/designing-apps-for-kids-best-practices/)  
8. Best way to create that neon glowing look in pixel art? : r/gamedev \- Reddit, accessed November 23, 2025, [https://www.reddit.com/r/gamedev/comments/1oobwrw/best\_way\_to\_create\_that\_neon\_glowing\_look\_in/](https://www.reddit.com/r/gamedev/comments/1oobwrw/best_way_to_create_that_neon_glowing_look_in/)  
9. I made a glitter effect using Unity Shader Graph : r/Unity3D \- Reddit, accessed November 23, 2025, [https://www.reddit.com/r/Unity3D/comments/p5gmnv/i\_made\_a\_glitter\_effect\_using\_unity\_shader\_graph/](https://www.reddit.com/r/Unity3D/comments/p5gmnv/i_made_a_glitter_effect_using_unity_shader_graph/)  
10. I made a glitter effect using Shader Graph : r/gamedev \- Reddit, accessed November 23, 2025, [https://www.reddit.com/r/gamedev/comments/p5glw9/i\_made\_a\_glitter\_effect\_using\_shader\_graph/](https://www.reddit.com/r/gamedev/comments/p5glw9/i_made_a_glitter_effect_using_shader_graph/)  
11. Digital painting and drawing app | Adobe Fresco, accessed November 23, 2025, [https://www.adobe.com/products/fresco.html](https://www.adobe.com/products/fresco.html)  
12. UX/UI Case Study: I-Track, Productivity Tool for Kids | by Brenda Joan Matos \- Medium, accessed November 23, 2025, [https://medium.com/fluxyeah/ux-ui-case-study-i-track-productivity-tool-for-kids-fbfa87ac0a2a](https://medium.com/fluxyeah/ux-ui-case-study-i-track-productivity-tool-for-kids-fbfa87ac0a2a)