namespace Notadream
{
    using System;

    using cAlgo.API;

    /// <summary>
    /// 通用工具类。
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// 表示未找到起始位置。
        /// </summary>
        public const int IndexNotFound = -1;

        /// <summary>
        /// 确定计算开始位置的bar的位置。最少需3个bar才有效。
        /// </summary>
        /// <param name="totalBarCount">可用bar的总数。大于0。</param>
        /// <param name="barCount">准备最多计算bar的数量。大于0。</param>
        /// <param name="bufferSize">计算一个bar时，所需前面bar的数量。大于0，默认为1。</param>
        /// <returns>开始的位置，返回<see cref="IndexNotFound"/>表示发生错误，未找到位置。</returns>
        public static int GetStartBarIndex(int totalBarCount, int barCount, int bufferSize = 1)
        {
            // 校验参数的有效性，以及总的可用bar数必须足够。
            // 最后一个条件3是用于确定两个bar之间最小时差时（不跨休息日时），最少要3个bar。
            if (totalBarCount < bufferSize || bufferSize < 1 || totalBarCount < 1 || totalBarCount < 3)
            {
                return IndexNotFound;
            }

            // barCount超出范围表示全部，留出缓冲区即可，从缓冲区后的位置开始。
            if (barCount >= totalBarCount || barCount <= 0)
            {
                return bufferSize;
            }

            // 从最后位置向前调整barCount个位置。
            var startIndex = totalBarCount - barCount;

            if (startIndex < bufferSize)
            {
                // 确保起始位置前有足够的元素供计算使用。
                startIndex = bufferSize;
            }

            return startIndex;
        }

        /// <summary>
        /// 获得周期时间的时间间隔。
        /// </summary>
        /// <param name="ts">时间间隔序列。</param>
        /// <returns>时间间隔的秒数。</returns>
        public static int GetTimeFrameSeconds(TimeSeries ts)
        {
            var sec0 = (int)(ts[1] - ts[0]).TotalSeconds;
            var sec1 = (int)(ts[2] - ts[1]).TotalSeconds;

            return sec0 < sec1 ? sec0 : sec1;
        }

        /// <summary>
        /// 获得计算所需的缓冲区在整个序列中的起始位置。
        /// </summary>
        /// <param name="index">当前位置。</param>
        /// <param name="bufferSize">缓冲区长度。</param>
        /// <returns>起始位置。</returns>
        public static int GetBufferStartBarIndex(int index, int bufferSize)
        {
            return index - bufferSize + 1;
        }

        /// <summary>
        /// 获得给序列价格的最高值。
        /// </summary>
        /// <param name="data">价格序列。</param>
        /// <param name="index">待计算价格在序列中的起始下标。</param>
        /// <param name="count">待计算价格的数量。</param>
        /// <returns>最高价格。</returns>
        public static double GetMaxPrice(DataSeries data, int index, int count)
        {
            double result = 0;

            for (int i = index; i < index + count; i++)
            {
                result = result > data[i] ? result : data[i];
            }

            return result;
        }

        /// <summary>
        /// 获得给序列价格的最低值。
        /// </summary>
        /// <param name="data">价格序列。</param>
        /// <param name="index">待计算价格在序列中的起始下标。</param>
        /// <param name="count">待计算价格的数量。</param>
        /// <returns>最低价格。</returns>
        public static double GetMinPrice(DataSeries data, int index, int count)
        {
            double result = double.MaxValue;

            for (int i = index; i < index + count; i++)
            {
                result = result < data[i] ? result : data[i];
            }

            return result;
        }

        /// <summary>
        /// 确定在bar的上下画图时需调整的距离点数或价格。
        /// <para>方法是求count个bar的平均高度，然后除以factor。</para>
        /// </summary>
        /// <param name="bars">计算所依据的bar集合。</param>
        /// <param name="pipSize">一单位货币包含点的数量。默认为0，表示返回点数，否则返回价格。</param>
        /// <param name="count">计算bar的数量，默认为10。</param>
        /// <param name="factor">距离分散因子，默认为5。</param>
        /// <returns>距离点数或价格。</returns>
        public static double GetDrawDistance(Bars bars, double pipSize = 0, int count = 10, int factor = 5)
        {
            int index = bars.Count - 1;
            int start = index - count;

            // 保证有足够的bar可供计算。
            if (start < 0)
            {
                start = 0;
                count = index + 1;
            }

            double avg = 0;

            // 先求这些bar的高度之和。
            for (int i = start; i <= index; i++)
            {
                var bar = bars[i];
                avg += bar.High - bar.Low;
            }

            // 再求平均值。如果要返回点数，需除PipSize。
            avg /= pipSize > 0 ? pipSize * count : count;

            return avg / factor;
        }

        /// <summary>
        /// 格式化周期时间字符串。
        /// </summary>
        /// <param name="time">周期时间。</param>
        /// <param name="toShort">是否为短格式。默认为true。短格式无年份。</param>
        /// <returns>周期时间字符串。</returns>
        public static string FormatTimeFrame(DateTime time, bool toShort = true)
        {
            if (toShort)
            {
                return time.ToString("MM-dd HH:mm");
            }
            else
            {
                return time.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
}
