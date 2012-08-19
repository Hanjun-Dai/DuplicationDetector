using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Palas.Common.Data;
using System.Threading;
using ArticleDuplicateDetector.Test;
using HooLab.Log;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    class Client
    {
        private static DetectorFacade myDetector = new DetectorFacade(new Parameters(), null);
        private static void SimpleTest()
        {
            #region 建立一个item
            StringBuilder st = new StringBuilder();
            string[] article = File.ReadAllLines(@"1.txt");
            ItemToDuplication s = new ItemToDuplication(new Item());
            s.ItemID = "source"; s.MediaType = Enums.MediaType.WebNews; s.SpliteTitle = article[0]; s.PubDate = new DateTime(2011, 10, 20);
            st.Clear();
            for (int i = 1; i < article.Length; ++i)
                st.Append(article[i]);
            s.SpliteText = st.ToString();
            #endregion

            //将刚才建立的item添加到myDetector中
            myDetector.AddItem(s);

            #region 建立一个测试item
            article = File.ReadAllLines(@"2.txt");
            //s = new ItemToDuplication();
            s.ItemID = "test"; s.MediaType = Enums.MediaType.WebNews; s.SpliteTitle = article[0];
            st.Clear();
            for (int i = 1; i < article.Length; ++i)
                st.Append(article[i]);
            s.SpliteText = st.ToString();
            #endregion

            //测试转载检测
            string id = null;
            id = myDetector.IsItemCopied(s);
            if (id != null)
                Console.WriteLine("Copied Item: " + id);
            else Console.WriteLine("Not copied.");
            /*
            //删除指定日期之前的item（不包含当天，即如果删除的是2011.10.20之前的，那么就不会删除2011.10.20插入的）
            myDetector.RemoveOldItem(new DateTime(2011, 10, 20));

            //删除之后再测试一次
            id = myDetector.IsItemCopied(s);
            if (id != null)
                Console.WriteLine("Copied Item:id");
            else Console.WriteLine("Not copied.");

            //获取与之类似的所有itemID
            string[] ids = myDetector.GetSimilarItemIDs(s);
            if (ids != null)
            {
                Console.WriteLine("Contains:");
                for (int i = 0; i < ids.Length; ++i)
                    Console.WriteLine(ids[i]);
            }
            else Console.WriteLine("none"); */
        }

        private static string PATH = @"D:\Workspace\TempWorkspace\TestData";

        private static Enums.MediaType GetMediaType(string type) 
        {
            if (type.Equals("Twitter")) return Enums.MediaType.Weibo;
            if (type.Equals("Website")) return Enums.MediaType.WebNews;
            if (type.Equals("Unknown")) return Enums.MediaType.Unknown;
            if (type.Equals("Video")) return Enums.MediaType.Video;
            if (type.Equals("Blog")) return Enums.MediaType.Blog;
            if (type.Equals("PaperMagazine")) return Enums.MediaType.PaperMagazine;
            if (type.Equals("Forum")) return Enums.MediaType.Forum;
            if (type.Equals("RadioTV")) return Enums.MediaType.RadioTV;
            if (type.Equals("SNS")) return Enums.MediaType.SNS;
            if (type.Equals("eCommercial")) return Enums.MediaType.eCommercial;
            return Enums.MediaType.Unknown;
        }

        private static Enums.MediaType GetMediaTypeOfFile(string filename)
        {
            if (!File.Exists(filename)) return Enums.MediaType.Unknown;
            string[] content = File.ReadAllLines(filename);
            for (int k = 0; k < content.Length; ++k)
                if (content[k].Contains("MediaType"))
                {
                    string type = content[k].Substring(content[k].IndexOf('>') + 1, content[k].IndexOf('/') - content[k].IndexOf('>') - 2);
                    if (type.Equals("Twitter")) return Enums.MediaType.Weibo;
                    if (type.Equals("Website")) return Enums.MediaType.WebNews;
                    if (type.Equals("Unknown")) return Enums.MediaType.Unknown;
                    if (type.Equals("Video")) return Enums.MediaType.Video;
                    if (type.Equals("Blog")) return Enums.MediaType.Blog;
                    if (type.Equals("PaperMagazine")) return Enums.MediaType.PaperMagazine;
                    if (type.Equals("Forum")) return Enums.MediaType.Forum;
                    if (type.Equals("RadioTV")) return Enums.MediaType.RadioTV;
                    if (type.Equals("SNS")) return Enums.MediaType.SNS;
                    if (type.Equals("eCommercial")) return Enums.MediaType.eCommercial;
                }
            return Enums.MediaType.Unknown;
        }

        private static ItemToDuplication MakeItem(string filename, Enums.MediaType type)
        {
            string[] context = File.ReadAllLines(filename, Encoding.Default);
            StringBuilder buf = new StringBuilder();
            buf.Clear();
            for (int i = 1; i < context.Length; ++i)
                buf.Append(context[i]);
            ItemToDuplication item = new ItemToDuplication(new Item());
            item.ItemID = filename;
            item.MediaType = type;
            item.SpliteTitle = context[0];
            item.SpliteText = buf.ToString();
            item.PubDate = new DateTime(2011, 10, 20);
            return item;
        }

        private static void TestCount(int start, int end)
        {
            int articleCount = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Loading and initializing...");
            string[] days = Directory.GetDirectories(PATH);
            List<ItemToDuplication> items = new List<ItemToDuplication>();
            for (int i = start; i <= end; ++i)
            {
                Console.WriteLine(i);
                string[] files = Directory.GetFiles(days[i] + @"\SplitText");
                for (int j = 0; j < files.Length; ++j)
                {
                    string indexFile = files[j].Replace("SplitText", "Indexed").Replace(".txt", ".xml");
                    Enums.MediaType type = GetMediaTypeOfFile(indexFile);
                    items.Add(MakeItem(files[j], type));
                    articleCount++;
                }
            }
            int pre = 0, cnt = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                if (i * 100 / items.Count - pre >= 10) 
                {
                    pre = i * 100 / items.Count;
                    Console.WriteLine(pre + "%");
                }
                string filename = myDetector.IsItemCopied(items[i]);
                if (filename == null)
                    myDetector.AddItem(items[i]);
                else 
                {
                    cnt++;
                }
                items[i] = null;
            }
            items = null;
            GC.Collect();
            sw.Stop();
            Console.WriteLine("All {0} files were load. Time cost: {1}s", articleCount, sw.ElapsedMilliseconds / 1000);
            Console.WriteLine(cnt);
        }

        private static List<ItemToDuplication> items;
        private static int cnt;
        private static int curpos;
        private static object mylock = new object();

        private static void Test() 
        {
            int start = 0;
            lock (mylock) 
            {
                start = curpos;
                curpos += cnt;
            }
            int dest = start + cnt;
            if (items.Count - dest < 10)
                dest = items.Count;
            int cc = dest - start;
            int code = Thread.CurrentThread.GetHashCode();
            int pre = 0;
            Console.WriteLine("Thread " + code + " starts at " + start);
            for (int i = start; i < dest; ++i)
            {
                myDetector.IsItemCopied(items[i]);
                if ((i - start + 1) * 100 / cc - pre >= 10)
                {
                    pre = (i - start + 1) * 100 / cc;
                    Console.WriteLine("Thread " + code + " : " + pre + "%");
                }
            }
        }

        private static void LoadAll(int start, int end, int num) 
        {
            int articleCount = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Loading and initializing...");
            string[] days = Directory.GetDirectories(PATH);
            items = new List<ItemToDuplication>();
            for (int i = start; i <= end; ++i)
            {
                Console.WriteLine(i);
                string[] files = Directory.GetFiles(days[i] + @"\SplitText");
                for (int j = 0; j < files.Length; ++j)
                {
                    string indexFile = files[j].Replace("SplitText", "Indexed").Replace(".txt", ".xml");
                    Enums.MediaType type = GetMediaTypeOfFile(indexFile);
                    items.Add(MakeItem(files[j], type));
                    articleCount++;
                }
            }
            int pre = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                if (i * 100 / items.Count - pre >= 10)
                {
                    pre = i * 100 / items.Count;
                    Console.WriteLine(pre + "%");
                }
                myDetector.AddItem(items[i]);
            }
            cnt = items.Count / num;
            sw.Stop();
            Console.WriteLine("All {0} files were load. Time cost: {1}s", articleCount, sw.ElapsedMilliseconds / 1000);
            curpos = 0;
        }

        private static void TestSpeed(int start, int end) 
        {
            int articleCount = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Loading and initializing...");
            string[] days = Directory.GetDirectories(PATH);
            List<ItemToDuplication> items = new List<ItemToDuplication>();
            for (int i = start; i <= end; ++i)
            {
                Console.WriteLine(i);
                string[] files = Directory.GetFiles(days[i] + @"\SplitText");
                for (int j = 0; j < files.Length; ++j)
                {
                    string indexFile = files[j].Replace("SplitText", "Indexed").Replace(".txt", ".xml");
                    Enums.MediaType type = GetMediaTypeOfFile(indexFile);
                    items.Add(MakeItem(files[j], type));
                    articleCount++;
                }
            }
            int pre = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                myDetector.AddItem(items[i]);
                if ((i + 1) * 100 / items.Count - pre >= 10)
                {
                    pre = (i + 1) * 100 / items.Count;
                    Console.WriteLine(pre + "%");
                }
            }
            GC.Collect();
            sw.Stop();
            Console.WriteLine("All {0} files were load. Time cost: {1}s", articleCount, sw.ElapsedMilliseconds / 1000);
            Console.WriteLine("Ready to check!");
            sw.Restart();
            pre = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                if (i * 100 / items.Count - pre >= 10)
                {
                    pre = i * 100 / items.Count;
                    Console.WriteLine(pre + "%");
                }
                myDetector.IsItemCopied(items[i]);
                items[i] = null;
            }
            sw.Stop();
            Console.WriteLine("Step1 finished. Time cost: {0}s", sw.ElapsedMilliseconds / 1000);
            GC.Collect();
            items = null;
            //myDetector.RemoveOldItem(new DateTime(2012, 4, 8));
        }

        private static void GetDup(int start, int end) 
        {
            string[] days = Directory.GetDirectories(PATH);
            total = 0;
            for (int i = start; i <= end; ++i)
            {
                Console.WriteLine(i);
                string[] files = Directory.GetFiles(days[i] + @"\Indexed");
                for (int j = 0; j < files.Length; ++j)
                {
                    string dupID, mediaType;
                    GetDupID_Type(files[j], out mediaType, out dupID);
                    if (dupID != null) 
                    {
                        for (int k = 0; k <= i; ++k) 
                        {
                            string filename = days[k] + @"\SplitText\" + dupID + ".txt";
                            if (File.Exists(filename))
                            {
                                string textname = files[j].Replace("Indexed", "SplitText").Replace(".xml", ".txt");
                                File.Copy(filename, @"dup\" + ++total + ".txt");
                                File.Copy(textname, @"dup\" + ++total + ".txt");
                                string[] ans = new string[1];
                                ans[0] = mediaType;
                                File.AppendAllLines("type.txt", ans); ;
                                if (total % 200 == 0) Console.WriteLine(total);
                                break;
                            }
                        }
                    }
                }
            }
            Console.WriteLine(total / 2);
        }

        private static void GetDupID_Type(string filename, out string mediatype, out string dupID)
        {
            dupID = mediatype = null;
            if (!File.Exists(filename)) return;
            string[] content = File.ReadAllLines(filename);
            for (int k = 0; k < content.Length; ++k)
                if (content[k].Contains("DuplicationID"))
                {
                    dupID = content[k].Substring(content[k].IndexOf('>') + 1, content[k].IndexOf('/') - content[k].IndexOf('>') - 2);
                } else if (content[k].Contains("MediaType"))
                    mediatype = content[k].Substring(content[k].IndexOf('>') + 1, content[k].IndexOf('/') - content[k].IndexOf('>') - 2);
        }

        private static int total = 0;

        private static void TestAcc() 
        {
//            GetDup(0, 30);
            string[] types = File.ReadAllLines("type.txt");
            int result = 0;
            for (int k = 1; k <= 1000; k += 2) 
            {
                Enums.MediaType type = GetMediaType(types[k / 2]);
                DetectorFacade detector = new DetectorFacade(new Parameters(), null);
                #region 建立一个item
                StringBuilder st = new StringBuilder();
                string[] article = File.ReadAllLines(@"dup\" + k + @".txt");
                //string[] article = File.ReadAllLines(@"D:\Workspace\TempWorkspace\tmp\sample\" + k + @".txt");
                ItemToDuplication s = new ItemToDuplication(new Item());
                s.ItemID = "source"; s.MediaType = Enums.MediaType.Forum; s.SpliteTitle = article[0]; s.PubDate = new DateTime(2011, 10, 20);
                st.Clear();
                for (int i = 1; i < article.Length; ++i)
                    st.Append(article[i]);
                s.SpliteText = st.ToString();
                #endregion

                //将刚才建立的item添加到myDetector中
                detector.AddItem(s);

                #region 建立一个测试item
                //article = File.ReadAllLines(@"D:\Workspace\TempWorkspace\tmp\sample\" + (k+1) + @".txt");
                article = File.ReadAllLines(@"dup\" + (k + 1) + @".txt");
                s = new ItemToDuplication(new Item());
                s.ItemID = "test"; s.MediaType = Enums.MediaType.Forum; s.SpliteTitle = article[0];
                st.Clear();
                for (int i = 1; i < article.Length; ++i)
                    st.Append(article[i]);
                s.SpliteText = st.ToString();
                #endregion

                if (detector.IsItemCopied(s) != null)
                    result++;
                else Console.WriteLine(k);
            }
            Console.WriteLine(result);
        }

        private static void TestDualTest(int start, int end, int num) 
        {
            LoadAll(start, end, num);
            for (int i = 0; i < num; ++i) 
            {
                Thread t = new Thread(Test);
                t.Start();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                FingerPrintUpperTest.StartTest();
                //SimpleTest();
                //TestCount(0, 5);
                //TestSpeed(0, 12);
                //  TestAcc();
                // GetDup(0, 30);
                // TestDualTest(0, 12, 5);
            }
            catch (Exception ex) 
            {
                Logger.Error(ex.Message + '\n' + ex.StackTrace);
            }
            
            Console.ReadLine();
        }
    }
}
