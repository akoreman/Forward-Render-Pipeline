**WORK IN PROGRESS - Currently being expanded -**
**Shader pipeline using Unity3D and HLSL**

In order to gain some more intuition for Unity's render pipelines I implemented a custom render
pipeline in Unity3D by using its scriptable render pipeline and HLSL for the shaders. For some parts following
tutorial by CatLikeCoding.

Currently supports:
- Unlit and Lit shaders
  - opaque and transparant
  - Texture sampling
- Illumination by directional lights
  - BRDF controlled by metallic and smoothness parameters
  - Casted shadows using shadow maps
    - Support for cascaded shadow maps
 
