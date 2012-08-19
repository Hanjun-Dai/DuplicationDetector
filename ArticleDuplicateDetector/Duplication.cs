using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Analyzer.Core.Algorithm.FingerPrint;
//using Crawler.Core.Utility;
using HooLab.Config;
using HooLab.Log;
using MongoDB.Driver.Builders;
using Palas.Common;
using Palas.Common.Data;

namespace Analyzer.Core.Algorithm
{
    /// <summary>
    /// 基于标题的去重算法
    /// </summary>
    public class Duplication
    {
        /// <summary>
        /// 转载检查的回溯天数(默认10)
        /// </summary>
        public static int nBackwardDays = Configger.GetConfigInt("Palas.Analyzer.Core.Duplication.BackwardDays", 7);

        /// <summary>
        /// 算法1：相似度阈值，高于此值则认为是相似
        /// </summary>
        static double SimThreshold_TitleCompare = double.Parse(Configger.GetConfig("Palas.Analyzer.Core.Duplication.SimThreshold_TitleCompare", "0.37"));

        /// <summary>
        /// 算法1：纯标题-DuplicationID集合
        /// </summary>
        ConcurrentDictionary<string, string> PureTitles = new ConcurrentDictionary<string, string>(/*nBackwardDays * 3000*/);

        /// <summary>
        /// 算法2：文本指纹比较器
        /// </summary>
        static DetectorFacade FingerDetector;

        static Duplication _Instance = null;
        static Duplication Instance
        {
            get 
            {
                //如果未曾初始化或上次初始化已经超过24小时，则重新初始化
                if (_Instance == null || LastInitTime.AddHours(RefreshHours) < DateTime.Now)
                    lock(typeof(Duplication))
                        if (_Instance == null || LastInitTime.AddHours(RefreshHours) < DateTime.Now)
                        {
                            _Instance = new Duplication();
                        }
                return _Instance;
            }
        }

        static DateTime LastInitTime = DateTime.Now.AddDays(-1);
        const int RefreshHours = 24;

        /// <summary>
        /// 构造函数
        /// </summary>
        private Duplication()
        {
            try
            {
                //加载
                init_FingerPrint();
            }
            catch (Exception e)
            {
                Logger.Error("Duplication Init Fail", e);
                Console.WriteLine("Duplication Init Fail:" + e.Message);
            }
        }

        /* 基于文章标题字符串距离的比较函数
        
        /// <summary>
        /// 初始化，读入所有文章标题
        /// </summary>
        void init_TitleCompare()
        {
            PureTitles.Clear();

            //对过去的nBackwardDays的每一天标题
            for (int Day = nBackwardDays; Day >= 0; Day--)
            {
                //foreach (string Line in File.ReadLines(FN, Encoding.UTF8))
                //    if (!string.IsNullOrEmpty(Line))
                //    {
                //        string DupID = Line.Substring(0, 32);
                //        string PureTitle = Line.Substring(33);

                //        if (!PureTitles.ContainsKey(PureTitle))
                //            PureTitles.Add(PureTitle, DupID);
                //    }
            }

            LastInitTime = DateTime.Now;
        }

        /// <summary>
        /// 检索转载
        /// </summary>
        /// <param name="CleanTitle"></param>
        /// <returns>空或者DuplicationID</returns>
        string IsDup_TitleCompare(string CleanTitle, string ItemID)
        {
            string PureTitle = GetPureTitle(CleanTitle, true);
            if (string.IsNullOrEmpty(PureTitle)) return null;

            //对考察范围内的所有标题进行相似度匹配
            foreach (string OldTitle in PureTitles.Keys)
            {
                double Sim = StringCompare.NeedlemanWunsch.Sim(PureTitle, OldTitle);
                //如标题相似度符合要求
                if (Sim >= SimThreshold)
                    //如果与ItemID一致，很可能是上次运行出错导致此Item二次监测，返回null
                    if (PureTitles[OldTitle] == ItemID)
                        return null;
                    else
                        //匹配上
                        return PureTitles[OldTitle];
            }

            //不含，添加入列表
            PureTitles.TryAdd(PureTitle, ItemID);
            return null;
        }

        /// <summary>
        /// 检索转载，基于标题字符串距离比较
        /// </summary>
        /// <param name="CleanTitle"></param>
        /// <returns>空或者DuplicationID</returns>
        public static string IsDuplication_TitleCompare(string CleanTitle, string ItemID)
        {
            return Instance.IsDup_TitleCompare(CleanTitle, ItemID);
        }
         
         */

        private void init_FingerPrint()
        {
            //Console.WriteLine("============ Duplication ============");
            //Console.WriteLine(string.Format("[{0}]Strart loading {1} days' items for duplication. All threads blocked.", DateTime.Now.ToShortTimeString(), nBackwardDays));
            //DateTime startTime = DateTime.Now;

            //获取N天内的Items总数
            //var query = Query.GT("PubDate", DateTime.Now.AddDays(-nBackwardDays));
            //int count = -1;
            //try
            //{
            //    count = MongoItemAccess.Items.Count(query);
            //}
            //catch (Exception e)
            //{
            //    Logger.Error(e.Message + "\n" + e.StackTrace);
            //}

            //创造指纹识别器
            FingerDetector = new DetectorFacade(new Parameters());

            //分天读取
            //if (count > 0)
            //{
            //    for (int i = -nBackwardDays; i <= 0; i++)
            //    {
            //        Console.Write(DateTime.Now.AddDays(i).ToShortDateString()+" : ");
            //        LoadDaily_Mongo(DateTime.Now.AddDays(i), FingerDetector);
            //        Console.WriteLine();
            //    }
            //}

            
            //LastInitTime = DateTime.Now;
            //Console.WriteLine(string.Format("Loaded {0} items for {1:F1} mins.", FingerDetector.GetItemCount(), (DateTime.Now - startTime).TotalMinutes));
            //Logger.Info(string.Format("Loaded {0} items for dup for {1:F1} mins.", FingerDetector.GetItemCount(), (DateTime.Now - startTime).TotalMinutes));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Date"></param>
        /// <returns></returns>
        //static void LoadDaily_Mongo(DateTime Date, DetectorFacade detector)
        //{
        //    //左闭右开
        //    //QueryConditionList low = Query.GTE("PubDate", Date.Date);
        //    //QueryConditionList high = Query.LT("PubDate", Date.Date.AddDays(1));
        //    //var query = Query.And(low, high);

        //    //这里就已经挂了
        //    //int count = MongoItemAccess.Items.Count(query);

        //    //总count>4000要再分批
        //    //const int MaxSetSize = 4000;
        //    //if (count < MaxSetSize)
        //    //    LoadPeriod_Mongo(Date.Date, Date.Date.AddDays(1), Items);
        //    //else
        //    {
        //        int HourStep = 1; //24 / ((count + MaxSetSize - 1) / MaxSetSize);
        //        int Hour = 0;
        //        while (Hour < 24)
        //        {
        //            LoadPeriod_Mongo(Date.Date.AddHours(Hour), Date.Date.AddHours(Hour + HourStep > 24 ? 24 : Hour + HourStep), detector);
        //            Hour += HourStep;
        //            Console.Write('*');
        //        }
        //    }
        //}

        //static void LoadPeriod_Mongo(DateTime lowTime, DateTime highTime, DetectorFacade detector)
        //{
        //    //左闭右开
        //    QueryConditionList low = Query.GTE("FetchTime", lowTime);
        //    QueryConditionList high = Query.LT("FetchTime", highTime);
        //    var query = Query.And(low, high);
        //    var sort = SortBy.Ascending("FetchTime");

        //    //int count = MongoItemAccess.Items.Count(query);

        //    //const int packsize = 1000;   //每次读取条数
        //    const int sleepMS = 200;   //两次读取的间隔时间

        //    //int skip = 0;
        //    //while (skip < count)
        //        try
        //        {
        //            var result = MongoItemAccess.Items.Find(query)./*SetSortOrder(sort).*/SetFields("ItemID", "PubDate", "DuplicationID", "SpliteTitle", "SpliteText", "ProsdDuplication");
        //                //.Take(packsize).Skip(skip);
        //            foreach (var item in result)
        //                detector.AddItem(new ItemToDuplication(item));
        //            //skip += packsize;
        //        }
        //        catch (Exception e)
        //        {
        //            Logger.Error(string.Format(@"Duplication读取文章失败[{0}-{1}]:{2}", lowTime, highTime, e.Message));
        //            //break;
        //        }
        //        Thread.Sleep(sleepMS);
        //}

        /// <summary>
        /// (对象实例方法)转载判别，基于文本指纹
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        string IsDuplication_FingerPrint(ItemToDuplication Item)
        {
            try
            {
                //没有则加入
                string r = FingerDetector.TestAndTryAdd(Item);
                //if (!string.IsNullOrEmpty(r))
                //    FingerDetector.AddItem(Item);
                return r;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Dup Err:{0} ItemID:{1}\n{2}", e.Message, Item.ItemID, e.StackTrace));
                return null;
            }
        }

        /// <summary>
        /// （静态方法）转载判别，基于文本指纹
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public static string IsDuplication_FingerPrint(Item Item)
        {
            try
            {
                return Instance.IsDuplication_FingerPrint(new ItemToDuplication(Item));
            }
            catch (Exception e)
            {
                Logger.Error("Duplication FingerPrint test fail", e);
                return null;
            }
        }

        /// <summary>
        /// 把新闻标题改写为用作转载比对的格式
        /// </summary>
        /// <remarks>
        /// 去掉所有html
        /// 去掉头尾的常见标题前后缀
        /// 去掉头尾的无意义前后缀段
        /// 去掉中间的小短语，如括号
        /// 去掉所有的空格和标点
        /// </remarks>
        /// <param name="Title"></param>
        /// <returns></returns>
        static string GetPureTitle(string Title, bool RemoveEmpty)
        {
            //以下用于前后缀判断&去除
            const string ConnectionMark = @"-_——";
            const string FixedTitleLikeWords = @"论坛 社区 网 门户 bbs blog 空间 主页 窝";

            //作为词组的分隔
            const string GroupChars = @"\(\)\[\]{}\<\>（）【】:：";
            //换为空格
            const string SeprateChars = @"[ `~!@#$%^&*()\\-=_+[]{}|;':"",./<>?·！￥……（）【】、；：‘“”，。、《》？]+";

            //1.清洗
            string s = "";//TextCleaner.FullClean(Title, true, true, true).ToLower();

            //2.去掉常见标题前后缀
            string[] TitleLike = FixedTitleLikeWords.Split();
            Match MatchedPreTitle = Regex.Match(s, string.Format(@"^(\S{1}[{0}\s]+\s*)+", ConnectionMark, "{1,6}"));
            if (MatchedPreTitle.Success && MatchedPreTitle.Length > 0)
                foreach (string t in TitleLike)
                    if (s.IndexOf(t, 0, MatchedPreTitle.Length) >= 0)
                    {
                        s = s.Substring(MatchedPreTitle.Length);
                        break;
                    }
            Match MatchedSufTitle = Regex.Match(s, string.Format(@"(\s*[{0}\s]+\S{1})+$", ConnectionMark, "{1,6}"));
            if (MatchedSufTitle.Success && MatchedSufTitle.Length > 0)
                foreach (string t in TitleLike)
                    if (s.IndexOf(t, 0, MatchedSufTitle.Length) >= 0)
                    {
                        s = s.Substring(0, s.Length - MatchedSufTitle.Length);
                        break;
                    }

            //去掉头尾和中间的小短语
            Match head = Regex.Match(s, string.Format(@"^[{0}]*[^{0}]{1}[{0}]+", GroupChars, "{1,6}"));
            if (head.Success)
                s = s.Substring(head.Length);
            Match tail = Regex.Match(s, string.Format(@"[{0}]+[^{0}]{1}[{0}]*$", GroupChars, "{1,4}"));
            if (tail.Success)
                s = s.Substring(0, s.Length - tail.Length);

            Match sub = Regex.Match(s, string.Format(@"[{0}]+[^{0}]{1}[{0}]+", GroupChars, "{1,3}"));
            if (sub.Success)
                s = s.Replace(sub.Value, string.Empty);

            //把间隔符号换为空格
            foreach (char c in SeprateChars.ToArray())
                s = s.Replace(c.ToString(), RemoveEmpty ? string.Empty : " ");

            return s;
        }
    }
}
