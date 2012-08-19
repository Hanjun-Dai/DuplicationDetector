using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    /// <summary>
    /// 指纹持有者，负责指纹注册以及指纹匹配查询
    /// </summary>
    class FingerPrintHolder
    {
        public SortedDictionary<int, LinkedList<int>> contextFingerPrints;
        public SortedDictionary<int, LinkedList<int>> titleFingerPrints;
        private LinkedList<string> itemInfo;

        public Tuple<int, int> GetFingerPrintCnt() 
        {
            return new Tuple<int, int>(titleFingerPrints.Count, contextFingerPrints.Count);
        }

        private void RegisterFingerPrints(int curIndex, int[] printList, SortedDictionary<int, LinkedList<int>> printHolder)
        {
            
            for (int i = 0; i < printList.Length; ++i)
                if (printHolder.ContainsKey(printList[i]))
                    printHolder[printList[i]].AddLast(curIndex);
                else
                {
                    LinkedList<int> list = new LinkedList<int>();
                    list.AddLast(curIndex);
                    printHolder.Add(printList[i], list);
                } 
        }

        private void CountMatch(int[] printList, SortedDictionary<int, LinkedList<int>> printHolder, int[] matchCount) 
        {
            for (int i = 0; i < printList.Length; ++i)
            {
                if (!printHolder.ContainsKey(printList[i])) continue;
                foreach (var j in printHolder[printList[i]])
                    matchCount[j]++;
            }
        }

        private void ClearMatchCount(int[] titleMatch, int[] contextMatch) 
        {
            for (int i = 0; i < titleMatch.Length; ++i)
                titleMatch[i] = contextMatch[i] = 0;
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

        public FingerPrintHolder() 
        {
            contextFingerPrints = new SortedDictionary<int, LinkedList<int>>();
            titleFingerPrints = new SortedDictionary<int, LinkedList<int>>();
            itemInfo = new LinkedList<string>();
        }

        /// <summary>
        /// 注册该文章的指纹
        /// </summary>
        /// <param name="printList"></param>
        public void RegisterArticleFingerPrint(int[] title, int[] context, string itemID, DateTime? pubDate) 
        {
            if (title != null) RegisterFingerPrints(itemInfo.Count, title, titleFingerPrints);
            if (context != null) RegisterFingerPrints(itemInfo.Count, context, contextFingerPrints);
            itemInfo.AddLast(itemID);
        }

        /// <summary>
        /// 获取与当前文章最相似的文章的匹配比率
        /// </summary>
        /// <returns></returns>
        public bool IsArticleCopied(int[] title, int[] context, double TITLE_WEIGHT, double THRESHOLD, out string DupItemID) 
        {
            int[] titleMatch = new int[itemInfo.Count], contextMatch = new int[itemInfo.Count];
            ClearMatchCount(titleMatch, contextMatch);
            if (title != null) CountMatch(title, titleFingerPrints, titleMatch);
            if (context != null) CountMatch(context, contextFingerPrints, contextMatch); 
            double weight = GetWeight(title, context, TITLE_WEIGHT), score1, score2;
            int j = 0;
            foreach (var id in itemInfo)
            {
                if (title != null) score1 = (double)titleMatch[j] / (double)title.Length;
                else score1 = 0;
                if (context != null) score2 = (double)(contextMatch[j]) / (double)(context.Length);
                else score2 = 0;
                if (score1 * weight + score2 * (1 - weight) >= THRESHOLD)
                {
                    DupItemID = id;
                    return true;
                }
                j++;
            }

            DupItemID = null; 
            return false;
        }

        public List<string> GetSimilarArticles(int[] title, int[] context, double TITLE_WEIGHT, double THRESHOLD) 
        {
            int[] titleMatch = new int[itemInfo.Count], contextMatch = new int[itemInfo.Count];
            ClearMatchCount(titleMatch, contextMatch);
            if (title != null) CountMatch(title, titleFingerPrints, titleMatch);
            if (context != null) CountMatch(context, contextFingerPrints, contextMatch);
            double weight = GetWeight(title, context, TITLE_WEIGHT);

            List<string> result = new List<string>();
            int j = 0;
            foreach (var id in itemInfo)
            {
                double score1 = 0;
                if (title != null) score1 = (double)titleMatch[j] / (double)title.Length;
                double score2 = 0;
                if (context != null) score2 = (double)contextMatch[j] / (double)context.Length;
                if (score1 * weight + score2 * (1 - weight) >= THRESHOLD) result.Add(id);
                j++;
            }
            return result;
        }
    }
}
