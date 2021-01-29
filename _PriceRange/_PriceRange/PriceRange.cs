namespace Notadream
{
    using System;
    using cAlgo.API;

    /// <summary>
    /// 显示每个bar实体和整体长度的指标。
    /// </summary>
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class PriceRange : Indicator
    {
        #region 变量

        /// <summary>
        /// 参数是否有效。
        /// </summary>
        private bool parameterIsValid = true;

        /// <summary>
        /// 开始执行计算的周期下标。
        /// </summary>
        private int startIndex;

        #region 参数

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 500, MinValue = 0)]
        public int BarCount { get; set; }

        /// <summary>
        /// 最小bar长度。
        /// </summary>
        [Parameter(DefaultValue = 15, MinValue = 0)]
        public int MinHeight { get; set; }

        /// <summary>
        /// 是否画出小于最小长度<see cref="MinHeight"/>的指标。 
        /// </summary>
        [Parameter(DefaultValue = false)]
        public bool DrawBelowMinHeight { get; set; }

        /// <summary>
        /// 十字星中bar的核心高度与整体高度的百分比。
        /// </summary>
        [Parameter(DefaultValue = 10, MinValue = 1, MaxValue = 20)]
        public int CrossPercent { get; set; }

        #endregion

        #region 指标输出

        /// <summary>
        /// bar的开盘、收盘价组成的核心高度。
        /// </summary>
        [Output("Core Height", LineColor = "Yellow", PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries CoreHeightResult { get; set; }

        /// <summary>
        /// bar的最高、最低价组成的整体高度。
        /// </summary>
        [Output("Bar Height", LineColor = "Green", PlotType = PlotType.Histogram, Thickness = 2)]
        public IndicatorDataSeries BarHeightResult { get; set; }

        /// <summary>
        /// 符合要求的整体最小高度。
        /// </summary>
        [Output("Min Height", LineColor = "LightBlue", LineStyle = LineStyle.Dots)]
        public IndicatorDataSeries Level { get; set; }

        /// <summary>
        /// 是否是十字星。要满足<see cref="BarHeightResult"/>大于等于<see cref="MinHeight"/>，
        /// 且<see cref="CoreHeightResult"/>占比小于等于<see cref="CrossPercent"/>。
        /// </summary>
        [Output("Cross Start", LineColor = "OrangeRed", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries CrossStart { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// 对当前序列进行指标计算。
        /// </summary>
        /// <param name="index">序列下标。</param>
        public override void Calculate(int index)
        {
            // 判断以下这些条件，说明顺序与代码判断顺序可能不同。
            // * 在指定的周期数之前不执行。
            // * 参数不合法不执行。
            var shouldNotRun = !parameterIsValid || index < startIndex;
            if (shouldNotRun)
            {
                return;
            }

            var bar = Bars[index];
            var barHeight = (bar.High - bar.Low) / Symbol.PipSize;

            // 由于对于当前bar重复计算，所以应先删除上一次计算的值，
            // 以避免上一次符合而这一次不符合但仍留在图上。
            CrossStart[index] = double.NaN;
            BarHeightResult[index] = double.NaN;
            CoreHeightResult[index] = double.NaN;

            // 允许画bar高度小于最小高度，或者bar高度本身大于等于调小高度的信号。
            if (DrawBelowMinHeight || barHeight >= MinHeight)
            {
                // 输出。
                BarHeightResult[index] = barHeight;

                var coreHeight = Math.Abs(bar.Open - bar.Close) / Symbol.PipSize;
                // 输出。
                CoreHeightResult[index] = coreHeight;

                // 判断是否为十字星。
                if (coreHeight * 100 / barHeight <= CrossPercent)
                {   
                    // 输出。
                    CrossStart[index] = barHeight + Common.GetDrawDistance(Bars, Symbol.PipSize);
                }
            }

            // 输出。
            Level[index] = MinHeight;
        }

        /// <summary>
        /// 初始化指标。
        /// </summary>
        protected override void Initialize()
        {
            startIndex = Common.GetStartBarIndex(Bars.Count, BarCount);

            // 有足够的序列供计算。
            parameterIsValid = startIndex != Common.IndexNotFound;
        }
    }
}
