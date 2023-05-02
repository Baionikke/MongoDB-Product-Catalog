---
theme: uncover
paginate: true
marp: true
style: |
  .small-text {
    font-size: 0.65rem;
  }
---

![bg fit blur:6px](src/UNVIPM.png)

## Transazioni MongoDB

Atomicità delle transazioni in un Mongo-Cluster
<br>
<p class="small-text">Progetto per il corso di New Generation Databases</p>
<p class="small-text">Baioni Francesco</p>
<p class="small-text">Caprari David</p>


---

- Cluster MongoDB
  - Configurazioni dei nodi
  - Script inizializzazione
- Applicazione interfaccia grafica
  - Semplice GUI (WinUI)
  - Backend con interfaccia al driver

---

## Cluster MongoDB
- MongoDB <span style="color:green"> 6.0.2 </span>
- MongoS <span style="color:green"> 6.0.2 </span>
- MongoShell

---
#### Struttura interna

- Shard A
- Shard B

Ogni shard composto da 3 Replica Set
In locale (localhost:porta)

---
#### Struttura interna
![](diagrams/Arch.png)

---
#### Dati
Singola collezione di due documenti

Documento **Y** --> Shard A
Documento **X** --> Shard B

```json
{
    "_id": ObjectId("..."),
    "name": "x",
    "value": "10",
    "shard_value": "0",
    "lastModified": "ISODate("aaaa-mm-ggT:...")"
}
```

---

## Applicazione WinUI
- VisualStudio2022
- Framework XAML/C# WinUI3

---

## Interfaccia al DB
Funzioni base:
- Inserimento
- Ricerca
- Aggregazione
- Cancellazione

Funzioni avanzate:
- **Replica Update**
- **Sharded Update**

---
#### <span style="color:white">Inserimento</span>
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/Insert.png)

---
#### <span style="color:white">Ricerca
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/Find.png)

---
#### <span style="color:white">Aggregazione
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/Aggregate.png)


---
#### <span style="color:white">Cancellazione
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/Delete.png)

---

#### Replica Update
- Aggiornamento asincrono attraverso MongoS (localhost:27030) del "value" nel documento X
- Contemporaneo check di consistenza dell'aggiornamento attraverso ReplicaSet secondario (localhost:27025)

---
#### Replica Update
![](diagrams/ReplicaUpdate.png)

---


#### <span style="color:white">Replica Update
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/ReplicaUpdate.png)

---

#### Replica Update

**Replica delle transazioni interne allo shard è atomica**

MongoDB $\geq$ <span style="color:green">4.2</span> supporta Multi-Document transactions

---

#### Sharded Update
- **Transazione asincrona a MongoS:**
   1. Aggiornamento del "value" nel documento Y
   2. Attesa di 1sec
   3. Aggiornamento del "value" nel documento X

- **Check contemporaneo di vincolo di integrità**
<span style="color:darkgrey">"value@X"=="value@Y" </span>

---

#### Sharded Update
![](diagrams/ShardedUpdate.png)

---

#### <span style="color:white">Sharded Update
<br>
<br>
<br>
<br>
<br>
<br>
<br>

![bg fit](src/ShardedUpdate.png)

---
#### Sharded Update

**Transazione multi-shard <span style="color:red">NON</span> è atomica**

Le transazioni che coinvolgono più di uno shard non sono atomiche

---

# Grazie per l'attenzione

---

### Post-Notes:
1. Bug multi-istanza nello stesso host
2. Codice Replica Update
3. Codice Sharded Update

---

#### Replica Update
```csharp
private async void ReplicaUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        ...
        using (var session = GetConnectionClient().StartSession())
        {
            ...
            IMongoCollection<BsonDocument> collection_sharded = database_sharded.GetCollection<BsonDocument>("sharded_coll");
            var filter = Builders<BsonDocument>.Filter.Eq(ToUpdateTextBox.Text.Split("\"")[1], ToUpdateTextBox.Text.Split("\"")[3]);
            var resFilter = collection.Find(filter).FirstOrDefault();
            if (resFilter != null)
            {
                ...
                var update = Builders<BsonDocument>.Update.Set(new_key, new_value).CurrentDate("lastModified");
                try
                {
                    await collection.UpdateManyAsync(filter, update);
                    connectionBlock.Text = "Updated";
                    var checks = 0;
                    DateTime now = DateTime.Now;
                    bool checked_replica_copy = false;
                    try
                    {
                        while (!checked_replica_copy && checks < 1000)
                        {
                            var resDocument = collection_sharded.Find(filter).FirstOrDefault();
                            if (resDocument != null)
                            {
                                if (resDocument[new_key].Equals(new_value))
                                {
                                    checked_replica_copy = true;
                                    DateTime new_now = DateTime.Now;
                                    now = resDocument["lastModified"].ToLocalTime();
                                    ResultTextBlock.Text = resDocument.ToString() + /
                                                        "\nUpdated in " + (new_now.Millisecond + new_now.Second * 1000 - now.Millisecond - now.Second * 1000).ToString() /
                                                        + " milliseconds, with " + checks.ToString() /
                                                        + " inconsistency checks.";
                                }
                                else { checks++; }
                            }
                        ...                                    
                        }
                        if (checks >= 1000)
                        {
                            ResultTextBlock.Text = "More than 1000 inconsistency checks. Are you on the correct shard? (Actually on " + new_connection_string + ").";
                        }
        }
    }
```

---

#### Sharded Update - ext

```csharp
private async void ShardedUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        ...
        using (var session = GetConnectionClient().StartSession())
        {
            ...
            var new_key = ShardedUpdatedTextBox.Text.Split("\"")[1];
            var new_value = ShardedUpdatedTextBox.Text.Split("\"")[3];-
            if (resFilterX != null && resFilterY != null)
            {
                DateTime now = DateTime.Now;
                session.StartTransaction();
                ShardedUpdateAsync(collection, new_key, new_value);
                var checks = 0;
                int num_checks = 100; //Number of consistency checks scheduled
                while (checks < num_checks)
                {
                    var x = collection_shardB.Find(filterX).First();
                    var y = collection_shardA.Find(filterY).First();
                    if (y["value"] == x["value"])
                    {
                        DateTime new_now = x["lastModified"].ToLocalTime();
                        ResultTextBlock.Text = "Updated in " / 
                        + (new_now.Millisecond + new_now.Second * 1000 - now.Millisecond - now.Second * 1000).ToString() /
                        + " milliseconds, with " + checks.ToString() + " inconsistency checks.";
                        break;
                    }
                    else
                    {
                        checks++;
                        await Task.Delay(100);
                    }
                }
                if (checks >= num_checks)
                {    ResultTextBlock.Text = "Check resulted in more than " + checks.ToString() + " consistency checks failed.";    }
            }
        ...
        }
    }
```

---

#### Sharded Update - transaction
```csharp
private async Task ShardedUpdateAsync(IMongoCollection<BsonDocument> collection, string new_key, string new_value)
    {
    
        collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("name", "y"), /
                                                    Builders<BsonDocument>.Update.Set(new_key, new_value)/
                                                    .CurrentDate("lastModified"));
        await Task.Delay(1000);
        collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("name", "x"), /
                                                    Builders<BsonDocument>.Update.Set(new_key, new_value)/
                                                    .CurrentDate("lastModified"));
        connectionBlock.Text = "Updated";
    }
```