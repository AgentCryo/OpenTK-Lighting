#version 330 core

in vec3 FragPos;

uniform int uLightType = -1;
const int POINT_LIGHT = 0;
const int DIRECTIONAL_LIGHT = 1;

uniform vec3 uLightPos;
const float farPlane = 1000;

void main()
{
    if(uLightType == POINT_LIGHT) {
        gl_FragDepth = distance(FragPos, uLightPos) / farPlane;
        return;
    }
    gl_FragDepth = gl_FragCoord.z;
}
