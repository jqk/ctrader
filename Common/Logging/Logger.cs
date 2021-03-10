namespace Notadream.Logging
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    using cAlgo.API;

    /// <summary>
    /// 供指标使用的日志类。
    /// <para>1，因为要建立文件，所以指标权限至少要求<see cref="AccessRights.FileSystem"/>。</para>
    /// <para>2，又因为要动态获取指标参数信息，所以要求权限为<see cref="AccessRights.FullAccess"/>。</para>
    /// <para>不满足第1点，只能通过Print()输出到屏幕。不满足第2点，仅获取不到参数信息，不影响其它使用。
    /// </para>
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

        #region 构造函数

        /// <summary>
        /// 创建日志对象。
        /// </summary>
        /// <param name="indicator">日志所针对的指标对象。</param>
        /// <param name="logAllBars">是否记录所有周期。默认为true。为false只记录LastBar。</param>
        /// <param name="path">日志文件基础路径，默认为null，使用用户文档路径。</param>
        public Logger(Indicator indicator, bool logAllBars, string path = null)
        {
            if (indicator == null)
            {
                throw new ArgumentNullException("indicator can not be null.");
            }

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

        #endregion

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
            if (CanLog)
            {
                WriteLog(message);
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
                WriteLog(string.Format(message, args));
            }
        }

        /// <summary>
        /// 将一行文本信息增加记录时间后写入日志文件。
        /// </summary>
        /// <param name="message">信息。</param>
        private void WriteLog(string message)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var s = string.Format("{0} {1:D4} - {2}", time, threadId, message);

            WriteLine(s);
        }

        /// <summary>
        /// 将一行文本信息写入日志文件。
        /// </summary>
        /// <param name="message">待写入的文本信息。</param>
        private void WriteLine(string message)
        {
            if (FileName != string.Empty)
            {
                using (StreamWriter sw = new StreamWriter(FileName, true, Encoding.UTF8))
                {
                    sw.WriteLine(message);
                    sw.Flush();
                    sw.Close();
                }
            }
            else
            {
                indicator.Print("Not log to file: " + message);
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
            try
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
            catch (Exception e)
            {
                indicator.Print("create log file failed: " + e.ToString());
                return string.Empty;
            }
        }

        /// <summary>
        /// 记录日志创建时的信息。
        /// </summary>
        /// <param name="indicator">指标对象。</param>
        private void LogStartInfo(Indicator indicator)
        {
            WriteLine("");
            WriteLog("--------------------------------------------------");

            var sb = new StringBuilder();
            sb.Append("Indicator ");
            sb.Append(Name);

            // 如果是IndicatorBase，则添加版本信息。
            var indicatorBase = indicator as IndicatorBase;
            if (indicatorBase != null)
            {
                sb.Append(" version [");
                sb.Append(indicatorBase.Version);
                sb.Append("]");
            }

            // 与参数列表使用": "分隔，即2个字符。
            sb.Append(" is started: ");

            // 记录当前的长度。如果遍历properties后长度无变化，说明没有定义输入属性。
            var length = sb.Length;
            var errorMessage = GetParameterInfo(sb);

            if (errorMessage == string.Empty)
            {
                // 删除结尾的2个字符，可能是": "，也可能是", "。
                sb.Remove(sb.Length - 2, 2);

                // 遍历properties后长度无变化，说明没有定义输入属性。
                if (sb.Length <= length)
                {
                    sb.Append(" without parameter.");
                }
            }
            else
            {
                // 去除可能在获取参数信息过程中添加的字符串。
                var len = sb.Length - length;
                if (len > 0)
                {
                    sb.Remove(length, len);
                }

                sb.Append("get indicator parameter info failed: ");
                sb.Append(errorMessage);
            }

            WriteLog(sb.ToString());
        }

        /// <summary>
        /// 获取指标的参数信息。
        /// </summary>
        /// <param name="sb">准备添加参数信息的字符串对象。</param>
        /// <returns>如果正常，返回空字符串。否则为异常信息。</returns>
        private string GetParameterInfo(StringBuilder sb)
        {
            var properties = indicator.GetType().GetProperties();
            var typeOfParameter = typeof(ParameterAttribute);
            var typeOfString = typeof(string);

            try
            {
                foreach (var property in properties)
                {
                    // 这里只做一个ParameterAttribute的验证，这里如果要做很多验证，需要好好设计一下，
                    // 千万不要用if else if去链接，会非常难于维护，类似这样的开源项目很多，有兴趣可以去看源码。
                    if (property.IsDefined(typeOfParameter, false))
                    {
                        // 如果指标权限不是FullAccess，此处将抛出异常。
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

                return string.Empty;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        #endregion

        #endregion
    }
}
