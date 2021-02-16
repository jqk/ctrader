namespace Notadream.Logging
{
    /// <summary>
    /// 供指标使用的日志接口。
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 日志文件名。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 日志对象名。
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// 记录Info级别的日志。
        /// </summary>
        /// <param name="message">信息。</param>
        void Info(string message);

        /// <summary>
        /// 记录Info级别的日志。
        /// </summary>
        /// <param name="message">带格式的信息。</param>
        /// <param name="args">信息参数列表。</param>
        void Info(string message, params object[] args);
    }
}
