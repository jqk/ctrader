namespace Notadream
{
    using cAlgo.API;
    using Logging;

    /// <summary>
    /// 提供共有变量的指标基类。
    /// <para>在基类中定义<see cref="ParameterAttribute"/>参数无效，只能在具体实现类中定义。</para>
    /// </summary>
    public abstract class IndicatorBase : Indicator
    {
        /// <summary>
        /// 日志对象。
        /// </summary>
        protected ILogger logger;

        /// <summary>
        /// 开始执行计算的周期下标。
        /// </summary>
        protected int startIndex;

        /// <summary>
        /// 上次计算的周期下标，避免对当前周期重复计算。
        /// </summary>
        protected int lastIndex = -1;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="version">指标版本。</param>
        protected IndicatorBase(string version)
        {
            Version = version;
        }

        /// <summary>
        /// 指标版本。
        /// </summary>
        public string Version { get; private set; }
    }
}
