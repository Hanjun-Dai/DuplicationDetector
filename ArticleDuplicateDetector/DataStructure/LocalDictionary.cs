using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArticleDuplicateDetector.DataStructure
{
    class LocalDictionary
    {
        private string collectionName;
        private sbyte collectionTag;
        private SortedDictionary<int, IDAndPubInfo[]> dict;

        public LocalDictionary(string collectionName, sbyte collectionTag) 
        {
            this.collectionName = collectionName; this.collectionTag = collectionTag;
            dict = new SortedDictionary<int, IDAndPubInfo[]>();
        }

        public IDAndPubInfo[] GetItemList(int fingerPrint) 
        {
            if (!dict.ContainsKey(fingerPrint))
                return null;
            return dict[fingerPrint];
        }

        public void PutbackItemList(int fingerPrint, IDAndPubInfo[] items) 
        {
            if (!dict.ContainsKey(fingerPrint))
                dict.Add(fingerPrint, items);
            else
                dict[fingerPrint] = items;
        }

        public void WriteToMongo() 
        {
            var collection = MongoDBManager.GetCollections(collectionName);
            foreach (var data in dict) 
            {
                FingerPrintList result = new FingerPrintList();
                result.FingerPrint = data.Key;
                result.CollectionTag = collectionTag;
                result.Items = data.Value;
                collection.Insert(result);
            }
        }
    }
}
