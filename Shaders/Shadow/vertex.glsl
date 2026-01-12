#version 330 core
#extension GL_ARB_shader_viewport_layer_array : require

layout(location = 0) in vec3 aPos;

uniform int uLightType = -1;
const int POINT_LIGHT = 0;
const int DIRECTIONAL_LIGHT = 1;

uniform mat4 uModel;
uniform mat4 uLightProjection;

// Point
uniform mat4 uLightView[6];

//Directional
uniform mat4 uDirectionalLightView;

out vec3 FragPos;

void main()
{
    if(uLightType == POINT_LIGHT) {
        int face = gl_InstanceID;

        vec4 worldPos = uModel * vec4(aPos, 1.0);
        FragPos = worldPos.xyz;
        gl_Position = uLightProjection * uLightView[face] * worldPos;
        gl_Layer = face;
    }
    if(uLightType == DIRECTIONAL_LIGHT) {
        gl_Position = (uLightProjection * uDirectionalLightView) * uModel * vec4(aPos, 1.0);
    }
}
