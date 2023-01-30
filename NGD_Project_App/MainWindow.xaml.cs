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

namespace NGD_Project_App
{
    public sealed partial class MainWindow : Window
    {
        public string connectionString = "mongodb://MSI-GS75Stealth:27017,MSI-GS75Stealth:27018,MSI-GS75Stealth:27019?replicaSet=rs";
        //public string connectionString = "mongodb://localhost:27017";


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

            var cities = new BsonDocument[]
            {
                new BsonDocument()
                {
                    { "name", "Tokyo" },
                    { "country", "Japan" },
                    { "continent", "Asia" },
                    { "population", 37400 },
                },
                new BsonDocument()
                {
                    { "name", "Delhi" },
                    { "country", "India" },
                    { "continent", "Asia" },
                    { "population", 28.514 }
                },
                new BsonDocument()
                {
                    {"name", "Seoul" },
                    {"country", "South Korea" },
                    { "continent", "Asia" },
                    { "population", 25.674 }
                }
            };

            session.StartTransaction();
            var db = new MongoClient(connectionString).GetDatabase("NGD_Project");
            db.DropCollection("cities");
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("cities");
            collection.InsertMany(cities);
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
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("cities"); // QUESTA RIGA DI CODICE SI POTREBBE INSERIRE
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
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("cities");

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
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("cities");

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
                IMongoCollection<BsonDocument> collection = session.Client.GetDatabase("NGD_Project").GetCollection<BsonDocument>("cities");

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
            InitDB();
        }

    }

}
