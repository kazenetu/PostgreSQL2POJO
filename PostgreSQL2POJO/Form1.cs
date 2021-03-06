﻿using Npgsql;
using System;
using System.Data;
using System.IO;
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

        /// <summary>
        /// フォームオープン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 画面設定値を設定
            var path = string.Format(@"{0}\inputData.txt", Application.StartupPath);
            if (File.Exists(path))
            {
                var readDataArray = File.ReadAllText(path, Encoding.UTF8).Split(',');

                var index = 0;
                txtURL.Text = readDataArray[index++];
                txtDBName.Text = readDataArray[index++];
                txtUser.Text = readDataArray[index++];
                txtPW.Text = readDataArray[index++];
                txtNameSpace.Text = readDataArray[index++];
                txtOutputPath.Text = readDataArray[index++];
            }
        }

        /// <summary>
        /// フォームクローズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 画面設定値を保存
            var path = string.Format(@"{0}\inputData.txt", Application.StartupPath);

            var inputData = new StringBuilder();
            inputData.Append(txtURL.Text);
            inputData.Append(",");
            inputData.Append(txtDBName.Text);
            inputData.Append(",");
            inputData.Append(txtUser.Text);
            inputData.Append(",");
            inputData.Append(txtPW.Text);
            inputData.Append(",");
            inputData.Append(txtNameSpace.Text);
            inputData.Append(",");
            inputData.Append(txtOutputPath.Text);

            File.WriteAllText(path, inputData.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 出力パス設定ダイアログを表示ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetOutputPath_Click(object sender, EventArgs e)
        {
            txtOutputPath.Text = string.Empty;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// 生成ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCreate_Click(object sender, EventArgs e)
        {
            // テスト用
            var debugText = new StringBuilder();

            var tc = getTableCols();

            var tableName = string.Empty;
            var tableComment = string.Empty;
            var privates = new StringBuilder();
            var methods = new StringBuilder();

            foreach(DataRow row in tc.Rows)
            {
                if(tableName != row["table_name"].ToString())
                {
                    if(methods.Length > 0)
                    {
                        debugText.AppendLine("[" + tableName + "]");
                        debugText.AppendLine(outpuFile(tableName, tableComment, privates.ToString(), methods.ToString()));

                        // Codeのクリア
                        methods.Length = 0;
                        privates.Length = 0;
                    }
                    tableName = row["table_name"].ToString();
                    tableComment = row["table_comment"].ToString();
                }

                // プライベートフィールド
                var fieldName = snakeCase2CamelCase(row["column_name"].ToString(), false);
                var columnComment = row["column_comment"].ToString();
                var columnCommentTabIndex = columnComment.IndexOf("\t");
                if (columnCommentTabIndex > 0)
                {
                    columnComment = columnComment.Substring(0, columnCommentTabIndex);
                }

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
                else if (orgDataType == "numeric")
                {
                    dataType = "java.math.BigDecimal";
                }
                else if (orgDataType == "bigint")
                {
                    dataType = "long";
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
                else if (orgDataType == "time without time zone")
                {
                    dataType = "java.sql.Timestamp";
                }
                privates.AppendLine();
                privates.AppendLine("    /**");
                privates.AppendLine("     * " + columnComment);
                privates.AppendLine("     */");
                privates.AppendLine(string.Format("    private {0} {1};", dataType, fieldName));

                var methodBaseName = snakeCase2CamelCase(row["column_name"].ToString(), true);

                methods.AppendLine();
                methods.AppendLine("    /**");
                methods.AppendLine("     * " + columnComment + "を設定");
                methods.AppendLine("     * @param value 設定値");
                methods.AppendLine("     */");
                methods.AppendLine(string.Format("    public void set{0}({1} value) ", methodBaseName, dataType) + "{");
                if(dataType == "java.math.BigDecimal")
                {
                    methods.AppendLine("        if(value == null) {");
                    methods.AppendLine(string.Format("            {0} = new {1}(0);", fieldName, dataType));
                    methods.AppendLine("            return;");
                    methods.AppendLine("        }");

                }
                methods.AppendLine(string.Format("        {0} = value;", fieldName));
                methods.AppendLine("    }");

                methods.AppendLine();
                methods.AppendLine("    /**");
                methods.AppendLine("     * " + columnComment + "を取得");
                methods.AppendLine("     * @return " + fieldName + "を返す");
                methods.AppendLine("     */");
                methods.AppendLine(string.Format("    public {0} get{1}()", dataType, methodBaseName) + "{");
                methods.AppendLine(string.Format("        return {0};", fieldName));
                methods.AppendLine("    }");
            }
            if (methods.Length > 0)
            {
                debugText.AppendLine("[" + tableName + "]");
                debugText.AppendLine(outpuFile(tableName, tableComment, privates.ToString(), methods.ToString()));
            }

            if (txtOutputPath.Text.Length > 0)
            {
                var outputMessage = "ファイルを" + txtOutputPath.Text + "に出力しました。";
                debugText.AppendLine("---------------------------------------------------------");
                debugText.AppendLine(outputMessage);
                debugText.AppendLine("---------------------------------------------------------");

                // メッセージボックスを表示
                MessageBox.Show(outputMessage);
            }
            //テスト用文字列をテキストエリアに格納
            this.textBox1.Text = debugText.ToString();

        }

        /// <summary>
        /// ファイル出力
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <param name="tableComment">テーブルコメント</param>
        /// <param name="privates">プライベートフィールド</param>
        /// <param name="methods">パブリックメソッド</param>
        /// <returns>出力内容</returns>
        private string outpuFile(string tableName, string tableComment, string privates, string methods)
        {
            var className = snakeCase2CamelCase(tableName, true) + "Entity";

            var outputData = new StringBuilder();

            //名前空間の設定
            if (!string.IsNullOrEmpty(txtNameSpace.Text))
            {
                outputData.AppendLine("package " + txtNameSpace.Text + ";");
            }

            //クラス名をつける
            outputData.AppendLine("/**");
            outputData.AppendLine(" * " + tableComment);
            outputData.AppendLine(" */");
            outputData.AppendLine(string.Format("public class {0} ", className) + "{");

            //プライベートフィールドをつける
            outputData.Append(privates.ToString());

            //セッター/ゲッター メソッドをつける
            outputData.Append(methods.ToString());
            outputData.AppendLine("}");

            if (txtOutputPath.Text.Length > 0)
            {
                var path = string.Format(@"{0}\{1}.java", txtOutputPath.Text, className);
                File.WriteAllText(path, outputData.ToString(), Encoding.UTF8);
            }

            return outputData.ToString();
        }

        /// <summary>
        /// スネークケースからキャメルケースに変換
        /// </summary>
        /// <param name="srcSnakeCase">変換元</param>
        /// <param name="isUpper">アッパーキャメルにするか否か</param>
        /// <returns>キャメルケース</returns>
        private string snakeCase2CamelCase(string srcSnakeCase,bool isUpper)
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
            sql.AppendLine("  coalesce(pd.description,table_name) as table_comment,");
            sql.AppendLine("  information_schema.columns.data_type,");
            sql.AppendLine("  column_name,");
            sql.AppendLine("  coalesce(pdc.description,column_name) as column_comment");
            sql.AppendLine("from information_schema.columns");
            sql.AppendLine("inner join pg_stat_user_tables");
            sql.AppendLine("  on table_name = pg_stat_user_tables.relname");
            sql.AppendLine("left join pg_description pd");
            sql.AppendLine("  on pg_stat_user_tables.relid = pd.objoid and pd.objsubid = 0");
            sql.AppendLine("left join pg_description pdc");
            sql.AppendLine("  on pg_stat_user_tables.relid = pdc.objoid and pdc.objsubid = ordinal_position");
            sql.AppendLine("order by table_name,ordinal_position;");

            var result = new DataTable();
            result.Columns.Add("table_name", typeof(string));
            result.Columns.Add("table_comment", typeof(string));
            result.Columns.Add("data_type", typeof(string));
            result.Columns.Add("column_name", typeof(string));
            result.Columns.Add("column_comment", typeof(string));


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
