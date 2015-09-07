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
        private ISystemDiagnosticsService _SystemDiagnosticsService;
        private IDisplayLayoutService _DisplayLayoutService;
        private IDisplayControlService _DisplayControlService;

        public string Name
        {
            get { return "Diagnostics example"; }
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

            //Do init with pluginSettings
            int intervalSeconds = 10;
            if (pluginSettings.ContainsKey("intervalSeconds"))
            {
                int i;
                if (Int32.TryParse(pluginSettings["intervalSeconds"], out i))
                {
                    intervalSeconds = i;
                }
                else
                {
                    _PlayerPluginContext.Log(LogLevel.Error, "Failed to parse intervalSeconds from " + pluginSettings["intervalSeconds"]);
                }
            }

            //Register some services
            ITimerService timerService = _PlayerPluginContext.GetService<ITimerService>();
            timerService.RequestRepeatingCallback(new TimeSpan(0, 0, 10), new Action(TimerCallback));

            ICommandService commandService = _PlayerPluginContext.GetService<ICommandService>();
            commandService.RegisterCallback(ExecuteCommand);

            IReceiveDataService receiveDataService = _PlayerPluginContext.GetService<IReceiveDataService>();
            receiveDataService.ValuesChanged += new ValuesChangedEventHandler(receiveDataService_ValuesChanged);

            _DisplayLayoutService = _PlayerPluginContext.GetService<IDisplayLayoutService>();
            _DisplayLayoutService.DisplayLayoutChanged += _DisplayLayoutService_DisplayLayoutChanged;
            LogDisplayLayouts(_DisplayLayoutService.DisplayHeadsLayout, _DisplayLayoutService.VirtualDisplayLayout, _DisplayLayoutService.PhysicalDisplayLayout);

            _DisplayControlService = _PlayerPluginContext.GetService<IDisplayControlService>();

            ISystemInformationService systemInformationService = _PlayerPluginContext.GetService<ISystemInformationService>();
            systemInformationService.AppSettingsChanged += new AppSettingsChangedEventHandler(systemInformationService_AppSettingsChanged);
            _PlayerPluginContext.Log(LogLevel.Information, "System version " + systemInformationService.Version);
            _PlayerPluginContext.Log(LogLevel.Information, "System type " + systemInformationService.Type);
            _PlayerPluginContext.Log(LogLevel.Information, "AppSetting: fileCache=" + systemInformationService.GetAppSetting("fileCache"));

            _SystemDiagnosticsService = _PlayerPluginContext.GetService<ISystemDiagnosticsService>();

            _PlayerPluginContext.SetState(State.OK, "Up and running");

            return true;
        }

        private void LogDisplayLayouts(IDisplayLayout displayHeadsLayout, IDisplayLayout virtualDisplayLayout, IDisplayLayout physicalDisplayLayout)
        {
            LogDisplayLayout("Display heads layout", displayHeadsLayout);
            LogDisplayLayout("Virtual display layout", virtualDisplayLayout);
            LogDisplayLayout("Physical display layout", physicalDisplayLayout);
        }

        private void LogDisplayLayout(string title, IDisplayLayout displayHeadsLayout)
        {
            if (displayHeadsLayout != null)
            {
                if (displayHeadsLayout.Views != null && displayHeadsLayout.Views.Count > 0)
                {
                    _PlayerPluginContext.Log(LogLevel.Information, title);
                    foreach (KeyValuePair<int, IDisplayLayoutView> view in displayHeadsLayout.Views)
                    {
                        _PlayerPluginContext.Log(LogLevel.Information, "  view " + view.Key + " x " + view.Value.X + " y " + view.Value.Y + " w " + view.Value.Width + " h " + view.Value.Height);
                    }
                }
                else
                {
                    _PlayerPluginContext.Log(LogLevel.Information, title + " no views");
                }
            }
            else
            {
                _PlayerPluginContext.Log(LogLevel.Information, title + " no layout");
            }
        }

        private void _DisplayLayoutService_DisplayLayoutChanged(object sender, DisplayLayoutChangedEventArgs e)
        {
            _PlayerPluginContext.Log(LogLevel.Information, "Display layouts changed");
            LogDisplayLayouts(e.DisplayHeadsLayout, e.VirtualDisplayLayout, e.PhysicalDisplayLayout);
        }

        private void systemInformationService_AppSettingsChanged(object sender, AppSettingsChangedEventArgs e)
        {
            foreach (KeyValuePair<string, string> setting in e.ChangedAppSettings)
            {
                _PlayerPluginContext.Log(LogLevel.Information, "AppSetting changed " + setting.Key + "=" + setting.Value);
            }
        }

        private void receiveDataService_ValuesChanged(object sender, ValuesChangedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach(KeyValuePair<string, string> value in e.ChangedValues)
            {
                sb.Append(value.Key + "=" + value.Value + " ");
            }
            _PlayerPluginContext.Log(LogLevel.Information, "Received data " + sb.ToString());
        }

        private void ExecuteCommand(IPluginCommand pluginCommand)
        {
            _PlayerPluginContext.Log(LogLevel.Information, "Command " + pluginCommand.Key + "=" + pluginCommand.Value + " intercepted");
            pluginCommand.ReportProgress(0.5f, "Plugin command executing");
            pluginCommand.ReturnResult("OK");
        }

        private void TimerCallback()
        {
            _PlayerPluginContext.SetState(State.OK, "Timer callback");

            _PlayerPluginContext.Log(LogLevel.Information, "An information log message");
            _PlayerPluginContext.Log(LogLevel.Warning, "A warning log message");
            _PlayerPluginContext.Log(LogLevel.Error, "An error log message");

            //Report some diagnostics
            _SystemDiagnosticsService.ReportDiagnostics(new SystemDiagnosticsState()
                {
                    State = State.Warning, //State from diagnostics
                    Message = "Warning message from plugin diagnostics service",
                    ComputerName = "This computer",
                    OSInfo = "Operating system information",
                    TemperatureMeasure = new Measure()
                    {
                        Min = 45,     //Current min value from all sensors      
                        Average = 50, //Current average from all sensors
                        Max = 80      //Current max value from all sensors      
                    }
                });

            List<DisplayState> physicalDisplayState = new List<DisplayState>();
            physicalDisplayState.Add(new DisplayState()
            {
                Id = "Physical display 1",
                DisplayLayoutView = 1,
                State = State.OK,
                AspectRatio = "16:9",
                Vendor = "My screen",
                Orientation = Orientation.Landscape,
                PowerState = PowerState.On,
                Temperature = new Measure()
                {
                    Min = 35,     //Current min value from all sensors      
                    Average = 40, //Current average from all sensors
                    Max = 50      //Current max value from all sensors      
                },
                Input = InputType.HDMI,
                InputNumber = 1,
                Model = "My screen model",
                DiagonalSize = 65,
                Message = "Screen is working fine"
            });
            physicalDisplayState.Add(new DisplayState()
            {
                Id = "Physical display 2",
                DisplayLayoutView = 2,
                State = State.OK,
                AspectRatio = "16:9",
                Vendor = "My screen",
                Orientation = Orientation.Landscape,
                PowerState = PowerState.On,
                Temperature = new Measure()
                {
                    Min = 35,     //Current min value from all sensors      
                    Average = 40, //Current average from all sensors
                    Max = 50      //Current max value from all sensors      
                },
                Input = InputType.HDMI,
                InputNumber = 2,
                Model = "My screen model",
                DiagonalSize = 65,
                Message = "Screen is working fine"
            });

            List<DisplayState> virtualDisplayStates = new List<DisplayState>();
            virtualDisplayStates.Add(new DisplayState()
            {
                Id = "Virtual display 1",
                DisplayLayoutView = 1,
                State = State.OK,
                AspectRatio = "16:9",
                Vendor = "My view",
                Orientation = Orientation.Landscape,
                PowerState = PowerState.On,
                Temperature = new Measure()
                {
                    Min = 35,     //Current min value from all sensors      
                    Average = 40, //Current average from all sensors
                    Max = 50      //Current max value from all sensors      
                },
                Input = InputType.HDMI,
                InputNumber = 1,
                Model = "My virtual model",
                Message = "Virtual display is working fine"
            });
            virtualDisplayStates.Add(new DisplayState()
            {
                Id = "Virtual display 2",
                DisplayLayoutView = 2,
                State = State.OK,
                AspectRatio = "16:9",
                Vendor = "My view",
                Orientation = Orientation.Landscape,
                PowerState = PowerState.On,
                Temperature = new Measure()
                {
                    Min = 35,     //Current min value from all sensors      
                    Average = 40, //Current average from all sensors
                    Max = 50      //Current max value from all sensors      
                },
                Input = InputType.HDMI,
                InputNumber = 2,
                Model = "My virtual model",
                Message = "Virtual display is working fine"
            });
            _DisplayControlService.ReportDiagnostics(physicalDisplayState, virtualDisplayStates, null);
        }

        public string GetDocumentation()
        {
            return "Example plugin demonstrates a few features in the Instoremedia Plugin API";
        }

        public bool Check()
        {
            //Check is initiated by the server to perform a user triggered system check but without disturbing ongoing operation

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
