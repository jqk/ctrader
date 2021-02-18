namespace Notadream
{
    using cAlgo.API;
    using Logging;

    /// <summary>
    /// 画出每周分隔线的指标。
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class WeekSeperator : IndicatorBase
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
        /// 周期是否需要画分隔线。
        /// </summary>
        private bool timeFrameOk;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，仅为提供指标版本号。
        /// </summary>
        public WeekSeperator() : base("1.1.0")
        {
        }

        #endregion

        #region 参数，基类中定义无效

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 100, MinValue = 0, MaxValue = int.MaxValue)]
        public int BarCount { get; set; }

        /// <summary>
        /// 是否记录所有周期。为false只记录LastBar。
        /// </summary>
        [Parameter(DefaultValue = true)]
        public bool LogAllBars { get; set; }

        /// <summary>
        /// 是否为日周期画分隔线。
        /// <para>大于日周期的不画，小于日周期的必画。只有日周期自身可选择是否画分隔线。</para>
        /// </summary>
        [Parameter(DefaultValue = false)]
        public bool DrawForDaily { get; set; }

        /// <summary>
        /// 当<see cref="DrawForDaily"/>为false时，是否为最后一组日周期画分隔线。
        /// </summary>
        [Parameter(DefaultValue = true)]
        public bool DrawForLastDaily { get; set; }

        #endregion

        #region 函数

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

            if (timeFrameOk)
            {
                CheckAndDrawWeekSeperator(index);
            }
            else if (IsLastBar && DrawForLastDaily && TimeFrame == TimeFrame.Daily)
            {
                // 日图时，对于最后一组（6个）bar，从最近的日期，尝试画一根分隔线即结束。
                for (int i = index; i > index - 5; i--)
                {
                    if (CheckAndDrawWeekSeperator(i))
                    {
                        break;
                    }
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
            // 大于日周期的不画，小于日周期的必画。只有日周期自身可选择是否画分隔线。
            timeFrameOk = TimeFrame < TimeFrame.Daily || (TimeFrame == TimeFrame.Daily && DrawForDaily);

            // 有足够的序列供计算，且周期正确。
            parameterIsValid = startIndex != Common.IndexNotFound;
            logger = LogManager.GetLogger(this, LogAllBars);
        }

        /// <summary>
        /// 检查给定的bar，确认和其前面的bar之间是否是周分隔，如是则画分隔线。
        /// </summary>
        /// <param name="index">给定bar的下标。</param>
        /// <returns>是否是周分隔。</returns>
        private bool CheckAndDrawWeekSeperator(int index)
        {
            // 当前bar与上一个bar之间相差的秒数。
            var time0 = Bars.OpenTimes[index - 1];
            var time1 = Bars.OpenTimes[index];
            var seconds = (int)(time1 - time0).TotalSeconds;

            // 如果当前bar与上一个bar之间相差的秒数超过了周之间应有的秒数，说明是跨周了。
            if (seconds > WeekChangeSeconds)
            {
                // 在两个bar之间，调整秒数需除以2。
                seconds >>= 1;
                // 画线时间在当前bar与上一个bar之间。
                var drawTime = time0.AddSeconds(seconds);

                Chart.DrawVerticalLine("WS[" + index + "]", drawTime, Color.Yellow, 1, LineStyle.Dots);

                var timeString0 = Common.FormatTimeFrame(time0);
                var timeString1 = Common.FormatTimeFrame(time1);
                logger.Info("Week is changed between [{0}] and [{1}]", timeString0, timeString1);

                return true;
            }

            return false;
        }

        #endregion
    }
}
