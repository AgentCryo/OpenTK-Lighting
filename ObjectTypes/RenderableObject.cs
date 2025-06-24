using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_Lighting.ObjectTypes
{
    public class RenderableObject
    {
        public string Name;

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;

        public List<float> Vertices = new();
        public List<uint> Indices = new();
        public List<float> TextureCoordinants = new();
        public List<float> Normals = new();

        public List<float> Tangents = new();
        public List<float> Bitangents = new();

        public Vector3 baseColor = Vector3.One;
        public int colorTexture = -1;
        public int specularTexture = -1;
        public int normalTexture = -1;

        private int _vao, _vbo, _ibo;

        public RenderableObject(string name, List<float> vertices = null, List<float> normals = null, List<uint> indices = null, List<float> textureCoordinants = null)
        {
            Name = name;
            Vertices = vertices;
            Normals = normals;
            Indices = indices;
            TextureCoordinants = textureCoordinants;

            if (vertices != null || normals != null || indices != null)
            {
                InitializeBuffers(false);
            }
        }

        public void InitializeBuffers(bool flipVerticalNormals)
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ibo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            if (normalTexture != -1)
                ComputeTangents(Normals, flipVerticalNormals);

            List<float> vertexData = new();
            for (int i = 0; i < Vertices.Count / 3; i++)
            {
                vertexData.AddRange(Vertices.GetRange(i * 3, 3));
                vertexData.AddRange(Normals.GetRange(i * 3, 3));
                if (TextureCoordinants != null) vertexData.AddRange(TextureCoordinants.GetRange(i * 2, 2));
                if (normalTexture != -1)
                {
                    vertexData.AddRange(Tangents.GetRange(i * 3, 3));
                    vertexData.AddRange(Bitangents.GetRange(i * 3, 3));
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Count * sizeof(float), vertexData.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint), Indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = TextureCoordinants != null ? normalTexture != -1 ? 14 : 8 : 6;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            if (TextureCoordinants != null)
            {
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride * sizeof(float), 6 * sizeof(float));
                GL.EnableVertexAttribArray(2);
            }
            if (normalTexture != -1)
            {
                GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 8 * sizeof(float));
                GL.EnableVertexAttribArray(3);

                GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 11 * sizeof(float));
                GL.EnableVertexAttribArray(4);
            }
            GL.BindVertexArray(0);
        }
        public void DisposeBuffers()
        {
            if (_vao != 0)
            {
                GL.DeleteVertexArray(_vao);
                _vao = 0;
            }
            if (_vbo != 0)
            {
                GL.DeleteBuffer(_vbo);
                _vbo = 0;
            }
            if (_ibo != 0)
            {
                GL.DeleteBuffer(_ibo);
                _ibo = 0;
            }
        }

        public void Render(Shader shader, int instanceCount = 1)
        {
            Matrix4 model =
                Matrix4.CreateScale(Scale) *
                Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X)) *
                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y)) *
                Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z)) *
                Matrix4.CreateTranslation(Position);

            GL.UniformMatrix4(shader.GetUniform("uModel"), false, ref model);

            GL.BindVertexArray(_vao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0, instanceCount);
            GL.BindVertexArray(0);
        }
        public void ComputeTangents(List<float> normals, bool flipV = false)
        {
            int vertexCount = Vertices.Count / 3;
            Tangents.Clear();
            Bitangents.Clear();
            Tangents.AddRange(new float[vertexCount * 3]);
            Bitangents.AddRange(new float[vertexCount * 3]);

            // Optional V flip (if coming from Blender)
            if (flipV)
            {
                for (int i = 1; i < TextureCoordinants.Count; i += 2)
                    TextureCoordinants[i] = 1.0f - TextureCoordinants[i];
            }

            for (int i = 0; i < Indices.Count; i += 3)
            {
                int i0 = (int)Indices[i];
                int i1 = (int)Indices[i + 1];
                int i2 = (int)Indices[i + 2];

                Vector3 v0 = new Vector3(Vertices[i0 * 3], Vertices[i0 * 3 + 1], Vertices[i0 * 3 + 2]);
                Vector3 v1 = new Vector3(Vertices[i1 * 3], Vertices[i1 * 3 + 1], Vertices[i1 * 3 + 2]);
                Vector3 v2 = new Vector3(Vertices[i2 * 3], Vertices[i2 * 3 + 1], Vertices[i2 * 3 + 2]);

                Vector2 uv0 = new Vector2(TextureCoordinants[i0 * 2], TextureCoordinants[i0 * 2 + 1]);
                Vector2 uv1 = new Vector2(TextureCoordinants[i1 * 2], TextureCoordinants[i1 * 2 + 1]);
                Vector2 uv2 = new Vector2(TextureCoordinants[i2 * 2], TextureCoordinants[i2 * 2 + 1]);

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector2 deltaUV1 = uv1 - uv0;
                Vector2 deltaUV2 = uv2 - uv0;

                float denom = deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y;
                if (Math.Abs(denom) < 1e-6f)
                    continue;

                float f = 1.0f / denom;

                Vector3 tangent = f * (deltaUV2.Y * edge1 - deltaUV1.Y * edge2);
                Vector3 bitangent = f * (-deltaUV2.X * edge1 + deltaUV1.X * edge2);

                void AddTBN(int idx, Vector3 t, Vector3 b)
                {
                    Tangents[idx * 3 + 0] += t.X;
                    Tangents[idx * 3 + 1] += t.Y;
                    Tangents[idx * 3 + 2] += t.Z;

                    Bitangents[idx * 3 + 0] += b.X;
                    Bitangents[idx * 3 + 1] += b.Y;
                    Bitangents[idx * 3 + 2] += b.Z;
                }

                AddTBN(i0, tangent, bitangent);
                AddTBN(i1, tangent, bitangent);
                AddTBN(i2, tangent, bitangent);
            }

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 n = new Vector3(normals[i * 3], normals[i * 3 + 1], normals[i * 3 + 2]);
                Vector3 t = new Vector3(Tangents[i * 3], Tangents[i * 3 + 1], Tangents[i * 3 + 2]);
                Vector3 b = new Vector3(Bitangents[i * 3], Bitangents[i * 3 + 1], Bitangents[i * 3 + 2]);

                // Handle near-zero tangents
                if (t.LengthSquared < 1e-6f) t = Vector3.UnitX;
                if (b.LengthSquared < 1e-6f) b = Vector3.UnitY;

                t = Vector3.Normalize(t - n * Vector3.Dot(n, t));
                b = Vector3.Normalize(Vector3.Cross(n, t));

                float handedness = Vector3.Dot(Vector3.Cross(n, t), b) < 0.0f ? -1.0f : 1.0f;

                Bitangents[i * 3 + 0] = b.X * handedness;
                Bitangents[i * 3 + 1] = b.Y * handedness;
                Bitangents[i * 3 + 2] = b.Z * handedness;

                Tangents[i * 3 + 0] = t.X;
                Tangents[i * 3 + 1] = t.Y;
                Tangents[i * 3 + 2] = t.Z;
            }
        }

        public virtual void Update(float deltaTime)
        {

        }
    }
}
