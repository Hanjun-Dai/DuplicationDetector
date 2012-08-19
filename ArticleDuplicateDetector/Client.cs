using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Palas.Common.Data;
using System.Threading;
using HooLab.Log;
using ArticleDuplicateDetector.Test;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    class Client
    {
        private static DetectorFacade myDetector = new DetectorFacade(new Parameters());
        private static void SimpleTest()
        {
            #region 建立一个item
            StringBuilder st = new StringBuilder();
            string[] article = File.ReadAllLines(@"1.txt");
            ItemToDuplication s = new ItemToDuplication(new Item());
            s.ItemID = "source"; s.MediaType = Enums.MediaType.Weibo; s.SpliteTitle = article[0];
            st.Clear();
            for (int i = 1; i < article.Length; ++i)
                st.Append(article[i]);
            s.SpliteText = st.ToString();
            #endregion

            //将刚才建立的item添加到myDetector中
            myDetector.TestAndTryAdd(s);

            #region 建立一个测试item
            article = File.ReadAllLines(@"2.txt");
            //s = new ItemToDuplication();
            s.ItemID = "test"; s.MediaType = Enums.MediaType.Weibo; s.SpliteTitle = article[0];
            st.Clear();
            for (int i = 1; i < article.Length; ++i)
                st.Append(article[i]);
            s.SpliteText = st.ToString();
            #endregion

            //测试转载检测
            string id = null;
            id = myDetector.TestAndTryAdd(s);
            if (id != null)
                Console.WriteLine("Copied Item: " + id);
            else Console.WriteLine("Not copied.");
        }

        static void Main(string[] args)
        {
            //SimpleTest();
            FingerPrintUpperTest.StartTest();
            Console.WriteLine("all is well");
            Console.ReadLine();
        }
    }
}
