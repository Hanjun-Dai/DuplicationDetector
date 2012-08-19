using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    class WordList
    {
        private const UInt32 MASK = 4026531840;
        private static string QUESTION_MARK = @"/ww";
        private static string FULL_STOP = @"/wj";
        private static string EXCLAMATORY_MARK = @"/wt";
        private static string LEFT_BRACKET = @"/wkz";
        private static string RIGHT_BRACKET = @"/wky";
        private static string LEFT_QUOTATION_MARK = @"/wyz";
        private static string RIGHT_QUOTATION_MARK = @"/wyy";
        private static string[] B_LEFT = {"（/w", "〔/w", "［/w", "｛/w", "《/w", "【/w", "〖/w", "〈/w", "(/w", "[/w", "{/w", "</w"};
        private static string[] B_RIGHT = {"）/w", "〕/w", "］/w", "｝/w", "》/w", "】/w", "〗/w", "〉/w", ")/w", "]/w", "}/w", ">/w"};
        private static string[] Q_LEFT = {"“/w", "‘/w", "『/w"};
        private static string[] Q_RIGHT = {"”/w", "’/w", "』/w"};
        private static string[] STOP = { "。/w", "？/w", "?/w", "！/w", "!/w", "】/w", "〗/w", "]/w"};

        private static int ELFHash(string word)
        {
            UInt32 hash = 0, x = 0;
            for (int i = 0; i < word.Length; ++i)
            {
                hash = (hash << 4) + word[i];
                if ((x = hash & MASK) != 0)
                {
                    hash ^= (x >> 24);
                    hash &= ~x;
                }
            }
            return (int)hash;
        }

        private static int GetNextLocation(string content, int curIndex)
        {
            int nextIndex = curIndex;
            while (nextIndex < content.Length && content[nextIndex] != ' ')
                nextIndex++;
            return nextIndex;
        }

        private static bool IsEndOfSentence(string word)
        {
            for (int i = 0; i < STOP.Length; ++i)
                if (word.EndsWith(STOP[i])) return true;
            return (word.EndsWith(QUESTION_MARK) || word.EndsWith(FULL_STOP) || word.EndsWith(EXCLAMATORY_MARK));
        }

        private static bool IsPunctuation(string word) 
        {
            if (word.EndsWith("/w")) return true;
            for (int i = word.Length - 1; i >= 0; --i)
                if (word[i] == '/') 
                {
                    if (i < word.Length - 1 && word[i + 1] == 'w') return true;
                    return false;
                }
            return false;
        }

        /// <summary>
        /// 把分好词的string转换成二维int型list
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static List<List<int>> GetWordIDs(string content)
        {
            List<List<int>> result = new List<List<int>>();
            List<int> curSentence = new List<int>();
            int curIndex = 0;
            if (!string.IsNullOrEmpty(content))
            {
                while (curIndex < content.Length)
                {
                    int nextIndex = GetNextLocation(content, curIndex);
                    string curWord = content.Substring(curIndex, nextIndex - curIndex);
                    curIndex = nextIndex + 1;
                    if (string.IsNullOrEmpty(curWord)) continue;
                    //if (!IsPunctuation(curWord)) 
                    bool isEnd = IsEndOfSentence(curWord);
                    if (!isEnd)
                        curSentence.Add(ELFHash(curWord));
                    if (nextIndex == content.Length || isEnd == true)
                    {
                        if (curSentence.Count == 0) continue;
                        result.Add(curSentence);
                        curSentence = new List<int>();
                    }
                }
                if (curSentence.Count > 0) result.Add(curSentence);
            }
            return result;
        }
    }
}
