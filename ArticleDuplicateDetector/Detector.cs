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

        public Detector(Parameters _contextParameters, string collectionName) 
        {
            sentenceHolder = new FingerPrintHolder(collectionName, 0);
            kwordsHolder = new FingerPrintHolder(collectionName, 2);
            contextParameters = _contextParameters;
        }

        public void GetFingerPrints(ItemToDuplication item, out int[] sentenceTitle, out int[] sentenceContext, out int[] kwordsTitle, out int[] kwordsContext) 
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

        public void RegisterArticle(ItemToDuplication item, int[] sentenceTitle, int[] sentenceContext, int[] kwordsTitle, int[] kwordsContext) 
        {
            if (sentenceTitle == null && sentenceContext == null && kwordsTitle == null && kwordsContext == null) return;
 
            string id = item.ItemID;
            if (item.DuplicationID != null) id = item.DuplicationID;
            sentenceHolder.RegisterArticleFingerPrint(sentenceTitle, sentenceContext, id, item.PubDate);
            kwordsHolder.RegisterArticleFingerPrint(kwordsTitle, kwordsContext, id, item.PubDate);
        }

        public bool IsItemCopied(int[] sentenceTitle, int[] sentenceContext, int[] kwordsTitle, int[] kwordsContext, double TITLE_WEIGHT, double THRESHOLD, out string DupItemID) 
        {
            DupItemID = null;

            bool match1 = false, match2 = false;
            match1 = sentenceHolder.IsArticleCopied(sentenceTitle, sentenceContext, TITLE_WEIGHT, THRESHOLD, out DupItemID);

            if (!match1)
                match2 = kwordsHolder.IsArticleCopied(kwordsTitle, kwordsContext, TITLE_WEIGHT, THRESHOLD, out DupItemID);
            return (match1 || match2);
        }

        private static string[] MergeArticleNames(string[] listA, string[] listB)
        {
            HashSet<string> hash = new HashSet<string>();
            if (listA != null)
                for (int i = 0; i < listA.Length; ++i)
                    if (!hash.Contains(listA[i])) hash.Add(listA[i]);
            if (listB != null)
                for (int i = 0; i < listB.Length; ++i)
                    if (!hash.Contains(listB[i])) hash.Add(listB[i]);
            if (hash.Count == 0) return null;
            return hash.ToArray();
        }

        public string[] GetSimilarItems(ItemToDuplication item, double TITLE_WEIGHT, double THRESHOLD) 
        {
            if (string.IsNullOrEmpty(item.SpliteText) && string.IsNullOrEmpty(item.SpliteTitle)) return null;

            int[] sentenceTitle, sentenceContext, kwordsTitle, kwordsContext;
            GetFingerPrints(item, out sentenceTitle, out sentenceContext, out kwordsTitle, out kwordsContext);

            string[] listA = sentenceHolder.GetSimilarArticles(sentenceTitle, sentenceContext, TITLE_WEIGHT, THRESHOLD);
            string[] listB = kwordsHolder.GetSimilarArticles(kwordsTitle, kwordsContext, TITLE_WEIGHT, THRESHOLD);
            return MergeArticleNames(listA, listB);
        }
    }

    /// <summary>
    /// Detector的外观模式
    /// </summary>
    public class DetectorFacade 
    {
        public static TimeSpan DetectPeriod = new TimeSpan(7, 0, 0, 0);
        private ConcurrentDictionary<Enums.MediaType, Detector> detectorOFMedia;
        private Parameters defaultContextParameters;
        private ConcurrentDictionary<Enums.MediaType, ReaderWriterLockSlim> detectorLock;

        private void RegisterMediaType(Enums.MediaType mediaType, Parameters contextParameters)
        {
            if (mediaType != Enums.MediaType.Weibo && mediaType != Enums.MediaType.Forum)
                mediaType = Enums.MediaType.Unknown;
            if (detectorOFMedia.ContainsKey(mediaType)) return;
            string collectionName;
            switch (mediaType) 
            {
                case Enums.MediaType.Forum: collectionName = "FingerPrint_Forum"; break;
                case Enums.MediaType.Weibo: collectionName = "FingerPrint_Weibo"; break;
                default: collectionName = "FingerPrint_Other"; break;
            }
            Detector detector = new Detector(contextParameters, collectionName);
            detectorOFMedia.TryAdd(mediaType, detector);
        }

        private Detector GetCurDetector(Enums.MediaType mediaType) 
        {
            if (mediaType != Enums.MediaType.Weibo && mediaType != Enums.MediaType.Forum)
                mediaType = Enums.MediaType.Unknown;
            if (!detectorOFMedia.ContainsKey(mediaType))
                RegisterMediaType(mediaType, defaultContextParameters);
            return detectorOFMedia[mediaType];
        }

        /// <summary>
        /// 构造函数：除Unkonw类型外都使用提供的默认参数
        /// </summary>
        /// <param name="defaultParameters"></param>
        /// <param name="Items"></param>
        public DetectorFacade(Parameters defaultContextParameters)
        {
            detectorLock = new ConcurrentDictionary<Enums.MediaType, ReaderWriterLockSlim>();
            this.defaultContextParameters = defaultContextParameters;
            detectorOFMedia = new ConcurrentDictionary<Enums.MediaType, Detector>();
            DetectPeriod = new TimeSpan(Duplication.nBackwardDays, 0, 0, 0);
        }

        public string TestAndTryAdd(ItemToDuplication item, double TITLE_WEIGHT = -1, double THRESHOLD = -1)
        {
            if (string.IsNullOrEmpty(item.SpliteText) && string.IsNullOrEmpty(item.SpliteTitle))
                return null;
            string DupItemID = null;
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                if (TITLE_WEIGHT < 0)
                    TITLE_WEIGHT = curDetector.contextParameters.TITLE_WEIGHT;
                if (THRESHOLD < 0)
                    THRESHOLD = curDetector.contextParameters.THRESHOLD;

                int[] sentenceTitle, sentenceContext, kwordsTitle, kwordsContext;
                curDetector.GetFingerPrints(item, out sentenceTitle, out sentenceContext, out kwordsTitle, out kwordsContext);

                if (!curDetector.IsItemCopied(sentenceTitle, sentenceContext, kwordsTitle, kwordsContext, TITLE_WEIGHT, THRESHOLD, out DupItemID)) 
                {
                    if ((DateTime.Now - item.PubDate) < DetectPeriod)
                    {
                        targetLock.ExitReadLock();
                        EnterWriteLock(targetLock);
                        curDetector.RegisterArticle(item, sentenceTitle, sentenceContext, kwordsTitle, kwordsContext);
                        ExitWriteLock(targetLock);
                        targetLock.EnterReadLock();
                    }
                }
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
        /// <returns></returns>
        public string[] GetSimilarItemIDs(ItemToDuplication item, double TITLE_WEIGHT = -1, double THRESHOLD = -1)
        {
            ReaderWriterLockSlim targetLock = GetLock(item.MediaType);
            string[] result = null;
            try
            {
                targetLock.EnterReadLock();
                Detector curDetector = GetCurDetector(item.MediaType);
                if (TITLE_WEIGHT < 0)
                    TITLE_WEIGHT = curDetector.contextParameters.TITLE_WEIGHT;
                if (THRESHOLD < 0)
                    THRESHOLD = curDetector.contextParameters.THRESHOLD;
                result = curDetector.GetSimilarItems(item, TITLE_WEIGHT, THRESHOLD);
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

        private void EnterWriteLock(ReaderWriterLockSlim detectorLock) 
        {
            detectorLock.EnterUpgradeableReadLock();
            detectorLock.EnterWriteLock();
        }

        private void ExitWriteLock(ReaderWriterLockSlim detectorLock) 
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
