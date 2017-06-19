using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using DBLib;
using Loglib;
using Rlib;

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
                return ;
            }
            try
            {
                SQLHelper sqlMydb = new SQLHelper();
                sqlMydb.ExcuteSQL("DELETE FROM DSTree WHERE ModGUID = '" + strModGUID + "'");
                DataTable dtce = sqlMydb.GetTable("SELECT ECellName AS cn,CCellName AS cnz FROM dbo.DSTreeCEMap WHERE ModGUID = '" + strModGUID + "'");
                string strModDataSource = sqlMydb.GetTable("SELECT ModDataSource FROM dbo.DSTreeModel WHERE ModGUID = '" + strModGUID + "'").Rows[0][0].ToString();
                string strIsResultFactor = sqlMydb.GetTable("SELECT ECellName FROM dbo.DSTreeCEMap WHERE ModGUID = '" + strModGUID + "' AND IsResultFactor = 1").Rows[0][0].ToString();
                string strTargetConn = sqlMydb.GetObject("SELECT  'data source=' + ModServer + ';initial catalog=' + ModDataBase + ';user id=' + ModUid + ';password=' + ModPassword + ';' FROM    dbo.DSTreeModel").ToString();
                SQLHelper sqlTargetdb = new SQLHelper(strTargetConn);
                DataTable dtsc = sqlTargetdb.GetTable(strModDataSource);
                RDataFramePy rdfpy = new RDataFramePy();
                rdfpy.setDataFrameInRByDt(dtsc);
                dtsc.Clear();
                using (RC50 rc = new RC50(rdfpy.DfName, strIsResultFactor))
                {
                    if (rc.EvaluateByR(rdfpy.DfR))
                    {
                        rdfpy.DfR = "";
                    }
                    RC50Tree rct = rc.getC50Tree(strModGUID, dtce, rdfpy.DtPy);
                    sqlMydb.BulkToDB(rct.DtDstree, "DSTree");
                }
                sqlMydb = null;
                sqlTargetdb = null;
            }
            catch (Exception e)
            {
                MyLog.writeLog("ERROR", logtype.Error, e);
            }
        }
    }
}
