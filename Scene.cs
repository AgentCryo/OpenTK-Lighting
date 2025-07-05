using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = OpenTK_Lighting.ObjectTypes.Object;

namespace OpenTK_Lighting
{
	public class Scene
	{
		public List<Object> Objects = new List<Object>();

		public void Load()
		{
			foreach (var obj in Objects) obj.Load();
		}
		public void Update(float deltaTime)
		{
			foreach (var obj in Objects) obj.Update(deltaTime);
		}
	}
}
