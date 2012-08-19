using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace ArticleDuplicateDetector.DataStructure
{
    class MongoDBManager
    {
        protected static string _connectStr = null;
        protected static readonly object connectStringLock = new object();
        ///<summary>
        /// 获取Mongo连接字符串
        ///</summary>
        protected static string ConnectString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectStr))
                {
                    lock (connectStringLock)
                    {
                        if (string.IsNullOrEmpty(_connectStr))
                            _connectStr = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
                    }
                }
                return _connectStr;
            }
        }

        ///<summary>
        /// 数据库名称
        ///</summary>
        protected const string DatabaseName = "Palas";

        protected static MongoDatabase _db = null;
        protected static readonly object DBLock = new object();
        ///<summary>
        /// 获取当前数据库
        ///</summary>
        protected static MongoDatabase DB
        {
            get
            {
                if (_db == null)
                {
                    lock (DBLock)
                    {
                        if (_db == null)
                            _db = Server.GetDatabase(DatabaseName);
                    }
                }
                return _db;
            }
        }

        protected static MongoServer _server = null;
        protected static readonly object serverLock = new object();
        ///<summary>
        /// 获取Mongo服务器
        ///</summary>
        protected static MongoServer Server
        {
            get
            {
                if (_server == null)
                {
                    lock (serverLock)
                    {
                        if (_server == null)
                            _server = MongoServer.Create(ConnectString);
                    }
                }
                return _server;
            }
        }

        private static ConcurrentDictionary<string, MongoCollection> _mongoCollections = new ConcurrentDictionary<string, MongoCollection>();

        public static MongoCollection GetCollections(string collectionName) 
        {
            return _mongoCollections.GetOrAdd(collectionName, DB.GetCollection<FingerPrintList>);
        }
    }
}
