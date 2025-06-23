#version 330 core

in vec3 FragPos;

uniform vec3 uLightPos;
const float farPlane = 1000;

void main()
{
    gl_FragDepth = distance(FragPos, uLightPos) / farPlane;
}
