using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    /// <summary>
    /// 负责构建文章的指纹
    /// </summary>
    class FingerPrintBuilder
    {
        /// <summary>
        /// 将分词后的句子进行粗粒度哈希
        /// </summary>
        /// <param name="codes">分词后的单词ID列表</param>
        /// <param name="k">哈希粒度</param>
        /// <returns></returns>
        private static int[] GetKarpRabinHashCode(List<int> codes, int k) 
        {
            int[] result = new int[codes.Count - k + 1];
            result[0] = 0;
            int factor = 1;
            for (int i = 0; i < k; ++i)
            {
                if (i != 0) factor *= Parameters.MAGICNUM;
                result[0] = result[0] * Parameters.MAGICNUM + codes[i];
            }
            for (int i = k; i < codes.Count; ++i)
                result[i - k + 1] = (result[i - k] - factor * codes[i - k]) * Parameters.MAGICNUM + codes[i];
            return result;
        }

        /// <summary>
        /// 将粗粒度哈希后的结果进行筛选提取
        /// </summary>
        /// <param name="hashCodes">粗粒度哈希结果</param>
        /// <returns></returns>
        private static int[] GetSimplifiedFingerPrint(int[] hashCodes, Parameters parameters, bool isTitle) 
        {
            int windowSize = parameters.G - parameters.K + 1;
            int size = hashCodes.Length - windowSize + 1;
            if (size <= 0) size = 1;
            int[] result = new int[size];
            Tuple<int, int>[] q = new Tuple<int, int>[windowSize + hashCodes.Length + 10];
            int head = 0, tail = 0;
            q[0] = new Tuple<int, int>(hashCodes[0], 0);
            for (int i = 1; i < windowSize; ++i)
            {
                if (i >= hashCodes.Length) break;
                while (tail >= head && hashCodes[i] <= q[tail].Item1)
                    tail--;
                q[++tail] = new Tuple<int, int>(hashCodes[i], i);
            }
            if (!isTitle)
                result[0] = q[0].Item1;//* 999983 + q[0].Item2;
            else result[0] = q[0].Item1;
            for (int i = windowSize; i < hashCodes.Length; ++i) 
            {
                while (tail >= head && hashCodes[i] <= q[tail].Item1)
                    tail--;
                q[++tail] = new Tuple<int, int>(hashCodes[i], i);
                while (head <= tail && q[head].Item2 + windowSize <= i)
                head++;
                if (!isTitle)
                    result[i - windowSize + 1] = q[head].Item1;// * 999983 + q[head].Item2;
                else result[i - windowSize + 1] = q[head].Item1;
            }
            return result;
        }

        /// <summary>
        /// 以句子为最小粒度获得文章指纹
        /// </summary>
        /// <param name="article">待提取指纹的文章</param>
        /// <returns></returns>
        internal static int[] GetSentenceFingerPrint(string content, Parameters parameters, bool isTitle) 
        {
            List<List<int>> codes = WordList.GetWordIDs(content);
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < codes.Count; ++i) 
            {
                if (codes[i].Count < parameters.K) continue;
                int[] hashCodes = GetKarpRabinHashCode(codes[i], codes[i].Count);
                int curFingerPrint = hashCodes[0];
                if (!hashSet.Contains(curFingerPrint)) hashSet.Add(curFingerPrint);
            }
            if (hashSet.Count > 0)
                return hashSet.ToArray();
            else return null;
        }

        /// <summary>
        /// 用k-words算法获取文章的指纹
        /// </summary>
        /// <param name="article">待提取指纹的文章</param>
        /// <returns></returns>
        internal static int[] GetK_WordsFingerPrint(string content, Parameters parameters, bool isTitle) 
        {
            List<List<int>> codes = WordList.GetWordIDs(content);
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].Count < parameters.K) continue;
                int[] hashCodes = GetKarpRabinHashCode(codes[i], parameters.K);
                int[] curFingerPrint = GetSimplifiedFingerPrint(hashCodes, parameters, isTitle);
                for (int j = 0; j < curFingerPrint.Length; ++j)
                    if (!hashSet.Contains(curFingerPrint[j])) hashSet.Add(curFingerPrint[j]);
            }
            if (hashSet.Count > 0)
                return hashSet.ToArray();
            else return null;
        }
    }
}
