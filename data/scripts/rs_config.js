rs.initiate(
    {
      _id: "config",
      configsvr: true,
      members: [
        { _id : 0, host : "localhost:27018" },
        { _id : 1, host : "localhost:27019" },
        { _id : 2, host : "localhost:27020" }
      ]
    }
  )