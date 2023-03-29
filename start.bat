@echo Starting up...

start /MIN mongod -f .\data\configdb\config1.conf
start /MIN mongod -f .\data\configdb\config2.conf
start /MIN mongod -f .\data\configdb\config3.conf

timeout 10

mongosh 127.0.0.1:27018 --file .\data\scripts\rs_config.js 

@echo Config start up succesfull...

start /MIN mongod -f .\data\configdb\shard_a1.conf
start /MIN mongod -f .\data\configdb\shard_a2.conf
start /MIN mongod -f .\data\configdb\shard_a3.conf

timeout 10

mongosh 127.0.0.1:27021 --file .\data\scripts\sa_config.js 

@echo Shard_A start up succesfull...

start /MIN mongod -f .\data\configdb\shard_b1.conf
start /MIN mongod -f .\data\configdb\shard_b2.conf
start /MIN mongod -f .\data\configdb\shard_b3.conf

timeout 10

mongosh 127.0.0.1:27024 --file .\data\scripts\sb_config.js 

@echo Shard_B start up succesfull...

start /MIN mongos --config .\data\configdb\mongos.conf

timeout 10

mongosh 127.0.0.1:27030 --file .\data\scripts\addshards.js

@echo Shards connected. Init complete. Do NOT close the cmd windows unless you want to stop a server.
