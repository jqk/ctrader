namespace Notadream
{
    using System;

    using cAlgo.API;
    using Logging;

    /// <summary>
    /// 显示每个bar实体和整体长度的指标。
    /// </summary>
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PriceRange : Indicator
    {
        #region 变量

        /// <summary>
        /// 日志对象。
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// 参数是否有效。
        /// </summary>
        private bool parameterIsValid = true;

        /// <summary>
        /// 开始执行计算的周期下标。
        /// </summary>
        private int startIndex;

        /// <summary>
        /// 上次计算的周期下标，避免对当前周期重复计算。
        /// </summary>
        private int lastIndex = -1;

        #region 参数

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 200, MinValue = 0)]
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
        [Parameter(DefaultValue = 8, MinValue = 1, MaxValue = 20)]
        public int CrossPercent { get; set; }

        /// <summary>
        /// PinBar中bar的短端高度与整体高度的百分比。
        /// </summary>
        [Parameter(DefaultValue = 25, MinValue = 1, MaxValue = 40)]
        public int PinPercent { get; set; }

        /// <summary>
        /// 是否记录所有周期。为false只记录LastBar。
        /// </summary>
        [Parameter(DefaultValue = true)]
        public bool LogAllBars { get; set; }

        #endregion

        #region 指标输出

        /// <summary>
        /// bar的开盘、收盘价组成的核心高度。
        /// </summary>
        [Output("Body Height", LineColor = "Yellow", PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries BodyHeightResult { get; set; }

        /// <summary>
        /// bar的最高、最低价组成的整体高度。
        /// </summary>
        [Output("Bar Height", LineColor = "Yellow", PlotType = PlotType.Histogram, Thickness = 1)]
        public IndicatorDataSeries BarHeightResult { get; set; }

        /// <summary>
        /// 是否是十字星。要满足<see cref="BarHeightResult"/>大于等于<see cref="MinHeight"/>，
        /// 且<see cref="BodyHeightResult"/>占比小于等于<see cref="CrossPercent"/>。
        /// </summary>
        [Output("Cross Star", LineColor = "OrangeRed", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries CrossStar { get; set; }

        /// <summary>
        /// 是否是PinBar。要满足短端高度与整体高度的百分比小于等于<see cref="PinPercent"/>。
        /// </summary>
        [Output("Pin Bar", LineColor = "LightGreen", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries PinBar { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// 对当前序列进行指标计算。
        /// </summary>
        /// <param name="index">序列下标。</param>
        public override void Calculate(int index)
        {
            // 判断以下这些条件，说明顺序与代码判断顺序可能不同。
            // * 已执行的bar不再执行。
            // * 在指定的周期数之前不执行。
            // * 参数不合法不执行。
            var shouldNotRun = !parameterIsValid || index < startIndex || index == lastIndex;
            if (shouldNotRun)
            {
                return;
            }

            var bar = Bars[index];
            var barHeight = (bar.High - bar.Low) / Symbol.PipSize;

            // 由于对于当前bar重复计算，所以应先删除上一次计算的值，
            // 以避免上一次符合而这一次不符合但仍留在图上。
            CrossStar[index] = double.NaN;
            PinBar[index] = double.NaN;
            BarHeightResult[index] = double.NaN;
            BodyHeightResult[index] = double.NaN;

            // 允许画bar高度小于最小高度，或者bar高度本身大于等于调小高度的信号。
            if (DrawBelowMinHeight || barHeight >= MinHeight)
            {
                // 输出整个柱体高度。
                BarHeightResult[index] = barHeight;

                var bodyHeight = Math.Abs(bar.Open - bar.Close) / Symbol.PipSize;
                // 输出柱体核心高度。
                BodyHeightResult[index] = bodyHeight;

                var first = true;

                // 判断是否为十字星，并输出。
                var percent = bodyHeight * 100 / barHeight;
                if (percent <= CrossPercent)
                {   
                    CrossStar[index] = GetDrawPosition(barHeight, first);
                    first = false;
                    var time = Common.FormatTimeFrame(bar.OpenTime);
                    logger.Info("CrsStr at [{0}], height = {1:F1}, percent = {2:F1}%", time, barHeight, percent);
                }

                double openDistance, closeDistance, hammerHeight;

                if (bar.Open < bar.Close)
                {
                    openDistance = bar.High - bar.Open;
                    closeDistance = bar.Close - bar.Low;
                }
                else
                {
                    openDistance = bar.Open - bar.Low;
                    closeDistance = bar.High - bar.Close;
                }

                // 锤头选短的。
                hammerHeight = (openDistance > closeDistance ? closeDistance : openDistance) / Symbol.PipSize;

                // 判断是否为PinBar并输出。
                percent = hammerHeight * 100 / barHeight;
                if (percent <= PinPercent)
                {
                    PinBar[index] = GetDrawPosition(barHeight, first);
                    var time = Common.FormatTimeFrame(bar.OpenTime);
                    logger.Info("PinBar at [{0}], height = {1:F1}, percent = {2:F1}%", time, barHeight, percent);
                }
            }

            lastIndex = index;
        }

        /// <summary>
        /// 初始化指标。
        /// </summary>
        protected override void Initialize()
        {
            startIndex = Common.GetStartBarIndex(Bars.Count, BarCount);

            // 有足够的序列供计算。
            parameterIsValid = startIndex != Common.IndexNotFound;

            logger = LogManager.GetLogger(this, LogAllBars);
        }

        /// <summary>
        /// 获得画点的位置。
        /// </summary>
        /// <param name="barHeight">柱体高度。</param>
        /// <param name="first">是否是第一个点。</param>
        /// <returns>画点的位置。</returns>
        private double GetDrawPosition(double barHeight, bool first)
        {
            // 当只有第一个时，距离不要太远。
            // 当有第二个时，不要与第一个重叠。
            var rate = first ? 1 : 2.5;
            return barHeight + (Common.GetDrawDistance(Bars, Symbol.PipSize) * rate);
        }
    }
}
