using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PostgreSQL2POJO
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            // テスト用
            var debugText = new StringBuilder();

            var tc = getTableCols();

            var tableName = string.Empty;
            var privates = new StringBuilder();
            var code = new StringBuilder();

            foreach(DataRow row in tc.Rows)
            {
                if(tableName != row["table_name"].ToString())
                {
                    if(code.Length > 0)
                    {
                        var className = snakeCase2CamelCase(tableName, true) + "Entity";

                        //クラス名とプライベートフィールドをつける
                        string header = string.Format("public class {0} ", className) + "{" + Environment.NewLine;
                        header += privates.ToString();
                        code.Insert(0, header);
                        code.AppendLine("}");

                        // TODO ファイル書き出し

                        // TEST
                        debugText.AppendLine("[" + className + ".java]");
                        debugText.AppendLine(code.ToString());

                        // Codeのクリア
                        code.Length = 0;
                        privates.Length = 0;
                    }
                    tableName = row["table_name"].ToString();
                }

                var fieldName = snakeCase2CamelCase(row["column_name"].ToString());

                var orgDataType = row["data_type"].ToString().ToLower();
                var dataType = "String";
                if (orgDataType == "integer")
                {
                    dataType = "int";
                }
                else if (orgDataType == "decimal")
                {
                    dataType = "java.math.BigDecimal";
                }
                else if (orgDataType == "date")
                {
                    dataType = "java.sql.Date";
                }
                else if (orgDataType == "time")
                {
                    dataType = "java.sql.Time";
                }
                else if (orgDataType == "timestamp")
                {
                    dataType = "java.sql.Timestamp";
                }
                privates.AppendLine(string.Format("    private {0} {1};", dataType, fieldName));

                var methodBaseName = snakeCase2CamelCase(row["column_name"].ToString(), true);
                code.AppendLine(string.Format("    public void set{0}({1} value) ", methodBaseName, dataType) + "{");
                code.AppendLine(string.Format("        {0} = value;", fieldName));
                code.AppendLine("    }");

                code.AppendLine(string.Format("    public {0} get{1}()", dataType, methodBaseName) + "{");
                code.AppendLine(string.Format("        return {0};", fieldName));
                code.AppendLine("    }");
            }
            if (code.Length > 0)
            {
                var className = snakeCase2CamelCase(tableName, true) + "Entity";

                //クラス名とプライベートフィールドをつける
                string header = string.Format("public class {0} ", className) + "{" + Environment.NewLine;
                header += privates.ToString();
                code.Insert(0, header);
                code.AppendLine("}");

                // TODO ファイル書き出し

                // TEST
                debugText.AppendLine("[" + className + ".java]");
                debugText.AppendLine(code.ToString());

                // Codeのクリア
                code.Length = 0;
                privates.Length = 0;
            }



            //テスト用文字列をテキストエリアに格納
            this.textBox1.Text = debugText.ToString();

        }

        /// <summary>
        /// スネークケースからキャメルケースに変換
        /// </summary>
        /// <param name="srcSnakeCase">変換元</param>
        /// <param name="isUpper">アッパーキャメルにするか否か</param>
        /// <returns>キャメルケース</returns>
        private string snakeCase2CamelCase(string srcSnakeCase,bool isUpper = false)
        {
            if (srcSnakeCase.Length <= 0) return "";

            string[] words = srcSnakeCase.Split('_');

            string result = "";
            for(int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if ((i==0 && isUpper) || i > 0)
                {
                    word = word[0].ToString().ToUpper() + word.Substring(1).ToLower();
                }else
                {
                    word = word.ToLower();
                }
                result += word;
            }

            return result;
        }

        /// <summary>
        /// テーブルとカラムの情報をDataTableで取得する
        /// </summary>
        /// <returns></returns>
        private DataTable getTableCols()
        {
            // 接続文字列作成
            var connectionString = string.Format("Server={0};Port=5432;User Id={1};Password={2};Database={3}",
                txtURL.Text, txtUser.Text, txtPW.Text, txtDBName.Text);

            var sql = new StringBuilder();
            sql.AppendLine("select ");
            sql.AppendLine("  table_name,");
            sql.AppendLine("  information_schema.columns.data_type,");
            sql.AppendLine("  column_name");
            sql.AppendLine("from information_schema.columns");
            sql.AppendLine("inner join pg_stat_user_tables");
            sql.AppendLine("  on table_name = pg_stat_user_tables.relname");
            sql.AppendLine("order by table_name,ordinal_position;");

            var result = new DataTable();
            result.Columns.Add("table_name", typeof(string));
            result.Columns.Add("data_type", typeof(string));
            result.Columns.Add("column_name", typeof(string));


            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    // SELECT
                    cmd.CommandText = sql.ToString();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = result.NewRow();
                            foreach (DataColumn column in result.Columns)
                            {
                                row[column.ColumnName] = reader[column.ColumnName];
                            }
                            result.Rows.Add(row);

                        }
                    }
                }
            }
            return result;
        }
    }
}
