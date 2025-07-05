using ImGuiNET;
using OpenTK_Lighting.Helpers;
using OpenTK_Lighting.ObjectTypes.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK_Lighting.ObjectTypes
{
    public class Object
    {
		public string Name;
		public Transform Transform = new Transform();

		public List<Component> Components = new();

		#region Component Calls
		public virtual void Load()
        {
			foreach (var c in Components)
			{
				c.Owner = this;
				c.Load();
			}
		}
		public virtual void Update(float deltaTime)
		{
			foreach (var c in Components)
			{
				c.Update(deltaTime);
			}
		}
		public virtual void InspectorIMGUI()
        {
			ImGui.Text($"Object: {Name}");
			if (ImGui.CollapsingHeader("Transform"))
			{
				ImGui.Indent();
				Transform.InspectorIMGUI();
				ImGui.Unindent();
			}
			foreach (var c in Components)
				c.InspectorIMGUI();
		}
		#endregion

		#region Helpers
		public void AddComponent(Component component)
		{
			component.Owner = this;
			Components.Add(component);
		}
		#endregion
	}
}
