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

namespace NGD_Project_App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            Title = "Product Catalog";
            if ( StartConnection() )
            {
                connectionBlock.Visibility = Visibility.Visible;
                connectionBlock.Text = "Connected";
            }
        }

        private bool StartConnection()
        {
            try
            {
                var client = new MongoClient("mongodb://BAIONIKKES-PC:27017,BAIONIKKES-PC:27018,BAIONIKKES-PC:27019?replicaSet=rs");
            }
            catch (Exception ex) { connectionBlock.Text = ex.Message.ToString();  return false; }
            return true;
        }

        private MongoClient GetConnectionClient()
        {
            if (StartConnection() )
            {
                var client = new MongoClient("mongodb://BAIONIKKES-PC:27017,BAIONIKKES-PC:27018,BAIONIKKES-PC:27019?replicaSet=rs");
                return client;
            }
            else { return null; }
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
        }

    }

}
