using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ArticleDuplicateDetector.DataStructure;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    /// <summary>
    /// 指纹持有者，负责指纹注册以及指纹匹配查询
    /// </summary>
    class FingerPrintHolder
    {
        public MongoDictionary contextFingerPrints;
        public MongoDictionary titleFingerPrints;

        private void RegisterFingerPrints(string itemID, DateTime? pubDate, int[] printList, MongoDictionary printHolder) 
        {
            for (int i = 0; i < printList.Length; ++i) 
            {
                IDAndPubInfo[] items = printHolder.GetItemList(printList[i]);

                List<IDAndPubInfo> items_new = new List<IDAndPubInfo>((int)Math.Min(items == null ? 0 : items.Length + 1, 1000));
                if (items != null) 
                {
                    for (int j = 0; j < items.Length; ++j)
                        if ((DateTime.Now - items[j].PubDate) < DetectorFacade.DetectPeriod)
                            items_new.Add(items[j]);
                }
                items_new.Add(new IDAndPubInfo(itemID, pubDate));

                printHolder.PutbackItemList(printList[i], items_new.ToArray());
            }
        }

        private void CountMatch(int[] printList, MongoDictionary printHolder, Dictionary<string, int[]> matchCount, int idx) 
        {
            for (int i = 0; i < printList.Length; ++i) 
            {
                IDAndPubInfo[] items = printHolder.GetItemList(printList[i]);
                if (items == null || items.Length == 0) continue;
                for (int j = 0; j < items.Length; ++j)
                    if ((DateTime.Now - items[j].PubDate) < DetectorFacade.DetectPeriod) 
                    {
                        if (matchCount.ContainsKey(items[j].ItemID))
                            matchCount[items[j].ItemID][idx]++;
                        else 
                        {
                            int[] cnt = new int[2];
                            cnt[0] = cnt[1] = 0; cnt[idx] = 1;
                            matchCount.Add(items[j].ItemID, cnt);
                        }
                    }
            }
        }

        public FingerPrintHolder(string collectionName, sbyte collectionTag) 
        {
            contextFingerPrints = new MongoDictionary(collectionName, collectionTag);
            titleFingerPrints = new MongoDictionary(collectionName, (sbyte)(collectionTag + 1));
        }

        /// <summary>
        /// 注册该文章的指纹
        /// </summary>
        /// <param name="printList"></param>
        public void RegisterArticleFingerPrint(int[] title, int[] context, string itemID, DateTime? pubDate) 
        {
            if (title != null) RegisterFingerPrints(itemID, pubDate, title, titleFingerPrints);
            if (context != null) RegisterFingerPrints(itemID, pubDate, context, contextFingerPrints);
        }

        private Dictionary<string, int[]> GetMatchCnt(int[] title, int[] context) 
        {
            Dictionary<string, int[]> matchCount = new Dictionary<string, int[]>();
            if (title != null) CountMatch(title, titleFingerPrints, matchCount, 0);
            if (context != null) CountMatch(context, contextFingerPrints, matchCount, 1);
            return matchCount;
        }

        private double GetWeight(int[] title, int[] context, double TITLE_WEIGHT)
        {
            double weight = TITLE_WEIGHT;
            if (context == null)
                weight = 1;
            else if (title != null && title.Length > weight * (title.Length + context.Length))
                weight = (double)title.Length / (double)(title.Length + context.Length);
            return weight;
        }

        /// <summary>
        /// 获取与当前文章最相似的文章的匹配比率
        /// </summary>
        /// <returns></returns>
        public bool IsArticleCopied(int[] title, int[] context, double TITLE_WEIGHT, double THRESHOLD, out string DupItemID) 
        {
            var matchCount = GetMatchCnt(title, context);
            double weight = GetWeight(title, context, TITLE_WEIGHT);

            foreach (var itemCnt in matchCount)
            {
                double score1 = 0, score2 = 0;
                if (title != null && title.Length > 0) score1 = (double)itemCnt.Value[0] / (double)title.Length;
                if (context != null && context.Length > 0) score2 = (double)(itemCnt.Value[1]) / (double)(context.Length);
                if (score1 * weight + score2 * (1 - weight) >= THRESHOLD)
                {
                    DupItemID = itemCnt.Key;
                    return true;
                }
            }

            DupItemID = null; 
            return false;
        }

        public string[] GetSimilarArticles(int[] title, int[] context, double TITLE_WEIGHT, double THRESHOLD) 
        {
            var matchCount = GetMatchCnt(title, context);
            double weight = GetWeight(title, context, TITLE_WEIGHT);

            List<string> result = new List<string>();
            foreach (var itemCnt in matchCount)
            {
                double score1 = 0, score2 = 0;
                if (title != null && title.Length > 0) 
                    score1 = (double)itemCnt.Value[0] / (double)title.Length;
                if (context != null && context.Length > 0)
                    score2 = (double)(itemCnt.Value[1]) / (double)(context.Length);
                if (score1 * weight + score2 * (1 - weight) >= THRESHOLD)
                    result.Add(itemCnt.Key);
            }
            return result.ToArray();
        }
    }
}
