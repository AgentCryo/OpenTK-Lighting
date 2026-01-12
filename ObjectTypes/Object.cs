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
		string renameBuffer;
		Boolean openRename = false;
		
		public Transform Transform = new Transform();

		public List<Component> Components = new();
		
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

		public void Delete(ref List<Object> objects)
		{
			Components.Clear();
			objects.Remove(this);
		}
		
		public virtual void InspectorIMGUI(ref List<Object> objects)
        {
			ImGui.Text($"Object: {Name}");

			ObjectContexMenu(ref objects);
			
			if (openRename)
			{
				ImGui.OpenPopup("rename_popup");
				openRename = false;
			}
			if (ImGui.BeginPopup("rename_popup"))
			{
				ImGui.Text("Rename:");
				if (ImGui.InputText("##rename", ref renameBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue))
				{
					Name = renameBuffer;
					ImGui.CloseCurrentPopup();
				}

				if (ImGui.Button("OK"))
				{
					Name = renameBuffer;
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
			
			if (ImGui.CollapsingHeader("Transform"))
			{
				ImGui.Indent();
				Transform.InspectorIMGUI();
				ImGui.Unindent();
			}
			foreach (var c in Components)
				c.InspectorIMGUI();
		}

		public bool ObjectContexMenu(ref List<Object> objects, bool useItem = false)
		{
			if (ImGui.BeginPopupContextItem("object_context"))
			{
				if (ImGui.MenuItem("Rename"))
				{
					openRename = true;
					renameBuffer = Name;
					ImGui.EndPopup();
					return false;
				}
				if (ImGui.MenuItem("Delete"))
				{
					this.Delete(ref objects);
					ImGui.EndPopup();
					return true;
				}
				ImGui.EndPopup();
			}

			if (useItem) {
				if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("object_context");
			} else {
				if (ImGui.IsWindowHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("object_context");
			}
			return false;
		}
		
		#region Helpers
		public void AddComponent(Component component)
		{
			component.Owner = this;
			Components.Add(component);
		}
		#endregion
	}
}
