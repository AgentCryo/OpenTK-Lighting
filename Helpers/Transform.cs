using ImGuiNET;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace OpenTK_Lighting.Helpers
{
	public class Transform
	{
		public Vector3 Position = Vector3.Zero;
		public Vector3 Rotation = Vector3.Zero;
		public Vector3 Scale = Vector3.One;

		public Transform Parent = null;
		public List<Transform> Children = new List<Transform>();

		public Matrix4 LocalMatrix
		{
			get
			{
				Matrix4 translation = Matrix4.CreateTranslation(Position);
				Matrix4 rotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X)) *
								   Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y)) *
								   Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
				Matrix4 scale = Matrix4.CreateScale(Scale);

				return scale * rotation * translation;
			}
		}

		public Matrix4 WorldMatrix
		{
			get
			{
				if (Parent != null)
					return LocalMatrix * Parent.WorldMatrix;
				else
					return LocalMatrix;
			}
		}

		public void AddChild(Transform child)
		{
			child.Parent = this;
			Children.Add(child);
		}

		public void InspectorIMGUI()
		{
			// Position
			ImGui.Text("Position");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			Vector3 pos = Position;
			ImGui.DragFloat("X##pos", ref pos.X, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Y##pos", ref pos.Y, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Z##pos", ref pos.Z, 0.05f);
			Position = pos;

			// Rotation
			ImGui.Text("Rotation");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			Vector3 rot = Rotation;
			ImGui.DragFloat("X##rot", ref rot.X, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Y##rot", ref rot.Y, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Z##rot", ref rot.Z, 0.05f);
			Rotation = rot;

			// Scale
			ImGui.Text("Scale   ");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			Vector3 scale = Scale;
			ImGui.DragFloat("X##scale", ref scale.X, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Y##scale", ref scale.Y, 0.05f);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(70);
			ImGui.DragFloat("Z##scale", ref scale.Z, 0.05f);
			Scale = scale;
		}
	}
}
