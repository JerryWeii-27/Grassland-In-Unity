# Grassland In Unity

A procedural grassy terrain generator built in Unity.

I was greatly aided by Sebastian Lague and Acerola's YouTube videos and the FastNoiseLite library.

Unity version: 2021.3 SRP

Also works in URP, just need to set the grass subshader tags as `{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }`.

# Features

- Multithreaded procedural terrain generation with multiple noise maps.
- GPU instanced animated grass with compute shaders.
- Floating origin.
