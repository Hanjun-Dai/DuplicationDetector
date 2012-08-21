using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ArticleDuplicateDetector.DataStructure
{
    class IDAndPubInfo
    {
        public IDAndPubInfo(string _itemID, DateTime? _pubDate) 
        {
            ItemID = _itemID;
            PubDate = _pubDate;
        }

        public string ItemID { get; set; }

        public DateTime? PubDate { get; set; }
    }

    [Serializable]
    [BsonIgnoreExtraElements]
    class FingerPrintList
    {
        public int FingerPrint { get; set; }

        public sbyte CollectionTag { get; set; }

        public IDAndPubInfo[] Items { get; set; }
    }

    class MongoDictionary
    {
        private string collectionName;
        private sbyte collectionTag;

        public MongoDictionary(string collectionName, sbyte collectionTag) 
        {
            this.collectionName = collectionName; this.collectionTag = collectionTag;
        }

        public IDAndPubInfo[] GetItemList(int fingerPrint) 
        {
            var collection = MongoDBManager.GetCollections(collectionName);
            IMongoQuery query = Query.And(Query.EQ("FingerPrint", fingerPrint), Query.EQ("CollectionTag", collectionTag));
            var result = collection.FindOneAs<FingerPrintList>(query);
            if (result == null)
                return null;
            return result.Items;
        }

        public void PutbackItemList(int fingerPrint, IDAndPubInfo[] items) 
        {
            var collection = MongoDBManager.GetCollections(collectionName);
            IMongoQuery query = Query.And(Query.EQ("FingerPrint", fingerPrint), Query.EQ("CollectionTag", collectionTag));
            var result = collection.FindOneAs<FingerPrintList>(query);
            if (result != null)
                collection.Remove(query, SafeMode.False);
            result = new FingerPrintList();
            result.FingerPrint = fingerPrint;
            result.CollectionTag = collectionTag;
            result.Items = items;
            collection.Insert(result, SafeMode.False);
        }
    }
}
