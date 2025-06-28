#version 330 core

// ====== STRUCTS ======
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

// ====== INPUTS ======
in vec2 TexCoords;
in vec3 FragPos;
in vec3 vNormal;
in mat3 TBN;
in vec3 viewPos;

// ====== OUTPUT ======
layout(location = 0) out vec4 FragColor;
layout(location = 1) out vec4 NormalBuffer;

// ====== UNIFORMS ======
#define MAX_POINT_LIGHTS 8
uniform vec3 uLightPos;
uniform samplerCube shadowMap;
uniform vec3 uCameraPos;
uniform bool normalView;
uniform bool useShadows;

uniform Material material;

uniform samplerCube shadowMaps[MAX_POINT_LIGHTS];
uniform vec3 lightPositions[MAX_POINT_LIGHTS];
uniform vec3 lightColors[MAX_POINT_LIGHTS];
uniform float lightIntensities[MAX_POINT_LIGHTS];
uniform bool lightActives[MAX_POINT_LIGHTS];
uniform float lightSizes[MAX_POINT_LIGHTS];
uniform int numPointLights;

// ====== CONSTANTS ======
const int NUM_BLOCKER_SAMPLES = 8;
const int NUM_PCF_SAMPLES = 16;
const float farPlane = 1000.0;
const float PI = 3.14159265359;
const float GOLDEN_ANGLE = 2.39996322973;
const float ambientStrength = 0.05;

// ====== UTILS ======
float linearizeDepth(float depth) {
    return depth * farPlane;
}

vec2 vogelDiskSample(int i, int nSamples, float radius, vec3 fragPos) {
    float t = float(i) / float(nSamples);
    float r = radius * pow(t, 0.75); // 0.5 = uniform, <1.0 = more central density

    float theta = float(i) * GOLDEN_ANGLE;

    vec2 randomOffset = vec2(
        fract(sin(dot(fragPos.xy, vec2(12.9898, 78.233))) * 43758.5453),
        fract(sin(dot(fragPos.yz, vec2(93.9898, 67.345))) * 12345.6789)
    );

    vec2 offset = vec2(cos(theta), sin(theta));
    return offset * r + randomOffset * radius * 0.3;
}

float penumbraSize(float receiverDepth, float blockerDepth, float lightRadius) {
    return (receiverDepth - blockerDepth) * lightRadius / blockerDepth;
}

// ====== SHADOW FUNCTIONS ======
float avgBlockerDepth(vec3 fragToLight, samplerCube cubeMap, float searchRadius, vec3 fragPos) {
    float currentDepth = length(fragToLight);
    vec3 L = normalize(fragToLight);

    vec3 up = abs(L.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent = normalize(cross(up, L));
    vec3 bitangent = cross(L, tangent);

    float avgBlocker = 0.0;
    int blockerCount = 0;
    float angularRadius = searchRadius / currentDepth;

    for (int i = 0; i < NUM_BLOCKER_SAMPLES; ++i) {
        vec2 offset = vogelDiskSample(i, NUM_BLOCKER_SAMPLES, angularRadius, fragPos);
        vec3 sampleDir = normalize(L + tangent * offset.x + bitangent * offset.y);

        float sampleDepth = texture(cubeMap, sampleDir).r;
        sampleDepth = linearizeDepth(sampleDepth);

        if (sampleDepth < currentDepth) {
            avgBlocker += sampleDepth;
            blockerCount++;
        }
    }

    if (blockerCount == 0) return -1.0;
    return avgBlocker / float(blockerCount);
}

float PCFShadow(vec3 fragToLight, samplerCube cubeMap, float filterRadius, vec3 fragNormal, vec3 fragPos) {
    float shadow = 0.0;
    float receiverDepth = length(fragToLight);
    vec3 L = normalize(fragToLight);

    vec3 up = abs(L.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent = normalize(cross(up, L));
    vec3 bitangent = cross(L, tangent);

    float angularRadius = filterRadius / receiverDepth;

   float bias = max(0.01 * (1.0 - dot(fragNormal, L)), 0.001);

    for (int i = 0; i < NUM_PCF_SAMPLES; ++i) {
        vec2 offset = vogelDiskSample(i, NUM_PCF_SAMPLES, angularRadius, fragPos);
        vec3 sampleDir = normalize(L + tangent * offset.x + bitangent * offset.y);
        float sampleDepth = texture(cubeMap, sampleDir).r;
        sampleDepth = linearizeDepth(sampleDepth);

        float visibility = smoothstep(-0.01, 0.01, receiverDepth - bias - sampleDepth);
        shadow += visibility;
    }

    return shadow / float(NUM_PCF_SAMPLES);
}

float ShadowCalculation(vec3 fragPos, vec3 lightPos, samplerCube cubeMap, vec3 fragNormal, float lightRadius) {
    vec3 fragToLight = fragPos - lightPos;
    float receiverDepth = length(fragToLight);

    float searchRadius = 0.05 * receiverDepth;
    float avgBlocker = avgBlockerDepth(fragToLight, cubeMap, searchRadius, fragPos);

    if (avgBlocker == -1.0) return 0.0;

    float filterRadius = penumbraSize(receiverDepth, avgBlocker, lightRadius);
    filterRadius = clamp(filterRadius, 0.001, 0.2 * receiverDepth);

    return PCFShadow(fragToLight, cubeMap, filterRadius, fragNormal, fragPos);
}

// ====== MATERIAL HELPERS ======
vec3 GetMaterialColor(Material mat, vec2 texCoords) {
    return mat.useColorTexture
        ? texture(mat.colorTexture, texCoords).rgb
        : mat.color;
}

vec3 GetMaterialSpecular(Material mat, vec2 texCoords) {
    return mat.useSpecularTexture
        ? texture(mat.specularTexture, texCoords).rgb * mat.specular
        : vec3(mat.specular);
}

vec3 GetMaterialNormal(Material mat, vec2 texCoords, mat3 TBN) {
    if (mat.useNormalTexture) {
        vec3 tangentNormal = texture(mat.normalTexture, texCoords).rgb * 2.0 - 1.0;
        return normalize(TBN * tangentNormal);
    } else {
        return normalize(vNormal);
    }
}

// ====== MAIN FRAGMENT SHADER ======
void main() {
    vec3 norm = material.useNormalTexture
        ? GetMaterialNormal(material, TexCoords, TBN)
        : normalize(vNormal);

    vec3 viewDir = normalize(uCameraPos - FragPos);
    vec3 baseColor = GetMaterialColor(material, TexCoords);
    vec3 specularMap = GetMaterialSpecular(material, TexCoords);
    vec3 finalLighting = vec3(0.0);

    for (int i = 0; i < numPointLights; ++i) {
        if (!lightActives[i]) continue;

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
        vec3 halfwayDir = normalize(lightDir + viewDir);
        float spec = pow(max(dot(norm, halfwayDir), 0.0), material.shininess);
        float specularStrength = dot(specularMap, vec3(0.2126, 0.7152, 0.0722));
        vec3 specular = specularStrength * spec * lightColor;

        // Shadow
        float shadow = 0.0;
        if(useShadows) {
            shadow = ShadowCalculation(FragPos, lightPos, shadowMaps[i], norm, lightSizes[i]);
        }

        // Attenuation
        float distance = length(lightPos - FragPos);
        float attenuation = 1.0 / (distance * distance);
        ambient *= attenuation;
        diffuse *= attenuation;
        specular *= attenuation;

        // Final lighting
        vec3 lighting = ambient + (1.0 - (useShadows ? shadow : 0.0)) * (diffuse + specular);
        finalLighting += lighting;
    }

    vec3 result = finalLighting * baseColor;
    vec3 gammaCorrected = pow(result, vec3(1.0 / 2.2));

    FragColor = normalView
        ? vec4((norm + 1.0) * 0.5, 1.0)
        : vec4(gammaCorrected, 1.0);

    NormalBuffer = vec4(normalize(norm) * 0.5 + 0.5, 1.0);
}
