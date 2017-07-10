using System;
using Loglib;

namespace RWCF
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“Service1”。
    public class RService : IRService
    {
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

        /// <summary>
        /// 生成决策树
        /// </summary>
        /// <param name="strModGUID"></param>
        public static void GenerDstree(string strModGUID)
        {
            if (string.IsNullOrEmpty(strModGUID))
            {
                return;
            }
            try
            {
                using (RDSTree rt = new RDSTree(strModGUID))
                {
                    rt.GenerateDstree();
                }
            }
            catch (Exception e)
            {
                MyLog.writeLog("ERROR", logtype.Error, e);
            }
        }
    }
}
