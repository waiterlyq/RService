using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Rlib;
using Pylib;
using DBLib;
using Loglib;

namespace RWCF
{
    /// <summary>
    /// 描述模型的状态  等待执行、执行中、
    /// </summary>
    public enum ModStatus { WaitingForGenerate, Generating, Generated }

    public class RDSTree : IDisposable
    {
        /// <summary>
        /// 模型GUID
        /// </summary>
        public string ModGUID;
        /// <summary>
        /// 生产库连接类
        /// </summary>
        public SQLHelper mydb;
        /// <summary>
        /// 目标库连接类
        /// </summary>
        public SQLHelper targetdb;
        /// <summary>
        /// 字段拼音中文对照
        /// </summary>
        public DataTable dtce;
        /// <summary>
        /// 结果字段
        /// </summary>
        public string strResultFactor;
        public bool IsFileDataSource;
        public bool IsComplete;

        public RDSTree(string strModGUID)
        {
            ModGUID = strModGUID;
            try
            {
                mydb = new SQLHelper();
                string strTargetConn = mydb.GetObject("SELECT  'data source=' + ModServer + ';initial catalog=' + ModDataBase + ';user id=' + ModUid + ';password=' + ModPassword + ';' FROM    dbo.DSTreeModel  WHERE ModGUID = '" + strModGUID + "'").ToString();
                targetdb = new SQLHelper(strTargetConn);
                dtce = mydb.GetTable("SELECT ECellName AS cn,CCellName AS cnz FROM dbo.DSTreeCEMap WHERE ModGUID = '" + ModGUID + "'");
                strResultFactor = mydb.GetObject("SELECT ECellName FROM dbo.DSTreeCEMap WHERE ModGUID = '" + strModGUID + "' AND IsResultFactor = 1").ToString();
                IsFileDataSource = mydb.GetObject("SELECT ISNULL(IsFileDataSource,0) AS IsFileDataSource FROM dbo.DSTreeModel WHERE ModGUID = '" + strModGUID + "'").ToString() == "1" ? true : false;
                IsComplete = true;
            }
            catch (Exception e)
            {
                MyLog.writeLog("ERROR", e);
                IsComplete = false;
            }
        }

        /// <summary>
        /// 更改模型状态
        /// </summary>
        /// <param name="ms"></param>
        public void ChangeModStatus(ModStatus ms)
        {
            switch (ms)
            {
                case ModStatus.WaitingForGenerate:
                    mydb.ExcuteSQL("UPDATE dbo.DSTreeModel SET ModStatus = '等待生成' WHERE ModGUID = '" + ModGUID + "'");
                    break;
                case ModStatus.Generating:
                    mydb.ExcuteSQL("UPDATE dbo.DSTreeModel SET ModStatus = '正在生成' WHERE ModGUID = '" + ModGUID + "'");
                    mydb.ExcuteSQL("DELETE FROM dbo.DSTreeFactors WHERE ModGUID = '" + ModGUID + "'");
                    mydb.ExcuteSQL("DELETE FROM DSTree WHERE ModGUID = '" + ModGUID + "'");
                    break;
                case ModStatus.Generated:
                    mydb.ExcuteSQL("UPDATE dbo.DSTreeModel SET ModStatus = '生成成功',ModGenerateTime=GETDATE() WHERE ModGUID = '" + ModGUID + "'");
                    break;
            }
        }

        /// <summary>
        /// 生成决策树
        /// </summary>
        public void GenerateDstree()
        {
            if (!IsComplete)
            {
                return;
            }
            ChangeModStatus(ModStatus.Generating);
            RDataFramePy rdfpy = new RDataFramePy();
            rdfpy.setDataFrameInRByDt(GetDataSample());
            RC50 rc = new RC50(rdfpy.DfName, strResultFactor);
            if (rc.EvaluateByR(rdfpy.DfR))
            {
                rdfpy.DfR = "";
            }
            RC50Tree rct = rc.getC50Tree(ModGUID, dtce, rdfpy.DtPy);
            mydb.BulkToDB(rct.DtDstree, "DSTree");
            mydb.BulkToDB(rct.DtFactors, "DSTreeFactors");
            ReCalcFators();
            ChangeModStatus(ModStatus.Generated);
        }

        /// <summary>
        /// 重算属性贡献度
        /// </summary>
        public void ReCalcFators()
        {
            try
            {
                string strCsSql = @"WITH    tmp
                                AS ( SELECT   a.ID AS tm ,
                                a.*
                                FROM     dbo.DSTree a
                                WHERE    a.ModGUID = '" + ModGUID + "'";
                strCsSql += @" UNION ALL
                                SELECT   c.tm AS tm ,
                                b.*
                                FROM     dbo.DSTree b
                                JOIN tmp c ON b.PID = c.ID
                                WHERE    b.ModGUID = '" + ModGUID + "'";
                strCsSql += @")
                                UPDATE  dbo.DSTree
                                SET     CoverCount = d.CoverCount ,
                                ErrorCount = d.ErrorCount
                                FROM    DSTree ,
                                ( SELECT    tm ,
                                ModGUID ,
                                SUM(CoverCount) AS CoverCount ,
                                SUM(ErrorCount) AS ErrorCount
                                FROM      tmp
                                GROUP BY  tm ,
                                ModGUID
                                ) d
                                WHERE   dbo.DSTree.ModGUID = d.ModGUID
                                AND dbo.DSTree.ID = d.tm";
                mydb.ExcuteSQL(strCsSql);
            }
            catch (Exception e)
            {
                MyLog.writeLog("ERROR", e);
            }
        }

        /// <summary>
        /// 获取数据样本
        /// </summary>
        /// <returns></returns>
        public DataTable GetDataSample()
        {
            if(IsFileDataSource)
            {

            }
            string strModDataSource = mydb.GetObject("SELECT ModDataSource FROM dbo.DSTreeModel WHERE ModGUID = '" + ModGUID + "'").ToString();
            DataTable dt = new DataTable();
            try
            {
                dt = targetdb.GetTable(strModDataSource);
                RepDtCN(ref dt);
                return dt;
            }
            catch (Exception e)
            {
                MyLog.writeLog("ERROR", e);
                return dt;
            }

        }

        /// <summary>
        /// 获得拼音展示的表头
        /// </summary>
        /// <param name="dtce"></param>
        /// <param name="dt"></param>
        public void RepDtCN(ref DataTable dt)
        {
            int ic = dt.Columns.Count;
            for (int i = 0; i < ic; i++)
            {
                dt.Columns[i].ColumnName = dtce.Select("cnz = '" + dt.Columns[i].ColumnName + "'")[0][0].ToString();
            }
        }

        public void Dispose()
        {
            mydb = null;
            targetdb = null;
        }
    }
}
