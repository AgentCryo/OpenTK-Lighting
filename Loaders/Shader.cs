using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_Lighting.Loaders
{
    public class Shader
    {
        public int Handle { get; private set; }

		private Dictionary<string, int> _uniformLocations = new();
		public Shader(string vertexPath, string fragmentPath, string geometryPath = null)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            CheckCompile(vertexShader, "VERTEX");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);
            CheckCompile(fragmentShader, "FRAGMENT");

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);
            CheckLink(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private void CheckCompile(int shader, string type)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                throw new Exception($"{type} SHADER COMPILATION ERROR:\n{info}");
            }
        }

        private void CheckLink(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetProgramInfoLog(program);
                throw new Exception($"SHADER LINKING ERROR:\n{info}");
            }
        }

        public void SetBool(string name, bool value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location == -1)
                throw new Exception($"Uniform '{name}' not found.");
            GL.Uniform1(location, value ? 1 : 0);
        }
        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location == -1)
                throw new Exception($"Uniform '{name}' not found.");
            GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location == -1)
                throw new Exception($"Uniform '{name}' not found.");
            GL.Uniform1(location, value);
        }

        public void Use() => GL.UseProgram(Handle);

		public int GetUniform(string name)
		{
			if (_uniformLocations.TryGetValue(name, out int location)) return location;

			location = GL.GetUniformLocation(Handle, name);

			if (location == -1) Console.WriteLine($"Warning: Uniform '{name}' not found in shader {Handle}!");

			_uniformLocations[name] = location;
			return location;
		}
	}
}
