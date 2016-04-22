using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;

namespace MutiThreadForWebAPI
{
    public partial class Form1 : Form
    {
        #region 定数

        //デフォルトスレッド数
        private static int defaultThreadCount = 4;

        //デフォルト請求回数
        private static int defaultRequestCount = 200;

        //ロガー
        private static Logger logger = LogManager.GetCurrentClassLogger(); 

        #endregion

        #region 変数

        //WebAPIエンドポイント
        string endPoint = "";

        //スレッド数
        int threadCount = 0;

        //請求回数
        int requestCount = 0;

        //郵便番号
        List<string> postalCodesList = new List<string>();
        #endregion

        #region インスタンス
        /// <summary>
        /// インスタンス
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }
        #endregion

        #region イベント
        /// <summary>
        /// WebAPI請求実行ボタン押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //入力有効性チェック
            if(CheckInputValid())
            {
                button1.Hide();
                label4.Text = @"WebAPI請求処理中.......終了のメッセージ出すまで待ってください。";

                try
                {
                    var po = new ParallelOptions();

                    po.MaxDegreeOfParallelism = threadCount;

                    string[] postalCodes = postalCodesList.ToArray();
                    Parallel.ForEach(postalCodes, po, postalCode => { SendWebAPIRequest(postalCode); });

                }
                catch
                {
                    MessageBox.Show("異常発生しました。");
                }
                finally
                {
                    button1.Show();
                    label4.Text = "";
                    MessageBox.Show("処理終了。");
                }
            }            
        }
        /// <summary>
        /// ファイル選択ボタン押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Title = "ファイルを選択してください";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "csv";
            openFileDialog1.Filter = "TXTファイル|*.txt|CSVファイル|*.csv|すべてのファイル|*.*";
            openFileDialog1.FilterIndex = 3;
            openFileDialog1.RestoreDirectory = true;

            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = openFileDialog1.FileName;
            }
        }
        #endregion

        #region 内部メソッド
        /// <summary>
        /// WebAPI請求
        /// </summary>
        /// <param name="postalCode"></param>
        private void SendWebAPIRequest(string postalCode)
        {
            string strUrl = "";
            for (int i = 1; i <= requestCount; i++)
            {
                strUrl = endPoint;
                if (i % 2 == 1)
                {
                    strUrl = String.Format("{0}exists/?{1}", endPoint, postalCode);
                }
                else if (i % 2 == 0)
                {
                    strUrl = String.Format("{0}exists/?{1}-", endPoint, postalCode);
                }
                logger.Info(strUrl + Environment.NewLine);
                WebRequest webreq = WebRequest.Create(strUrl);
                //サーバーからの応答を受信するためのHttpWebResponseを取得
                var webres = (System.Net.HttpWebResponse)webreq.GetResponse();
                //応答ステータスコードを判定 
                if (webres.StatusCode == HttpStatusCode.OK)
                {
                    var stream = webres.GetResponseStream();
                    var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS"));
                    var responseString = reader.ReadToEnd();

                    logger.Info(responseString + Environment.NewLine);
                }
            }
            return;
        }
        /// <summary>
        /// 入力有効性チェック
        /// </summary>
        /// <returns></returns>
        private bool CheckInputValid()
        {
            //WebAPIエンドポイント入力チェック
            if (string.IsNullOrEmpty(this.textBox4.Text))
            {
                MessageBox.Show("WebAPIエンドポイントは入力不正です！");
                return false;
            }
            else
            {
                endPoint = this.textBox4.Text;
            }

            //WebAPIエンドポイント入力チェック
            if (Int32.TryParse(this.textBox2.Text, out requestCount) == false)
            {
            }

            //スレッド数入力チェック
            if (Int32.TryParse(this.textBox1.Text, out threadCount) == false)
            {
                DialogResult result = MessageBox.Show(string.Format("スレッド数異常入力：デフォルトスレッド数：{0} で実行しますか？", defaultThreadCount), 
                    "質問",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.OK)
                {
                    threadCount = defaultThreadCount;
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            //請求回数入力チェック
            if (Int32.TryParse(this.textBox2.Text, out requestCount) == false)
            {
                DialogResult result = MessageBox.Show(string.Format("請求回数異常入力：デフォルト請求回数：{0} で実行しますか？", defaultRequestCount),
                    "質問",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.OK)
                {
                    requestCount = defaultRequestCount;
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            //郵便番号選択入力チェック
            if (File.Exists(this.textBox3.Text))
            {
                postalCodesList.Clear();
                try
                {
                    string value;
                    using (TextReader fileReader = File.OpenText(this.textBox3.Text))
                    using (var csv = new CsvHelper.CsvReader(fileReader))
                    {
                        csv.Configuration.HasHeaderRecord = false;
                        while (csv.Read())
                        {
                            for (int i = 0; csv.TryGetField<string>(i, out value); i++)
                            {
                                postalCodesList.Add(value);
                            }
                        }
                        if (postalCodesList.Count <= 0)
                        {
                            MessageBox.Show("郵便番号ファイルに何もないです！");
                            return false;
                        }
                        else
                        {
                            this.label10.Text = string.Format("郵便番号数({0}) * 請求回数 : {1}", postalCodesList.Count, postalCodesList.Count * requestCount);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("郵便番号ファイルを正常に読み込めませんでした！");
                    return false;
                }
            }
            else
            {
                // filePathのファイルは存在しない
                MessageBox.Show("郵便番号ファイルを選択してください！");
                return false;
            }
            return true;
        }
        #endregion
    }

    ///// <summary>
    ///// 格納用クラス
    ///// </summary>
    //public class CarInfo
    //{
    //    public string carCode { get; set; }
    //    public string updYmd { get; set; }
    //}

    ///// <summary>
    ///// マッピング用クラス
    ///// </summary>
    //public sealed class CarInfoMapper : CsvHelper.Configuration.CsvClassMap<CarInfo>
    //{
    //    public CarInfoMapper()
    //    {
    //        Map(x => x.carCode).Index(0);
    //        Map(x => x.updYmd).Index(1);
    //    }
    //}
}
