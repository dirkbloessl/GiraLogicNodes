using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System.Collections.Generic;
using System;

namespace DB.GiraSDK.UnifiNodes
{ 
    public class UnifiPresenceDetectionNode : LogicNodeBase
    {

        private const string InputPrefix = "Device";

        private ITypeService TypeService;

        private Dictionary<string, string> DefinedDevicesAndPersons;

        [Input]
        public BoolValueObject Trigger { get; private set; }

        [Parameter(DisplayOrder = 2, InitOrder = 1, IsDefaultShown = false, IsRequired = true)]
        public StringValueObject BaseUrl { get; private set; }

        [Parameter(DisplayOrder = 3, InitOrder = 2, IsDefaultShown = false, IsRequired = true)]
        public StringValueObject SiteId { get; private set; }

        [Parameter(DisplayOrder = 4, InitOrder = 3, IsDefaultShown = false, IsRequired = true)]
        public StringValueObject Username { get; private set; }

        [Parameter(DisplayOrder = 5, InitOrder = 4, IsDefaultShown = false, IsRequired = true)]
        public StringValueObject Password { get; private set; }

        [Parameter(DisplayOrder = 6, InitOrder = 5, IsDefaultShown = false, IsRequired = true)]
        public IntValueObject DeviceAmount { get; private set; }

        [Parameter(DisplayOrder = 7, InitOrder = 6, IsDefaultShown = false, IsRequired = true)]
        public IList<IValueObject> Devices { get; private set; }

        [Output(DisplayOrder = 1, IsDefaultShown = true)]
        public IntValueObject ConnectedAmount { get; private set; }

        [Output(DisplayOrder = 2, IsDefaultShown = true)]
        public BoolValueObject IsOneConnected { get; private set; }

        [Output(DisplayOrder = 3, IsDefaultShown = true)]
        public BoolValueObject IsAllConnected { get; private set; }

        [Output(DisplayOrder = 4, IsDefaultShown = true)]
        public StringValueObject ConnectedPersons { get; private set; }

        [Output]
        public StringValueObject Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnifiPresenceDetectionNode"/> class.
        /// </summary>
        /// <param name="context">The node context.</param>
        public UnifiPresenceDetectionNode(INodeContext context)
        : base(context)
        {
            context.ThrowIfNull("context");
            
            // Get the TypeService from the context
            this.TypeService = context.GetService<ITypeService>();
            this.Trigger = this.TypeService.CreateBool(PortTypes.Bool, "Trigger", false);
            this.BaseUrl = this.TypeService.CreateString(PortTypes.String, "BaseURL", "https://127.0.0.1:8443");
            this.SiteId = this.TypeService.CreateString(PortTypes.String, "UniFi Site", "default");
            this.Username = this.TypeService.CreateString(PortTypes.String, "Username", "");
            this.Password = this.TypeService.CreateString(PortTypes.String, "Password", "");

            this.DeviceAmount = this.TypeService.CreateInt(PortTypes.Integer, "Anzahl Geräte", 1);
            this.DeviceAmount.MinValue = 1;
            this.DeviceAmount.MaxValue = 15;

            this.Devices = new List<IValueObject>();
            this.DeviceAmount.ValueSet += this.GenerateDevicePorts;

            this.ConnectedAmount = this.TypeService.CreateInt(PortTypes.Integer, "Anzahl angemeldete Clients", 0);
            this.IsOneConnected = this.TypeService.CreateBool(PortTypes.Bool, "Mindestens ein Device angemeldet", false);
            this.IsAllConnected = this.TypeService.CreateBool(PortTypes.Bool, "Alle Devices angemeldet", false);
            this.ConnectedPersons = this.TypeService.CreateString(PortTypes.String, "Angemeldete Personen", "");

            this.Error = this.TypeService.CreateString(PortTypes.String, "Fehlermeldung", "");

            this.DeviceAmount.Value = 1;

            this.DefinedDevicesAndPersons = new Dictionary<string, string>();
        }

        int lastValue = 0;

        private void GenerateDevicePorts(object sender, ValueChangedEventArgs e)
        {

            if (this.DeviceAmount.Value > this.lastValue)
            {
                while (this.lastValue != this.DeviceAmount.Value)
                {
                    IValueObject Port1 = this.TypeService.CreateString(PortTypes.String, "Gerät " + (this.lastValue + 1));
                    Devices.Add(Port1);

                    IValueObject Port2 = this.TypeService.CreateString(PortTypes.String, "Person " + (this.lastValue + 1));
                    Devices.Add(Port2);

                    this.lastValue++;
                }
            }
            else
            {
                while (this.lastValue != this.DeviceAmount.Value)
                {
                    int c = Devices.Count;
                    Devices.RemoveAt(c - 1);
                    Devices.RemoveAt(c - 2);
                    this.lastValue--;
                }

            }
        }

        protected void FillDefinedLists()
        {
            for (int i = 0; i < this.Devices.Count; i+=2)
            {
                var device = (String)this.Devices[i].Value;
                var person = (String)this.Devices[(i + 1)].Value;
                this.DefinedDevicesAndPersons.Add(device, person);
            }
        }

        /// <summary>
        /// This function is called every time any input (marked by attribute [Input]) receives a value and no input has no value.
        /// The inputs that were updated for this function to be called, have <see cref="IValueObject.WasSet"/> set to true. After this function returns 
        /// the <see cref="IValueObject.WasSet"/> flag will be reset to false.
        /// </summary>
        public override void Execute()
        {
            if (!this.Trigger.HasValue || !this.Trigger.WasSet || !this.Trigger.Value)
            {
                Error.BlockGraph();
                return;
            }

            this.FillDefinedLists();
            this.GetDataFromApi();
        }

        protected async void GetDataFromApi()
        {
            using (var uniFiApi = new Api.Api(new Uri(this.BaseUrl), this.SiteId))
            {
                uniFiApi.DisableSslValidation();
                var authenticationSuccessful = await uniFiApi.Authenticate(this.Username, this.Password);

                if (!authenticationSuccessful)
                {
                    this.Error.Value = "- Authentication failed";
                    return;
                }

                var activeClients = await uniFiApi.GetActiveClients();
                foreach (var activeClient in activeClients)
                {
                    if(DefinedDevicesAndPersons.ContainsKey(activeClient.MacAddress)
                        || DefinedDevicesAndPersons.ContainsKey(activeClient.Hostname)
                        || DefinedDevicesAndPersons.ContainsKey(activeClient.IpAddress)
                        || DefinedDevicesAndPersons.ContainsKey(activeClient.FriendlyName))
                    {
                        this.ConnectedAmount.Value++;
                        this.IsOneConnected.Value = true;
                    }
                }

                if(DefinedDevicesAndPersons.Count == this.ConnectedAmount && this.IsOneConnected)
                {
                    this.IsAllConnected.Value = true;
                }

            }
            return;
        }
    }
}
