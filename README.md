# Custom shader pipeline using Unity3D SRP and HLSL

In order to gain more intuition for scripting render pipelines I implemented a custom render
pipeline in Unity3D by using its scriptable render pipeline and HLSL for the shaders. For some parts following the
tutorial by CatLikeCoding.

**Currently supports:**
- Unlit and Lit shaders
  - Opaque and transparant.
  - Texture sampling.
- Illumination by multiple colored directional lights
  - BRDF controlled by metallic and smoothness parameters.
  - Casted shadows using light maps.
    - Support for cascaded light maps.
    - Support for varying light map resolution.
    - Clipped shadows for transparent objects.
  - Support for soft shadows using percentage closer filtering.
  - Support for normal maps.
- Global illumination
  - Support for sampling from the lightmaps generated by Unity for static objects.
  - Support for using light probes and LPPVs to approximate GI for dynamic objects.
  - Meta pass for colored GI.
- Specular reflections
  - Support for reflections from the skybox.
  - Support for reflections from unity reflection probes.
  - Reflection sharpness affected by surface rougness.
- Support for emmisive surfaces.

 
**Possible extensions:**
 - Support for spot lights + shadows.
 - Support for point lights + shadows.
 <!-- - Support for baked shadows. -->
 <!-- - Support for light cookies. -->
 
 ## Screenshots:  
 
<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/sampleRender.PNG" width="400">  

**Cascaded shadow maps**   

<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/CascShadowMaps.PNG" width="400">  

**Cascaded shadow maps culling spheres visualisation**  

<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/CascCullingSpheres.PNG" width="400"> 

**Effect of varying shadow map sizes on shadow details**  

<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/shadowLevels.png" width="400">  

**Colored global illumination**
  
<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/ColoredGI.PNG" width="400">  

**Reflections from skybox and reflection probes**

<img src="https://raw.github.com/akoreman/WIP-ShaderDemo/master/images/Reflections.png" width="400">  
