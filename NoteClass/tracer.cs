using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Profiler
{
    static class Tracer
    {
        static string _filename = @"TracerData.xml";
        public struct MethodInfo
        {
            public int ThreadId;
            public DateTime TraceTime;
            public string MethodName;
            public string MethodPackageName;
            public bool IsOpened;
        }

        public struct MethodInfoLog
        {
            public string MethodName;
            public int NestingLevel;
            public DateTime MethodStartExecutionTime;
            public TimeSpan MethodExecutionTime;
            public string MethodPackageName;
            public bool IsOpened;
        }

        public struct ThreadInfoLog
        {
            public int ThreadId;
            public TimeSpan ThreadExecutionTime;
            public List<MethodInfoLog> ExecutedMethods;
        }

        static MethodInfo _currentInfo;

        static readonly object Locker = new object();

        private static List<ThreadInfoLog> TraceLog = new List<ThreadInfoLog>();
        private static List<MethodInfo> MethodsLog = new List<MethodInfo>();

        public static void StartTrace()
        {
            lock (Locker)
            {
                _currentInfo.ThreadId = Thread.CurrentThread.ManagedThreadId;
                _currentInfo.TraceTime = DateTime.UtcNow;
                StackTrace trace = new StackTrace(1);
                _currentInfo.MethodName = trace.GetFrame(0).GetMethod().Name;
                _currentInfo.MethodPackageName = trace.GetFrame(0).GetMethod().DeclaringType.FullName;
                _currentInfo.IsOpened = true;
                MethodsLog.Add(_currentInfo);
            }
        }

        public static void StopTrace()
        {
            lock (Locker)
            {
                _currentInfo.ThreadId = Thread.CurrentThread.ManagedThreadId;
                _currentInfo.TraceTime = DateTime.UtcNow;
                StackTrace trace = new StackTrace(1);
                _currentInfo.MethodName = trace.GetFrame(0).GetMethod().Name;
                _currentInfo.MethodPackageName = trace.GetFrame(0).GetMethod().DeclaringType.FullName;
                _currentInfo.IsOpened = false;
                MethodsLog.Add(_currentInfo);
            }
        }

        public static void SaveXmlResult()
        {

            XmlTextWriter writer = new XmlTextWriter(_filename, Encoding.UTF8);
            writer.WriteStartDocument();
            writer.WriteStartElement("Threads");
            
            foreach (ThreadInfoLog logItem in TraceLog)
            {
                writer.WriteStartElement("Thread");
                writer.WriteAttributeString("id", logItem.ThreadId.ToString());
                writer.WriteAttributeString("execution_time", logItem.ThreadExecutionTime.TotalMilliseconds.ToString());
                int currentNestingLevel = 1;
                foreach (var method in logItem.ExecutedMethods)
                {
                    if (currentNestingLevel >= method.NestingLevel)
                    {
                        writer.WriteEndElement();
                    }
                    writer.WriteStartElement("Method");
                    writer.WriteAttributeString("method_name", method.MethodName);
                    writer.WriteAttributeString("nesting_level", method.NestingLevel.ToString());
                    writer.WriteAttributeString("package_name", method.MethodPackageName);
                    writer.WriteAttributeString("execution_time", method.MethodExecutionTime.TotalMilliseconds.ToString());

                    // 
                    writer.Flush();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
               }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        public static void ShowStringResult()
        {
            foreach (ThreadInfoLog logItem in TraceLog)
            {
                Console.WriteLine("Thread: {0}, ExecutionTime: {1}", logItem.ThreadId, logItem.ThreadExecutionTime.TotalMilliseconds);
                foreach (var method in logItem.ExecutedMethods)
                {
                    for (int i = 0; i < method.NestingLevel; i++)
                    {
                        Console.Write("     ");
                    }
                    Console.WriteLine("{0}  {1}  {2}  {3}", method.MethodName, method.NestingLevel, method.MethodPackageName, method.MethodExecutionTime.TotalMilliseconds);
                }
                Console.WriteLine();

            }

        }

        public static void ParseTracedData()
        {
            List<int> Threads = GetThreads();

            foreach (int ThreadId in Threads)
            {
                ThreadInfoLog ThreadInfo;
                ThreadInfo.ThreadId = ThreadId;

                List<MethodInfoLog> threadMethods = AddMethodsToThread(ThreadId);
                ThreadInfo.ExecutedMethods = threadMethods;
                ThreadInfo.ThreadExecutionTime = GetTimeSpanFromThreadMethods(threadMethods);
                TraceLog.Add(ThreadInfo);
            }

        }

        private static TimeSpan GetTimeSpanFromThreadMethods(List<MethodInfoLog> threadMethods)
        {
            int itemNumber = threadMethods.Count - 1;
            int nestedLevelDifference = threadMethods.Last().NestingLevel;
            TimeSpan threadTimeSpan = new TimeSpan(0);
            while (nestedLevelDifference > 0 && itemNumber >= 0)
            {
                threadTimeSpan += threadMethods[itemNumber].MethodExecutionTime;
                int nextItemNumber = itemNumber - 1;
                nestedLevelDifference = threadMethods[itemNumber].NestingLevel - threadMethods[nextItemNumber].NestingLevel;
                itemNumber--;
            }
            threadTimeSpan -= threadMethods[0].MethodExecutionTime;
            return (threadMethods.Last().MethodStartExecutionTime - threadMethods[0].MethodStartExecutionTime) + threadTimeSpan;
        }

        private static List<MethodInfoLog> AddMethodsToThread(int ThreadId)
        {
            int currentNestingLevel = 0;
            List<MethodInfoLog> threadMethods = new List<MethodInfoLog>();
            foreach (var data in MethodsLog)
            {
                if (data.ThreadId == ThreadId)
                {

                    if (data.IsOpened == true)
                    {
                        MethodInfoLog MethodInfo = new MethodInfoLog();
                        currentNestingLevel++;
                        MethodInfo = GetMethodInfoLogForData(currentNestingLevel, data);
                        threadMethods.Add(MethodInfo);
                    }
                    else
                    {
                        currentNestingLevel--;
                        for (int i = threadMethods.Count - 1; i >= 0; i--)
                        {
                            if (threadMethods[i].IsOpened == true && threadMethods[i].MethodName == data.MethodName)
                            {
                                MethodInfoLog bufferMethodInfo = threadMethods[i];
                                bufferMethodInfo.MethodExecutionTime += data.TraceTime.Subtract(bufferMethodInfo.MethodStartExecutionTime).Duration();

                                var nestingLevel = currentNestingLevel;
                                for (int j = i; j >= 0; j--)
                                {
                                    if (threadMethods[j].NestingLevel == nestingLevel)
                                    {
                                        MethodInfoLog bufferParentMethodInfo = threadMethods[j];
                                        bufferParentMethodInfo.MethodExecutionTime -= bufferMethodInfo.MethodExecutionTime;
                                        threadMethods[j] = bufferParentMethodInfo;
                                        nestingLevel--;
                                        continue;
                                    }
                                }
                                bufferMethodInfo.IsOpened = false;
                                threadMethods[i] = bufferMethodInfo;
                                break;
                            }
                        }
                    }
                }
            }
            return threadMethods;
        }

        private static MethodInfoLog GetMethodInfoLogForData(int currentNestingLevel, MethodInfo data)
        {
            MethodInfoLog MethodInfo = new MethodInfoLog();
            MethodInfo.MethodName = data.MethodName;
            MethodInfo.MethodPackageName = data.MethodPackageName.Split('+')[1];
            MethodInfo.NestingLevel = currentNestingLevel;
            MethodInfo.MethodExecutionTime = new TimeSpan(0);
            MethodInfo.MethodStartExecutionTime = data.TraceTime;
            MethodInfo.IsOpened = true;

            return MethodInfo;
        }

        private static List<int> GetThreads()
        {
            List<int> Threads = new List<int>();
            foreach (var data in MethodsLog)
            {
                if (!Threads.Contains(data.ThreadId))
                {
                    Threads.Add(data.ThreadId);
                }

            }
            return Threads;
        }

    }

}
