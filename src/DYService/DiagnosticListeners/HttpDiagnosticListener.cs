using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.DiagnosticListeners
{
    public class HttpDiagnosticListener
    {
        public static void CreateListener()
        {
            // https://www.cnblogs.com/aimigi/p/14416852.html
            DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
            // 检查监听的组件是否有RequestStart 这个事件
            if (httpLogger.IsEnabled("RequestStart"))
            {
                /*
                 * Write 方法接受两个参数
                 * param1 表示事件名
                 * param2 是要写入的数据，这个数据会被可观察对象在发生这个事件时抛出抛出
                 */
                httpLogger.Write("RequestStart", new { Url = "http://test.com", Request = new { Name = "ddd", Value = "ttttt" } });

            }

            // 为发布者注册订阅者（观察者）
            DiagnosticListener.AllListeners.Subscribe(new MyObserver<DiagnosticListener>(listener =>
            {
                if (listener.Name == "System.Net.Http")
                {
                    listener.Subscribe(new MyObserver<KeyValuePair<string, object?>>(listenerData =>
                    {
                        Console.WriteLine($"Listening Name:{listenerData.Key}");
                        dynamic data = listenerData.Value ?? new { };
                        Console.WriteLine($"Listening Data Name:{data.Name} Value:{data.Value}");
                    }));
                }

            }));
        }
    }

    // 定义一个观察者
    public class MyObserver<T> : IObserver<T>
    {
        private Action<T> _next;
        public MyObserver(Action<T> next)
        {
            _next = next;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value)
        {
            _next(value);
        }
    }
}
