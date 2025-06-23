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

vec3 sampleOffsetDirections[20] = vec3[]
(
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1), 
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
); 
const float farPlane = 1000.0;
float ShadowCalculation(vec3 fragPos, vec3 norm)
{
    vec3 fragToLight = fragPos - uLightPos;
    float currentDepth = distance(fragPos, uLightPos);

    float bias = max(0.015 * (1.0 - dot(norm, normalize(fragToLight))), 0.002);

    float shadow = 0.0;
    int samples  = 20;
    float viewDistance = length(viewPos - fragPos);
    float diskRadius = (1.0 + (viewDistance / farPlane)) / 75.0;  
    for(int i = 0; i < samples; ++i)
    {
        float closestDepth = texture(shadowMap, fragToLight + sampleOffsetDirections[i] * diskRadius).r;
        closestDepth *= farPlane;   // undo mapping [0;1]
        if(currentDepth - bias > closestDepth)
            shadow += 1.0;
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
vec3 lightColor = vec3(1.0,1.0,1.0);
void main()
{
    vec3 lightDir = normalize(uLightPos - FragPos);  
    vec3 viewDir = -normalize(uCameraPos - FragPos);

    vec3 norm = vec3(0);
    if(material.useNormalTexture) {
        norm = GetMaterialNormal(material, TexCoords, TBN);
    } else {
        norm = normalize(vNormal);
    }
    vec3 reflectDir = reflect(lightDir, norm);  

    vec3 ambient = ambientStrength * lightColor;

    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    float specularIntensity = (length(GetMaterialSpecular(material, TexCoords).rgb) / sqrt(3.0));
    vec3 specular = specularIntensity * spec * lightColor;

    float shadow = ShadowCalculation(FragPos, norm);

    vec3 lighting = ambient + (1.0 - (useShadows ? shadow : 0.0)) * (diffuse + specular);
    vec3 result = lighting * GetMaterialColor(material, TexCoords);

    if(!normalView) {
        FragColor = vec4(result, 1.0);
    } else {
        FragColor = vec4((norm + 1.0) * 0.5, 1.0);
    }
}
