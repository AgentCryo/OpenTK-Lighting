using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace OpenTK_Lighting.Loaders
{
	public class OBJ_Parser
	{
	public static (
		List<float> Vertices,          // xyz xyz ...
		List<uint> Indices,           // one uint per element in each face;
		List<float> TextureCoordinants,// uv  uv ...
		List<float> Normals            // xyz xyz ...
	) ParseOBJFile(string filePath)
		{
			//temporary storage for the raw OBJ data;
			var tempPositions = new List<Vector3>();   // v
			var tempTexCoords = new List<Vector2>();   // vt
			var tempNormals = new List<Vector3>();   // vn

			//final buffers;
			var verticesOut = new List<float>();
			var texCoordsOut = new List<float>();
			var normalsOut = new List<float>();
			var indicesOut = new List<uint>();

			// Each unique (v,vt,vn) triple becomes one “final” vertex.
			// Dictionary maps that triple -> new index.
			var vertexCache = new Dictionary<(int v, int vt, int vn), uint>();

			foreach (var line in File.ReadLines(filePath))
			{
				if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

				var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				switch (parts[0])
				{
					case "v":   // position;
						tempPositions.Add(new Vector3(
							float.Parse(parts[1], CultureInfo.InvariantCulture),
							float.Parse(parts[2], CultureInfo.InvariantCulture),
							float.Parse(parts[3], CultureInfo.InvariantCulture)));
						break;

					case "vt":  // tex-coord (u v);
						tempTexCoords.Add(new Vector2(
							float.Parse(parts[1], CultureInfo.InvariantCulture),
							float.Parse(parts[2], CultureInfo.InvariantCulture)));
						break;

					case "vn":  // normal;
						tempNormals.Add(new Vector3(
							float.Parse(parts[1], CultureInfo.InvariantCulture),
							float.Parse(parts[2], CultureInfo.InvariantCulture),
							float.Parse(parts[3], CultureInfo.InvariantCulture)));
						break;

					case "f":   // face: f v/vt/vn ...
						Span<string> faceTokens = parts.AsSpan()[1..];

						// triangulate n-gons on the fly (fan method);
						for (int i = 1; i < faceTokens.Length - 1; ++i)
						{
							AddVertex(faceTokens[0]);
							AddVertex(faceTokens[i]);
							AddVertex(faceTokens[i + 1]);
						}
						break;
				}
			}

			return (verticesOut, indicesOut, texCoordsOut, normalsOut);

			//local helper;
			void AddVertex(string token)
			{
				var comps = token.Split('/');
				int v = int.Parse(comps[0]) - 1;
				int vt = comps.Length > 1 && comps[1] != "" ? int.Parse(comps[1]) - 1 : -1;
				int vn = comps.Length > 2 && comps[2] != "" ? int.Parse(comps[2]) - 1 : -1;

				var key = (v, vt, vn);
				if (!vertexCache.TryGetValue(key, out uint index))
				{
					// Position;
					var pos = tempPositions[v];
					verticesOut.AddRange(new[] { pos.X, pos.Y, pos.Z });

					// TexCoord (fill zeros if none);
					if (vt >= 0)
					{
						var uv = tempTexCoords[vt];
						texCoordsOut.AddRange(new[] { uv.X, uv.Y });
					}
					else
					{
						texCoordsOut.AddRange(new[] { 0f, 0f });
					}

					// Normal (fill zeros if none);
					if (vn >= 0)
					{
						var nrm = tempNormals[vn];
						normalsOut.AddRange(new[] { nrm.X, nrm.Y, nrm.Z });
					}
					else
					{
						normalsOut.AddRange(new[] { 0f, 0f, 0f });
					}

					index = (uint)(verticesOut.Count / 3 - 1);  // one position per vertex
					vertexCache[key] = index;
				}

				indicesOut.Add(index);
			}
		}
	} 
}
