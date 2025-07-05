using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK_Lighting.ObjectTypes.Components
{
	public abstract class Component
	{
		public Object Owner;
		public abstract void Load();
		public abstract void Update(float dt);
		public abstract void InspectorIMGUI();
	}
}
