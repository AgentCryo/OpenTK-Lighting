#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in vec4 aTangent;
layout (location = 4) in vec3 aBitangent;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 FragPos;
out vec3 viewPos;
out vec2 TexCoords;

out vec3 vNormal;
out mat3 TBN;

void main()
{
    vec4 worldPosition = uModel * vec4(aPosition, 1.0);
    FragPos = worldPosition.xyz;

    TexCoords = aTexCoords;

    mat3 normalMatrix = mat3(transpose(inverse(uModel)));
    vNormal = normalize(normalMatrix * aNormal);

    vec3 T = normalize(normalMatrix * aTangent.xyz);
    float handedness = aTangent.w;

    // Reconstruct bitangent using handedness
    vec3 B = handedness * cross(vNormal, T);

    // Optional orthonormalization if needed:
    T = normalize(T - dot(T, vNormal) * vNormal);
    B = normalize(cross(vNormal, T));

    TBN = mat3(T, B, vNormal);

    gl_Position = uProjection * uView * vec4(FragPos, 1.0);
}
