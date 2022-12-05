# MongoDB-Product-Catalog
Build of an app based on MongoDB driver to further explore MongoDB transactions.

Before starting the application, from terminale run the command 
`run-rs -v 5.0.0 --shell`
in order to build up a MongoDB server with three nodes (replica set) available. Insert also a sample database with the following:
`use NGD_Project`
`db.cities.insertMany([ {"name": "Tokyo", "country": "Japan", "continent": "Asia", "population": 37.400 }, {"name": "Delhi", "country": "India", "continent": "Asia", "population": 28.514 }, {"name": "Seoul", "country": "South Korea", "continent": "Asia", "population": 25.674 } ])`
And nowm enjoy the app! :)
