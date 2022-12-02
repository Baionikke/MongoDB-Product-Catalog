from pymongo import MongoClient, WriteConcern, ReadPreference
from pymongo.read_concern import ReadConcern

# For a replica set, include the replica set name and a seedlist of the members in the URI string; e.g.
uriString = 'mongodb://BAIONIKKES-PC:27017,BAIONIKKES-PC:27018,BAIONIKKES-PC:27019/?replicaSet=rs'
# For a sharded cluster, connect to the mongos instances; e.g.
# uriString = 'mongodb://localhost:27017'

client = MongoClient(uriString)
wc_majority = WriteConcern("majority", wtimeout=1000)

# Prereq: Create collections.  -> ### DA ESEGUIRE LA PRIMA VOLTA, POI COMMENTARE ###
client.get_database("mydb1", write_concern=wc_majority).foo.insert_one({"abc": 0})

# Step 1: Define the callback that specifies the sequence of operations to perform inside the transactions.
def callback(session):
    collection_one = session.client.NGD_Project.cities
    collection_two = session.client.NGD_Project.tsi
    collection_three = session.client.mydb1.foo

    # Important: You must pass the session to the operations.
    collection_one.insert_one({"name": "Cupra", "country": "Italy", "continent": "Europe", "population": 1490 }, session=session)
    collection_two.insert_one({ "address": {"building": "007","coord": [-75,66],"street": "Morris Park Ave","zipcode": "10462"},
                                "borough": "Bronx","cuisine": "Bakery","grades": "","name": "Morris Park Bake Shop","restaurant_id": "34545656"}, session=session)
    collection_three.insert_one({"abc": 1}, session=session)

# Step 2: Start a client session.
with client.start_session() as session:
    # Step 3: Use with_transaction to start a transaction, execute the callback, and commit (or abort on error).
    session.with_transaction(
        callback,
        read_concern=ReadConcern("local"),
        write_concern=wc_majority,
        read_preference=ReadPreference.PRIMARY,
    )
