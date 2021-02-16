namespace Notadream
{
    using cAlgo.API;
    using cAlgo.API.Internals;
    using Logging;

    /// <summary>
    /// Group Pin Bar指标。
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    //[Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class GroupPinBar : Indicator
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

        /// <summary>
        /// 以价格表示的组合的最小高度。
        /// </summary>
        private double minHeight;

        #region 参数

        /// <summary>
        /// 需计算的周期数量，等于0表示不限制。
        /// </summary>
        [Parameter(DefaultValue = 500, MinValue = 0, MaxValue = int.MaxValue)]
        public int BarCount { get; set; }

        /// <summary>
        /// 组合中元素的数量，大于0。
        /// 因为组合中的元素越多，信号画在图上越乱，所以不要超过3个。
        /// </summary>
        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 3)]
        public int GroupSize { get; set; }

        /// <summary>
        /// 以点数表示的组合的最小高度，大于等于0。
        /// </summary>
        [Parameter(DefaultValue = 20, MinValue = 0, MaxValue = 100)]
        public int MinPips { get; set; }

        /// <summary>
        /// 符合要求的组合实体占比，0至100。
        /// </summary>
        [Parameter(DefaultValue = 30, MinValue = 0, MaxValue = 100)]
        public int Percent { get; set; }

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

            for (int i = 1; i <= GroupSize; i++)
            {
                // 确定要计算的第1个bar的位置。index是当前bar的位置。i是本次计算包含的bar数量。
                var start = Common.GetBufferStartBarIndex(index, i);
                var bar = CreateGroupBar(start, index, i);

                if (bar.IsSignal)
                {
                    var openTime = Bars.OpenTimes[index].ToString("yyyy-MM-dd HH:mm");
                    var height = bar.GroupHeight / Symbol.PipSize;
                    var core = bar.CoreHeight / Symbol.PipSize;

                    logger.Info("[{0}] [{1}] height = {2:F1}, core = {3:F1}, percent = {4:F1}%", openTime, i, height, core, bar.CorePercent);

                    DrawSignal(bar, start, index, i);
                }
            }

            // 记录已执行的bar位置。
            lastIndex = index;
        }

        /// <summary>
        /// 初始化指标。
        /// </summary>
        protected override void Initialize()
        {
            startIndex = Common.GetStartBarIndex(Bars.Count, BarCount, GroupSize);
            parameterIsValid = startIndex != Common.IndexNotFound;
            minHeight = MinPips * Symbol.PipValue;

            logger = LogManager.GetLogger(this);
        }

        /// <summary>
        /// 计算所需的序列。
        /// </summary>
        /// <param name="startIndex">组的第一个价格所在位置。</param>
        /// <param name="endIndex">当前价格所在位置。</param>
        /// <param name="groupSize">缓冲区长度。</param>
        /// <returns>组全价格<see cref="GroupBar"/>对象。</returns>
        private GroupBar CreateGroupBar(int startIndex, int endIndex, int groupSize)
        {
            var high = Common.GetMaxPrice(Bars.HighPrices, startIndex, groupSize);
            var low = Common.GetMinPrice(Bars.LowPrices, startIndex, groupSize);
            var open = Bars.OpenPrices[startIndex];
            var close = Bars.ClosePrices[endIndex];

            return new GroupBar(high, low, open, close, Percent, minHeight);
        }

        /// <summary>
        /// 绘制信号。
        /// </summary>
        /// <param name="bar">价格组合<see cref="GroupBar"/>对象。</param>
        /// <param name="start">组中第一个元素在序列中的下标。</param>
        /// <param name="index">当前元素的下标。</param>
        /// <param name="groupSize">缓冲区长度。</param>
        private void DrawSignal(GroupBar bar, int start, int index, int groupSize)
        {
            // 画图时的第1个价格。
            double price1;
            // 画图时的第2个价格。
            double price2;
            // 画图时的颜色。
            Color color;

            if (bar.IsUp)
            {
                // 组合实体靠近高端。是正锤体。
                price1 = bar.HighPrice;
                price2 = bar.LowerPrice;
                color = Color.Yellow;
            }
            else
            {
                // 组合实体靠近低端。是倒锤体。
                price1 = bar.UpperPrice;
                price2 = bar.LowPrice;
                color = Color.Red;
            }

            var name = "RP[" + index + "," + groupSize + "]";

            // 由于在系统初始化时已经确保有足够的空间，所以，start - 1位置一定是有效的。
            // 每次都进行记算，是因为在跨越休息日时，两个相邻周期之间的时间与平常的周期时间不同。
            // 例如，4H周期时，两个周期之间间隔4小时。但是一周第一个4H的开始时间与上周最后一个
            // 的开始时间相关甚远。
            // 此处希望得到负数，所以用较早的时间减较晚的时间。
            var adjust1 = (Bars.OpenTimes[start - 1] - Bars.OpenTimes[start]).TotalSeconds / 2;
            var time1 = Bars.OpenTimes[start].AddSeconds(adjust1);

            // 与处理addjust1不同，start + 1位置不一定是有效的：如果start是最后一个位置，则其之后是没有任何东西的。
            // 所以，如果程序允许计算当前最新的bar，此处就需要修改。
            var adjust2 = (Bars.OpenTimes[index + 1] - Bars.OpenTimes[index]).TotalSeconds / 2;
            var time2 = Bars.OpenTimes[index].AddSeconds(adjust2);

            Chart.DrawRectangle(name, time1, price1, time2, price2, color);

            logger.Info("{0} time1 = [{1}], time2 = [{2}]", name, time1.ToString("yyyy-MM-dd HH:mm"), time2.ToString("yyyy-MM-dd HH:mm"));
        }

        #region 内部类

        /// <summary>
        /// 将多个（可以是一个）bar组合后的信息。
        /// </summary>
        public sealed class GroupBar
        {
            /// <summary>
            /// 创建<see cref="GroupBar"/>对象。
            /// </summary>
            /// <param name="high">最高价。</param>
            /// <param name="low">最低价。</param>
            /// <param name="open">开盘价。</param>
            /// <param name="close">收盘价。</param>
            /// <param name="percent">实体占比。</param>
            /// <param name="minHeight">实体最小高度。</param>
            public GroupBar(double high, double low, double open, double close, int percent, double minHeight)
            {
                HighPrice = high;
                LowPrice = low;
                OpenPrice = open;
                ClosePrice = close;

                Calculate(percent, minHeight);
            }

            #region 属性

            /// <summary>
            /// 组的最高价。
            /// </summary>
            public double HighPrice { get; private set; }

            /// <summary>
            /// 组的最低价。
            /// </summary>
            public double LowPrice { get; private set; }

            /// <summary>
            /// 组的开盘价。
            /// </summary>
            public double OpenPrice { get; private set; }

            /// <summary>
            /// 组的收盘价。
            /// </summary>
            public double ClosePrice { get; private set; }

            /// <summary>
            /// 开盘价和收盘价的较高者。
            /// </summary>
            public double UpperPrice { get; private set; }

            /// <summary>
            /// 开盘价和收盘价的较低者。
            /// </summary>
            public double LowerPrice { get; private set; }

            /// <summary>
            /// 组的高度。
            /// </summary>
            public double GroupHeight { get; private set; }

            /// <summary>
            /// 组内实体的高度。包含近端的影线长度。
            /// </summary>
            public double CoreHeight { get; private set; }

            /// <summary>
            /// 组内实体的百分比。
            /// </summary>
            public double CorePercent { get; private set; }

            /// <summary>
            /// 指示上升还是下降。不只由开盘、收盘价决定，还是其到对端距离有关。
            /// </summary>
            public bool IsUp { get; private set; }

            /// <summary>
            /// 是否是合格的信号。
            /// </summary>
            public bool IsSignal { get; private set; }

            #endregion

            /// <summary>
            /// 执行计算。
            /// </summary>
            /// <param name="percent">实体占比。</param>
            /// <param name="minHeight">实体最小高度。</param>
            private void Calculate(int percent, double minHeight)
            {
                if (ClosePrice > OpenPrice)
                {
                    // 收盘价大于开盘价，较高价为收盘价，较低价为开盘价。
                    UpperPrice = ClosePrice;
                    LowerPrice = OpenPrice;
                }
                else
                {
                    // 收盘价大于开盘价，较高价为开盘价，较低价为收盘价。
                    UpperPrice = OpenPrice;
                    LowerPrice = ClosePrice;
                }

                // 较高价到组最低价的距离。
                var upperToLow = UpperPrice - LowPrice;
                // 较低价到组最高价的距离。
                var lowerToHigh = HighPrice - LowerPrice;

                // 组合后的实体高度，包含近端引线。
                // upperToLow小表示实体更靠近低端，是倒锤体。
                // lowerToHigh小表示实体更靠近高端，是正锤体。
                if (upperToLow < lowerToHigh)
                {
                    CoreHeight = upperToLow;
                    IsUp = false;
                }
                else
                {
                    CoreHeight = lowerToHigh;
                    IsUp = true;
                }

                GroupHeight = HighPrice - LowPrice;
                CorePercent = CoreHeight * 100 / GroupHeight;

                IsSignal = GroupHeight >= minHeight && CorePercent <= percent;
            }
        }

        #endregion
    }
}
