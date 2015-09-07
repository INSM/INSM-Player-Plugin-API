using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using INSM.Player.Plugin.Framework.v2;

namespace INSM.Example.Player.Plugin
{
    public class MyPlugin : IPlayerPlugin
    {
        private IPlayerPluginContext _PlayerPluginContext;

        public string Name
        {
            get { return "EmptyExample"; }
        }

        public string Vendor
        {
            get { return "INSM"; }
        }

        public string Version
        {
            get { return "1.0"; }
        }

        public bool Initialize(IPlayerPluginContext playerPluginContext, IDictionary<string, string> pluginSettings)
        {
            _PlayerPluginContext = playerPluginContext;

            _PlayerPluginContext.SetState(State.OK, "Up and running");

            return true;
        }

        public string GetDocumentation()
        {
            return "Empty example plugin demonstrates a minimal Instoremedia Plugin API";
        }

        public bool Check()
        {
            //Do some internal run-time checks and report all is ok
            return true;
        }

        public IDictionary<string, string> DefaultPluginSettings
        {
            get 
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
