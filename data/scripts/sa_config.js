rs.initiate(
    {
      _id: "shard_a",
      members: [
        { _id : 0, host : "localhost:27021" },
        { _id : 1, host : "localhost:27022" },
        { _id : 2, host : "localhost:27023" }
      ]
    }
  )