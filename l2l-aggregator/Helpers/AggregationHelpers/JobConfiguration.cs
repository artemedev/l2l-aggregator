namespace l2l_aggregator.Helpers.AggregationHelpers
{
    public class JobConfiguration : MD.ServiceSettings
    {
        /// <summary>
        /// Какая головка принтера используется
        /// для печати этикеток
        /// </summary>
        public FastReport.Export.Zpl.ZplExport.ZplDensity ZplDensity { get; set; } = FastReport.Export.Zpl.ZplExport.ZplDensity.d6_dpmm_152_dpi;

        /// <summary>
        /// Количество кодов
        /// генерируемых за один проход
        /// </summary>
        public int QuantityPerPass { get; set; } = 100;
    }

}
