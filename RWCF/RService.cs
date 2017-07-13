using System;
using System.Web;
using System.IO;
using Loglib;

namespace RWCF
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“Service1”。
    public class RService : IRService
    {

        /// <summary>
        /// 添加到执行队列
        /// </summary>
        /// <param name="strModGUID"></param>
        /// <returns></returns>
        public bool AddRq(string strModGUID)
        {
            if (string.IsNullOrEmpty(strModGUID))
            {
                return false;
            }
            RQueue rq = RQueue.getInstance();
            rq.Enqueue(strModGUID);
            return true;
        }

        public bool saveFile(string fileName, string context)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                File.WriteAllText(filePath, context);
            }
            catch(Exception e)
            {
                MyLog.writeLog("ERROR", e);
            }
            
            return true;
        }
    }
}
