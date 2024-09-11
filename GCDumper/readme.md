# how to build

This project supports AOT

```shell
dotnet publish --configuration Release
```

# how to run the project

Before running make sure you run the exe from elevated access. Run cmd as Administrator

```shell
GCDumper.exe RedGate.Monitor.BaseMonitor interval2:30 interval1:15 memDump:true
```

# options

- dumpBeforeGc : Whether to dump before GC (default: true)
- memDump : Whether to generate DMP files (default: false)
- intervall : Seconds to wait before second dump (default: IO)
- interva12 : Seconds to wait before third dump (default: 20)