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
        public bool GenerDSTree(string strModGUID)
        {
            DataTable dtce = SQLHelper.GetTable("SELECT ECellName AS cn,CCellName AS cnz FROM dbo.DSTreeCEMap WHERE ModGUID = '" + strModGUID + "'");
            string strModDataSource = SQLHelper.GetTable("SELECT ModDataSource FROM dbo.DSTreeModel WHERE ModGUID = '" + strModGUID + "'").Rows[0][0].ToString();
            string strIsResultFactor = SQLHelper.GetTable("SELECT ECellName FROM dbo.DSTreeCEMap WHERE ModGUID = '" + strModGUID + "' AND IsResultFactor = 1").Rows[0][0].ToString();
            DataTable dtsc = SQLHelper.GetTable(strModDataSource);
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
                SQLHelper.BulkToDB(rct.DtDstree, "DSTree");
            }
            return true;
        }
    }
}
