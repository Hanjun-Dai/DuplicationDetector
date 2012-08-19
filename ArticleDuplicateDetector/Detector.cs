using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Palas.Common.Data;
using System.Threading;
using System.Collections.Concurrent;
using HooLab.Log;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    /// <summary>
    /// 提供注册文章和抄袭检测的接口
    /// </summary>
    class Detector
    {
        public FingerPrintHolder sentenceHolder, kwordsHolder;
        public Parameters contextParameters;

        public Detector(Parameters _contextParameters) 
        {
            sentenceHolder = new FingerPrintHolder();
            kwordsHolder = new FingerPrintHolder();
            contextParameters = _contextParameters;
        }

        private void GetFingerPrints(ItemToDuplication item, out int[] sentenceTitle, out int[] sentenceContext, out int[] kwordsTitle, out int[] kwordsContext) 
        {
            sentenceTitle = sentenceContext = kwordsTitle = kwordsContext = null;
            Parameters titleParameters = new Parameters(1, 1, contextParameters.TITLE_WEIGHT, contextParameters.THRESHOLD);
            if (string.IsNullOrEmpty(item.SpliteText) && item.SpliteTitle.Length > Parameters.MAX_TITLE_LENGTH)
                titleParameters = contextParameters;
            if (!string.IsNullOrEmpty(item.SpliteTitle))
            {
                sentenceTitle = FingerPrintBuilder.GetSentenceFingerPrint(item.SpliteTitle, titleParameters, true);
                kwordsTitle = FingerPrintBuilder.GetK_WordsFingerPrint(item.SpliteTitle, titleParameters, true);
            }
            if (!string.IsNullOrEmpty(item.SpliteText)) 
            {
                sentenceContext = FingerPrintBuilder.GetSentenceFingerPrint(item.SpliteText, contextParameters, false);
                kwordsContext = FingerPrintBuilder.GetK_WordsFingerPrint(item.SpliteText, contextParameters, false);
            }
        }

        private static string[] MergeArticleNames(List<string> listA, List<string> listB) 
        {
            HashSet<string> hash = new HashSet<string>();
            if (listA != null)
                for (int i = 0; i < listA.Count; ++i)
                    if (!hash.Contains(listA[i])) hash.Add(listA[i]);
            if (listB != null)
                for (int i = 0; i < listB.Count; ++i)
                    if (!hash.Contains(listB[i])) hash.Add(listB[i]);
            if (hash.Count == 0) return null;
            return hash.ToArray();
        }

        public void RegisterArticle(ItemToDuplication item) 
        {
            if (string.IsNullOrEmpty(item.SpliteText) && string.IsNullOrEmpty(item.SpliteTitle)) return;

            int[] sentenceTitle, sentenceContext, kwordsTitle, kwordsContext;
            GetFingerPrints(item, out sentenceTitle, out sentenceContext, out kwordsTitle, out kwordsContext);
            if (sentenceTitle == null && sentenceContext == null && kwordsTitle == null && kwordsContext == null) return;
 
            string id = item.ItemID;
            if (item.DuplicationID != null) id = item.DuplicationID;
            sentenceHolder.RegisterArticleFingerPrint(sentenceTitle, sentenceContext, id, item.PubDate);
            kwordsHolder.RegisterArticleFingerPrint(kwordsTitle, kwordsContext, id, item.PubDate);
        }

        public bool IsItemCopied(ItemToDuplication item, double TITLE_WEIGHT, double THRESHOLD, out string DupItemID) 
        {
            DupItemID = null;
            if (string.IsNullOrEmpty(item.SpliteText) && string.IsNullOrEmpty(item.SpliteTitle))
                return false;

            int[] sentenceTitle, sentenceContext, kwordsTitle, kwordsContext;
            GetFingerPrints(item, out sentenceTitle, out sentenceContext, out kwordsTitle, out kwordsContext);

            bool match1 = false, match2 = false;
            match1 = sentenceHolder.IsArticleCopied(sentenceTitle, sentenceContext, TITLE_WEIGHT, THRESHOLD, out DupItemID);

            if (!match1)
                match2 = kwordsHolder.IsArticleCopied(kwordsTitle, kwordsContext, TITLE_WEIGHT, THRESHOLD, out DupItemID);
            return (match1 || match2);
        }

        public string[] GetSimilarItems(ItemToDuplication item, double TITLE_WEIGHT, double THRESHOLD) 
        {
            if (string.IsNullOrEmpty(item.SpliteText) && string.IsNullOrEmpty(item.SpliteTitle)) return null;

            int[] sentenceTitle, sentenceContext, kwordsTitle, kwordsContext;
            GetFingerPrints(item, out sentenceTitle, out sentenceContext, out kwordsTitle, out kwordsContext);

            List<string> listA = sentenceHolder.GetSimilarArticles(sentenceTitle, sentenceContext, TITLE_WEIGHT, THRESHOLD);
            List<string> listB = kwordsHolder.GetSimilarArticles(kwordsTitle, kwordsContext, TITLE_WEIGHT, THRESHOLD);
            return MergeArticleNames(listA, listB);
        }
    }

    /// <summary>
    /// Detector的外观模式
    /// </summary>
    public class DetectorFacade 
    {
        private ConcurrentDictionary<Enums.MediaType, Detector> detectorOFMedia;
        private Parameters defaultContextParameters;
        private ConcurrentDictionary<Enums.MediaType, ReaderWriterLockSlim> detectorLock;
        private ReaderWriterLockSlim lockRemove;

        public Tuple<int, int> GetFCount() 
        {
            int c1 = 0, c2 = 0;
            Tuple<int, int> tmp;
            foreach (var detector in detectorOFMedia.Values)
            {
                tmp = detector.sentenceHolder.GetFingerPrintCnt();
                c1 += tmp.Item1; c2 += tmp.Item2;
                tmp = detector.kwordsHolder.GetFingerPrintCnt();
                c1 += tmp.Item1; c2 += tmp.Item1;
            }
            return new Tuple<int, int>(c1, c2);
        }

        int itemCount;
        public int GetItemCount() 
        {
            return itemCount;
        }

        private void RegisterMediaType(Enums.MediaType mediaType, Parameters contextParameters)
        {
            if (detectorOFMedia.ContainsKey(mediaType)) return;
            Detector detector = new Detector(contextParameters);
            detectorOFMedia.TryAdd(mediaType, detector);
        }

        private Detector GetCurDetector(Enums.MediaType mediaType) 
        {
            if (!detectorOFMedia.ContainsKey(mediaType))
                RegisterMediaType(mediaType, defaultContextParameters);
            return detectorOFMedia[mediaType];
        }

        /// <summary>
        /// 增加文章到匹配库
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(ItemToDuplication item)
        {
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            try
            {
                EnterWriterLock(targetLock);
                Detector curDetector = GetCurDetector(item.MediaType);
                curDetector.RegisterArticle(item);
                itemCount++;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("DetectorFacade AddItem Exp:{0}\n{1}", e.Message, e.StackTrace));
            }
            finally
            {
                ExitWriterLock(targetLock);
            }
        }

        /// <summary>
        /// 构造函数：除Unkonw类型外都使用提供的默认参数
        /// </summary>
        /// <param name="defaultParameters"></param>
        /// <param name="Items"></param>
        public DetectorFacade(Parameters defaultContextParameters, ItemToDuplication[] Items)
        {
            detectorLock = new ConcurrentDictionary<Enums.MediaType, ReaderWriterLockSlim>();
            lockRemove = new ReaderWriterLockSlim();
            this.defaultContextParameters = defaultContextParameters;
            detectorOFMedia = new ConcurrentDictionary<Enums.MediaType, Detector>();
            if (Items != null)
            {
                for (int i = 0; i < Items.Length; ++i)
                    AddItem(Items[i]);
            }
            itemCount = 0;
        }

        /// <summary>
        /// 判断是否相似
        /// </summary>
        /// <param name="item"></param>
        /// <param name="TITLE_WEIGHT">标题所占权重，介于0到1之间</param>
        /// <param name="THRESHOLD">匹配阈值，介于0到1之间，越大越接近,大于该值即视为匹配。</param>
        /// <returns>无匹配null，有匹配则是第一个ID</returns>
        public string IsItemCopied(ItemToDuplication item, double TITLE_WEIGHT = 0.7, double THRESHOLD = 0.6) 
        {
            string DupItemID = null;
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                curDetector.IsItemCopied(item, TITLE_WEIGHT, THRESHOLD, out DupItemID);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("DetectorFacade IsItemCopied Exp:{0}\n{2}", e.Message, e.StackTrace));
            }
            finally
            {
                targetLock.ExitReadLock();
            }
            return DupItemID;
        }

        /// <summary>
        /// 判断是否相似,标题权重和匹配阈值使用初始化时给出的默认值
        /// </summary>
        /// <param name="item"></param>
        /// <returns>无匹配null，有匹配则是第一个ID</returns>
        public string IsItemCopied(ItemToDuplication item)
        {
            string DupItemID = null;
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                curDetector.IsItemCopied(item, curDetector.contextParameters.TITLE_WEIGHT, curDetector.contextParameters.THRESHOLD, out DupItemID);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("DetectorFacade IsItemCopied Exp:{0}\n{1}", e.Message, e.StackTrace));
            }
            finally
            {
                targetLock.ExitReadLock();
            }
            return DupItemID;
        }

        /// <summary>
        /// 获取与某篇文章相似的内存中的所有文章
        /// </summary>
        /// <param name="item"></param>
        /// <param name="TITLE_WEIGHT">标题所占权重，介于0到1之间</param>
        /// <param name="THRESHOLD">匹配阈值，介于0到1之间，越大越接近,大于该值即视为匹配。</param>
        /// <returns></returns>
        public string[] GetSimilarItemIDs(ItemToDuplication item, double TITLE_WEIGHT = 0.7, double THRESHOLD = 0.6)
        {
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            string[] result = null;
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                result = curDetector.GetSimilarItems(item, TITLE_WEIGHT, THRESHOLD);
            }
            catch (Exception e) 
            {
                Logger.Error(string.Format("DetectorFacade GetSimilarItemIDs Exp:{0}\n{2}", e.Message, e.StackTrace));
            }
            targetLock.ExitReadLock();
            return result;
        }

        /// <summary>
        /// 获取与某篇文章相似的内存中的所有文章，标题权重和匹配阈值使用初始化时给出的默认值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string[] GetSimilarItemIDs(ItemToDuplication item)
        {
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            string[] result = null;
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                result = curDetector.GetSimilarItems(item, curDetector.contextParameters.TITLE_WEIGHT, curDetector.contextParameters.THRESHOLD);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("DetectorFacade GetSimilarItemIDs Exp:{0}\n{2}", e.Message, e.StackTrace));
            }
            finally
            {
                targetLock.ExitReadLock();
            }
            return result;
        }

        private ReaderWriterLockSlim GetLock(Enums.MediaType mediaType) 
        {
            ReaderWriterLockSlim targetLock = null;
            if (detectorLock.TryGetValue(mediaType, out targetLock))
                //有锁
                return targetLock;
            else
            {
                targetLock = new ReaderWriterLockSlim();
                if (detectorLock.TryAdd(mediaType, targetLock))
                    //新锁已加入
                    return targetLock;
                else return detectorLock[mediaType];
            }
        }

        private void EnterWriterLock(ReaderWriterLockSlim detectorLock) 
        {
            detectorLock.EnterUpgradeableReadLock();
            detectorLock.EnterWriteLock();
        }

        private void ExitWriterLock(ReaderWriterLockSlim detectorLock) 
        {
            detectorLock.ExitWriteLock();
            detectorLock.EnterReadLock();
            detectorLock.ExitUpgradeableReadLock();
            detectorLock.ExitReadLock();
        }
    }

    public class Parameters
    {
        public const int MAGICNUM = 999983;                                                   //哈希因子，最好是个质数
        public const int MAX_TITLE_LENGTH = 500;                                              //标题最大长度，否则将认为是正文
        public double TITLE_WEIGHT;                                                           //标题所占权重，介于0到1之间
        public int K;                                                                         //匹配粒度，忽略词数少于该值的句子，越大获取到的指纹数量越少
        public int G;                                                                         //匹配窗口大小，不能小于K，越大获取到的指纹数量越少
        public double THRESHOLD;                                                              //匹配阈值，介于0到1之间，大于该值即视为匹配。

        public Parameters() 
        {
            K = 8; G = 16; TITLE_WEIGHT = 0.7; THRESHOLD = 0.6;
        }

        public Parameters(int K, int G, double TITLE_WEIGHT, double THRESHOLD) 
        {
            this.K = K; this.G = G; this.TITLE_WEIGHT = TITLE_WEIGHT; this.THRESHOLD = THRESHOLD;
        }

    }
}
