using System;
using System.Diagnostics;
using System.Management; // WMI (需要添加 System.Management 程序集引用)
using System.Linq;

namespace EgoTools.Helper
{
    class Program
    {
        static void Main(string[] args)
        {
            // 检查启动参数
            if (args.Length == 2 && args[0] == "setLimit")
            {
                if (int.TryParse(args[1], out int limit))
                {
                    // (这里是你从 App.cs 移过来的 WMI 方法)
                    ApplyBatteryChargeLimit(limit);
                }
            }
            // 任务完成，程序自动退出
        }

        // 
        // 把你的 ApplyBatteryChargeLimit 方法粘贴到这里
        //
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
        public static void ApplyBatteryChargeLimit(int percentageLimit)
        {
            Debug.WriteLine($"[WMI] 准备设置电池上限为: {percentageLimit}%");

            try
            {
                // 1. 钳制数值 (与 PS1 脚本逻辑一致)
                int clampedLimit = Math.Max(percentageLimit, 50);
                clampedLimit = Math.Min(clampedLimit, 100);

                // 2. 准备 WMI 请求的字节数组
                byte[] request = new byte[64];
                request[0] = 0x03; // MFID
                request[1] = 0x15; // SFID = SBCM
                request[2] = 0x01; // \SBCM.CHMD
                request[3] = 0x18; // \SBCM.DELY
                request[4] = (byte)(clampedLimit - 5);  // \SBCM.STCP start charge percentage threshold
                request[5] = (byte)clampedLimit;    // \SBCM.SOCP stop charge percentage threshold

                // 3. 查找 WMI 实例
                // (C# 中等效于 Get-WmiObject -Namespace ROOT\WMI -Class OemWMIMethod)
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"ROOT\WMI", "SELECT * FROM OemWMIMethod");

                // 4. 获取第一个实例
                ManagementObject? inst = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                if (inst != null)
                {
                    // 5. 准备方法参数并调用
                    // (C# 中等效于 $inst.OemWMIfun($request))
                    object[] methodArgs = { request };
                    inst.InvokeMethod("OemWMIfun", methodArgs);

                    Debug.WriteLine($"[WMI] 成功调用 OemWMIfun，设置上限为: {clampedLimit}%");
                }
                else
                {
                    Debug.WriteLine("[WMI] 错误: 未找到 'OemWMIMethod' 实例。");
                    // 可以在此处向用户显示错误（例如，驱动未安装或设备不支持）
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WMI] 调用失败: {ex.Message}");
                Debug.WriteLine("[WMI] 严重错误：请确保应用程序是以管理员权限运行的。");
                // 可以在此处向用户显示权限不足的错误
            }
        }
    }
}
//```

//#### 3. 为帮助程序添加 `app.manifest`

//1.在你新的 `EgoTools.Helper` 项目上，`右键` > `添加` > `新建项...` > `应用程序清单文件`。
//2.  将其 `requestedExecutionLevel` 设置为 `requireAdministrator`。
//    ```xml
//    <requestedExecutionLevel  level="requireAdministrator" uiAccess="false" />


