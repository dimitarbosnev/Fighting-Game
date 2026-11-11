// Instanced 2D sprite shader.
//
// There is no vertex buffer: each instance is one sprite (the Instance struct in
// game/engine/abi.odin), drawn as a 4-vertex triangle strip whose corners come
// from the vertex index.

struct Camera {
    viewport:   vec2f, // canvas size in pixels
    offset:     vec2f, // screen position where the target appears
    target_pos: vec2f, // world position that rotation and zoom pivot around
    rotation:   f32,
    zoom:       f32,
}

@group(0) @binding(0) var<uniform> camera: Camera;
@group(1) @binding(0) var sprite_texture: texture_2d<f32>;
@group(1) @binding(1) var sprite_sampler: sampler;

struct InstanceInput {
    @location(0) pos:      vec2f, // center, world units
    @location(1) size:     vec2f,
    @location(2) uv:       vec4f, // u0, v0, u1, v1
    @location(3) rotation: f32,
    @location(4) color:    vec4f,
}

struct VertexOutput {
    @builtin(position) position: vec4f,
    @location(0) uv:    vec2f,
    @location(1) color: vec4f,
}

fn rotate(v: vec2f, angle: f32) -> vec2f {
    let c = cos(angle);
    let s = sin(angle);
    return vec2f(v.x * c - v.y * s, v.x * s + v.y * c);
}

@vertex
fn vs_main(@builtin(vertex_index) vertex: u32, instance: InstanceInput) -> VertexOutput {
    let corner = vec2f(f32(vertex & 1u), f32(vertex >> 1u)); // (0,0) (1,0) (0,1) (1,1)
    let world = instance.pos + rotate((corner - 0.5) * instance.size, instance.rotation);
    let screen = rotate(world - camera.target_pos, camera.rotation) * camera.zoom + camera.offset;
    let ndc = screen / camera.viewport * vec2f(2.0, -2.0) + vec2f(-1.0, 1.0); // y down -> clip y up

    var out: VertexOutput;
    out.position = vec4f(ndc, 0.0, 1.0);
    out.uv = mix(instance.uv.xy, instance.uv.zw, corner);
    out.color = instance.color;
    return out;
}

@fragment
fn fs_main(in: VertexOutput) -> @location(0) vec4f {
    return textureSample(sprite_texture, sprite_sampler, in.uv) * in.color;
}
