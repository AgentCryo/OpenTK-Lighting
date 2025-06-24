#version 330 core

struct Material {
    sampler2D colorTexture;
    vec3 color;
    bool useColorTexture;
    sampler2D specularTexture;
    float specular;
    bool useSpecularTexture;
    float shininess;
    bool useNormalTexture;
    sampler2D normalTexture;
};
in vec2 TexCoords;

in vec3 FragPos;

in vec3 vNormal;
in mat3 TBN;

in vec3 viewPos;

uniform vec3 uLightPos;

uniform samplerCube shadowMap;
uniform vec3 uCameraPos;
uniform bool normalView;
uniform bool useShadows;

uniform Material material;

out vec4 FragColor;

float remapDepth(float x) {
    // Only stretch if x is in the upper tail range
    if (x < 0.971) {
        return 0.0;
    }
    return (x - 0.971) / (1.0 - 0.971);
}

#define MAX_POINT_LIGHTS 8

uniform samplerCubeShadow shadowMaps[MAX_POINT_LIGHTS];
uniform vec3 lightPositions[MAX_POINT_LIGHTS];
uniform int numPointLights;

vec3 sampleOffsetDirections[20] = vec3[]
(
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1), 
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
); 
const float farPlane = 1000.0;
float ShadowCalculation(vec3 fragPos, vec3 lightPos, samplerCubeShadow cubeMap)
{
    vec3 fragToLight = fragPos - lightPos;
    float currentDepth = length(fragToLight);
    float shadow = 0.0;
    int samples = 20;
    float diskRadius = (1.0 + (length(viewPos - fragPos) / farPlane)) / 75.0;

    // Normalize depth to [0, 1]
    float compareDepth = currentDepth / farPlane;

    for (int i = 0; i < samples; ++i)
    {
        vec3 sampleDir = normalize(fragToLight + sampleOffsetDirections[i] * diskRadius);
        float sample = texture(cubeMap, vec4(sampleDir, compareDepth));
        shadow += 1.0 - sample; // 1.0 if lit, 0.0 if in shadow;
    }

    shadow /= float(samples);
    return shadow;
}

vec3 GetMaterialColor(Material mat, vec2 texCoords) {
    if (mat.useColorTexture) {
        return texture(mat.colorTexture, texCoords).rgb;
    } else {
        return mat.color;
    }
}
vec3 GetMaterialSpecular(Material mat, vec2 texCoords) {
    if (mat.useSpecularTexture) {
        return texture(mat.specularTexture, texCoords).rgb;
    } else {
        return vec3(mat.specular);
    }
}
vec3 GetMaterialNormal(Material mat, vec2 texCoords, mat3 TBN) {
    if (mat.useNormalTexture) {
        // Sample and convert from [0,1] to [-1,1]
        vec3 tangentNormal = texture(mat.normalTexture, texCoords).rgb;
        tangentNormal = tangentNormal * 2.0 - 1.0;

        // Transform to world space
        return normalize(TBN * tangentNormal);
    } else {
        return normalize(vNormal);
    }
}

float ambientStrength = 0.1;
uniform vec3 lightColors[MAX_POINT_LIGHTS];
uniform float lightIntensities[MAX_POINT_LIGHTS];
uniform bool lightActives[MAX_POINT_LIGHTS];
void main()
{
    vec3 norm = material.useNormalTexture
        ? GetMaterialNormal(material, TexCoords, TBN)
        : normalize(vNormal);

    vec3 viewDir = normalize(uCameraPos - FragPos);
    vec3 baseColor = GetMaterialColor(material, TexCoords);
    vec3 specularMap = GetMaterialSpecular(material, TexCoords);
    vec3 finalLighting = vec3(0.0);

    for (int i = 0; i < numPointLights; ++i)
    {
        if (!lightActives[i])
        continue;

        vec3 lightPos = lightPositions[i];
        vec3 lightColor = lightColors[i] * lightIntensities[i];
        vec3 lightDir = normalize(lightPos - FragPos);
        vec3 reflectDir = reflect(-lightDir, norm);

        // Ambient
        vec3 ambient = ambientStrength * lightColor;

        // Diffuse
        float diff = max(dot(norm, lightDir), 0.0);
        vec3 diffuse = diff * lightColor;

        // Specular
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        float specularStrength = length(specularMap) / sqrt(3.0); // average of RGB
        vec3 specular = specularStrength * spec * lightColor;

        // Shadow
        float shadow = ShadowCalculation(FragPos, lightPos, shadowMaps[i]);

        // Combine
        vec3 lighting = ambient + (1.0 - (useShadows ? shadow : 0.0)) * (diffuse + specular);
        finalLighting += lighting;
    }

    vec3 result = finalLighting * baseColor;

    FragColor = normalView
        ? vec4((norm + 1.0) * 0.5, 1.0)
        : vec4(result, 1.0);
}
