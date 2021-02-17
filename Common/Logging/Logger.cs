namespace Notadream.Logging
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    using cAlgo.API;

    /// <summary>
    /// 供指标使用的日志类。
    /// </summary>
    public class Logger : ILogger
    {
        #region 变量

        /// <summary>
        /// 是否记录所有周期。默认为true。为false只记录LastBar。
        /// </summary>
        private bool logAllBars;

        /// <summary>
        /// 日志对应的指标对象。
        /// </summary>
        private Indicator indicator;

        #endregion

        /// <summary>
        /// 创建日志对象。
        /// </summary>
        /// <param name="indicator">日志所针对的指标对象。</param>
        /// <param name="logAllBars">是否记录所有周期。默认为true。为false只记录LastBar。</param>
        /// <param name="path">日志文件基础路径，默认为null，使用用户文档路径。</param>
        public Logger(Indicator indicator, bool logAllBars, string path = null)
        {
            this.logAllBars = logAllBars;
            this.indicator = indicator;
            Name = GetLoggerName(indicator);
            FileName = GetLogFileName(path);
            LogStartInfo(indicator);
        }

        /// <summary>
        /// 创建日志对象。
        /// </summary>
        /// <param name="indicator">日志所针对的指标对象。</param>
        /// <param name="path">日志文件基础路径，默认为null，使用用户文档路径。</param>
        public Logger(Indicator indicator, string path = null) : this(indicator, true)
        {
        }

        #region 属性

        /// <summary>
        /// 日志文件名。
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// 日志对象名。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 是否可以执行记录日志的操作。
        /// </summary>
        private bool CanLog
        {
            get { return logAllBars || indicator.IsLastBar; }
        }

        #endregion

        #region 函数

        #region 接口实现

        /// <summary>
        /// 记录Info级别的日志。
        /// </summary>
        /// <param name="message">信息。</param>
        public void Info(string message)
        {
            if (!CanLog)
            {
                return;
            }

            var id = Thread.CurrentThread.ManagedThreadId;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var s = string.Format("{0} {1:D4} - {2}", time, id, message);

            using (StreamWriter sw = new StreamWriter(FileName, true, Encoding.UTF8))
            {
                sw.WriteLine(s);
                sw.Flush();
                sw.Close();
            }
        }

        /// <summary>
        /// 记录Info级别的日志。
        /// </summary>
        /// <param name="message">带格式的信息。</param>
        /// <param name="args">信息参数列表。</param>
        public void Info(string message, params object[] args)
        {
            if (CanLog)
            {
                var s = string.Format(message, args);
                Info(s);
            }
        }

        #endregion

        #region 构造函数使用的工具函数

        /// <summary>
        /// 获取日志对象名称。
        /// </summary>
        /// <param name="indicator">指标对象。</param>
        /// <returns>日志对象名称。</returns>
        private string GetLoggerName(Indicator indicator)
        {
            return indicator.SymbolName + "-" + indicator.TimeFrame + "-" + indicator.GetType().Name;
        }

        /// <summary>
        /// 获取日志文件名。路径不存在则尝试创建。创建时可能抛出异常。
        /// </summary>
        /// <param name="path">日志文件基本路径，若为null，使用用户文档路径。</param>
        /// <returns>如未抛出异常，则为完整文件名。</returns>
        private string GetLogFileName(string path)
        {
            if (path == null)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\cAlgo\\logs";
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var pathInfo = new DirectoryInfo(path);
            return pathInfo.FullName + "\\" + Name + ".log";
        }

        /// <summary>
        /// 记录日志创建时的信息。
        /// </summary>
        /// <param name="indicator">指标对象。</param>
        private void LogStartInfo(Indicator indicator)
        {
            var sb = new StringBuilder();
            sb.Append("Indicator ");
            sb.Append(Name);
            // 与参数列表使用": "分隔，即2个字符。
            sb.Append(" is started: ");

            // 记录当前的长度。如果遍历properties后长度无变化，说明没有定义输入属性。
            var length = sb.Length;
            var properties = indicator.GetType().GetProperties();
            var typeOfParameter = typeof(ParameterAttribute);
            var typeOfString = typeof(string);

            foreach (var property in properties)
            {
                // 这里只做一个ParameterAttribute的验证，这里如果要做很多验证，需要好好设计一下，
                // 千万不要用if else if去链接，会非常难于维护，类似这样的开源项目很多，有兴趣可以去看源码。
                if (property.IsDefined(typeOfParameter, false))
                {
                    var value = property.GetValue(indicator, null);
                    var useQuotation = value.GetType() == typeOfString;
                    var valueStart = useQuotation ? "=\"" : "=";
                    // 每个参数结尾使用", "分隔，即2个字符。
                    var valueEnd = useQuotation ? "\", " : ", ";

                    sb.Append(property.Name);
                    sb.Append(valueStart);
                    sb.Append(value);
                    sb.Append(valueEnd);
                }
            }

            // 删除结尾的2个字符，可能是": "，也可能是", "。
            sb.Remove(sb.Length - 2, 2);

            // 遍历properties后长度无变化，说明没有定义输入属性。
            if (sb.Length <= length)
            {
                sb.Append(" without parameter.");
            }

            var logAll = logAllBars;
            // 无论logAllBars具体值为何，均让当前函数可以记录初始日志。
            logAllBars = true;

            Info(sb.ToString());

            logAllBars = logAll;
        }

        #endregion

        #endregion
    }
}
