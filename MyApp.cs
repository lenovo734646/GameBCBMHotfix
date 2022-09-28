using Hotfix.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.BCBM
{
	public class MyApp : AppBase
	{
		public ThisGameConfig conf = new ThisGameConfig();
		public override void Start()
		{
			game = new GameController();
			conf.Init();
			base.Start();
		}
	}
}
