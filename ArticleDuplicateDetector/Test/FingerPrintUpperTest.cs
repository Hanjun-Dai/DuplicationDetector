using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Builders;
using Palas.Common.Data;
using Palas.Common;
using HooLab.Log;
using System.Threading;
using Analyzer.Core.Algorithm.FingerPrint;

namespace ArticleDuplicateDetector.Test
{
    class FingerPrintUpperTest
    {
        static void LoadPeriod_Mongo(DateTime lowTime, DateTime highTime, List<ItemToDuplication> Items)
        {
            //左闭右开
            var low = Query.GTE("FetchTime", lowTime);
            var high = Query.LT("FetchTime", highTime);
            var query = Query.And(low, high);
            var sort = SortBy.Ascending("FetchTime");

            const int sleepMS = 2000;

            try
            {
                var result = MongoItemAccess.Items.Find(query)./*SetSortOrder(sort).*/SetFields("ItemID", "PubDate", "DuplicationID", "SpliteTitle", "SpliteText", "ProsdDuplication");
                Console.WriteLine("items found");
                foreach (var item in result)
                    Items.Add(new ItemToDuplication(item));
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(@"Duplication读取文章失败[{0}-{1}]:{2}", lowTime, highTime, e.Message));
            }
            Thread.Sleep(sleepMS);
        }

        public static void StartTest()
        {
            DateTime curTime = DateTime.Now;
            DetectorFacade detector = new DetectorFacade(new Parameters());
            bool finished = false;
            int total = 0, cnt = 0;
            while (!finished)
            {
                Console.WriteLine(cnt++);
                if (cnt > 7 * 24) break;
                List<ItemToDuplication> Items = new List<ItemToDuplication>();
                DateTime nextTime = curTime.Subtract(new TimeSpan(1, 0, 0));
                Console.WriteLine("Fetch time: from {0} to {1}", nextTime, curTime);
                LoadPeriod_Mongo(nextTime, curTime, Items);
                Console.WriteLine("fetch finished. Now inserting...");
                for (int i = 0; i < Items.Count; ++i)
                {
                    try
                    {
                        detector.TestAndTryAdd(Items[i]);
                        total++;
                        if (total % 1000 == 0)
                        {
                            Console.WriteLine("Have tested {0} items.", total);
                        }
                        Items[i] = null;
                    }
                    catch (Exception e)
                    {
                        finished = true;
                        Logger.Error(string.Format("DetectorFacade AddItem Exp:{0}\n{1}\nTotal {2} Articles", e.Message, e.StackTrace, total));
                    }
                }
                Console.WriteLine("insert finished");
                curTime = nextTime;
                Thread.Sleep(1000);
            }
        }
    }
}
