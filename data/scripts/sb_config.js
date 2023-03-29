rs.initiate(
    {
      _id: "shard_b",
      members: [
        { _id : 0, host : "127.0.0.1:27024" },
        { _id : 1, host : "127.0.0.1:27025" },
        { _id : 2, host : "127.0.0.1:27026" }
      ]
    }
  )