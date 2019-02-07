using System;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace DB.GiraSDK.WeatherNodes
{
    /// <summary>
    /// Class converts relative humidity and temperature to an absolute humidity
    /// </summary>
    public class HumidityConverterNode : LogicNodeBase
    {
        [Input(DisplayOrder = 1, IsInput = true, IsRequired = false)]
        public DoubleValueObject Temperature { get; private set; }

        [Input(DisplayOrder = 2, IsInput = true, IsRequired = false)]
        public DoubleValueObject RelativeHumidity { get; private set; }

        [Output(DisplayOrder = 1, IsRequired = true)]
        public DoubleValueObject AbsoluteHumidity { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="HumidityConverterNode"/> class.
        /// </summary>
        /// <param name="context">The node context.</param>
        public HumidityConverterNode(INodeContext context)
        : base(context)
        {
            context.ThrowIfNull("context");
            // Get the TypeService from the context
            var typeService = context.GetService<ITypeService>();

            this.Temperature = typeService.CreateDouble(PortTypes.Temperature, "Temperatur (°C)");
            this.Temperature.MinValue = -50;
            this.Temperature.MaxValue = 50;

            this.RelativeHumidity = typeService.CreateDouble(PortTypes.Float, "Relative Luftfeuchtigkeit (%)");
            this.RelativeHumidity.MinValue = 0;
            this.RelativeHumidity.MaxValue = 100;

            this.AbsoluteHumidity = typeService.CreateDouble(PortTypes.Float, "Absolute Luftfeuchtigkeit (g/m3)");
        }

        public override void Execute()
        {
            if (!this.Temperature.HasValue || !this.RelativeHumidity.HasValue)
            {
                AbsoluteHumidity.BlockGraph();
                return;
            }

            AbsoluteHumidity.Value = CalculateAbsoluteHumidity(Temperature.Value, RelativeHumidity.Value);
        }

        protected double CalculateAbsoluteHumidity(double Temp, double RelHumidity)
        {
            double result = (6.112 * Math.Exp((17.67 * Temp) / (Temp + 243.5)) * RelHumidity * 2.1674) / (273.15 + Temp);
            return Math.Round(result, 2);
        }
    }
}
