namespace Notadream.Logging
{
    using cAlgo.API;

    /// <summary>
    /// 日志对象管理类。
    /// </summary>
    public static class LogManager
    {
        /// <summary>
        /// 获取日志对象。
        /// </summary>
        /// <param name="indicator">指标对象。</param>
        /// <param name="logAllBars">是否记录所有周期。默认为true。为false只记录LastBar。</param>
        /// <param name="path">日志基础路径。如为null，则使用用户文档目录。</param>
        /// <returns>日志对象。</returns>
        public static ILogger GetLogger(Indicator indicator, bool logAllBars, string path = null)
        {
            return new Logger(indicator, logAllBars, path);
        }

        /// <summary>
        /// 获取日志对象。
        /// </summary>
        /// <param name="indicator">指标对象。</param>
        /// <param name="path">日志基础路径。如为null，则使用用户文档目录。</param>
        /// <returns>日志对象。</returns>
        public static ILogger GetLogger(Indicator indicator, string path = null)
        {
            return new Logger(indicator, path);
        }
    }
}
