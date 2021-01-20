namespace Notadream
{
    using cAlgo.API;

    /// <summary>
    /// WeekSeperator指标。
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class WeekSeperator : Indicator
    {
        #region 变量

        /// <summary>
        /// 当前交易周最后一个时间与下一交易周第一个时间相差的秒数，暂定为36小时。
        /// </summary>
        private const int WeekChangeSeconds = 129600;

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
        [Parameter(DefaultValue = 500, MinValue = 0, MaxValue = int.MaxValue)]
        public int BarCount { get; set; }

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

            // 当前bar与上一个bar之间相差的秒数。
            var seconds = (int)(Bars.OpenTimes[index] - Bars.OpenTimes[index - 1]).TotalSeconds;

            // 如果当前bar与上一个bar之间相差的秒数超过了周之间应有的秒数，说明是跨周了。
            if (seconds > WeekChangeSeconds)
            {
                // 在两个bar之间，调整秒数需除以2。
                seconds >>= 1;
                // 画线时间在当前bar与上一个bar之间。
                var drawTime = Bars.OpenTimes[index - 1].AddSeconds(seconds);

                Chart.DrawVerticalLine("WS[" + index + "]", drawTime, Color.Yellow, 1, LineStyle.Dots);
            }

            lastIndex = index;
        }

        /// <summary>
        /// 初始化指标。
        /// </summary>
        protected override void Initialize()
        {
            startIndex = Common.GetStartBarIndex(Bars.Count, BarCount);

            // 有足够的序列供计算，且周期小于等于1天。
            parameterIsValid = startIndex != Common.IndexNotFound && TimeFrame <= TimeFrame.Daily;
        }
    }
}
