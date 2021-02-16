namespace Notadream
{
    using cAlgo.API;
    using Logging;

    /// <summary>
    /// 用于直接在源码级别测试Common项目的类。
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class CommonTest : Indicator
    {
        /// <summary>
        /// 日志对象。
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// 开始执行计算的周期下标。
        /// </summary>
        private int startIndex;

        /// <summary>
        /// 上次计算的周期下标，避免对当前周期重复计算。
        /// </summary>
        private int lastIndex = -1;

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 100, MinValue = 0, MaxValue = int.MaxValue)]
        public int BarCount { get; set; }

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
            var shouldNotRun = index < startIndex || index == lastIndex;
            if (shouldNotRun)
            {
                return;
            }

            var time = Bars[index].OpenTime.ToString("dd HH:mm");
            logger.Info("index = {0}, time = {1}, IsLast = {2}", index, time, IsLastBar);

            // 不再处理最后一个柱体。
            lastIndex = index;
        }

        /// <summary>
        /// 初始化指标。
        /// </summary>
        protected override void Initialize()
        {
            startIndex = Common.GetStartBarIndex(Bars.Count, BarCount);
            logger = LogManager.GetLogger(this);
        }
    }
}
