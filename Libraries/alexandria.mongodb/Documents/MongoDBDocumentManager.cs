﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB;
using MongoDB.Configuration;
using MongoDB.Connections;
using Alexandria.Documents.Adaptors;
using Alexandria.Documents.GraphRegistry;
using Alexandria.Utilities;

namespace Alexandria.Documents
{
    public class MongoDBDocumentManager : BaseDocumentManager<Document,Document>
    {
        private Mongo _connection;
        private IMongoDatabase _db;
        private IGraphRegistry _registry;
        private String _collection;

        private const String GraphRegistryDocument = "graphs";
        private const String DefaultCollection = "dotnetrdf";

        public MongoDBDocumentManager(MongoConfiguration config, String db, String collection)
            : base(new MongoDBRdfToJsonAdaptor())
        {
            this._connection = new Mongo(config);
            this._db = this._connection.GetDatabase(db);
            this._connection.Connect();
            this._collection = collection;

            //Ensure the DB is setup correctly
            this._db.GetCollection(Collection);

            if (!this.HasDocument(GraphRegistryDocument))
            {
                if (!this.CreateDocument(GraphRegistryDocument))
                {
                    throw new AlexandriaException("Unable to create the Required Graph Registry Document");
                }
            }
            this._registry = new MongoDBGraphRegistry(this.GetDocument(GraphRegistryDocument));
        }

        public MongoDBDocumentManager(MongoConfiguration config, String db)
            : this(config, db, DefaultCollection) { }

        public MongoDBDocumentManager(String db)
            : this(new MongoConfiguration(), db) { }

        public MongoDBDocumentManager(String connectionString, String db)
            : this(MongoDBHelper.GetConfiguration(connectionString), db) { }

        public MongoDBDocumentManager(String connectionString, String db, String collection)
            : this(MongoDBHelper.GetConfiguration(connectionString), db, collection) { }

        internal IMongoDatabase Database
        {
            get
            {
                return this._db;
            }
        }

        internal String Collection
        {
            get
            {
                return this._collection;
            }
        }

        protected override bool HasDocumentInternal(string name)
        {
            MongoDBDocument doc = new MongoDBDocument(name, this);
            return doc.Exists;
        }

        protected override bool CreateDocumentInternal(string name)
        {
            MongoDBDocument doc = new MongoDBDocument(name, this);
            doc.BeginWrite(true);
            doc.EndWrite();
            return true;
        }

        protected override bool DeleteDocumentInternal(string name)
        {
            MongoDBDocument doc = new MongoDBDocument(name, this);
            if (!doc.Exists) return false;
            Document mongoDoc = doc.BeginRead();
            doc.EndRead();
            this._db[Collection].Remove(mongoDoc);
            return true;
        }

        protected override IDocument<Document, Document> GetDocumentInternal(string name)
        {
            MongoDBDocument doc = new MongoDBDocument(name, this);
            if (doc.Exists)
            {
                return doc;
            }
            else
            {
                throw new AlexandriaException("The requested Document " + name + " is not present in this Store");
            }
        }

        public override IGraphRegistry GraphRegistry
        {
            get 
            {
                return this._registry;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            this._connection.Disconnect();
        }
    }
}
