**WORK IN PROGRESS/Currently being expanded -**
**Custom shader pipeline using Unity3D SRP and HLSL**

In order to gain more intuition for Unity3D's render pipelines I implemented a custom render
pipeline in Unity3D by using its scriptable render pipeline and HLSL for the shaders. For some parts following the
tutorial by CatLikeCoding.

Currently supports:
- Unlit and Lit shaders
  - opaque and transparant
  - Texture sampling
- Illumination by multiple colored directional lights
  - BRDF controlled by metallic and smoothness parameters
  - Casted shadows using shadow maps
    - Support for cascaded shadow maps
    - Support for varying shadow maps sizes
  - Support for soft shadows using percentage closer filtering
 
 To do:
 - Support for spot lights + shadows
 - Support for point lights + shadows
 - Suport for normal maps
 - Support for baked lights/GI/emmisive surfaces
 - Support for baked shadows
 - Support for reflections
 
 Images:  
 **Cascaded shadow maps**  
 ![Cascaded Shadow Maps](/../images/CascShadowMaps.png?raw=true)
 **Cascaded shadow maps culling spheres visualisation**  
 **Effect of varying shadow map sizes on shadow details**
 **Hard and soft shadows**
