﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace RX_Explorer.Class
{
    /// <summary>
    /// 提供对错误的捕获和记录，以及蓝屏的导向
    /// </summary>
    public static class LogTracer
    {
        private static readonly string UniqueName = $"Log_GeneratedTime[{DateTime.Now:yyyy-MM-dd HH-mm-ss.fff}].txt";

        private static readonly ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();

        private static readonly Thread BackgroundProcessThread = new Thread(LogProcessThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        private static readonly AutoResetEvent Locker = new AutoResetEvent(false);

        static LogTracer()
        {
            BackgroundProcessThread.Start();
        }

        public static async Task ExportLogAsync(StorageFile ExportFile)
        {
            try
            {
                if (await ApplicationData.Current.TemporaryFolder.TryGetItemAsync(UniqueName) is StorageFile InnerFile)
                {
                    await InnerFile.CopyAndReplaceAsync(ExportFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error was threw in {nameof(ExportLogAsync)}, message: {ex.Message}");
            }
        }

        public static async Task<bool> CheckHasAnyLogAvailableAsync()
        {
            try
            {
                foreach (StorageFile LogFile in from StorageFile File in await ApplicationData.Current.TemporaryFolder.GetFilesAsync()
                                                let Mat = Regex.Match(File.Name, @"(?<=\[)(.+)(?=\])")
                                                where Mat.Success && DateTime.TryParseExact(Mat.Value, "yyyy-MM-dd HH-mm-ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime _)
                                                select File)
                {
                    BasicProperties Properties = await LogFile.GetBasicPropertiesAsync();

                    if (Properties.Size > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error was threw in {nameof(CheckHasAnyLogAvailableAsync)}, message: {ex.Message}");
                return false;
            }
        }

        public static async Task ExportAllLogAsync(StorageFile ExportFile)
        {
            try
            {
                using (Stream ExportStream = await ExportFile.OpenStreamForWriteAsync().ConfigureAwait(false))
                {
                    foreach ((DateTime LogDate, StorageFile LogFile) in from StorageFile File in await ApplicationData.Current.TemporaryFolder.GetFilesAsync()
                                                                        let Mat = Regex.Match(File.Name, @"(?<=\[)(.+)(?=\])")
                                                                        where Mat.Success && DateTime.TryParseExact(Mat.Value, "yyyy-MM-dd HH-mm-ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime Date)
                                                                        let LogDate = DateTime.ParseExact(Mat.Value, "yyyy-MM-dd HH-mm-ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal)
                                                                        orderby LogDate ascending
                                                                        select (LogDate, File))
                    {
                        BasicProperties Properties = await LogFile.GetBasicPropertiesAsync();

                        if (Properties.Size > 0)
                        {
                            using (StreamWriter Writer = new StreamWriter(ExportStream, Encoding.Unicode, 1024, true))
                            {
                                Writer.WriteLine();
                                Writer.WriteLine("*************************");
                                Writer.WriteLine($"LogDate: {LogDate:G}");
                                Writer.WriteLine("*************************");
                            }

                            using (Stream LogFileStream = await LogFile.OpenStreamForReadAsync().ConfigureAwait(false))
                            {
                                await LogFileStream.CopyToAsync(ExportStream).ConfigureAwait(false);
                            }

                            ExportStream.Seek(0, SeekOrigin.End);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error was threw in {nameof(ExportAllLogAsync)}, message: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="Ex">错误</param>
        /// <param name="AdditionalComment">附加信息</param>
        /// <returns></returns>
        public static void Log(Exception Ex, string AdditionalComment = null, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFilePath = "", [CallerLineNumber] int SourceLineNumber = 0)
        {
            if (Ex == null)
            {
                throw new ArgumentNullException(nameof(Ex), "Exception could not be null");
            }

            try
            {
                string[] MessageSplit;

                try
                {
                    if (string.IsNullOrWhiteSpace(Ex.Message))
                    {
                        MessageSplit = Array.Empty<string>();
                    }
                    else
                    {
                        MessageSplit = Ex.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select((Line) => $"        {Line.Trim()}").ToArray();
                    }
                }
                catch
                {
                    MessageSplit = Array.Empty<string>();
                }

                string[] StackTraceSplit;

                try
                {
                    if (string.IsNullOrWhiteSpace(Ex.StackTrace))
                    {
                        StackTraceSplit = Array.Empty<string>();
                    }
                    else
                    {
                        StackTraceSplit = Ex.StackTrace.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select((Line) => $"        {Line.Trim()}").ToArray();
                    }
                }
                catch
                {
                    StackTraceSplit = Array.Empty<string>();
                }

                StringBuilder Builder = new StringBuilder()
                                        .AppendLine("------------------------------------")
                                        .AppendLine($"AdditionalComment: {AdditionalComment ?? "<Empty>"}")
                                        .AppendLine($"------------------------------------")
                                        .AppendLine($"Source: RX-Explorer")
                                        .AppendLine()
                                        .AppendLine($"Exception: {Ex}")
                                        .AppendLine()
                                        .AppendLine("Message:")
                                        .AppendLine(MessageSplit.Length == 0 ? "        Unknown" : string.Join(Environment.NewLine, MessageSplit))
                                        .AppendLine()
                                        .AppendLine("StackTrace:")
                                        .AppendLine(StackTraceSplit.Length == 0 ? "        Unknown" : string.Join(Environment.NewLine, StackTraceSplit))
                                        .AppendLine()
                                        .AppendLine("Extra info: ")
                                        .AppendLine($"        CallerMemberName: {MemberName}")
                                        .AppendLine($"        CallerFilePath: {SourceFilePath}")
                                        .AppendLine($"        CallerLineNumber: {SourceLineNumber}")
                                        .AppendLine("------------------------------------")
                                        .AppendLine();

                Log(Builder.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error was threw in {nameof(Log)}, message: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="Message">错误消息</param>
        /// <returns></returns>
        public static void Log(string Message)
        {
            try
            {
                LogQueue.Enqueue(Message + Environment.NewLine);

                if (BackgroundProcessThread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin))
                {
                    Locker.Set();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error was threw in {nameof(Log)}, message: {ex.Message}");
            }
        }

        private static void LogProcessThread()
        {
            while (true)
            {
                try
                {
                    if (LogQueue.IsEmpty)
                    {
                        Locker.WaitOne();
                    }

                    StorageFile LogFile = ApplicationData.Current.TemporaryFolder.CreateFileAsync(UniqueName, CreationCollisionOption.OpenIfExists).AsTask().Result;

                    using (FileStream LogFileStream = LogFile.LockAndBlockAccess())
                    {
                        LogFileStream.Seek(0, SeekOrigin.End);

                        using (StreamWriter Writer = new StreamWriter(LogFileStream, Encoding.Unicode, 1024, true))
                        {
                            while (LogQueue.TryDequeue(out string LogItem))
                            {
                                Writer.WriteLine(LogItem);
                                Debug.WriteLine(LogItem);
                            }

                            Writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in writing log file: {ex.Message}");
                }
            }
        }
    }
}
