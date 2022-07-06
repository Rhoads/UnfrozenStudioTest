using System;
using System.Collections.Generic;
using System.Text;

namespace USTestChat.Server
{
	class NetClient
	{
		public int connectionId;

		public bool connectMsgReceived;

		public string name;
		public string color;

		public NetClient(int connectionId)
		{
			this.connectionId = connectionId;
		}
	}
}
