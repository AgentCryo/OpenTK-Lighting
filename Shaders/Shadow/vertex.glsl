#version 330 core
#extension GL_ARB_shader_viewport_layer_array : require

layout(location = 0) in vec3 aPos;

uniform mat4 uModel;
uniform mat4 uLightView[6];
uniform mat4 uLightProjection;

out vec3 FragPos;

void main()
{
    int face = gl_InstanceID;

    vec4 worldPos = uModel * vec4(aPos, 1.0);
    FragPos = worldPos.xyz;
    gl_Position = uLightProjection * uLightView[face] * worldPos;
    gl_Layer = face;
}
