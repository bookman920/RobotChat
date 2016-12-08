using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

using GameSystem.BaseFunc;

namespace GameSystem.DataBase
{
    /// <summary>
    /// CommData 的摘要说明.
    /// </summary>
    public static class CommData
    {
        /// <summary>
        /// 通用排序算法
        /// </summary>
        /// <param name="_tb">查询的表名</param>
        /// <param name="_cn">查询的字段列表（逗号分隔）</param>
        /// <param name="_order">指定的排序字段</param>
        /// <param name="_cond">查询条件</param>
        /// <param name="pageNum">指定的页码</param>
        /// <param name="maxPageNum">符合条件的总页码</param>
        /// <returns></returns>
        public static string GetSortQueryString(string _tb, string _cn, string _order, string _cond, int _pageSize, int pageNum, out int maxPageNum, string ConnStr)
        {
            int totalCount = ExeFirstValue<int>("select count(*) from " + _tb + " where " + _cond, ConnStr);
            if (totalCount % _pageSize == 0)
            {
                maxPageNum = totalCount / _pageSize;
            }
            else
            {
                maxPageNum = totalCount / _pageSize + 1;
            }
            return "Select * from (select top " + _pageSize + " * from (SELECT top " + Math.Min(_pageSize * pageNum, totalCount) + _cn + " From " + _tb + " Where " + _cond + " ORDER BY " + _order + " DESC) as b order by " + _order + ") as a order by " + _order + " desc";
        }

        /// <summary>
        /// 查询并返回第一行第一列的数值.
        /// </summary>
        /// <param name="sql">执行SQL语句</param>
        /// <returns>第一行第一列的数值</returns>
        public static T ExeFirstValue<T>(string sql, string ConnStr)
        {
            T result = default(T);

            SqlConnection conn = new SqlConnection(ConnStr);

            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    object ret = cmd.ExecuteScalar();
                    if (ret != null && !Convert.IsDBNull(ret))
                    {
                        result = (T)ret;
                    }
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 检索并遍历记录集合，为每条记录执行ac，当ac返回false时时结束遍历
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static DBOperInfo OpenReadAllCondition(string sql, Func<SqlDataReader, bool> ac, string ConnStr)
        {
            DBOperInfo ret = new DBOperInfo(DBOperStatus.succ, "");

            SqlConnection conn = new SqlConnection(ConnStr);
            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (rs != null)
                    {
                        try
                        {
                            if (rs.HasRows)
                            {
                                while (rs.Read())
                                {
                                    if (ac(rs) == false)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                ret.status = DBOperStatus.noRecord;
                            }
                        }
                        catch (Exception ee)
                        {
                            ret.status = DBOperStatus.operError;
                            ret.desc = ee.Message;
                        }
                        finally
                        {
                            rs.Close();
                        }
                    }
                    else
                    {
                        ret.status = DBOperStatus.netError;
                    }
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                ret.status = DBOperStatus.netError;
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return ret;
        }

        /// <summary>
        /// 检索并遍历记录集合，为每条记录执行ac
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static DBOperInfo OpenReadAll(string sql, Action<SqlDataReader> ac, string ConnStr)
        {
            DBOperInfo ret = new DBOperInfo(DBOperStatus.succ, "");

            SqlConnection conn = new SqlConnection(ConnStr);
            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (rs != null)
                    {
                        try
                        {
                            if (rs.HasRows)
                            {
                                while (rs.Read())
                                {
                                    ac(rs);
                                }
                            }
                            else
                            {
                                ret.status = DBOperStatus.noRecord;
                            }
                        }
                        catch (Exception ee)
                        {
                            ret.status = DBOperStatus.operError;
                            ret.desc = ee.Message;
                        }
                        finally
                        {
                            rs.Close();
                        }
                    }
                    else
                    {
                        ret.status = DBOperStatus.netError;
                    }
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                ret.status = DBOperStatus.netError;
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return ret;
        }

        /// <summary>
        /// 检索并为集合的第一条记录执行ac
        /// </summary>
        /// <param name="conn">数据库连接串</param>
        /// <param name="sql"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static DBOperInfo OpenReadFirst(string sql, Action<SqlDataReader> ac, string ConnStr)
        {
            DBOperInfo ret = new DBOperInfo(DBOperStatus.succ, "");

            SqlConnection conn = new SqlConnection(ConnStr);
            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    SqlDataReader rs = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (rs != null)
                    {
                        try
                        {
                            if (rs.HasRows)
                            {
                                if (rs.Read())
                                {
                                    ac(rs);
                                }
                            }
                            else
                            {
                                ret.status = DBOperStatus.noRecord;
                            }
                        }
                        catch (Exception ee)
                        {
                            ret.status = DBOperStatus.operError;
                            ret.desc = ee.Message;
                        }
                        finally
                        {
                            rs.Close();
                        }
                    }
                    else
                    {
                        ret.status = DBOperStatus.netError;
                    }
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                ret.status = DBOperStatus.netError;
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return ret;
        }

        /// <summary>
        /// 采用事务机制，确保sqlList中包含的各语句在同一个事务中执行.
        /// </summary>
        /// <param name="al">Sql语句列表</param>
        /// <returns>执行状态</returns>
        public static bool ExecuteTranscation(ArrayList al, string ConnStr)
        {
            bool retValue = false;
            //下面开始一个通用的、完整的事务处理流程
            SqlConnection myConnection = new SqlConnection(ConnStr);
            try
            {
                myConnection.Open();
                // Start a local transaction
                SqlTransaction myTrans = myConnection.BeginTransaction(IsolationLevel.ReadCommitted, "SampleTransaction");
                // Must assign both transaction object and connection to Command object for a pending local transaction
                SqlCommand myCommand = myConnection.CreateCommand();
                myCommand.Connection = myConnection;
                myCommand.Transaction = myTrans;

                try
                {
                    foreach (string str in al)
                    {
                        myCommand.CommandText = str;
                        myCommand.ExecuteNonQuery();
                    }
                    myTrans.Commit();
                }
                catch (Exception e)
                {
                    try
                    {
                        myTrans.Rollback("SampleTransaction");
                    }
                    catch (SqlException ex)
                    {
                        Logger.Error("ExecuteTranscation", ex);
                    }

                    string retLog = "\r\n\r\n";
                    foreach (string str in al)
                    {
                        retLog += str.ToString() + "\r\n";
                    }
                    Logger.Error(retLog, e);
                }
                finally
                {
                    myCommand.Dispose();
                    myTrans.Dispose();
                }
                retValue = true;
            }
            catch (Exception e)
            {
                Logger.Error("ExecuteTranscation", e);
            }
            finally
            {
                myConnection.Dispose();
            }
            return retValue;
        }

        /// <summary>
        /// 执行SQL语句，返回影响的行数 如 Delete、Update、Insert
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteRow(string sql, string ConnStr)
        {
            int retValue = -1;

            SqlConnection conn = new SqlConnection(ConnStr);

            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    retValue = cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return retValue;
        }

        /// <summary>
        /// 执行Sql语句，返回本语句组作用域内的最新种子值.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ConnectString"></param>
        /// <returns></returns>
        public static int ExecuteCurrentScalar(string sql, string ConnStr)
        {
            int retValue = -1;
            SqlConnection conn = new SqlConnection(ConnStr);

            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteScalar();
                    cmd.CommandText = "select SCOPE_IDENTITY() as SerialNo";
                    object ret = cmd.ExecuteScalar();
                    if (ret != null && !Convert.IsDBNull(ret))
                    {
                        retValue = Convert.ToInt32(ret.ToString());
                    }
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(sql, ex);
            }
            finally
            {
                conn.Dispose();
            }
            return retValue;
        }

        /// <summary>
        /// 执行SQL语句(如 Delete、Update、Insert)
        /// </summary>
        /// <param name="sql">SQL语句</param>
        public static bool Execute(string sql, string ConnStr)
        {
            return Execute(sql, null, ConnStr);
        }

        /// <summary>
        /// 执行SQL语句 如 Delete、Update、Insert
        /// </summary>
        /// <param name="sql">SQL语句</param>
        public static bool Execute(string sql, string ConnStr, Action<SqlCommand> ac)
        {
            bool retValue = false;
            SqlConnection conn = new SqlConnection(ConnStr);

            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    if (ac != null)
                    {
                        ac(cmd);
                    }
                    cmd.ExecuteNonQuery();
                    retValue = true;
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                conn.Dispose();
            }
            return retValue;
        }
        
        /// <summary>
        /// 执行SQL语句 如 Delete、Update、Insert
        /// </summary>
        /// <param name="sql">要执行的Sql语句</param>
        /// <param name="cmdParms">附加参数</param>
        /// <param name="ConnStr">连接串</param>
        /// <returns>True 执行成功 False 执行失败</returns>
        public static bool Execute(string sql, SqlParameter[] cmdParms, string ConnStr)
        {
            bool retValue = false;
            SqlConnection conn = new SqlConnection(ConnStr);

            try
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                try
                {
                    cmd.CommandText = sql;
                    if (cmdParms != null && cmdParms.Length > 0) {
                        foreach (SqlParameter item in cmdParms) {
                            cmd.Parameters.Add(item);
                        }
                    }
                    cmd.ExecuteNonQuery();
                    retValue = true;
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                conn.Dispose();
            }
            return retValue;
        }

        /// <summary>
        /// 执行SQL语句，返回影响的记录数.
        /// </summary>
        /// <param name="sql">要执行的Sql语句</param>
        /// <param name="cmdParms">附加参数</param>
        /// <param name="ConnStr">连接串</param>
        /// <returns></returns>
        public static int ExecuteSql(string sql, SqlParameter[] cmdParms, string ConnStr)
        {
            int retValue = -1;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {

                        PrepareCommand(cmd, conn, null, sql, cmdParms);
                        object ret = cmd.ExecuteScalar();
                        if (ret != null && !Convert.IsDBNull(ret))
                        {
                            retValue = Convert.ToInt32(ret.ToString());
                        }
                    }
                    finally
                    {
                        conn.Dispose();
                    }
                }
            }
            return retValue;
        }

        public static bool ExecuteForEach(ArrayList al, string ConnStr)
        {
            foreach (string s in al)
            {
                CommData.Execute(s, ConnStr);
            }
            return true;
        }

        public enum DBOperStatus
        {
            /// <summary>
            /// 操作成功
            /// </summary>
            succ = 0,
            /// <summary>
            /// 没有记录
            /// </summary>
            noRecord,
            /// <summary>
            /// 网络故障
            /// </summary>
            netError,
            /// <summary>
            /// 执行错误
            /// </summary>
            operError
        }

        public class DBOperInfo
        {
            public DBOperStatus status;
            public string desc;

            public DBOperInfo(DBOperStatus _status, string _desc)
            {
                this.status = _status;
                this.desc = _desc;
            }
        }

        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, string cmdText, SqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = CommandType.Text;
            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }
    }
}
