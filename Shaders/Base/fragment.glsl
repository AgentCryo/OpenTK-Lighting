#version 430 core

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
#define MAX_LIGHTS 16
uniform vec3 uLightPos;
uniform samplerCube shadowMap;
uniform vec3 uCameraPos;
uniform bool normalView;
uniform bool useShadows;

uniform Material material;

struct Light {
    int type;
    int shadowMapResolution;
    int padding0;
    int padding1;

    vec3 position;
    float padding2;

    vec3 direction;
    float padding3;

    vec3 color;
    float padding4;

    float intensity;
    float radius;
    float padding5;
    float padding6;
};


layout(std430, binding = 2) buffer Lights
{
    Light lights[MAX_LIGHTS];
};

uniform bool lightActives[MAX_LIGHTS];
uniform int numLights;

uniform samplerCube shadowCubeMaps[MAX_LIGHTS]; // For point light shadows
uniform sampler2D shadow2DMaps[MAX_LIGHTS];     // For directional light shadows
uniform mat4 lightViews[MAX_LIGHTS];            // For directional light shadows
uniform mat4 lightProjections[MAX_LIGHTS];      // For directional light shadows

//uniform vec3 lightPositions[MAX_LIGHTS];
//uniform vec3 lightColors[MAX_LIGHTS];
//uniform float lightIntensities[MAX_LIGHTS];
//uniform float lightSizes[MAX_LIGHTS];

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

// ====== POINT LIGHT SHADOWS ======
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

// ====== DIRECTIONAL LIGHT SHADOWS ======
float avgBlockerDepth_Dir(sampler2D shadowMap, vec2 uv, float searchRadius, float currentDepth) {
    float avgBlocker = 0.0;
    int blockerCount = 0;
    float texelSize = 1.0 / float(textureSize(shadowMap, 0).x); // assuming square shadow map

    for (int i = 0; i < NUM_BLOCKER_SAMPLES; ++i) {
        // Note: radius is already in UV units here, so do NOT multiply by texelSize again
        vec2 offset = vogelDiskSample(i, NUM_BLOCKER_SAMPLES, searchRadius, vec3(uv, 0.0));
        float sampleDepth = texture(shadowMap, uv + offset).r;

        if (sampleDepth < currentDepth) {
            avgBlocker += sampleDepth;
            blockerCount++;
        }
    }

    if (blockerCount == 0) return -1.0;
    return avgBlocker / float(blockerCount);
}

float PCFShadow_Dir(sampler2D shadowMap, vec2 uv, float filterRadius, float currentDepth, float bias) {
    float shadow = 0.0;
    float texelSize = 1.0 / float(textureSize(shadowMap, 0).x);

    for (int i = 0; i < NUM_PCF_SAMPLES; ++i) {
        vec2 offset = vogelDiskSample(i, NUM_PCF_SAMPLES, filterRadius, vec3(uv, 0.0));
        float sampleDepth = texture(shadowMap, uv + offset).r;

        float visibility = smoothstep(currentDepth - bias, currentDepth + bias, sampleDepth);
        shadow += visibility;
    }
    return shadow / float(NUM_PCF_SAMPLES);
}

// ====== MAIN SHADOW CALC ======
float ShadowCalculation(int lightIndex, vec3 fragPos, vec3 normal) {
    Light light = lights[lightIndex];

    switch(light.type) {
        case 0:
    // Point light shadow (cubemap)
            vec3 fragToLight = fragPos - light.position;
            float receiverDepth = length(fragToLight);
            float searchRadius = 0.05 * receiverDepth;
            float avgBlocker = avgBlockerDepth(fragToLight, shadowCubeMaps[lightIndex], searchRadius, fragPos);

            if (avgBlocker == -1.0) return 0.0;

            float filterRadius = penumbraSize(receiverDepth, avgBlocker, light.radius);
            filterRadius = clamp(filterRadius, 0.001, 0.2 * receiverDepth);

            return PCFShadow(fragToLight, shadowCubeMaps[lightIndex], filterRadius, normal, fragPos);

        case 1: {
                vec4 fragPosLightSpace = lightProjections[lightIndex] * lightViews[lightIndex] * vec4(fragPos, 1.0);
                vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
                vec2 uv = projCoords.xy * 0.5 + 0.5;

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0 || projCoords.z > 1.0)
                return 0.0;

                float currentDepth = projCoords.z * 0.5 + 0.5;

                float avgBlocker2D = avgBlockerDepth_Dir(shadow2DMaps[lightIndex], uv, 0.01, currentDepth);
                if (avgBlocker2D == -1.0)
                return 0.0;

                float filterRadius2D = clamp(0.01 * light.radius, 0.001, 0.05);

                vec3 lightDir = normalize(-light.direction);
                float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.001);

                float shadow = PCFShadow_Dir(shadow2DMaps[lightIndex], uv, filterRadius2D, currentDepth, bias);

                float penumbra = clamp((currentDepth - avgBlocker2D) * 100.0, 0.0, 1.0);
                float softShadow = shadow * (1.0 - penumbra);
                return 1.0 - softShadow;
            }
        default:
            return 0.0;
    }
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

    for (int i = 0; i < numLights; ++i) {
        if (!lightActives[i]) continue;
        Light light = lights[i];
        vec3 lightDir;
        float attenuation = 1.0;
        vec3 ambient, diffuse, specular;

        switch(light.type) {
            case 0:
                lightDir = normalize(light.position - FragPos);
                float distance = length(light.position - FragPos);
                attenuation = 1.0 / (distance * distance);
                ambient = ambientStrength * light.color * light.intensity * attenuation;
                diffuse = max(dot(norm, lightDir), 0.0) * light.color * light.intensity * attenuation;
                break;
            case 1:
                lightDir = normalize(-light.direction);
                ambient = ambientStrength * light.color * light.intensity;
                diffuse = max(dot(norm, lightDir), 0.0) * light.color * light.intensity;
                break;
        }

        vec3 viewDir = normalize(uCameraPos - FragPos);
        vec3 halfwayDir = normalize(lightDir + viewDir); 
        float spec = pow(max(dot(norm, halfwayDir), 0.0), material.shininess);
        vec3 specularMap = GetMaterialSpecular(material, TexCoords);
        float specularStrength = dot(specularMap, vec3(0.2126, 0.7152, 0.0722));
        specular = specularStrength * spec * light.color * light.intensity * attenuation;

        float shadow = 0.0;
        if(useShadows) {
            shadow = ShadowCalculation(i, FragPos, norm);
        }

        vec3 lighting = ambient + (1.0 - shadow) * (diffuse + specular);
        finalLighting += lighting;
    }

    vec3 result = finalLighting * baseColor;
    vec3 gammaCorrected = pow(result, vec3(1.0 / 2.2));

    FragColor = normalView
        ? vec4((norm + 1.0) * 0.5, 1.0)
        : vec4(gammaCorrected, 1.0);

    NormalBuffer = vec4(normalize(norm) * 0.5 + 0.5, 1.0);
}
