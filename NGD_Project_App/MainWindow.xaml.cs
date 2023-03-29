// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using MongoDB.Driver;
using Windows.UI.Text;
using MongoDB.Bson;
using System.Reflection.Metadata;
using SharpCompress.Common;
using Windows.System;
using Microsoft.VisualBasic;
using Windows.Media.Protection.PlayReady;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Documents;
using Windows.Media.Effects;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Configuration;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks;

namespace NGD_Project_App
{
    public sealed partial class MainWindow : Window
    {
        //public string connectionString = "mongodb://MSI-GS75Stealth:27017,MSI-GS75Stealth:27018,MSI-GS75Stealth:27019?replicaSet=rs";
        public string connectionString = "mongodb://localhost:27030";


        public MainWindow()
        {
            this.InitializeComponent();
            Title = "Product Catalog";
            connectionBlock.Visibility = Visibility.Visible;
            connectionBlock.Text = "Waiting...";
            StartConnection();
        }

        private bool StartConnection()
        {
            try
            {
                var client = new MongoClient(connectionString);
            }
            catch (Exception ex) { 
                connectionBlock.Text = ex.Message.ToString();  
                return false; }
            return true;
        }

        private MongoClient GetConnectionClient()
        {
            if (StartConnection() )
            {
                var client = new MongoClient(connectionString);
                return client;
            }
            else { return null; }
        }

        private void InitDB()
        {
            var session = GetConnectionClient().StartSession();

            var sharded_coll = new BsonDocument[]
            {
                new BsonDocument()
                {
                    { "name", "x" },
                    { "value", "0" },
                    { "shard_value" , "0" }
                },
                new BsonDocument()
                {
                    { "name", "y" },
                    { "value", "0" },
                    { "shard_value" , "1" }
                }
            };

            session.StartTransaction();
            var db = new MongoClient(connectionString).GetDatabase("NGD_Project");
            db.DropCollection("sharded_coll");
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("sharded_coll");
            collection.InsertMany(sharded_coll);
            connectionBlock.Text = "Connected";
        }

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            InitDB();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility = Visibility.Visible;
            using (var session = GetConnectionClient().StartSession()) // letto un articolo dove facevano con l'async
            {
                session.StartTransaction();
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("sharded_coll"); // QUESTA RIGA DI CODICE SI POTREBBE INSERIRE
                                                                                                                                             // DENTRO LA FUNZIONE GETCONNECTION() SE SI 
                                                                                                                                             // CONSIDERA SOLO IL DB DELLE CITTA' PER FARE
                                                                                                                                             // LE TRANSAZIONI
                BsonDocument bsonDoc = BsonDocument.Parse(InsertTextBox.Text);
                try
                {
                    collection.InsertOne(bsonDoc); // fare prove con insertMany() e altri metodi per testare diversi scenari di transazioni
                    session.CommitTransaction();
                    ResultTextBlock.Text = "Inserted";
                }
                catch (MongoWriteException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ResultTextBlock.Text = "NOT Inserted, error:" + ex.Message.ToString();
                    session.AbortTransaction();
                }
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility= Visibility.Visible;
            var client = GetConnectionClient();
            IMongoDatabase database = client.GetDatabase("NGD_Project");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("sharded_coll");

            var filter = Builders<BsonDocument>.Filter.Eq(FindTextBox.Text.Split("\"")[1], FindTextBox.Text.Split("\"")[3]);
            var resDocument = collection.Find(filter).FirstOrDefault(); // FirstOrDefault mostra solo il primo documento -> bisogna mostrare tutti i risultati
            if (resDocument != null)
            {
                ResultTextBlock.Text = resDocument.ToString();
            }
            else { ResultTextBlock.Text = "NO RESULT"; }
        }

        private void AggregateButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility = Visibility.Visible;
            var client = GetConnectionClient();
            IMongoDatabase database = client.GetDatabase("NGD_Project");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("sharded_coll");

            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[] { BsonDocument.Parse(AggregateTextBox.Text) };
            var resultAggr = collection.Aggregate(pipeline).FirstOrDefault();

            if (resultAggr != null)
            {
                ResultTextBlock.Text = resultAggr.ToString();
            }
            else { ResultTextBlock.Text = "NO RESULT"; }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility = Visibility.Visible;
            using (var session = GetConnectionClient().StartSession())
            {
                session.StartTransaction();
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("sharded_coll");

                BsonDocument bsonDoc = BsonDocument.Parse(DeleteTextBox.Text);
                try
                {
                    var resDocument = collection.Find(bsonDoc).FirstOrDefault();
                    if (resDocument != null)
                    {
                        collection.DeleteOne(bsonDoc);
                        session.CommitTransaction();
                        ResultTextBlock.Text = "Deleted";
                    }
                    else
                    {
                        ResultTextBlock.Text = "NOT Deleted";
                    }
                }
                catch (MongoWriteException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    ResultTextBlock.Text = "NOT Deleted, error: "+ex.Message.ToString();
                    session.AbortTransaction();
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Visibility = Visibility.Collapsed;
            ResultTextBlock.Text = "";
            FindTextBox.Text = " { " + "\"" + "field" + "\"" + " : " + "\"" + "value" + "\"" + " } ";
            InsertTextBox.Text = " { " + "\"" + "field" + "\"" + " : " + "\"" + "value" + "\"" + " } ";
            AggregateTextBox.Text = " { $match: { country: \"South Korea" + "\" }} ";
            DeleteTextBox.Text = " { " + "\"" + "field" + "\"" + " : " + "\"" + "value" + "\"" + " } ";
            ToUpdateTextBox.Text = " { " + "\"" + "field" + "\"" + " : " + "\"" + "value" + "\"" + " } ";
            UpdatedTextBox.Text = " { " + "\"" + "field" + "\"" + " : " + "\"" + "value" + "\"" + " } ";
            InitDB();
        }

        private void ReplicaUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility = Visibility.Visible;

            using (var session = GetConnectionClient().StartSession()) 
            {
                session.StartTransaction();
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("sharded_coll");

                var new_connection_string = "mongodb://localhost:27025";
                var client_sharded = new MongoClient(new_connection_string);
                IMongoDatabase database_sharded = client_sharded.GetDatabase("NGD_Project");
                IMongoCollection<BsonDocument> collection_sharded = database_sharded.GetCollection<BsonDocument>("sharded_coll");

                var filter = Builders<BsonDocument>.Filter.Eq(ToUpdateTextBox.Text.Split("\"")[1], ToUpdateTextBox.Text.Split("\"")[3]);
                var resFilter = collection.Find(filter).FirstOrDefault();

                if (resFilter != null)
                {
                    var new_key = UpdatedTextBox.Text.Split("\"")[1];
                    var new_value = UpdatedTextBox.Text.Split("\"")[3];
                    var update = Builders<BsonDocument>.Update.Set(new_key, new_value).CurrentDate("lastModified");

                    try
                    {
                        collection.UpdateManyAsync(filter, update);
                        ResultTextBlock.Text = "Updated";

                        var checks = 0;

                        DateTime now = DateTime.Now;
                        bool checked_replica_copy = false;
                        try
                        {
                            while (!checked_replica_copy && checks < 10000)
                            {
                                var resDocument = collection_sharded.Find(filter).FirstOrDefault();
                                if (resDocument != null)
                                {
                                    if (resDocument[new_key].Equals(new_value))
                                    {
                                        checked_replica_copy = true;
                                        DateTime new_now = DateTime.Now;
                                        now = resDocument["lastModified"].ToLocalTime();
                                        ResultTextBlock.Text = resDocument.ToString() + "\nUpdated in " + (new_now.Millisecond + new_now.Second * 1000 - now.Millisecond - now.Second * 1000).ToString() + " milliseconds, with " + checks.ToString() + " inconsistency checks.";
                                    }
                                    else
                                    {
                                        checks++;
                                    }
                                }
                                else
                                {
                                    ResultTextBlock.Text = "Document not found. Are you on the correct shard? (Actually on " + new_connection_string + ").";
                                    break;
                                }
                            }

                            if (checks >= 10000)
                            {
                                ResultTextBlock.Text = "More than 1000 inconsistency checks. Are you on the correct shard? (Actually on " + new_connection_string + ").";
                            }

                        }
                        catch (Exception ex)
                        {
                            connectionBlock.Text = ex.Message.ToString();
                        }

                    }
                    catch (MongoWriteException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        ResultTextBlock.Text = "NOT Updated, error:" + ex.Message.ToString();
                        session.AbortTransaction();
                    }
                }
                else
                {
                    ResultTextBlock.Text = "NO RESULT FOUND";
                }

            }

        }

        private void ShardedUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";
            ResultTextBlock.Visibility = Visibility.Visible;

            using (var session = GetConnectionClient().StartSession())
            {
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("sharded_coll");

                var shardA_connection_string = "mongodb://localhost:27022";
                var shardB_connection_string = "mongodb://localhost:27025";
                var client_shardA = new MongoClient(shardA_connection_string);
                var client_shardB = new MongoClient(shardB_connection_string);
                IMongoDatabase database_shardA = client_shardA.GetDatabase("NGD_Project");
                IMongoDatabase database_shardB = client_shardB.GetDatabase("NGD_Project");
                IMongoCollection<BsonDocument> collection_shardA = database_shardA.GetCollection<BsonDocument>("sharded_coll"); // Same collection, different chunks
                IMongoCollection<BsonDocument> collection_shardB = database_shardB.GetCollection<BsonDocument>("sharded_coll");

                var filterX = Builders<BsonDocument>.Filter.Eq("name", "x");
                var filterY = Builders<BsonDocument>.Filter.Eq("name", "y");
                var resFilterX = collection.Find(filterX).FirstOrDefault();
                var resFilterY = collection.Find(filterY).FirstOrDefault();

                var new_key = ShardedUpdatedTextBox.Text.Split("\"")[1];
                var new_value = ShardedUpdatedTextBox.Text.Split("\"")[3];

                if (resFilterX != null && resFilterY != null)
                {
                    DateTime now = DateTime.Now;
                    ShardedUpdateAsync(collection, new_key, new_value);
                    
                    var checks = 0;
                    while (checks < 1000)
                    {
                        var x = collection_shardB.Find(filterX).FirstOrDefault();
                        var y = collection_shardA.Find(filterY).FirstOrDefault();
                        
                        if (x["value"] == y["value"])
                        {
                            DateTime new_now = x["lastModified"].ToLocalTime();
                            ResultTextBlock.Text = "Updated in " + (new_now.Millisecond + new_now.Second * 1000 - now.Millisecond - now.Second * 1000).ToString() + " milliseconds, with " + checks.ToString() + " inconsistency checks.";
                            break;
                        }
                        else
                        {
                            checks++;
                        }
                    }
                }
                else
                {
                    if (resFilterX == null && resFilterY != null)
                    {
                        ResultTextBlock.Text = "x NOT FOUND";
                    }
                    if (resFilterY == null && resFilterX != null)
                    {
                        ResultTextBlock.Text = "y NOT FOUND";
                    }
                    else
                    {
                        ResultTextBlock.Text = "x AND y NOT FOUND";
                    }
                }

            }

        }

        private async void ShardedUpdateAsync(IMongoCollection<BsonDocument> collection, string new_key, string new_value)
        {
            await collection.UpdateManyAsync(Builders<BsonDocument>.Filter.Eq("name", "y"), Builders<BsonDocument>.Update.Set(new_key, new_value).CurrentDate("lastModified"));
            await Task.Delay(500);
            await collection.UpdateManyAsync(Builders<BsonDocument>.Filter.Eq("name", "x"), Builders<BsonDocument>.Update.Set(new_key, new_value).CurrentDate("lastModified"));
        }
    }

}
