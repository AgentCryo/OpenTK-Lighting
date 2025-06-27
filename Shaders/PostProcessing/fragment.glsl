#version 330 core
in vec2 TexCoords;

uniform sampler2D colorTexture;
uniform sampler2D normalTexture;
uniform sampler2D depthTexture;
uniform sampler2D noiseTexture;

uniform vec3 samples[64];
uniform mat4 projection;
uniform mat4 inverseProjection;
uniform vec2 noiseScale;

uniform bool useSSAO;

out vec4 FragColor;

// Constants
const float radius = 0.1;
const float bias = 0.025;
const float near = 0.1;  // near plane
const float far = 1000.0; // far plane

// Linearize non-linear depth from depth buffer
float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0; // back to NDC
    return (2.0 * near * far) / (far + near - z * (far - near));
}

// Get view space position from depth and UV
vec3 getViewPos(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    vec4 clipSpace = vec4(uv * 2.0 - 1.0, z, 1.0);
    vec4 viewSpace = inverseProjection * clipSpace;
    return viewSpace.xyz / viewSpace.w;
}

void main()
{
    if(useSSAO) {
        float depth = texture(depthTexture, TexCoords).r;

        vec3 fragPos = getViewPos(depth, TexCoords);
        vec3 normal = normalize(texture(normalTexture, TexCoords).rgb * 2.0 - 1.0);

        vec3 randomVec = normalize(texture(noiseTexture, TexCoords * noiseScale).xyz);
        vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
        vec3 bitangent = cross(normal, tangent);
        mat3 TBN = mat3(tangent, bitangent, normal);

        float occlusion = 0.0;
        for (int i = 0; i < 64; ++i)
        {
            vec3 sampleVec = TBN * samples[i];  // rotate sample vector to view space
            sampleVec = fragPos + sampleVec * radius;

            vec4 offset = projection * vec4(sampleVec, 1.0);
            offset.xyz /= offset.w;
            offset.xyz = offset.xyz * 0.5 + 0.5;

            float sampleDepth = texture(depthTexture, offset.xy).r;
            if (sampleDepth >= 1.0) continue;

            float sampleDepthLinear = LinearizeDepth(sampleDepth);
            float samplePosZ = -sampleVec.z; // view space z is negative in front of camera

            // Range check: difference between sample depth and sample point distance
            float rangeCheck = smoothstep(0.0, 1.0, radius / abs(samplePosZ - sampleDepthLinear));

            if ((sampleDepthLinear + bias) < samplePosZ)
            {
                occlusion += rangeCheck;
            }
        }

        occlusion = 1.0 - (occlusion / 64.0);
        FragColor = vec4(vec3(texture(colorTexture, TexCoords) * occlusion), 1.0);
    } else {
        FragColor = texture(colorTexture, TexCoords);
    }
}
