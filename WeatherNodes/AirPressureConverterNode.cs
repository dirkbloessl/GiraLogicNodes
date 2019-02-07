using System;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace DB.GiraSDK.WeatherNodes
{
    /// <summary>
    /// Class converts absolute air pressure to relative air pressure
    /// </summary>
    public class AirPressureConverterNode : LogicNodeBase
    {
        [Input(DisplayOrder = 1, IsInput = true, IsRequired = false)]
        public DoubleValueObject Temperature { get; private set; }

        [Input(DisplayOrder = 2, IsInput = true, IsRequired = false)]
        public DoubleValueObject AbsoluteAirPressure { get; private set; }

        [Parameter(DisplayOrder = 3, IsRequired = true)]
        public IntValueObject MeasurementHeight { get; private set; }

        [Output(DisplayOrder = 1, IsRequired = true)]
        public DoubleValueObject RelativeAirPressure { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="AirPressureConverterNode"/> class.
        /// </summary>
        /// <param name="context">The node context.</param>
        public AirPressureConverterNode(INodeContext context)
        : base(context)
        {
            context.ThrowIfNull("context");
            // Get the TypeService from the context
            var typeService = context.GetService<ITypeService>();

            this.Temperature = typeService.CreateDouble(PortTypes.Temperature, "Temperatur (°C)");
            this.Temperature.MinValue = -50;
            this.Temperature.MaxValue = 50;

            this.AbsoluteAirPressure = typeService.CreateDouble(PortTypes.Float, "Absoluter Luftdruck (hPA)");
            this.MeasurementHeight = typeService.CreateInt(PortTypes.Integer, "Messhöhe über N.N.");
            this.RelativeAirPressure = typeService.CreateDouble(PortTypes.Float, "Relative Luftfeuchtigkeit (hPA)");
        }

        public override void Execute()
        {
            if (!this.Temperature.HasValue || !this.AbsoluteAirPressure.HasValue || !this.MeasurementHeight.HasValue)
            {
                RelativeAirPressure.BlockGraph();
                return;
            }

            RelativeAirPressure.Value = CalculateRelativeAirPressure(Temperature.Value, AbsoluteAirPressure.Value, MeasurementHeight.Value);
        }

        protected double CalculateRelativeAirPressure(double Temp, double AbsAirPressure, int MeasureHeight)
        {
            double a, x;
            if(Temp < 9.1)
            {
                a = 5.6402 * (-0.0916 + Math.Exp(0.06 * Temp));
            } else
            {
                a = 18.2194 * (1.0463 - Math.Exp(-0.0666 * Temp));
            }

            x = MeasureHeight * 9.80665 / (287.05 * (Temp + 273.15 + 0.12 * a + 0.0065 * MeasureHeight / 2));
            return AbsAirPressure * Math.Exp(x);
        }
    }
}
