namespace Notadream
{
    using cAlgo.API;
    using Logging;

    /// <summary>
    /// 用于直接在源码级别测试Common项目的类。
    /// <para>基类<see cref="IndicatorBase"/>中定义<see cref="ParameterAttribute"/>是无效的，
    /// 只能在当前实现类中定义，所以无法统一提取。</para>
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class CommonTest : IndicatorBase
    {
        #region 构造函数

        /// <summary>
        /// 构造函数，仅为提供指标版本号。
        /// </summary>
        public CommonTest() : base("1.1.0")
        {
        }

        #endregion

        #region 参数，基类中定义无效

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 10, MinValue = 0, MaxValue = int.MaxValue)]
        public int BarCount { get; set; }

        /// <summary>
        /// 是否记录所有周期。为false只记录LastBar。
        /// </summary>
        [Parameter(DefaultValue = true)]
        public bool LogAllBars { get; set; }

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
            logger = LogManager.GetLogger(this, LogAllBars);
        }

        #endregion
    }
}
