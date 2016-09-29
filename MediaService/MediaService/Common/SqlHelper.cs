using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace MediaService
{
    class SqlHelper
    {
        public static string connectionString = MediaService.wyDataBase;

        /// <summary>   
        /// 执行update,delete 语句，返回影响的记录数   
        /// </summary>   
        /// <param name="sqlString">SQL语句</param>   
        /// <returns>影响的记录数</returns>   
        public static int ExecuteNonQuery(string sqlString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        return cmd.ExecuteNonQuery();
                    }
                    catch (Exception E)
                    {
                        MediaService.WriteLog("ExecuteNonQuerySQL:" + sqlString + E.Message, MediaService.wirtelog);
                        //throw new Exception(E.Message);
                        return 0;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行insert语句,并返回最新插入的id
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public static int ExecuteInsertReturnIDENTITY(string sqlString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        if (cmd.ExecuteNonQuery() > 0)
                        {
                            var cmdselcet = new SqlCommand("SELECT SCOPE_IDENTITY();", connection);
                            var id = (int) cmdselcet.ExecuteScalar();
                            cmdselcet.Dispose();
                            return id;
                        }
                        return 0;
                    }
                    catch (Exception E)
                    {
                        MediaService.WriteLog("ExecuteInsertReturnIDENTITY:" + sqlString + E.Message, MediaService.wirtelog);
                        return 0;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>   
        /// 执行查询结果语句，返回查询结果（object）。   
        /// </summary>   
        /// <param name="sqlString">查询结果语句</param>   
        /// <returns>查询结果（object）</returns>   
        public static object ExecuteScalar(string sqlString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        return cmd.ExecuteScalar();
                    }
                    catch (Exception E)
                    {
                        //throw new Exception(e.Message);
                        MediaService.WriteLog("ExecuteScalarSQL:" + sqlString + E.Message, MediaService.wirtelog);
                        return null;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>   
        /// 执行查询语句，返回DataTable   
        /// </summary>   
        /// <param name="SQLString">查询语句</param>   
        /// <returns>DataTable</returns>   
        public static DataTable ExecuteTable(string SQLString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                DataTable dt = new DataTable();
                try
                {
                    connection.Open();
                    SqlDataAdapter command = new SqlDataAdapter(SQLString, connection);
                    command.Fill(dt);
                    return dt;
                }
                catch (Exception E)
                {
                    //throw new Exception(ex.Message);
                    MediaService.WriteLog("ExecuteTableSQL:" + SQLString + E.Message, MediaService.wirtelog);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public static int ExecuteNonQuery(string sqlString, params SqlParameter[] paras)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(sqlString, connection);
            try
            {
                connection.Open();
                cmd.Parameters.AddRange(paras);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception E)
            {
                //throw new Exception(e.Message);
                MediaService.WriteLog("ExecuteTableSQL:" + sqlString + E.Message, MediaService.wirtelog);
                return 0;
            }
            finally
            {
                cmd.Parameters.Clear();
                cmd.Dispose();
                connection.Close();
            }
        }

        public static object ExecuteScalar(string sqlString, params SqlParameter[] paras)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(sqlString, connection);
            try
            {
                connection.Open();
                cmd.Parameters.AddRange(paras);
                return cmd.ExecuteScalar();
            }
            catch (Exception E)
            {
                //throw new Exception(e.Message);
                MediaService.WriteLog("ExecuteScalarSQL:" + sqlString + E.Message, MediaService.wirtelog);
                return null;
            }
            finally
            {
                cmd.Parameters.Clear();
                cmd.Dispose();
                connection.Close();
            }
        }

        /// <summary>
        /// 执行查询语句，返回DataTable
        /// </summary>
        /// <param name="sqlString">查询语句</param>
        /// <param name="paras">参数集合</param>
        /// <returns>DataTable</returns>
        public static DataTable ExecuteTable(string sqlString, params SqlParameter[] paras)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(sqlString, connection);
            try
            {
                cmd.Parameters.AddRange(paras);//添加参数
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception E)
            {
                //throw new Exception(E.Message);
                MediaService.WriteLog("ExecuteTableSQL:" + sqlString + E.Message, MediaService.wirtelog);
                return null;
            }
            finally
            {
                cmd.Parameters.Clear();
                cmd.Dispose();
                connection.Close();
            }
        }
    }
}