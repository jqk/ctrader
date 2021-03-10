namespace Notadream
{
    /// <summary>
    /// 将多个（可以是一个）bar组合后的信息。
    /// </summary>
    sealed class GroupBar
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
}
