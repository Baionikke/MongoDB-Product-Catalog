rs.initiate(
    {
      _id: "shard_b",
      members: [
        { _id : 0, host : "localhost:27024" },
        { _id : 1, host : "localhost:27025" },
        { _id : 2, host : "localhost:27026" }
      ]
    }
  )